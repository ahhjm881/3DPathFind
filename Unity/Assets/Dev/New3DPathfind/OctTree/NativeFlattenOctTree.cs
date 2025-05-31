using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using Unity.Mathematics.Geometry;

namespace Candy.Pathfind3D
{
    public struct NativeFlattenOctTree : IDisposable
    {
        public struct IndexRange
        {
            // inclusive
            public int Begin;
            
            // exclusive
            public int End;

            public bool IsValid()
            {
                if (Begin == -1) return false;
                if (End == -1) return false;

                return true;
            }
        }
        
        [NativeDisableContainerSafetyRestriction]
        public NativeArray<NativeOctNode> FlattenArr;
        [NativeDisableContainerSafetyRestriction]
        public NativeArray<int> IndexArr;
        [NativeDisableContainerSafetyRestriction]
        public NativeArray<int> TreeArr;

        public unsafe NativeOctNode* FlattenPtr;
        public unsafe int* IndexPtr;
        public unsafe int* TreePtr;


        public int Depth;
        public int TreeIndex;

        public long Size
        {
            get
            {
                long size = 0;
                long totalSize = 0;
                
                size = NativeUtility.GetMemorySizeBytes(FlattenArr);
                if (size == 0)
                {
                    return 0;
                }
                totalSize += size;

                size = NativeUtility.GetMemorySizeBytes(IndexArr);
                if (size == 0)
                {
                    return 0;
                }
                totalSize += size;

                size = NativeUtility.GetMemorySizeBytes(TreeArr);
                if (size == 0)
                {
                    return 0;
                }
                totalSize += size;

                return totalSize;
            }
        }

        public int RootIndex => 0;

        public static int FindLeafNodeIndex(float3 position, float3 size ,NativeArray<NativeOctNode> flattenArr, NativeArray<int> treeArr, NativeArray<int> indexArr, NativeList<int> tempBuffer0, NativeList<int> tempBuffer1)
        {
            MinMaxAABB myAABB = MinMaxAABB.CreateFromCenterAndExtents(position , size);
            tempBuffer0.Clear();
            tempBuffer1.Clear();
            
            // root index
            tempBuffer0.Add(0);

            while (true)
            {
                int len = tempBuffer0.Length;
                tempBuffer1.Clear();
                if (len < 1)
                {
                    return -1;
                }

                for (int i = 0; i < len; i++)
                {
                    int targetIndex = tempBuffer0[i];
                    NativeOctNode targetNode = flattenArr[targetIndex];


                    MinMaxAABB targetAABB =
                        MinMaxAABB.CreateFromCenterAndExtents(targetNode.WorldPosition,
                            targetNode.Scale * new float3(1f, 1f, 1f));

                    if (targetAABB.Contains(myAABB.Center))
                    {
                        IndexRange targetIndexRage = GetChildIndexRange(targetNode.FlattenIndex, indexArr);
                        
                        if (targetNode.IsObstacle == false && targetIndexRage.IsValid() == false)
                            return targetNode.FlattenIndex;
                        
                        if (HasChild(targetIndexRage, treeArr))
                        {
                            for (int j = targetIndexRage.Begin; j < targetIndexRage.End; j++)
                            {
                                int mapIndex = MapIndex(j, treeArr);
                                if (mapIndex == -1) continue;
                                tempBuffer1.Add(mapIndex);
                            }

                            continue;
                        }


                        if (targetNode.IsObstacle) continue;
                        return targetNode.FlattenIndex;

                    }
                }

                var tempArr = tempBuffer0;
                tempBuffer0 = tempBuffer1;
                tempBuffer1 = tempArr;
            }
        }

        public NativeOctNode GetNode(int index,NativeArray<NativeOctNode> flattenArr)
        {
            if (index < 0)
            {
                return new NativeOctNode()
                {
                    Index = -1,
                    IsGenerated = false
                };
            }
            if (index >= flattenArr.Length)
            {
                return new NativeOctNode()
                {
                    Index = -1,
                    IsGenerated = false
                };
            }

            return flattenArr[index];
        }
        public NativeOctNode GetNode(int index)
        {
            return GetNode(index, FlattenArr);
        }

        public static int MapIndex(int index, NativeArray<int> treeArr)
        {
            if (index < 0)
            {
                return -1;
            }
            if (index >= treeArr.Length)
            {
                return -1;
            }

            return treeArr[index];
        }

        public int MapIndex(int index)
        {
            return MapIndex(index, TreeArr);
        }

        public static bool HasChild(IndexRange range, NativeArray<int> treeArr)
        {
            if (range.IsValid() == false) return false;

            for (int i = range.Begin; i < range.End; i++)
            {
                if (treeArr[i] == -1) return false;
            }

            return true;
        }

        public bool HasChild(IndexRange range)
        {
            return HasChild(range, TreeArr);
        }

        public static IndexRange GetChildIndexRange(int index, NativeArray<int> indexArr)
        {
            if (index < 0)
            {
                return new IndexRange()
                {
                    Begin = -1,
                    End = -1,
                };
            }
            if (index >= indexArr.Length)
            {
                return new IndexRange()
                {
                    Begin = -1,
                    End = -1,
                };
            }
            
            int begin = indexArr[index];
            int end = (index + 1) >= indexArr.Length ? indexArr[index] + 1 : indexArr[index + 1];

            return new()
            {
                Begin = begin,
                End = end
            };
        }
        
        public IndexRange GetChildIndexRange(int index)
        {
            return GetChildIndexRange(index, IndexArr);
        }

        public void Dispose()
        {
            if (FlattenArr.IsCreated)
            {
                FlattenArr.Dispose();
            }
            if (IndexArr.IsCreated)
            {
                IndexArr.Dispose();
            }
            if (TreeArr.IsCreated)
            {
                TreeArr.Dispose();
            }
        }
    }
}