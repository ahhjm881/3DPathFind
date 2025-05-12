using UnityEngine;

namespace Candy.Pathfind3D
{
    public static class IntegerMath
    {
        // 8진트리에서 depth에 해당하는 노드의 총 개수를 반환
        public static int GetNodeCountOfDepth(int depth)
        {
            int d = 0;
            
            for (int i = 0; i <= depth; i++)
            {
                d += IntPow8(i);
            }

            return d;
        }
        
        // 8의 거듭제곱 
        public static int IntPow8(int x)
        {
            if (x < 0) return 0;

            int d = 1;
            for (int i = 0; i < x; i++)
            {
                d *= 8;
            }

            return d;
        }
        // 2의 거듭제곱 
        public static int IntPow2(int x)
        {
            if (x < 0) return 0;

            int d = 1;
            for (int i = 0; i < x; i++)
            {
                d *= 2;
            }

            return d;
        }
        
        // 8의 3 거듭제곱근
        public static int CubeRootOf8(int x)
        {
            /*
             * x^3 = 8^x
             * x = cube_sqrt(8^x)
             * x = cube_sqrt((2^3)^x) = cube_sqrt(2^3x) 
             * x = 2^x
            */
            
            return IntPow2(x);
        }
    }
}