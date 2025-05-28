using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

namespace Candy.Pathfind3D
{
    public struct NativeEdge
    {
        public float Weight;
        public int PrevTreeIndex;
        public int NextTreeIndex;
        public int PrevNodeFlattenIndex;
        public int NextNodeFlattenIndex;
    }
}