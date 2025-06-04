using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;

namespace Candy.Pathfind3D
{
    public unsafe struct NativeOctTree3D : System.IDisposable
    {
        // 유니티 job system에서는 중첩 native array를 금지하고 있어 (NativeFlattenOctTree 내부적으로 NativeArray를 사용)
        // NativeFlattenOctTree* 으로 변경해
        [NativeDisableContainerSafetyRestriction]
        private NativeFlattenOctTree* _trees;
        private int3 _size;
        private int _length;
        
        public NativeFlattenOctTree* Trees => _trees;

        public int3 Size3D => _size;
        public int TreeCount => _length;
        public bool IsCreated => _trees != null;
        public Vector3 RootPosition { get; private set; }
        public float TreeScale { get; private set; }

        public NativeOctTree3D(Vector3 rootPosition, float treeScale, OctTree[,,] trees, Vector3Int size)
        {
            RootPosition = rootPosition;
            TreeScale = treeScale;
            _size = new int3(size.x, size.y, size.z);
            _length = size.x * size.y * size.z;
            long totalSize = _length * sizeof(NativeFlattenOctTree);
            _trees = (NativeFlattenOctTree*)Unity.Collections.LowLevel.Unsafe.UnsafeUtility.Malloc(totalSize, 16, Allocator.Persistent);
            
            int idx = 0;
            for (int x = 0; x < size.x; x++)
            {
                for (int y = 0; y < size.y; y++)
                {
                    for (int z = 0; z < size.z; z++)
                    {
                        OctTree tree = trees[x, y, z];
                        _trees[idx++] = tree.NativeTree;
                    }
                }
            }
        }
        
        public NativeFlattenOctTree GetTree(int treeIndex)
        {
            return _trees[treeIndex];
        }

        public NativeFlattenOctTree GetTree(int x, int y, int z)
        {
            return _trees[To1DIndex(x, y, z, _size)];
        }
        
        public int3 GetCoords(int flatIndex)
        {
            return To3DIndex(flatIndex, _size);
        }
        
        public int GetFlatIndex(int x, int y, int z)
        {
            return To1DIndex(x, y, z, _size);
        }
        
        public int FindTreeToIncludePoint(Vector3 position)
        {
            for (int i = 0; i < TreeCount; i++)
            {
                int3 pos = GetCoords(i);
                Vector3 scale = new Vector3(pos.x, pos.y, pos.z) * TreeScale;
                Bounds bounds = new Bounds(RootPosition + scale , Vector3.one * TreeScale);
                if (bounds.Contains(position))
                {
                    return i;
                }
            }

            return -1;
        }
        
        public void Dispose()
        {
            if (_trees != null)
            {
                Unity.Collections.LowLevel.Unsafe.UnsafeUtility.Free(_trees, Allocator.Persistent);
                _trees = null;
            }
        }

        private static int To1DIndex(int x, int y, int z, int3 size)
        {
            return x * size.y * size.z + y * size.z + z;
        }
        
        private static int3 To3DIndex(int index, int3 size)
        {
            int x = index / (size.y * size.z);
            int y = (index / size.z) % size.y;
            int z = index % size.z;
            return new int3(x, y, z);
        }
    }
}
