using System;
using System.IO;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;

namespace Candy.Pathfind3D
{
    public class MapBakeHandler
    {
        public static readonly bool IsLittleEndian = BitConverter.IsLittleEndian;

        public void BakeTree(NativeOctTree3D tree3d)
        {
            string path = "Assets/BakedTree.bytes";
            
            using FileStream stream = new(path, FileMode.Create, FileAccess.Write);
            using BinaryWriter writer = new(stream);
            
            WriteBinary(tree3d.TreeCount, writer);
            WriteBinary(tree3d.Size3D, writer);
            WriteBinary(tree3d.RootPosition, writer);
            WriteBinary(tree3d.TreeScale, writer);

            unsafe
            {
                for (int i = 0; i < tree3d.TreeCount; i++)
                {
                    NativeFlattenOctTree tree = tree3d.Trees[i];
                    NativeArray<NativeOctNode> flattenTree = tree.FlattenArr;
                    NativeArray<int> indexArr = tree.IndexArr;
                    NativeArray<int> treeArr = tree.TreeArr;

                    WriteBinary(tree.Depth, writer);
                    WriteBinary(tree.TreeIndex, writer);
                    
                    WriteBinary(flattenTree.Length, writer);
                    for (int j = 0; j < flattenTree.Length; j++)
                    {
                        WriteBinary(flattenTree[j], writer);
                    }
                    WriteBinary(indexArr.Length, writer);
                    for (int j = 0; j < indexArr.Length; j++)
                    {
                        WriteBinary(indexArr[j], writer);
                    }
                    WriteBinary(treeArr.Length, writer);
                    for (int j = 0; j < treeArr.Length; j++)
                    {
                        WriteBinary(treeArr[j], writer);
                    }
                }
            }
        }
        public void LoadTree(out NativeOctTree3D tree3d)
        {
            string path = "Assets/BakedTree.bytes";

            using FileStream stream = new(path, FileMode.Open, FileAccess.Read);
            using BinaryReader reader = new(stream);

            unsafe
            {

                // 기본 정보 복원
                int treeCount = ReadBinary<int>(reader);
                int3 treeSize = ReadBinary<int3>(reader);
                Vector3 rootPosition = ReadBinary<float3>(reader);
                float treeScale = ReadBinary<float>(reader);
                
                NativeFlattenOctTree* trees = (NativeFlattenOctTree*)UnsafeUtility.Malloc(
                    sizeof(NativeFlattenOctTree) * treeCount, UnsafeUtility.AlignOf<NativeFlattenOctTree>(),
                    Allocator.Persistent
                );

                for (int i = 0; i < treeCount; i++)
                {
                    NativeFlattenOctTree tree = new NativeFlattenOctTree();

                    tree.Depth = ReadBinary<int>(reader);
                    tree.TreeIndex = ReadBinary<int>(reader);

                    // FlattenArr
                    int flattenLen = ReadBinary<int>(reader);
                    tree.FlattenArr = new NativeArray<NativeOctNode>(flattenLen, Allocator.Persistent);
                    for (int j = 0; j < flattenLen; j++)
                    {
                        tree.FlattenArr[j] = ReadBinary<NativeOctNode>(reader);
                    }

                    // IndexArr
                    int indexLen = ReadBinary<int>(reader);
                    tree.IndexArr = new NativeArray<int>(indexLen, Allocator.Persistent);
                    for (int j = 0; j < indexLen; j++)
                    {
                        tree.IndexArr[j] = ReadBinary<int>(reader);
                    }

                    // TreeArr
                    int treeLen = ReadBinary<int>(reader);
                    tree.TreeArr = new NativeArray<int>(treeLen, Allocator.Persistent);
                    for (int j = 0; j < treeLen; j++)
                    {
                        tree.TreeArr[j] = ReadBinary<int>(reader);
                    }

                    tree.FlattenPtr = (NativeOctNode*)tree.FlattenArr.GetUnsafePtr();
                    tree.IndexPtr = (int*)tree.IndexArr.GetUnsafePtr();
                    tree.TreePtr = (int*)tree.FlattenArr.GetUnsafePtr();
                    trees[i] = tree;
                }

                tree3d = new NativeOctTree3D(rootPosition, treeScale, trees, treeSize, treeCount);
            }
        }
        public void BakeGraph(NativeOctGraph graph)
        {
            string path = "Assets/BakedGraph.bytes";

            // 파일 스트림 생성
            using (FileStream stream = new FileStream(path, FileMode.Create, FileAccess.Write))
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                unsafe
                {
                    Debug.Assert(graph.Edge2PtrLength == graph.EdgeLen.Length);
                    
                    WriteBinary(graph.Edge2PtrLength, writer);

                    for (int i = 0; i < graph.Edge2PtrLength; i++)
                    {
                        WriteBinary(graph.EdgeLen[i], writer);
                    }
                    
                    for (int i = 0; i < graph.Edge2PtrLength; i++)
                    {
                        for (int j = 0; j < graph.EdgeLen[i]; j++)
                        {
                            WriteBinary(graph.Edge2Ptr[i][j], writer);
                        }
                    }
                    
                    WriteBinary(graph.EdgeTreeOffset.Length, writer);
                    for (int i = 0; i < graph.EdgeTreeOffset.Length; i++)
                    {
                        WriteBinary(graph.EdgeTreeOffset[i], writer);
                    }
                }
            }
        }
        
