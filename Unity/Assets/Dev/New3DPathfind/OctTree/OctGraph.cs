using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Candy.Pathfind3D
{
    public class OctGraph : IDisposable
    {
        private unsafe NativeEdge** _edge2Ptr;
        private NativeList<int> _edgeLen;
        private int _edge2PtrLength;

        public NativeArray<int> EdgeLen => _edgeLen.AsArray();

        public int EdgeArrLength => _edge2PtrLength;

        public bool IsCreated
        {
            get
            {
                bool r = false;
                unsafe
                {
                    r = _edge2Ptr != null;
                }

                return r;
            }
        }

        public NativeEdge GetEdge(int nodeIndex, int edgeIndex)
        {
            if (nodeIndex < 0 || nodeIndex >= _edge2PtrLength)
            {
                throw new IndexOutOfRangeException();
            }

            if (edgeIndex < 0 || edgeIndex >= _edgeLen[nodeIndex])
            {
                throw new IndexOutOfRangeException($"index:{edgeIndex}, max len:{_edgeLen[nodeIndex]}");
            }

            NativeEdge edge;
            unsafe
            {
                NativeEdge* edges = _edge2Ptr[nodeIndex];
                edge = edges[edgeIndex];
            }

            return edge;
        }

        public OctGraph()
        {
            _edge2PtrLength = 0;

            unsafe
            {
                _edge2Ptr = null;
            }
        }


        public static int CalculateMaxEdgeArrayLength(int depth)
        {
            int lineNodeCount = IntegerMath.CubeRootOf8(depth);
            int faceNodeCount = lineNodeCount * lineNodeCount;

            // 정육면체 기준
            // 면 6개, 기둥(면의 선) 8개, 모서리 8개
            int totalNodeCount = (6 * faceNodeCount) + (8 * lineNodeCount) + 8;

            return totalNodeCount;
        }

        public void Tree2Graph(NativeFlattenOctTree tree, NativeFlattenOctTree targetTree)
        {
            int cpuCoreCount = 32;

            int maxEdgeArrayLength = IntegerMath.CubeRootOf8(tree.Depth);


            unsafe
            {
                int offset = _edge2PtrLength;
                
                if (_edge2Ptr == null)
                {
                    Debug.Assert(_edgeLen.IsCreated is false);
                    _edgeLen = new NativeList<int>(tree.FlattenArr.Length, Allocator.Persistent);
                    _edgeLen.ResizeUninitialized(tree.FlattenArr.Length);
                    _edge2PtrLength = tree.FlattenArr.Length;
                    offset = 0;
                    
                    int size = _edge2PtrLength * sizeof(NativeEdge*);
                    _edge2Ptr = (NativeEdge**)UnsafeUtility.Malloc(
                        size,
                        sizeof(AlignOfHelper) - sizeof(NativeEdge*),
                        Allocator.Persistent);
                    
                    
                    UnsafeUtility.MemClear(_edge2Ptr, size);
                }
                else
                {
                    _edge2Ptr = ReAlloc2D(_edge2Ptr, _edge2PtrLength, _edge2PtrLength + tree.FlattenArr.Length,
                        Allocator.Persistent, Allocator.Persistent);
                    
                    UnsafeUtility.MemClear(_edge2Ptr + _edge2PtrLength, sizeof(NativeEdge*) * tree.FlattenArr.Length);
                        
                    _edge2PtrLength += tree.FlattenArr.Length;
                    Debug.Assert(_edgeLen.IsCreated);
                    _edgeLen.ResizeUninitialized(_edge2PtrLength);
                }
                
                
                new Tree2GraphJob()
                {
                    MyNodes = tree.FlattenArr,
                    MyTreeArr = tree.TreeArr,
                    MyIndexArr = tree.IndexArr,
                    TargetArr = targetTree.FlattenArr,
                    TargetTreeArr = targetTree.TreeArr,
                    TargetIndexArr = targetTree.IndexArr,
                    TargetTreeIndex = targetTree.TreeIndex,
                    EdgeLen = new NativeSlice<int>(_edgeLen.AsArray(), offset),
                    UnsafeEdge2dArr = _edge2Ptr + offset,
                    AllocationStep = maxEdgeArrayLength, 
                }.ScheduleBatch(tree.FlattenArr.Length, Mathf.CeilToInt(tree.FlattenArr.Length / (float)cpuCoreCount)).Complete();
            }
        }


        
        // 얕은 복사
        public static unsafe NativeEdge** ReAlloc2D(NativeEdge** src, int srcLen, int targetLen, Allocator srcAllocator, Allocator targetAllocator)
        {
            int ptrSize = sizeof(NativeEdge*);
            int ptrAlign = sizeof(AlignOfHelper) - sizeof(NativeEdge*);

            NativeEdge** tempArr = (NativeEdge**)UnsafeUtility.Malloc((long)targetLen * ptrSize, ptrAlign, targetAllocator);

            if (srcLen > 0 && src != null)
            {
                UnsafeUtility.MemCpy(tempArr, src, srcLen * ptrSize);
            }

            if (src != null)
                UnsafeUtility.Free(src, srcAllocator);

            return tempArr;
        }
        private unsafe struct AlignOfHelper
        {
            public byte dummy;
            public unsafe NativeEdge* data;
        }
        
        public void Dispose()
        {
            _edgeLen.Dispose();

            unsafe
            {
                if (_edge2Ptr != null)
                {
                    for (int i = 0; i < _edge2PtrLength; i++)
                    {
                        if (_edge2Ptr[i] != null)
                        {
                            UnsafeUtility.Free(_edge2Ptr[i], Allocator.Persistent);
                        }
                    }

                    UnsafeUtility.Free(_edge2Ptr, Allocator.Persistent);
                }

                _edge2Ptr = null;
            }
        }
    }
}