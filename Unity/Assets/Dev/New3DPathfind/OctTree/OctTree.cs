using System;
using System.Threading;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Profiling;

namespace Candy.Pathfind3D
{
    public class OctTree : IDisposable
    {
        [Serializable]
        public struct InitParameter
        {
            public int MinDepth;
            public int MaxDepth;
            public float Scale;
            
            [NonSerialized] public float3 WorldPosition;
            
            public InitParameter(int minDepth, int maxDepth, float scale, float3 worldPosition)
            {
                MinDepth = minDepth;
                MaxDepth = maxDepth;
                Scale = scale;
                WorldPosition = worldPosition;
            }
        }

        public readonly InitParameter Parameter;

        public object LockObj = new();
        public NativeList<OctNode> OctNodes;

        public int MaxOctNodeArrayLength => GetMaxOctNodeArrayLength(Parameter);

        public static int GetMaxOctNodeArrayLength(InitParameter parameter)
        {
            int depth = Mathf.Max(parameter.MinDepth, parameter.MaxDepth);

            int size = GetNodeCountOfDepth(depth);

            return size;
        }


        public OctTree(InitParameter parameter)
        {
            Parameter = parameter;
        }

        private static int GetNodeCountOfDepth(int depth)
        {
            int d = 0;

            /*
            d += IntPow8(depth);
            d += IntPow8(depth - 1);

            return d;*/
            
            for (int i = 0; i <= depth; i++)
            {
                d += IntPow8(i);
            }

            return d;
        }

        private static int IntPow8(int x)
        {
            if (x < 0) return 0;

            int d = 1;
            for (int i = 0; i < x; i++)
            {
                d *= 8;
            }

            return d;
        }

        public void CreateSpace(NativeList<OverlapBoxCommand> overlapBoxCommands, NativeList<ColliderHit> results)
        {
            Divide(overlapBoxCommands, results);
        }

        public static (NativeList<OverlapBoxCommand> overlapBoxCommands, NativeList<ColliderHit> results) CreatePhysicsBuffer(InitParameter parameter)
        {
            Profiler.BeginSample("Physics buffer"); 
            NativeList<OverlapBoxCommand> commands = new NativeList<OverlapBoxCommand>(GetMaxOctNodeArrayLength(parameter), Allocator.TempJob);
            NativeList<ColliderHit> results = new NativeList<ColliderHit>(GetMaxOctNodeArrayLength(parameter), Allocator.TempJob);
            Profiler.EndSample();

            return (commands, results);
        }

