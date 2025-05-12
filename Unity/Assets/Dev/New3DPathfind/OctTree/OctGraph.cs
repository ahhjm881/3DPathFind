using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using UnityEngine;

namespace Candy.Pathfind3D
{
    public class OctGraph : IDisposable
    {
        private unsafe NativeEdge** _edge2Ptr;
        private NativeArray<int> _edgeLen;
        private int _edge2PtrLength;

        public NativeArray<int> EdgeLen => _edgeLen;

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
                edge = _edge2Ptr[nodeIndex][edgeIndex];
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

        public void Tree2Graph(NativeFlattenOctTree tree)
        {
            int cpuCoreCount = 32;

            int maxEdgeArrayLength = IntegerMath.CubeRootOf8(tree.Depth);
            NativeArray<int> edgeLenBuf = new NativeArray<int>(tree.FlattenArr.Length, Allocator.Persistent);


            unsafe
            {
                int size = tree.FlattenArr.Length * sizeof(NativeEdge*);
                _edge2PtrLength = tree.FlattenArr.Length;
                NativeEdge** edge2Ptr = (NativeEdge**)UnsafeUtility.Malloc(
                    size,
                    UnsafeUtility.AlignOf<NativeEdge>(),
                    Allocator.Persistent);
                _edge2Ptr = edge2Ptr;
                _edgeLen = edgeLenBuf;
                
                UnsafeUtility.MemClear(edge2Ptr, size);
                new Tree2GraphJob()
                {
                    MyNodes = tree.FlattenArr,
                    TargetArr = tree.FlattenArr,
                    IndexArr = tree.IndexArr,
                    TreeArr = tree.TreeArr,
                    TargetTreeIndex = tree.TreeIndex,
                    EdgeLen = edgeLenBuf,
                    UnsafeEdge2dArr = edge2Ptr,
                    AllocationStep = maxEdgeArrayLength, 
                }.ScheduleBatch(tree.FlattenArr.Length, Mathf.CeilToInt(tree.FlattenArr.Length / (float)cpuCoreCount)).Complete();
            }
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