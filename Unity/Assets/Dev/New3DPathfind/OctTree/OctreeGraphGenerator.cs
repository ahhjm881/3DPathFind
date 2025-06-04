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

        public NativeOctGraph NativeOctGraph { get; private set; }
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
            if (NativeOctGraph.IsCreated)
            {
                NativeOctGraph.Dispose();
            }
            NativeOctGraph = graph;
        }

        public void SetTree(NativeOctTree3D tree3d)
        {
            if (NativeOctTree3D.IsCreated)
            {
                NativeOctTree3D.Dispose();
            }
            NativeOctTree3D = tree3d;
        }

        private void CreateSpace()
        {
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
            List<NativeFlattenOctTree> nativeTreeList = new List<NativeFlattenOctTree>();
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
                        nativeTreeList.Add(tree.NativeTree);
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
            str.AppendLine($"Flatten Tree Avg size: {NativeUtility.FormatBytes(totalFlattenTreeSize / (Size.x * Size.y * Size.z))}");
            str.AppendLine($"총 피크 메모리: {NativeUtility.FormatBytes(sizes.Sum() + totalFlattenTreeSize)}");
            str.AppendLine($"최종 메모리: {NativeUtility.FormatBytes(totalFlattenTreeSize)}");
            str.AppendLine($"걸린 시간: {w.ElapsedMilliseconds}ms");
            Debug.Log(str);

            Profiler.BeginSample("Release Physics Buffer");
            overlapBoxCommands.Dispose();
            results.Dispose();
            treeBuffer.Dispose();
            Profiler.EndSample();

            if (NativeOctTree3D.IsCreated)
            {
                NativeOctTree3D.Dispose();
            }
            NativeOctTree3D = new NativeOctTree3D(transform.position + Vector3.one * _param.Scale * 0.5f, _param.Scale, nativeTreeList, Size);
        }

        private void CreateGraph()
        {
            if (NativeOctTree3D.IsCreated is false) return;
            
            Stopwatch w = new Stopwatch();
            w.Start();
            
            Profiler.BeginSample("Create Graph");
            var graph = new OctGraph();

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
                        int offset = graph.NativeGraph.Edge2PtrLength;
                        int treeIndex = NativeOctTree3D.GetFlatIndex(x, y, z);
                        int treeCount = Size.x * Size.y * Size.z;
                        
                        NativeFlattenOctTree tree = NativeOctTree3D.GetTree(treeIndex);
                        graph.Tree2Graph(tree, tree, treeCount, false, offset);

                        int count = calculator.GetNeighbors(x, y, z, neighborCoords);

                        for (int i = 0; i < count; i++)
                        {
                            int nx = neighborCoords[i, 0];
                            int ny = neighborCoords[i, 1];
                            int nz = neighborCoords[i, 2];

                            int neighborTreeIndex = NativeOctTree3D.GetFlatIndex(nx, ny, nz);
                            NativeFlattenOctTree neighborTree = NativeOctTree3D.GetTree(neighborTreeIndex);

                            graph.Tree2Graph(tree, neighborTree, treeCount, true, offset);
                        }

                        graph.NativeGraph.EdgeTreeOffset[tree.TreeIndex] = (b += tree.FlattenArr.Length);

                        size += graph.TotalSize;
                    }
                }
            }

            if (NativeOctGraph.IsCreated)
            {
                NativeOctGraph.Dispose();
            }
            NativeOctGraph = graph.NativeGraph;
            w.Stop();
            Profiler.EndSample();
            
            Debug.Log($"====");
            Debug.Log($"Graph 생성 소요 시간: {w.ElapsedMilliseconds}ms");
            Debug.Log($"Graph 메모리 사용량: {NativeUtility.FormatBytes(size)}");
        }

        private void OnDestroy()
        {
            if (NativeOctTree3D.IsCreated)
            {
                NativeOctTree3D.Dispose();
            }
            if (NativeOctGraph.IsCreated)
            {
                NativeOctGraph.Dispose();
            }
        }

        private void DrawNode()
        {
            Gizmos.DrawWireCube(transform.position + NativeOctTree3D.TreeScale * (Vector3)Size * 0.5f, NativeOctTree3D.TreeScale * (Vector3)Size);

            if (IsDrawCollisionNode is false && IsDrawEmptyNode is false) return;
            if (NativeOctTree3D.IsCreated is false) return;


            for (int x = 0; x < NativeOctTree3D.Size3D.x; x++)
            {
                for (int y = 0; y < NativeOctTree3D.Size3D.y; y++)
                {
                    for (int z = 0; z < NativeOctTree3D.Size3D.z; z++)
                    {
                        int treeIndex = NativeOctTree3D.GetFlatIndex(x, y, z);
                        NativeFlattenOctTree nativeTree = NativeOctTree3D.GetTree(treeIndex);
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
            if (IsDrawEdge is false) return;
            if (NativeOctGraph.IsCreated is false) return;
            if (NativeOctTree3D.IsCreated is false) return;

            for (int i = 0; i < NativeOctGraph.Edge2PtrLength; i++)
            {
                for (int j = 0; j < NativeOctGraph.EdgeLen[i]; j++)
                {
                    NativeEdge edge = NativeOctGraph.GetEdge(i, j);
                    
                    Gizmos.color = Color.yellow;

                    NativeOctNode fromNode = default;
                    NativeOctNode toNode = default;

                    unsafe
                    {
                        if (!(edge.PrevTreeIndex >= NativeOctTree3D.TreeCount || edge.PrevTreeIndex < 0))
                        {
                            fromNode = NativeOctTree3D.Trees[edge.PrevTreeIndex].GetNode(edge.PrevNodeFlattenIndex);
                        }
                        if (!(edge.NextTreeIndex >= NativeOctTree3D.TreeCount || edge.NextTreeIndex < 0))
                        {
                            toNode = NativeOctTree3D.Trees[edge.NextTreeIndex].GetNode(edge.NextNodeFlattenIndex);
                        }
                    }
                    
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