using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Profiling;
using Debug = UnityEngine.Debug;

namespace Candy.Pathfind3D
{

    public class Test : MonoBehaviour
    {
        public Vector3Int Size;
        public bool IsDrawNode;
        public bool IsDrawEdge;

        [SerializeField] private OctTree.InitParameter _param;

        private OctTree[,,] _trees;
        private OctGraph _graph;

        private void Start()
        {
            _trees = new OctTree[Size.x, Size.y, Size.z];
            
            Stopwatch w = new Stopwatch();

            Profiler.BeginSample("Init Common Buffer");
            (NativeArray<OverlapBoxCommand> overlapBoxCommands, NativeArray<ColliderHit> results) =
                OctTree.CreatePhysicsBuffer(_param);
            NativeArray<NativeOctNode> treeBuffer = OctTree.CreateTreeBuffer(_param);
            Profiler.EndSample();

            long totalFlattenTreeSize = 0;
            
            w.Start();
            Profiler.BeginSample("Create");
            int treeIndex = 0;
            for (int i = 0; i < Size.x; i++)
            {
                for (int j = 0; j < Size.y; j++)
                {
                    for (int k = 0; k < Size.z; k++)
                    {
                        _param.WorldPosition = transform.position + new Vector3(i, j, k) * _param.Scale +
                                               Vector3.one * _param.Scale * 0.5f;
                        var tree = new OctTree(_param, treeIndex++);
                        tree.CreateSpace(overlapBoxCommands, results, treeBuffer);
                        totalFlattenTreeSize += tree.NativeTree.Size;
                        _trees[i, j, k] = tree;
                    }
                }
            }
            Profiler.EndSample();
            w.Stop();

            StringBuilder str = new();
            long[] sizes = new long[3];
            str.AppendLine(NativeArrayMemoryTracker.GetMemoryUsageMessage(overlapBoxCommands, out sizes[0], "OverlapBoxCommand"));
            str.AppendLine(NativeArrayMemoryTracker.GetMemoryUsageMessage(results, out sizes[1], "ColliderHit"));
            str.AppendLine(NativeArrayMemoryTracker.GetMemoryUsageMessage(treeBuffer, out sizes[2], "Buffer"));
            str.AppendLine($"Flatten Tree Avg size: {NativeArrayMemoryTracker.FormatBytes(totalFlattenTreeSize / _trees.Length)}");
            str.AppendLine($"총 피크 메모리: {NativeArrayMemoryTracker.FormatBytes(sizes.Sum() + totalFlattenTreeSize)}");
            str.AppendLine($"최종 메모리: {NativeArrayMemoryTracker.FormatBytes(totalFlattenTreeSize)}");
            str.AppendLine($"걸린 시간: {w.ElapsedMilliseconds}ms");
            Debug.Log(str);

            Profiler.BeginSample("Release Physics Buffer");
            overlapBoxCommands.Dispose();
            results.Dispose();
            treeBuffer.Dispose();
            Profiler.EndSample();
            
            Profiler.BeginSample("Create Graph");
            _graph = new OctGraph();
            _graph.Tree2Graph(_trees[0,0,0].NativeTree);
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
            
            _graph.Dispose();
            _graph = null;
        }

        private void DrawNode()
        {
            Gizmos.DrawWireCube(transform.position + _param.Scale * (Vector3)Size * 0.5f, _param.Scale * (Vector3)Size);

            if (IsDrawNode is false) return;
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

                                if (nativeTree.HasChild(range) is false)
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

        private void DrawEdge()
        {
            if (_graph is null) return;
            if (IsDrawEdge is false) return;
            
            if (_graph.IsCreated is false) return;
            
            for (int i = 0; i < _graph.EdgeArrLength; i++)
            {
                for (int j = 0; j < _graph.EdgeLen[i]; j++)
                {
                    NativeEdge edge = _graph.GetEdge(i, j);
                    
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawLine(edge.DEBUG_POINT_START, edge.DEBUG_POINT_END);
                }
            }
        }

        private void OnDrawGizmos()
        {
            DrawNode();
            DrawEdge();
        }
    }
}