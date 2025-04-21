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

        public NativeFlattenOctTree NativeTree { get; private set; }

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

        public void CreateSpace(NativeArray<OverlapBoxCommand> overlapBoxCommands, NativeArray<ColliderHit> results)
        {
            NativeArray<NativeOctNode> arrayTree = Divide(overlapBoxCommands, results);
            (NativeArray<NativeOctNode> flattenArr, NativeArray<int> indexArr, NativeArray<int> treeArr) = ToFlatten(arrayTree);

            arrayTree.Dispose();

            NativeTree = new NativeFlattenOctTree()
            {
                FlattenArr = flattenArr,
                IndexArr = indexArr,
                TreeArr = treeArr
            };
        }

        public static (NativeArray<OverlapBoxCommand> overlapBoxCommands, NativeArray<ColliderHit> results) CreatePhysicsBuffer(InitParameter parameter)
        {
            Profiler.BeginSample("Physics buffer"); 
            NativeArray<OverlapBoxCommand> commands = new NativeArray<OverlapBoxCommand>(GetMaxOctNodeArrayLength(parameter), Allocator.TempJob);
            NativeArray<ColliderHit> results = new NativeArray<ColliderHit>(GetMaxOctNodeArrayLength(parameter), Allocator.TempJob);
            Profiler.EndSample();
            
            return (commands, results);
        }

        public NativeArray<NativeOctNode> Divide(NativeArray<OverlapBoxCommand> commands, NativeArray<ColliderHit> results)
        {
            int cpuCount = 32;

            Profiler.BeginSample("Ready Array"); 
            Profiler.BeginSample("Write buffer"); 
            NativeArray<NativeOctNode> writeOctNode = new NativeArray<NativeOctNode>(MaxOctNodeArrayLength, Allocator.Persistent);
            Profiler.BeginSample("Clear buffer"); 
            new OctNodeClearJob()
            {
                Nodes = writeOctNode
            }.Schedule(MaxOctNodeArrayLength, MaxOctNodeArrayLength / cpuCount).Complete();
            
            Profiler.EndSample();
            writeOctNode[0] =  new NativeOctNode(0, true, 0, Parameter.Scale, true, Parameter.WorldPosition);
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

                Profiler.BeginSample("Divide Job");
                Profiler.BeginSample("#" + i);
                OctNodeDivideJob divideJob = new OctNodeDivideJob()
                {
                    WriteOctNodeList = writeOctNode,
                    ChildDirectionVectorArray = directionVectorArray,
                    Offset = beforeDepthNodeCount,
                    QueryParameters = query,
                    Commands = commands
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
                    NativeArray<NativeOctNode> output = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<NativeOctNode>(
                        (byte*)writeOctNode.GetUnsafePtr() + sizeof(NativeOctNode) * beforeDepthNodeCount,
                        currentDepthNodeCount,
                        Allocator.None);
                    
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                    var safetyHandle = AtomicSafetyHandle.Create();
                    NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref output, safetyHandle);
#endif
                    
                    OctNodeSetObstacleJob setObstacleJob = new OctNodeSetObstacleJob()
                    {
                        Nodes = output,
                        Hits = results,
                    };
                    setObstacleJob.Schedule(currentDepthNodeCount, currentDepthNodeCount / cpuCount).Complete();
                }
                
                Profiler.EndSample();
            } 

            return writeOctNode;
        }

        private (NativeArray<NativeOctNode> flattenArr, NativeArray<int> indexArr, NativeArray<int> treeArr) ToFlatten(NativeArray<NativeOctNode> nonFlattenArray)
        {
            Profiler.BeginSample("Flatten");
            using NativeArray<int> flattenCountResult = new NativeArray<int>(1, Allocator.TempJob);
            using NativeArray<int> indexTargetResult = new NativeArray<int>(1, Allocator.TempJob);
            
            Profiler.BeginSample("Counting");
            OctNodeFlattenCountingJob countJob = new OctNodeFlattenCountingJob()
            {
                Nodes = nonFlattenArray,
                IndexTargetCountResult = indexTargetResult,
                FlattenCountResult = flattenCountResult,
            };
            countJob.Schedule().Complete();
            Profiler.EndSample();
            
            int indexTargetArrLength = indexTargetResult[0];
            int flattenArrLength = flattenCountResult[0];
            
            Profiler.BeginSample("Init Buffer");
            NativeArray<int> mapperIndexArr = new NativeArray<int>(flattenArrLength, Allocator.Persistent);
            NativeArray<int> mapperTargetArr = new NativeArray<int>(indexTargetArrLength, Allocator.Persistent);
            NativeArray<NativeOctNode> flattenArr = new NativeArray<NativeOctNode>(flattenArrLength, Allocator.Persistent);
            Profiler.EndSample();
            
            Profiler.BeginSample("First Job");
            OctNodeFlattenFirstJob firstFlattenJob = new OctNodeFlattenFirstJob()
            {
                Nodes = nonFlattenArray,
                Flatten = flattenArr,
            };
            firstFlattenJob.Schedule().Complete();
            Profiler.EndSample();


            Profiler.BeginSample("Second Buffer");
            OctNodeFlattenSecondJob secondFlattenJob = new OctNodeFlattenSecondJob()
            {
                Count = flattenArrLength,
                Tree = nonFlattenArray,
                MapperIndexOutput = mapperIndexArr,
                MapperTargetOutput = mapperTargetArr,
                Flatten = flattenArr
            };
            secondFlattenJob.Schedule().Complete();
            Profiler.EndSample();
            
            Profiler.EndSample();

            return (flattenArr, mapperIndexArr, mapperTargetArr);
        }


        public void Dispose()
        {
            NativeTree.Dispose();
        }
    }
}