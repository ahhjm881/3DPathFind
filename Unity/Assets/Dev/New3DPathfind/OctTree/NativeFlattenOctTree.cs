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

        public int RootIndex => 0;

        public NativeOctNode GetNode(int index)
        {
            if (index < 0)
            {
                return new NativeOctNode()
                {
                    Index = -1,
                    IsGenerated = false
                };
            }
            if (index >= FlattenArr.Length)
            {
                return new NativeOctNode()
                {
                    Index = -1,
                    IsGenerated = false
                };
            }

            return FlattenArr[index];
        }

        public int MapIndex(int index)
        {
            if (index < 0)
            {
                return -1;
            }
            if (index >= TreeArr.Length)
            {
                return -1;
            }

            return TreeArr[index];
        }

        public bool HasChild(IndexRange range)
        {
            if (range.IsValid() == false) return false;

            for (int i = range.Begin; i < range.End; i++)
            {
                if (TreeArr[i] == -1) return false;
            }

            return true;
        }
        
        public IndexRange GetChildIndexRange(int index)
        {
            if (index < 0)
            {
                return new IndexRange()
                {
                    Begin = -1,
                    End = -1,
                };
            }
            if (index >= IndexArr.Length)
            {
                return new IndexRange()
                {
                    Begin = -1,
                    End = -1,
                };
            }
            
            int begin = IndexArr[index];
            int end = (index + 1) >= IndexArr.Length ? IndexArr.Length : IndexArr[index + 1];

            return new()
            {
                Begin = begin,
                End = end
            };
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