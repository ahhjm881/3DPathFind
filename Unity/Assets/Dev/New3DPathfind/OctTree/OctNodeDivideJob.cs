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
        public NativeArray<OctNode> Nodes;
        
        public void Execute(int index)
        {
            Nodes[index] = new OctNode(0, false, 0, 0f, false, float3.zero);
        }
    }

    [BurstCompile(DisableSafetyChecks = true)]
    public struct OctNodeSetObstacleJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<ColliderHit> Hits;
        
        public NativeArray<OctNode> Nodes;
        
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
            OctNode parentNode = WriteOctNodeList[parentIndex];
            
            if (parentNode.IsObstacle is false || parentNode.IsGenerated is false)
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
                
                QueryParameters q = QueryParameters;
                q.layerMask = 0;
                Commands[originIndex] = new OverlapBoxCommand(Vector3.zero, Vector3.zero, Quaternion.identity, q);
                return;
            }

            float childScale = parentNode.Scale * 0.5f;
            float3 parentPos = parentNode.WorldPosition;

            int childNum = index - (8 * parentIndex) - 1;
            float3 childPos = (ChildDirectionVectorArray[childNum] * childScale) + parentPos;

            OctNode childNode = new OctNode(
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
}