        public void Divide(NativeList<OverlapBoxCommand> commands, NativeList<ColliderHit> results)
        {
            int cpuCount = 32;

            Profiler.BeginSample("Ready Array"); 
            Profiler.BeginSample("Write buffer"); 
            NativeList<OctNode> writeOctNode = new NativeList<OctNode>(MaxOctNodeArrayLength, Allocator.Persistent);
            writeOctNode.ResizeUninitialized(MaxOctNodeArrayLength);
            unsafe
            {
                Profiler.BeginSample("Clear buffer"); 
                //UnsafeUtility.MemClear(writeOctNode.GetUnsafePtr(), sizeof(OctNode) * MaxOctNodeArrayLength);
                
                new OctNodeClearJob()
                {
                    Nodes = writeOctNode.AsArray()
                }.Schedule(MaxOctNodeArrayLength, MaxOctNodeArrayLength / cpuCount).Complete();
                
                Profiler.EndSample();
                
                (*writeOctNode.GetUnsafePtr()) =
                    new OctNode(0, true, 0, Parameter.Scale, true, Parameter.WorldPosition);
                
            }
            Profiler.EndSample();
            
            Profiler.BeginSample("Direction Buffer"); 
            float3 oneVector = new float3(1f, 1f, 1f);
            using NativeArray<float3> directionVectorArray = new NativeArray<float3>(new float3[]
            {
                oneVector * 0.5f,
                math.mul(quaternion.Euler(0f, 90f * math.TORADIANS, 0f), oneVector * 0.5f),
                math.mul(quaternion.Euler(0f, 180f * math.TORADIANS, 0f), oneVector * 0.5f),
                math.mul(quaternion.Euler(0f, 270f * math.TORADIANS, 0f), oneVector * 0.5f),

                -oneVector * 0.5f,
                math.mul(quaternion.Euler(0f, 90f * math.TORADIANS, 0f), -oneVector * 0.5f),
                math.mul(quaternion.Euler(0f, 180f * math.TORADIANS, 0f), -oneVector * 0.5f),
                math.mul(quaternion.Euler(0f, 270f * math.TORADIANS, 0f), -oneVector * 0.5f),
            }, Allocator.TempJob);
            
            Profiler.EndSample();
            Profiler.EndSample();

            var query = new QueryParameters(~0);

            for (int i = 1; i <= Parameter.MaxDepth; i++)
            {
                int currentDepthNodeCount = IntPow8(i);
                int beforeDepthNodeCount = IntPow8(i - 1);

                Profiler.BeginSample("ResizeUninitialized Physics Buffer");
                commands.ResizeUninitialized(currentDepthNodeCount);
                results.ResizeUninitialized(currentDepthNodeCount);
                Profiler.EndSample();
                
                Profiler.BeginSample("Divide Job");
                Profiler.BeginSample("#" + i);
                OctNodeDivideJob divideJob = new OctNodeDivideJob()
                {
                    WriteOctNodeList = writeOctNode.AsArray(),
                    ChildDirectionVectorArray = directionVectorArray,
                    Offset = beforeDepthNodeCount,
                    QueryParameters = query,
                    Commands = commands.AsArray()
                };

                divideJob.Schedule(currentDepthNodeCount, currentDepthNodeCount / cpuCount).Complete();
                Profiler.EndSample();
                Profiler.EndSample();


                Profiler.BeginSample("OverlapBox Job"); 
                unsafe
                {
                    NativeArray<OverlapBoxCommand> commandsView = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<OverlapBoxCommand>(
                        (byte*)commands.GetUnsafePtr(),
                        currentDepthNodeCount,
                        Allocator.None);
                    NativeArray<ColliderHit> resultsView = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<ColliderHit>(
                        (byte*)results.GetUnsafePtr(),
                        currentDepthNodeCount,
                        Allocator.None);
                    
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                    AtomicSafetyHandle safetyHandle = AtomicSafetyHandle.Create();
                    safetyHandle = AtomicSafetyHandle.Create();
                    NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref commandsView, safetyHandle);
                    safetyHandle = AtomicSafetyHandle.Create();
                    NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref resultsView, safetyHandle);
#endif
                    OverlapBoxCommand.ScheduleBatch(commandsView, resultsView, commands.Length / cpuCount, 1).Complete();
                }
                Profiler.EndSample();

                Profiler.BeginSample("Obstacle Job"); 
                unsafe
                {
                    NativeArray<OctNode> output = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<OctNode>(
                        (byte*)writeOctNode.GetUnsafePtr() + sizeof(OctNode) * beforeDepthNodeCount,
                        currentDepthNodeCount,
                        Allocator.None);
                    
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                    var safetyHandle = AtomicSafetyHandle.Create();
                    NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref output, safetyHandle);
#endif
                    
                    OctNodeSetObstacleJob setObstacleJob = new OctNodeSetObstacleJob()
                    {
                        Nodes = output,
                        Hits = results.AsArray(),
                    };
                    setObstacleJob.Schedule(currentDepthNodeCount, currentDepthNodeCount / cpuCount).Complete();
                }
                
                Profiler.EndSample();
            } 

            Profiler.BeginSample("End");
            lock (LockObj)
            {
                OctNodes = writeOctNode;
            }
            Profiler.EndSample();
        }


        public void Dispose()
        {
            OctNodes.Dispose();
        }
    }
}