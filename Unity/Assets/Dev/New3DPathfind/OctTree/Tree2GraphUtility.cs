using System.Collections.Generic;
using UnityEngine;

namespace Candy.Pathfind3D
{
    public enum NodeDirection : int
    {
        Front,
        Right,
        Back,
        Left,
        Up,
        Down,
        FrontLeftUp,
        FrontRightUp,
        FrontLeft,
        FrontRight,
        FrontLeftDown,
        FrontRightDown,
        FrontUp,
        FrontDown,
        LeftUp,
        RightUp,
        LeftDown,
        RightDown,
        BackLeftUp,
        BackRightUp,
        BackLeft,
        BackRight,
        BackLeftDown,
        BackRightDown,
        BackUp,
        BackDown
    }

    public class OctTreeNeighborIndexCalculator
    {
        public int SizeX => _sizeX;

        public int SizeY => _sizeY;

        public int SizeZ => _sizeZ;

        public OctTreeNeighborIndexCalculator(int sizeX, int sizeY, int sizeZ)
        {
            _sizeX = sizeX;
            _sizeY = sizeY;
            _sizeZ = sizeZ;
            _trees = new OctTree[sizeX, sizeY, sizeZ];
        }
        public int GetNeighbors(int x, int y, int z, int[,] xyzArray)
        {
            int count = 0;

            foreach (var offset in DirectionOffsets.Values)
            {
                int nx = x + offset.x;
                int ny = y + offset.y;
                int nz = z + offset.z;

                if (IsInBounds(nx, ny, nz))
                {
                    xyzArray[count, 0] = nx;
                    xyzArray[count, 1] = ny;
                    xyzArray[count, 2] = nz;
                    count++;
                }
            }

            return count;
        }
        
        private OctTree[,,] _trees;
        private int _sizeX, _sizeY, _sizeZ;

        private static readonly Dictionary<NodeDirection, Vector3Int> DirectionOffsets = new()
        {
            { NodeDirection.Front, new Vector3Int(0, 0, 1) },
            { NodeDirection.Right, new Vector3Int(1, 0, 0) },
            { NodeDirection.Back, new Vector3Int(0, 0, -1) },
            { NodeDirection.Left, new Vector3Int(-1, 0, 0) },
            { NodeDirection.Up, new Vector3Int(0, 1, 0) },
            { NodeDirection.Down, new Vector3Int(0, -1, 0) },

            { NodeDirection.FrontLeftUp, new Vector3Int(-1, 1, 1) },
            { NodeDirection.FrontRightUp, new Vector3Int(1, 1, 1) },
            { NodeDirection.FrontLeft, new Vector3Int(-1, 0, 1) },
            { NodeDirection.FrontRight, new Vector3Int(1, 0, 1) },
            { NodeDirection.FrontLeftDown, new Vector3Int(-1, -1, 1) },
            { NodeDirection.FrontRightDown, new Vector3Int(1, -1, 1) },
            { NodeDirection.FrontUp, new Vector3Int(0, 1, 1) },
            { NodeDirection.FrontDown, new Vector3Int(0, -1, 1) },

            { NodeDirection.LeftUp, new Vector3Int(-1, 1, 0) },
            { NodeDirection.RightUp, new Vector3Int(1, 1, 0) },
            { NodeDirection.LeftDown, new Vector3Int(-1, -1, 0) },
            { NodeDirection.RightDown, new Vector3Int(1, -1, 0) },

            { NodeDirection.BackLeftUp, new Vector3Int(-1, 1, -1) },
            { NodeDirection.BackRightUp, new Vector3Int(1, 1, -1) },
            { NodeDirection.BackLeft, new Vector3Int(-1, 0, -1) },
            { NodeDirection.BackRight, new Vector3Int(1, 0, -1) },
            { NodeDirection.BackLeftDown, new Vector3Int(-1, -1, -1) },
            { NodeDirection.BackRightDown, new Vector3Int(1, -1, -1) },
            { NodeDirection.BackUp, new Vector3Int(0, 1, -1) },
            { NodeDirection.BackDown, new Vector3Int(0, -1, -1) },
        };

        private bool IsInBounds(int x, int y, int z)
        {
            return x >= 0 && x < _sizeX &&
                   y >= 0 && y < _sizeY &&
                   z >= 0 && z < _sizeZ;
        }
    }
}