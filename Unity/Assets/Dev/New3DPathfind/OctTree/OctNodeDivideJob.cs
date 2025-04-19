using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Serialization;

namespace Candy.Pathfind3D
{
    [BurstCompile]
    public struct OctNodeSetCommandJob : IJobParallelFor
    {
        [WriteOnly] public NativeArray<OverlapBoxCommand> Output;
        [ReadOnly] public NativeArray<OctNode> Input;
        [ReadOnly] public QueryParameters QueryParameters;
        
        public void Execute(int index)
        {
            OverlapBoxCommand command = new OverlapBoxCommand(
                Input[index].WorldPosition,
                Input[index].Scale * new float3(1f, 1f, 1f) * 0.5f,
                Quaternion.identity,
                QueryParameters
            );

            Output[index] = command;
        }
    }

    [BurstCompile]
    public struct OctNodeSetObstacleJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<ColliderHit> Hits;
        [ReadOnly] public NativeArray<OctNode> Input;
        
        [WriteOnly]
        public NativeArray<OctNode> Output;
        
        public void Execute(int index)
        {
            if (Hits[index].instanceID != 0)
            {
                OctNode tempNode = Input[index];
                tempNode.IsObstacle = true;
                Output[index] = tempNode;
            }
        }
    }
    
    [BurstCompile]
    public struct OctNodeGenerateJob : IJobParallelFor
    {
        [WriteOnly] public NativeArray<OctNode> Output;
        
        public void Execute(int index)
        {
            if (index == 0) return;
            
            Output[index] = new OctNode(
                index, false, 0, 0, false, Vector3.zero
            );
        }
    }

    [BurstCompile]
    public struct OctNodeDivideJob : IJobParallelFor
    {
        [WriteOnly]
        public NativeArray<OctNode> WriteOctNodeList;

        [ReadOnly] 
        public int CurrentDepth;
        
        [ReadOnly]
        public NativeArray<OctNode> ReadOctNodeList;
        
        [ReadOnly]
        public NativeArray<float3> ChildDirectionVectorArray;

        public void Execute(int index)
        {
            if (index == 0) return;
            int parentIndex = (index - 1) / 8;

            if (ReadOctNodeList[parentIndex].Depth != CurrentDepth - 1)
            {
                return;
            }
            if (ReadOctNodeList[parentIndex].IsObstacle is false)
            {
                OctNode n = new OctNode(
                    0,
                    false,
                    0,
                    0f,
                    false,
                    float3.zero
                );

                WriteOctNodeList[index] = n;
                return;
            }


            OctNode parentNode = ReadOctNodeList[parentIndex];

            float childScale = parentNode.Scale * 0.5f;
            float3 parentPos = parentNode.WorldPosition;

            int childNum = index - (8 * parentIndex) - 1;
            float3 childPos = (ChildDirectionVectorArray[childNum] * childScale) + parentPos;

            OctNode childNode = new OctNode(
                index,
                false,
                CurrentDepth,
                childScale,
                true,
                childPos
            );

            WriteOctNodeList[index] = childNode;
        }
    }
}