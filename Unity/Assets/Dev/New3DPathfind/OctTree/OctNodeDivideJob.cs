using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Serialization;

namespace Candy.Pathfind3D
{
    [BurstCompile]
    public struct OctNodeClearJob : IJobParallelFor
    {
        [WriteOnly]
        public NativeArray<NativeOctNode> Nodes;
        
        public void Execute(int index)
        {
            Nodes[index] = new NativeOctNode(0, false, 0, 0f, false, float3.zero);
        }
    }

    [BurstCompile(DisableSafetyChecks = true)]
    public struct OctNodeSetObstacleJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<ColliderHit> Hits;
        
        public NativeArray<NativeOctNode> Nodes;
        
        public void Execute(int index)
        {
            if (Hits[index].instanceID != 0)
            {
                NativeOctNode tempNode = Nodes[index];
                tempNode.IsObstacle = true;
                Nodes[index] = tempNode;
            }
        }
    }

    [BurstCompile(DisableSafetyChecks = true)]
    public struct OctNodeDivideJob : IJobParallelFor
    {
        public NativeArray<NativeOctNode> WriteOctNodeList;
        public NativeArray<OverlapBoxCommand> Commands;
        
        [ReadOnly]
        public QueryParameters QueryParameters;

        [ReadOnly]
        public NativeArray<float3> ChildDirectionVectorArray;

        [ReadOnly] 
        public int Offset;

        public void Execute(int index)
        {
            int originIndex = index;
            
            if (Offset == 0) return;
            index += Offset;
            int parentIndex = (index - 1) / 8;
            NativeOctNode parentNode = WriteOctNodeList[parentIndex];
            
            if (parentNode.IsObstacle is false || parentNode.IsGenerated is false)
            {
                NativeOctNode n = new NativeOctNode(
                    0,
                    false,
                    0,
                    0f,
                    false,
                    float3.zero
                );
                WriteOctNodeList[index] = n;
                
                QueryParameters q = QueryParameters;
                q.layerMask = 0;
                Commands[originIndex] = new OverlapBoxCommand(Vector3.zero, Vector3.zero, Quaternion.identity, q);
                return;
            }

            float childScale = parentNode.Scale * 0.5f;
            float3 parentPos = parentNode.WorldPosition;

            int childNum = index - (8 * parentIndex) - 1;
            float3 childPos = (ChildDirectionVectorArray[childNum] * childScale) + parentPos;

            NativeOctNode childNode = new NativeOctNode(
                index,
                false,
                parentNode.Depth + 1,
                childScale,
                true,
                childPos
            );

            WriteOctNodeList[index] = childNode;
            Commands[originIndex] = new OverlapBoxCommand(childPos, Vector3.one * childScale * 0.5f, Quaternion.identity, QueryParameters);
        }
    }

    [BurstCompile(DisableSafetyChecks = true)]
    public struct OctNodeFlattenCountingJob : IJob
    {
        public NativeArray<NativeOctNode> Nodes;
        
        [WriteOnly]
        public NativeArray<int> IndexTargetCountResult;

        [WriteOnly] 
        public NativeArray<int> FlattenCountResult;
        
        public void Execute()
        {
            int indexTargetCount = 0;
            int flattenCount = 0;

            for (int i = 0; i < Nodes.Length; i++)
            {
                NativeOctNode node = Nodes[i];
                if (node.IsGenerated == false) continue;
                node.FlattenIndex = flattenCount;
                Nodes[i] = node;
                flattenCount++;

                int beforeIndexTargetCount = indexTargetCount;
                for (int j = 1; j <= 8; j++)
                {
                    int childIndex = node.Index * 8 + j;
                    if (childIndex >= Nodes.Length) break;
                    
                    if (Nodes[childIndex].IsGenerated)
                    {
                        indexTargetCount++;
                    }
                }
                
                // 자식이 없으면, -1 삽입
                if (beforeIndexTargetCount == indexTargetCount)
                {
                    indexTargetCount++;
                }
            }

            IndexTargetCountResult[0] = indexTargetCount;
            FlattenCountResult[0] = flattenCount;
        }
    }


    [BurstCompile]
    public struct OctNodeFlattenFirstJob : IJob
    {
        [ReadOnly]
        public NativeArray<NativeOctNode> Nodes;

        [WriteOnly]
        public NativeArray<NativeOctNode> Flatten;
        
        public void Execute()
        {
            int flattenCount = 0;

            for (int i = 0; i < Nodes.Length; i++)
            {
                NativeOctNode node = Nodes[i];
                if (node.IsGenerated == false) continue;
                Flatten[flattenCount] = node;
                flattenCount++;
            }
        }
    }

    [BurstCompile]
    public struct OctNodeFlattenSecondJob : IJob
    {
        [WriteOnly]
        public NativeArray<int> MapperIndexOutput;
        
        [WriteOnly]
        public NativeArray<int> MapperTargetOutput;
        
        [ReadOnly]
        public NativeArray<NativeOctNode> Tree;
        [ReadOnly]
        public NativeArray<NativeOctNode> Flatten;

        [ReadOnly] public int Count;
        
        public void Execute()
        {
            int mapperTargetCount = 0;
            int mapperIndexCount = 0;
            for (int i = 0; i < Count; i++)
            {
                NativeOctNode node = Flatten[i];
                
                int index = node.Index;

                int beforeMapperTargetCount = mapperTargetCount;
                for (int j = 1; j <= 8; j++)
                {
                    int childIndex = index * 8 + j;
                    if (childIndex >= Tree.Length) break;

                    node = Tree[childIndex];
                    if (node.IsGenerated)
                    {
                        MapperTargetOutput[mapperTargetCount] = node.FlattenIndex;
                        ++mapperTargetCount;
                    }
                }

                // 자식이 없으면
                if (mapperTargetCount == beforeMapperTargetCount)
                {
                    MapperTargetOutput[mapperTargetCount] = -1;
                    ++mapperTargetCount;
                }

                MapperIndexOutput[mapperIndexCount] = beforeMapperTargetCount;
                ++mapperIndexCount;
            }
        }
    }
}