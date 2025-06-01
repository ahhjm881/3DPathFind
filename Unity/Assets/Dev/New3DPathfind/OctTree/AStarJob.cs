using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Candy.Pathfind3D
{
    public struct AStarNodeKey : IEquatable<AStarNodeKey>, IComparable<AStarNodeKey>
    {
        public int TreeIndex;
        public int FlattenIndex;
        public float Weight;

        public AStarNodeKey(int treeIndex, int flattenIndex, float weight = 0f)
        {
            TreeIndex = treeIndex;
            FlattenIndex = flattenIndex;
            Weight = weight;
        }

        public bool Equals(AStarNodeKey other)
        {
            return TreeIndex == other.TreeIndex && FlattenIndex == other.FlattenIndex;
        }

        public override bool Equals(object obj)
        {
            return obj is AStarNodeKey other && Equals(other);
        }
        public override int GetHashCode()
        {
            return (int)math.hash(new int2(TreeIndex, FlattenIndex));
        }
        public int CompareTo(AStarNodeKey other)
        {
            return Weight.CompareTo(other.Weight);
        }

        public static bool operator ==(AStarNodeKey left, AStarNodeKey right) => left.Equals(right);
        public static bool operator !=(AStarNodeKey left, AStarNodeKey right) => !left.Equals(right);
    }
    
    [BurstCompile(DisableSafetyChecks = true)]
    public unsafe struct AStarJob : IJob
    {
        [NativeDisableUnsafePtrRestriction]
        public NativeEdge** Edge2Ptr;
        [ReadOnly]
        public NativeList<int> EdgeLen;
        [ReadOnly]
        public NativeArray<int> EdgeTreeOffset;
        public int Edge2PtrLength;

        [NativeDisableUnsafePtrRestriction]
        public NativeFlattenOctTree* Trees;
        public int TreesLength;

        public int StartTreeIndex;
        public int StartFlattenIndex;
        public int EndTreeIndex;
        public int EndFlattenIndex;

        [NativeDisableContainerSafetyRestriction]
        public MinHeap<AStarNodeKey> OpenSet;
        [NativeDisableContainerSafetyRestriction]
        public NativeHashSet<AStarNodeKey> ClosedSet;
        [NativeDisableContainerSafetyRestriction]
        public NativeHashMap<AStarNodeKey, float> CostFromStart;
        [NativeDisableContainerSafetyRestriction]
        public NativeHashMap<AStarNodeKey, float> EstimatedTotalCost;
        [NativeDisableContainerSafetyRestriction]
        public NativeHashMap<AStarNodeKey, AStarNodeKey> CameFrom;

        public void Execute()
        {
            var start = new AStarNodeKey(StartTreeIndex, StartFlattenIndex);
            var goal = new AStarNodeKey(EndTreeIndex, EndFlattenIndex);

            OpenSet.Insert(start);
            CostFromStart[start] = 0f;
            EstimatedTotalCost[start] = Heuristic(start);

            while (OpenSet.Count > 0)
            {
                AStarNodeKey current = OpenSet.Pop();

                if (current.TreeIndex == goal.TreeIndex && current.FlattenIndex == goal.FlattenIndex)
                    break;

                ClosedSet.Add(current);


                int edgeLenOffset = 0;
                if (current.TreeIndex - 1 < 0)
                {
                    edgeLenOffset = 0;
                }
                else
                {
                    edgeLenOffset = EdgeTreeOffset[current.TreeIndex - 1];
                }
                NativeEdge* neighbors = Edge2Ptr[edgeLenOffset + current.FlattenIndex];
                int count = EdgeLen[edgeLenOffset + current.FlattenIndex];

                for (int i = 0; i < count; i++)
                {
                    
                    NativeEdge edge = neighbors[i];
                    var neighbor = new AStarNodeKey(edge.NextTreeIndex, edge.NextNodeFlattenIndex);

                    if (ClosedSet.Contains(neighbor))
                        continue;

                    float tentativeG = CostFromStart[current] + edge.Weight;

                    bool notInOpenSet = !CostFromStart.ContainsKey(neighbor);
                    if (notInOpenSet || tentativeG < CostFromStart[neighbor])
                    {
                        CameFrom[neighbor] = current;
                        CostFromStart[neighbor] = tentativeG;
                        EstimatedTotalCost[neighbor] = tentativeG + Heuristic(neighbor);
                        neighbor.Weight =  tentativeG + Heuristic(neighbor);;

                        if (notInOpenSet)
                            OpenSet.Insert(neighbor);
                    }
                }
            }
        }

        private float Heuristic(AStarNodeKey node)
        {
            var octNode = Trees[node.TreeIndex].FlattenPtr[node.FlattenIndex];
            var endNode =  Trees[EndTreeIndex].FlattenPtr[EndFlattenIndex];
            
            return math.distancesq(octNode.WorldPosition, endNode.WorldPosition);
        }
    }
}
