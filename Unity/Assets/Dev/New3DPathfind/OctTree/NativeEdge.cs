using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

namespace Candy.Pathfind3D
{
    public struct NativeEdge
    {
        public float Weight;
        public int TreeIndex;
        public int NodeFlattenIndex;

        public static int ALIGNMENT_SIZE = UnsafeUtility.AlignOf<NativeEdge>(); 

        public float3 DEBUG_POINT_START;
        public float3 DEBUG_POINT_END;
    }
}