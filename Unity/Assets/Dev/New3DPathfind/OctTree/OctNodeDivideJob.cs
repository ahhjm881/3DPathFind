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

    [BurstCompile(DisableSafetyChecks = true)]
    public struct OctNodeSetObstacleJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<ColliderHit> Hits;
        [ReadOnly] public NativeArray<OctNode> Nodes;
        
        public void Execute(int index)
        {
            if (Hits[index].instanceID != 0)
            {
                OctNode tempNode = Nodes[index];
                tempNode.IsObstacle = true;
                Nodes[index] = tempNode;
            }
        }
    }

    [BurstCompile(DisableSafetyChecks = true)]
    public struct OctNodeDivideJob : IJobParallelFor
    {
        public NativeArray<OctNode> WriteOctNodeList;

        [ReadOnly] 
        public int CurrentDepth;
        
        [ReadOnly]
        public NativeArray<float3> ChildDirectionVectorArray;

        [ReadOnly] public int Offset;

        public void Execute(int index)
        {
            index += Offset;
            if (index == 0) return;
            int parentIndex = (index - 1) / 8;

            if (WriteOctNodeList[parentIndex].Depth != CurrentDepth - 1)
            {
                return;
            }
            if (WriteOctNodeList[parentIndex].IsObstacle is false)
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


            OctNode parentNode = WriteOctNodeList[parentIndex];

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