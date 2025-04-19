using System;
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

        public NativeList<OctNode> OctNodes;

        public int MaxOctNodeArrayLength
        {
            get
            {
                int depth = Mathf.Max(Parameter.MinDepth, Parameter.MaxDepth);

                int size = GetNodeCountOfDepth(depth);

                return size;
            }
        }


        public OctTree(InitParameter parameter)
        {
            Parameter = parameter;
        }

        private int GetNodeCountOfDepth(int depth)
        {
            int d = 0;
            for (int i = 0; i <= depth; i++)
            {
                d += IntPow8(i);
            }

            return d;
        }

        private int IntPow8(int x)
        {
            if (x < 0) return 0;

            int d = 1;
            for (int i = 0; i < x; i++)
            {
                d *= 8;
            }

            return d;
        }

        public void Divide()
        {
            int cpuCount = 32;

            Profiler.BeginSample("Ready Array"); 
            Profiler.BeginSample("Read buffer"); 
            using NativeList<OctNode> readOctNode = new NativeList<OctNode>(MaxOctNodeArrayLength, Allocator.TempJob);
            readOctNode.Add(new OctNode(0, true, 0, Parameter.Scale, true, Parameter.WorldPosition));
            readOctNode.ResizeUninitialized(MaxOctNodeArrayLength);
            Profiler.EndSample();
            Profiler.BeginSample("Write buffer"); 
            NativeList<OctNode> writeOctNode = new NativeList<OctNode>(MaxOctNodeArrayLength, Allocator.Persistent);
            writeOctNode.Add(new OctNode(0, true, 0, Parameter.Scale, true, Parameter.WorldPosition));
            writeOctNode.ResizeUninitialized(MaxOctNodeArrayLength);
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

            for (int i = 0; i < Parameter.MaxDepth; i++)
            {
                int currentDepthNodeCount = IntPow8(i);
                int beforeDepthNodeCount = IntPow8(i - 1);

                
                Profiler.BeginSample("Divide Job"); 
                OctNodeDivideJob divideJob = new OctNodeDivideJob()
                {
                    CurrentDepth = i,
                    ReadOctNodeList = readOctNode.AsArray(),
                    WriteOctNodeList = writeOctNode.AsArray(),
                    ChildDirectionVectorArray = directionVectorArray,
                };

                divideJob.Schedule(MaxOctNodeArrayLength, MaxOctNodeArrayLength / cpuCount).Complete();
                Profiler.EndSample();

                var commands = new NativeArray<OverlapBoxCommand>(currentDepthNodeCount, Allocator.TempJob);
                var results = new NativeArray<ColliderHit>(currentDepthNodeCount, Allocator.TempJob);

                Profiler.BeginSample("OverlapBox Job"); 
                unsafe
                {
                    NativeArray<OctNode> input = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<OctNode>(
                        (byte*)writeOctNode.GetUnsafePtr() + sizeof(OctNode) * beforeDepthNodeCount,
                        currentDepthNodeCount,
                        Allocator.None);
                    
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                    var safetyHandle = AtomicSafetyHandle.Create();
                    NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref input, safetyHandle);
#endif
                    OctNodeSetCommandJob setCommandJob = new OctNodeSetCommandJob()
                    {
                        Output = commands,
                        Input = input,
                        QueryParameters = query
                    };
                    setCommandJob.Schedule(currentDepthNodeCount, currentDepthNodeCount / cpuCount).Complete();
                }
                OverlapBoxCommand.ScheduleBatch(commands, results, 1, 1).Complete();
                Profiler.EndSample();

                Profiler.BeginSample("Obstacle Job"); 
                OctNodeSetObstacleJob setObstacleJob = new OctNodeSetObstacleJob()
                {
                    OctNodes = writeOctNode.AsArray(),
                    Hits = results,
                    Offset = beforeDepthNodeCount
                };
                setObstacleJob.Schedule(results.Length, results.Length / cpuCount).Complete();
                Profiler.EndSample();

                Profiler.BeginSample("Copy Array");
                unsafe
                {
                    UnsafeUtility.MemCpy(
                        (byte*)readOctNode.GetUnsafePtr() + sizeof(OctNode) * IntPow8(i - 1), 
                        (byte*)writeOctNode.GetUnsafeReadOnlyPtr() + sizeof(OctNode) * IntPow8(i - 1),
                        sizeof(OctNode) * currentDepthNodeCount
                    );
                }
                Profiler.EndSample();
            } 

            Profiler.BeginSample("End");
            OctNodes = writeOctNode;
            Profiler.EndSample();
        }


        public void Dispose()
        {
            OctNodes.Dispose();
        }
    }
}