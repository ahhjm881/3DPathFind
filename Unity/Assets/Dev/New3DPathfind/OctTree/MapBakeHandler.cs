using System;
using System.IO;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace Candy.Pathfind3D
{
    public class MapBakeHandler
    {
        public static readonly bool IsLittleEndian = BitConverter.IsLittleEndian;

        public void BakeTree(NativeOctTree3D tree3d)
        {
            
        }
        
        public void BakeGraph(NativeOctGraph graph)
        {
            string path = "Assets/BakedGraph.bytes";

            // 파일 스트림 생성
            using (FileStream stream = new FileStream(path, FileMode.Create, FileAccess.Write))
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                // ↓ 여기에 graph 데이터를 writer로 write하는 코드 작성할 것
                // 예: writer.Write(graph.NodeCount);
                // 예: for (...) writer.Write(graph.Nodes[i].Position.x);

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
        
        public void LoadGraph(out NativeOctGraph graph, Allocator allocator)
        {
            string path = "Assets/BakedGraph.bytes";

            using FileStream stream = new(path, FileMode.Open, FileAccess.Read);
            using BinaryReader reader = new(stream);

            unsafe
            {
                // 1. Edge2PtrLength
                graph.Edge2PtrLength = ReadBinary<int>(reader);

                // 2. EdgeLen
                graph.EdgeLen = new NativeList<int>(graph.Edge2PtrLength, allocator);
                for (int i = 0; i < graph.Edge2PtrLength; i++)
                {
                    graph.EdgeLen.Add(ReadBinary<int>(reader));
                }

                // 3. Edge2Ptr
                graph.Edge2Ptr = (NativeEdge**)UnsafeUtility.Malloc(sizeof(NativeEdge*) * graph.Edge2PtrLength, OctGraph.GetNativeEdgeAlign(), allocator);

                for (int i = 0; i < graph.Edge2PtrLength; i++)
                {
                    int len = graph.EdgeLen[i];
                    if (len < 0) len = 0;
                    NativeEdge* edgeArray = (NativeEdge*)UnsafeUtility.Malloc(sizeof(NativeEdge) * len, UnsafeUtility.AlignOf<NativeEdge>(), allocator);

                    for (int j = 0; j < len; j++)
                    {
                        edgeArray[j] = ReadBinary<NativeEdge>(reader);
                    }

                    graph.Edge2Ptr[i] = edgeArray;
                }

                // 4. EdgeTreeOffset
                int offsetLength = ReadBinary<int>(reader);
                graph.EdgeTreeOffset = new NativeArray<int>(offsetLength, allocator);
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