using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Profiling;

namespace Candy.Pathfind3D
{
    public class Test : MonoBehaviour
    {
        public Vector3Int Size;
        public bool IsDraw;

        [SerializeField] private OctTree.InitParameter _param;

        private static int GetNodeCountOfDepth(int depth)
        {
            int d = 0;

            /*
            d += IntPow8(depth);
            d += IntPow8(depth - 1);

            return d;*/
            
            for (int i = 0; i <= depth; i++)
            {
                d += IntPow8(i);
            }

            return d;
        }

        private static int IntPow8(int x)
        {
            if (x < 0) return 0;

            int d = 1;
            for (int i = 0; i < x; i++)
            {
                d *= 8;
            }

            return d;
        }
        private OctTree[,,] _trees;
        private void Start()
        {
            _trees = new OctTree[Size.x, Size.y, Size.z];

            Profiler.BeginSample("Create Space");
            
            Profiler.BeginSample("Init Physics Buffer");
            (NativeArray<OverlapBoxCommand> overlapBoxCommands, NativeArray<ColliderHit> results) = OctTree.CreatePhysicsBuffer(_param);
            Profiler.EndSample();
            
            
            Profiler.BeginSample("Create");
            for (int i = 0; i < Size.x; i++)
            {
                for (int j = 0; j < Size.y; j++)
                {
                    for (int k = 0; k < Size.z; k++)
                    {
                        _param.WorldPosition = transform.position + new Vector3(i, j, k) * _param.Scale +
                                               Vector3.one * _param.Scale * 0.5f;
                        var tree = new OctTree(_param);
                        tree.CreateSpace(overlapBoxCommands, results);
                        _trees[i, j, k] = tree;
                    }
                }
            }
            Profiler.EndSample();
            
            Profiler.BeginSample("Release Physics Buffer");
            overlapBoxCommands.Dispose();
            results.Dispose();
            Profiler.EndSample();
            
            Profiler.EndSample();
        }

        private void OnDestroy()
        {
            if (_trees is null) return;
            
            for (int i = 0; i < Size.x; i++)
            {
                for (int j = 0; j < Size.y; j++)
                {
                    for (int k = 0; k < Size.z; k++)
                    {
                        _trees[i, j, k].Dispose();
                    }
                }
            }

            _trees = null;
        }

        private void OnDrawGizmos()
        {
            Gizmos.DrawWireCube(transform.position + _param.Scale * (Vector3)Size * 0.5f, _param.Scale * (Vector3)Size);

            if (IsDraw is false) return;
            if (_trees is null) return;
            

            for (int x = 0; x < Size.x; x++)
            {
                for (int y = 0; y < Size.y; y++)
                {
                    for (int z = 0; z < Size.z; z++)
                    {
                        OctTree tree = _trees[x, y, z];
                        NativeFlattenOctTree nativeTree = tree.NativeTree;
                        Queue<int> queue = new Queue<int>(100);
                        queue.Enqueue(nativeTree.RootIndex);

                        bool exit = false;

                        while (queue.Any() && exit is false)
                        {
                            int count = queue.Count;
                            for (int i = 0; i < count; i++)
                            {
                                int index = queue.Dequeue();
                                NativeOctNode node = nativeTree.GetNode(index);
                                Color nodeColor;

                                if (node.IsGenerated is false)
                                {
                                    Debug.LogError($"[ERROR] node index: {node.Index}");
                                    nodeColor = Color.red;
                                }
                                else
                                {
                                    nodeColor = node.IsObstacle ? Color.yellow : Color.blue;
                                }

                                NativeFlattenOctTree.IndexRange range = nativeTree.GetChildIndexRange(index);
                                if (range.IsValid())
                                {
                                    for (int j = range.Begin; j < range.End; j++)
                                    {
                                        int childIndex = nativeTree.MapIndex(j);
                                        if (childIndex == -1) continue;
                                        queue.Enqueue(childIndex);
                                    }
                                }
                                
                                if(nativeTree.HasChild(range) is false)
                                {
                                    Gizmos.color = nodeColor;
                                    Gizmos.DrawWireCube(node.WorldPosition, Vector3.one * node.Scale);
                                }
                            }
                        }
                    }
                }
            }

        }
    }
}