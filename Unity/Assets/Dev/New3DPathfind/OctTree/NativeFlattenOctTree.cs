using System;
using Unity.Collections;

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
        
        public NativeArray<NativeOctNode> FlattenArr;
        public NativeArray<int> IndexArr;
        public NativeArray<int> TreeArr;

        public int Depth;
        public int TreeIndex;

        public long Size
        {
            get
            {
                long size = 0;
                long totalSize = 0;
                
                size = NativeArrayMemoryTracker.GetMemorySizeBytes(FlattenArr);
                if (size == 0)
                {
                    return 0;
                }
                totalSize += size;

                size = NativeArrayMemoryTracker.GetMemorySizeBytes(IndexArr);
                if (size == 0)
                {
                    return 0;
                }
                totalSize += size;

                size = NativeArrayMemoryTracker.GetMemorySizeBytes(TreeArr);
                if (size == 0)
                {
                    return 0;
                }
                totalSize += size;

                return totalSize;
            }
        }

        public int RootIndex => 0;

        public NativeOctNode GetNode(int index, NativeArray<NativeOctNode> flattenArr)
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