using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Candy.Pathfind3D
{
    public struct NativeOctGraph : IDisposable
    {
        [NativeDisableUnsafePtrRestriction]
        public unsafe NativeEdge** Edge2Ptr;
        
        [ReadOnly]
        public NativeList<int> EdgeLen;
        public int Edge2PtrLength;

        public NativeArray<int> EdgeTreeOffset;
        
        public NativeEdge GetEdge(int nodeIndex, int edgeIndex)
        {
            if (nodeIndex < 0 || nodeIndex >= Edge2PtrLength)
                throw new IndexOutOfRangeException();

            if (edgeIndex < 0 || edgeIndex >= EdgeLen[nodeIndex])
                throw new IndexOutOfRangeException($"index:{edgeIndex}, max len:{EdgeLen[nodeIndex]}");

            unsafe
            {
                NativeEdge* edges = Edge2Ptr[nodeIndex];
                return edges[edgeIndex];
            }
        }

        public void Dispose()
        {
            EdgeLen.Dispose();

            unsafe
            {
                if (Edge2Ptr != null)
                {
                    for (int i = 0; i < Edge2PtrLength; i++)
                    {
                        if (Edge2Ptr[i] != null)
                        {
                            UnsafeUtility.Free(Edge2Ptr[i], Allocator.Persistent);
                        }
                    }

                    UnsafeUtility.Free(Edge2Ptr, Allocator.Persistent);
                }

                Edge2Ptr = null;
            }
        }
    }

    public class OctGraph : IDisposable
    {
        public NativeOctGraph NativeGraph;

        public NativeArray<int> EdgeLen => NativeGraph.EdgeLen.AsArray();
        public int EdgeArrLength => NativeGraph.Edge2PtrLength;
        public long TotalSize { get; private set; }

        public bool IsCreated
        {
            get
            {
                unsafe { return NativeGraph.Edge2Ptr != null; }
            }
        }

        public NativeEdge GetEdge(int nodeIndex, int edgeIndex)
        {
            if (nodeIndex < 0 || nodeIndex >= NativeGraph.Edge2PtrLength)
                throw new IndexOutOfRangeException();

            if (edgeIndex < 0 || edgeIndex >= NativeGraph.EdgeLen[nodeIndex])
                throw new IndexOutOfRangeException($"index:{edgeIndex}, max len:{NativeGraph.EdgeLen[nodeIndex]}");

            unsafe
            {
                NativeEdge* edges = NativeGraph.Edge2Ptr[nodeIndex];
                return edges[edgeIndex];
            }
        }

        public OctGraph()
        {
            NativeGraph.Edge2PtrLength = 0;
            unsafe { NativeGraph.Edge2Ptr = null; }
        }

        public static int CalculateMaxEdgeArrayLength(int depth)
        {
            int lineNodeCount = IntegerMath.CubeRootOf8(depth);
            int faceNodeCount = lineNodeCount * lineNodeCount;
            return (6 * faceNodeCount) + (8 * lineNodeCount) + 8;
        }

        public void Tree2Graph(NativeFlattenOctTree tree, NativeFlattenOctTree targetTree, int treeCount, bool isNeighbor, int offset)
        {
            int cpuCoreCount = 32;
            int maxEdgeArrayLength = IntegerMath.CubeRootOf8(tree.Depth);

            unsafe
            {
                if (isNeighbor is false)
                {
                    if (NativeGraph.Edge2Ptr == null)
                    {
                        Debug.Assert(!NativeGraph.EdgeLen.IsCreated);
                        NativeGraph.EdgeLen = new NativeList<int>(tree.FlattenArr.Length, Allocator.Persistent);
                        NativeGraph.EdgeLen.ResizeUninitialized(tree.FlattenArr.Length);
                        NativeGraph.Edge2PtrLength = tree.FlattenArr.Length;
                        NativeGraph.EdgeTreeOffset = new NativeArray<int>(treeCount, Allocator.Persistent);

                        int size = NativeGraph.Edge2PtrLength * sizeof(NativeEdge*);
                        NativeGraph.Edge2Ptr = (NativeEdge**)UnsafeUtility.Malloc(size, sizeof(AlignOfHelper) - sizeof(NativeEdge*), Allocator.Persistent);
                        UnsafeUtility.MemClear(NativeGraph.Edge2Ptr, size);
                    }
                    else
                    {
                        NativeGraph.Edge2Ptr = ReAlloc2D(NativeGraph.Edge2Ptr, NativeGraph.Edge2PtrLength, NativeGraph.Edge2PtrLength + tree.FlattenArr.Length, Allocator.Persistent, Allocator.Persistent);
                        UnsafeUtility.MemClear(NativeGraph.Edge2Ptr + NativeGraph.Edge2PtrLength, sizeof(NativeEdge*) * tree.FlattenArr.Length);
                        NativeGraph.Edge2PtrLength += tree.FlattenArr.Length;
                        Debug.Assert(NativeGraph.EdgeLen.IsCreated);
                        NativeGraph.EdgeLen.ResizeUninitialized(NativeGraph.Edge2PtrLength);
                    }
                }

                new Tree2GraphJob
                {
                    MyNodes = tree.FlattenArr,
                    MyTreeArr = tree.TreeArr,
                    MyIndexArr = tree.IndexArr,
                    TargetArr = targetTree.FlattenArr,
                    TargetTreeArr = targetTree.TreeArr,
                    TargetIndexArr = targetTree.IndexArr,
                    TargetTreeIndex = targetTree.TreeIndex,
                    MyTreeIndex = tree.TreeIndex,
                    IsNeighbor = isNeighbor,
                    EdgeLen = new NativeSlice<int>(NativeGraph.EdgeLen.AsArray(), offset),
                    UnsafeEdge2dArr = NativeGraph.Edge2Ptr + offset,
                    AllocationStep = maxEdgeArrayLength
                }.ScheduleBatch(tree.FlattenArr.Length, Mathf.CeilToInt(tree.FlattenArr.Length / (float)cpuCoreCount)).Complete();
                
                try
                {
                    checked
                    {
                        long size = sizeof(NativeEdge*) + sizeof(NativeEdge) * EdgeArrLength;
                        size += sizeof(int) + NativeGraph.EdgeLen.Length;
                        size += sizeof(int);
                        TotalSize = size;
                    }
                }
                catch (OverflowException e)
                {
                    TotalSize = 0;
                    Debug.LogException(e);
                }
            }
        }

        public static unsafe NativeEdge** ReAlloc2D(NativeEdge** src, int srcLen, int targetLen, Allocator srcAllocator, Allocator targetAllocator)
        {
            int ptrSize = sizeof(NativeEdge*);
            int ptrAlign = sizeof(AlignOfHelper) - sizeof(NativeEdge*);
            NativeEdge** tempArr = (NativeEdge**)UnsafeUtility.Malloc((long)targetLen * ptrSize, ptrAlign, targetAllocator);

            if (srcLen > 0 && src != null)
                UnsafeUtility.MemCpy(tempArr, src, srcLen * ptrSize);

            if (src != null)
                UnsafeUtility.Free(src, srcAllocator);

            return tempArr;
        }

        private unsafe struct AlignOfHelper
        {
            public byte dummy;
            public NativeEdge* data;
        }

        public void Dispose()
        {
            NativeGraph.Dispose();
        }
    }
}
