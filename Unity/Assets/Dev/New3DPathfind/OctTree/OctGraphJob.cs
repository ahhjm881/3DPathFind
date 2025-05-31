using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Mathematics.Geometry;
using UnityEngine;
using UnityEngine.Serialization;

namespace Candy.Pathfind3D
{
    [BurstCompile(DisableSafetyChecks = true)]
    public unsafe struct Tree2GraphJob :  IJobParallelForBatch
    {
        [ReadOnly]
        public NativeArray<NativeOctNode> MyNodes;
        
        [ReadOnly, NativeDisableParallelForRestriction]
        public NativeArray<int> MyIndexArr;
        [ReadOnly, NativeDisableParallelForRestriction]
        public NativeArray<int> MyTreeArr;
        
        [ReadOnly]
        public NativeArray<NativeOctNode> TargetArr;
        [ReadOnly, NativeDisableParallelForRestriction]
        public NativeArray<int> TargetIndexArr;
        [ReadOnly, NativeDisableParallelForRestriction]
        public NativeArray<int> TargetTreeArr;

        public int TargetTreeIndex;
        public int MyTreeIndex;
        
        [NativeDisableUnsafePtrRestriction]
        public NativeEdge** UnsafeEdge2dArr;

        [WriteOnly]
        public NativeSlice<int> EdgeLen;

        public int AllocationStep;
        
        public void Execute(int index, ref int* treeSearch2Arr, ref int* swapSearch2Arr)
        {
            NativeOctNode myNode = MyNodes[index];
            
            NativeFlattenOctTree.IndexRange myIndexRage = NativeFlattenOctTree.GetChildIndexRange(myNode.FlattenIndex, MyIndexArr);
            if (myNode.IsObstacle || NativeFlattenOctTree.HasChild(myIndexRage, MyTreeArr))
            {
                EdgeLen[index] = -1;
                return;
            }
            
            MinMaxAABB myAABB =
                MinMaxAABB.CreateFromCenterAndExtents(myNode.WorldPosition, myNode.Scale * 1.5f * new float3(1f, 1f, 1f));

            NativeEdge* edgeArr = (NativeEdge*)UnsafeUtility.Malloc(
                AllocationStep * UnsafeUtility.SizeOf<NativeEdge>(), 
                UnsafeUtility.AlignOf<NativeEdge>(),
                Allocator.TempJob);
            
            
            int* swapSearchArr = swapSearch2Arr;
            int swapSearchCapacity = AllocationStep;

            // root 노드는 항상 0
            int* treeSearchArr = treeSearch2Arr;
            treeSearchArr[0] = 0;
            swapSearchArr[0] = 0;
            int currentTreeSearchIndex = 1;
            int treeSearchCapacity = AllocationStep;
            
            int currentEdgeIndex = 0;
            int edgeArrCapacity = AllocationStep;


            int counter = 0;
            while (true)
            {
                if (counter > 10000000)
                {
                    Debug.Assert(false);
                    break;
                }

                counter++;
                
                int len = currentTreeSearchIndex;
                currentTreeSearchIndex = 0;
                if (len < 1)
                {
                    break;
                }
                
                for (int i = 0; i < len; i++)
                {
                    Debug.Assert(i < swapSearchCapacity, $"{i}, {swapSearchCapacity}, {len}");
                    int targetIndex = swapSearchArr[i];
                    NativeOctNode targetNode = TargetArr[targetIndex];

                    
                    MinMaxAABB targetAABB =
                        MinMaxAABB.CreateFromCenterAndExtents(targetNode.WorldPosition, targetNode.Scale * new float3(1f, 1f, 1f));
                    
                    if (myAABB.Overlaps(targetAABB))
                    {
                        NativeFlattenOctTree.IndexRange targetIndexRage = NativeFlattenOctTree.GetChildIndexRange(targetNode.FlattenIndex, TargetIndexArr);
                        if (NativeFlattenOctTree.HasChild(targetIndexRage, TargetTreeArr))
                        {
                            for (int j = targetIndexRage.Begin; j < targetIndexRage.End; j++)
                            {
                                int mapIndex = NativeFlattenOctTree.MapIndex(j, TargetTreeArr);
                                if(mapIndex == -1)continue;
                                treeSearchArr = AddList(treeSearchArr, mapIndex, currentTreeSearchIndex++, &treeSearchCapacity);
                            }
                            continue;
                        }
                        
                        
                        if (targetNode.IsObstacle) continue;
                        float distance = math.distancesq(myAABB.Center, targetAABB.Center);
                        NativeEdge edge = new NativeEdge()
                        {
                            Weight = distance,
                            PrevTreeIndex = MyTreeIndex,
                            NextTreeIndex = TargetTreeIndex,
                            PrevNodeFlattenIndex = myNode.FlattenIndex,
                            NextNodeFlattenIndex = targetNode.FlattenIndex,
                        };
                        
                        edgeArr = AddList(edgeArr, edge, currentEdgeIndex++, &edgeArrCapacity);
                    }
                }
                
                var tempArr = swapSearchArr;
                swapSearchArr = treeSearchArr;
                treeSearchArr = tempArr;

                var tempCapacity = treeSearchCapacity;
                treeSearchCapacity = swapSearchCapacity;
                swapSearchCapacity = tempCapacity;
            }
            

            UnsafeEdge2dArr[index] = ReAlloc(currentEdgeIndex, currentEdgeIndex, edgeArr, true);
            EdgeLen[index] = currentEdgeIndex;
            treeSearch2Arr = treeSearchArr;
            swapSearch2Arr = swapSearchArr;
        }

        private T* AddList<T>(T* arr, T value, int curIndex, int* capacity)
            where T : unmanaged
        {
            if (curIndex >= (*capacity))
            {
                arr = ReAlloc<T>(curIndex + 1, *capacity * 2, arr, false);
                (*capacity) *= 2;
            }

            arr[curIndex] = value;
            return arr;
        }

        private T* ReAlloc<T>(int currentLen, int newLen, T* arr, bool isOutputBufferPersistent)
            where T : unmanaged
        {
            int edgeSize = UnsafeUtility.SizeOf<T>();
            int alignment = UnsafeUtility.AlignOf<T>();
            T* tempArr = (T*)UnsafeUtility.Malloc((long)newLen * edgeSize, alignment,
                isOutputBufferPersistent ? Allocator.Persistent : Allocator.TempJob);
            
            if(currentLen > 0)
                UnsafeUtility.MemCpy(tempArr, arr, currentLen * edgeSize);
            
            UnsafeUtility.Free(arr, Allocator.TempJob);
            
            return tempArr;
        }

        public void Execute(int startIndex, int count)
        {
            int size = UnsafeUtility.SizeOf<int>();
            int alignment = UnsafeUtility.AlignOf<int>();
            int* treeSearchArr =
                (int*)UnsafeUtility.Malloc(
                    (long)AllocationStep * size,
                    alignment,
                    Allocator.TempJob
                );
            int* swapArr =
                (int*)UnsafeUtility.Malloc(
                    (long)AllocationStep * size,
                    alignment,
                    Allocator.TempJob
                );
                
            for (int i = startIndex; i < startIndex + count; i++)
            {
                Execute(i, ref treeSearchArr, ref swapArr);
            }
            
            UnsafeUtility.Free(treeSearchArr, Allocator.TempJob);
            UnsafeUtility.Free(swapArr, Allocator.TempJob);
        }
    }
}