        public void LoadGraph(out NativeOctGraph graph)
        {
            string path = "Assets/BakedGraph.bytes";

            using FileStream stream = new(path, FileMode.Open, FileAccess.Read);
            using BinaryReader reader = new(stream);

            unsafe
            {
                // 1. Edge2PtrLength
                graph.Edge2PtrLength = ReadBinary<int>(reader);

                // 2. EdgeLen
                graph.EdgeLen = new NativeList<int>(graph.Edge2PtrLength, Allocator.Persistent);
                for (int i = 0; i < graph.Edge2PtrLength; i++)
                {
                    graph.EdgeLen.Add(ReadBinary<int>(reader));
                }

                // 3. Edge2Ptr
                graph.Edge2Ptr = (NativeEdge**)UnsafeUtility.Malloc(sizeof(NativeEdge*) * graph.Edge2PtrLength, OctGraph.GetNativeEdgeAlign(), Allocator.Persistent);

                for (int i = 0; i < graph.Edge2PtrLength; i++)
                {
                    int len = graph.EdgeLen[i];
                    if (len < 0) len = 0;
                    NativeEdge* edgeArray = (NativeEdge*)UnsafeUtility.Malloc(sizeof(NativeEdge) * len, UnsafeUtility.AlignOf<NativeEdge>(), Allocator.Persistent);

                    for (int j = 0; j < len; j++)
                    {
                        edgeArray[j] = ReadBinary<NativeEdge>(reader);
                    }

                    graph.Edge2Ptr[i] = edgeArray;
                }

                // 4. EdgeTreeOffset
                int offsetLength = ReadBinary<int>(reader);
                graph.EdgeTreeOffset = new NativeArray<int>(offsetLength, Allocator.Persistent);
                for (int i = 0; i < offsetLength; i++)
                {
                    graph.EdgeTreeOffset[i] = ReadBinary<int>(reader);
                }
            }
        }
        
        private static T ReadBinary<T>(BinaryReader reader) where T : unmanaged
        {
            
            unsafe
            {
                int size = sizeof(T);
                byte* tempBuf = stackalloc byte[size];

                for (int i = 0; i < size; i++)
                {
                    if (IsLittleEndian)
                    {
                        tempBuf[size - i - 1] = reader.ReadByte(); 
                    }
                    else
                    {
                        tempBuf[i] = reader.ReadByte();
                    }
                }

                T data = *((T*)tempBuf);

                return data;
            }
        }


        private static void WriteBinary<T>(T data, BinaryWriter writer) where T : unmanaged
        {
            unsafe
            {
                int size = sizeof(T);
                byte* edgeBuf = (byte*)&data;

                for (int i = 0; i < size; i++)
                {
                    if (IsLittleEndian)
                    {
                        writer.Write(edgeBuf[size - i - 1]);
                    }
                    else
                    {
                        writer.Write(edgeBuf[i]);
                    }
                }
            }
        }
    }
}