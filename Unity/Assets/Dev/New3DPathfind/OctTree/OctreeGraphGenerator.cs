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

    public class OctreeGraphGenerator : MonoBehaviour
    {
        public Vector3Int Size;
        public bool AwakeGenerateSpace;
        public bool AwakeGenerateGraph;
        public bool IsDrawCollisionNode;
        public bool IsDrawEmptyNode;
        public bool IsDrawEdge;
 
        
        [SerializeField] private OctTree.InitParameter _param;

        private OctTree[,,] _trees;
        private (int x, int y, int z)[] _treeIndices;
        private OctGraph _graph;

        public NativeOctGraph NativeOctGraph => _graph.NativeGraph;
        public NativeOctTree3D NativeOctTree3D { get; private set; }

        private void Start()
        {
            if (AwakeGenerateSpace)
            {
                CreateSpace();
            }
            if (AwakeGenerateGraph)
            {
                CreateGraph(); 
            }
        }

        public void SetGraph(NativeOctGraph graph)
        {
            if (_graph == null)
            {
                _graph = new OctGraph(graph);
                return;
            }
            NativeOctGraph.Dispose();
            _graph.NativeGraph = graph;
        }

        private void CreateSpace()
        {
            _trees = new OctTree[Size.x, Size.y, Size.z];
            _treeIndices = new (int, int, int)[Size.x * Size.y * Size.z];
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
                        _treeIndices[treeIndex - 1] = (i, j, k);
                    }
                }
            }
            Profiler.EndSample();
            w.Stop();

            StringBuilder str = new();
            long[] sizes = new long[3];
            str.AppendLine(NativeUtility.GetMemoryUsageMessage(overlapBoxCommands, out sizes[0], "OverlapBoxCommand"));
            str.AppendLine(NativeUtility.GetMemoryUsageMessage(results, out sizes[1], "ColliderHit"));
            str.AppendLine(NativeUtility.GetMemoryUsageMessage(treeBuffer, out sizes[2], "Buffer"));
            str.AppendLine($"Flatten Tree Avg size: {NativeUtility.FormatBytes(totalFlattenTreeSize / _trees.Length)}");
            str.AppendLine($"총 피크 메모리: {NativeUtility.FormatBytes(sizes.Sum() + totalFlattenTreeSize)}");
            str.AppendLine($"최종 메모리: {NativeUtility.FormatBytes(totalFlattenTreeSize)}");
            str.AppendLine($"걸린 시간: {w.ElapsedMilliseconds}ms");
            Debug.Log(str);

            Profiler.BeginSample("Release Physics Buffer");
            overlapBoxCommands.Dispose();
            results.Dispose();
            treeBuffer.Dispose();
            Profiler.EndSample();

            NativeOctTree3D = new NativeOctTree3D(transform.position + Vector3.one * _param.Scale * 0.5f, _param.Scale, _trees, Size);
        }

        private void CreateGraph()
        {
            Stopwatch w = new Stopwatch();
            w.Start();
            
            Profiler.BeginSample("Create Graph");
            _graph = new OctGraph();

            OctTreeNeighborIndexCalculator calculator = new(Size.x, Size.y, Size.z);
            int[,] neighborCoords = new int[26, 3]; // [i, 0]=x, [i,1]=y, [i,2]=z
            long size = 0;
            int b = 0;

            for (int x = 0; x < calculator.SizeX; x++)
            {
                for (int y = 0; y < calculator.SizeY; y++)
                {
                    for (int z = 0; z < calculator.SizeZ; z++)
                    {
                        int offset = _graph.NativeGraph.Edge2PtrLength;
                        _graph.Tree2Graph(_trees[x, y, z].NativeTree, _trees[x, y, z].NativeTree, Size.x * Size.y * Size.z, false, offset);

                        int count = calculator.GetNeighbors(x, y, z, neighborCoords);

                        for (int i = 0; i < count; i++)
                        {
                            int nx = neighborCoords[i, 0];
                            int ny = neighborCoords[i, 1];
                            int nz = neighborCoords[i, 2];

                            _graph.Tree2Graph(_trees[x, y, z].NativeTree, _trees[nx, ny, nz].NativeTree, Size.x * Size.y * Size.z, true, offset);
                        }

                        _graph.NativeGraph.EdgeTreeOffset[_trees[x, y, z].NativeTree.TreeIndex] = (b += _trees[x, y, z].NativeTree.FlattenArr.Length);

                        size += _graph.TotalSize;
                    }
                }
            }
            w.Stop();
            Profiler.EndSample();
            
            Debug.Log($"====");
            Debug.Log($"Graph 생성 소요 시간: {w.ElapsedMilliseconds}ms");
            Debug.Log($"Graph 메모리 사용량: {NativeUtility.FormatBytes(size)}");
        }

        private void OnDestroy()
        {
            NativeOctTree3D.Dispose();
            _trees = null;
            
            NativeOctGraph.Dispose();
            _graph.Dispose();
            _graph = null;
        }

        private void DrawNode()
        {
            Gizmos.DrawWireCube(transform.position + _param.Scale * (Vector3)Size * 0.5f, _param.Scale * (Vector3)Size);

            if (IsDrawCollisionNode is false && IsDrawEmptyNode is false) return;
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
                                    if ((IsDrawCollisionNode && node.IsObstacle) ||
                                        (IsDrawEmptyNode && node.IsObstacle is false))
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

                    var fromTreeXYZ = _treeIndices[edge.PrevTreeIndex];
                    var toTreeXYZ = _treeIndices[edge.NextTreeIndex];
                    NativeOctNode fromNode = _trees[fromTreeXYZ.x, fromTreeXYZ.y, fromTreeXYZ.z].NativeTree.GetNode(edge.PrevNodeFlattenIndex);
                    NativeOctNode toNode = _trees[toTreeXYZ.x, toTreeXYZ.y, toTreeXYZ.z].NativeTree.GetNode(edge.NextNodeFlattenIndex);
                    Gizmos.DrawLine(fromNode.WorldPosition,toNode.WorldPosition);
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