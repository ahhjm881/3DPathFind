using System;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace Candy.Pathfind3D
{
    [StructLayout(LayoutKind.Sequential)]
    public struct NativeOctNode
    {
        public int Index;

        public bool IsObstacle;
        
        public int Depth;
        
        public float Scale;

        public bool IsGenerated;
        
        public float3 WorldPosition;
        
        public static int IndexSize => sizeof(int);

        public NativeOctNode Default => new(-1, false, 0, 0f, false, float3.zero);
        public NativeOctNode(int index, bool isObstacle, int depth, float scale, bool isGenerated, float3 worldPosition)
        {
            Index = index;
            IsObstacle = isObstacle;
            Depth = depth;
            Scale = scale;
            WorldPosition = worldPosition;
            IsGenerated = isGenerated;
        }
    }
} 