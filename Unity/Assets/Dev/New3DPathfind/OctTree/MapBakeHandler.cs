using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using NUnit.Framework;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Candy.Pathfind3D.Editor
{
#if UNITY_EDITOR

    using UnityEditor;

    public class MapBakeHandler
    {
        public enum ResultType
        {
            Fail,
            Success,
            Canceled
        }

        public struct Result
        {
            public ResultType Type;
            public Exception Exception;
        }

        public static readonly bool IsLittleEndian = BitConverter.IsLittleEndian;
        public const string FILE_EXTENSTION = "bin";

        public readonly string Path;

        public MapBakeHandler(string filePath, string fileName)
        {
            char separator = System.IO.Path.DirectorySeparatorChar;
            Path = $"{filePath}{separator}{fileName}.{FILE_EXTENSTION}";
        }

        public MapBakeHandler(string fullPath)
        {
            Path = $"{fullPath}.{FILE_EXTENSTION}";
        }


        public void BakeTree(NativeOctTree3D tree3d, bool displayProgress)
        {
            using FileStream stream = new(Path, FileMode.Create, FileAccess.Write);
            using BinaryWriter writer = new(stream);

            if (displayProgress)
            {
                EditorUtility.DisplayProgressBar("Baking...", "Start", 0f);
            }

            WriteBinary(tree3d.TreeCount, writer);
            WriteBinary(tree3d.Size3D, writer);
            WriteBinary(tree3d.RootPosition, writer);
            WriteBinary(tree3d.TreeScale, writer);

            unsafe
            {
                for (int i = 0; i < tree3d.TreeCount; i++)
                {
                    if (displayProgress)
                    {
                        EditorUtility.DisplayProgressBar("Baking...", $"Progress: {i}%",
                            i / (float)tree3d.TreeCount);
                    }

                    NativeFlattenOctTree tree = tree3d.Trees[i];
                    NativeArray<NativeOctNode> flattenTree = tree.FlattenArr;
                    NativeArray<int> indexArr = tree.IndexArr;
                    NativeArray<int> treeArr = tree.TreeArr;

                    WriteBinary(tree.Depth, writer);
                    WriteBinary(tree.TreeIndex, writer);

                    WriteBinary(flattenTree.Length, writer);
                    WriteBinary(indexArr.Length, writer);
                    WriteBinary(treeArr.Length, writer);
                    
                    for (int j = 0; j < flattenTree.Length; j++)
                    {
                        WriteBinary(flattenTree[j], writer);
                    }

                    for (int j = 0; j < indexArr.Length; j++)
                    {
                        WriteBinary(indexArr[j], writer);
                    }

                    for (int j = 0; j < treeArr.Length; j++)
                    {
                        WriteBinary(treeArr[j], writer);
                    }
                }
            }

            if (displayProgress)
            {
                EditorUtility.ClearProgressBar();
                EditorUtility.DisplayDialog("Baking Done", "Baking Complete", "OK");
            }
        }

        public void LoadTree(out NativeOctTree3D tree3d, int? id)
        {
            Stopwatch s = new();
            s.Start();
            using FileStream stream = new FileStream(Path, FileMode.Open, FileAccess.Read, FileShare.Read);
            using BinaryReader reader = new BinaryReader(stream);

            int cpuCoreCount = 4;
            tree3d = default;
            long seek = 0;

            unsafe
            {
                // 기본 정보 복원
                int treeCount = ReadBinary<int>(reader);
                seek += sizeof(int);
                int3 treeSize = ReadBinary<int3>(reader);
                seek += sizeof(int3);
                Vector3 rootPosition = ReadBinary<float3>(reader);
                seek += sizeof(float3);
                float treeScale = ReadBinary<float>(reader);
                seek += sizeof(float);
                
                NativeFlattenOctTree* trees = (NativeFlattenOctTree*)UnsafeUtility.Malloc(
                    sizeof(NativeFlattenOctTree) * treeCount, UnsafeUtility.AlignOf<NativeFlattenOctTree>(),
                    Allocator.Persistent
                );

                for (int i = 0; i < treeCount; i++)
                {
                    if (id.HasValue)
                    {
                        Progress.Report(id.Value, i / (float)treeCount, $"Processing Tree {i}");
                    }

                    NativeFlattenOctTree tree = new NativeFlattenOctTree();

                    tree.Depth = ReadBinary<int>(reader);
                    seek += sizeof(int);
                    tree.TreeIndex = ReadBinary<int>(reader);
                    seek += sizeof(int);
                    
                    int flattenLen = ReadBinary<int>(reader);
                    seek += sizeof(int);
                    int indexLen = ReadBinary<int>(reader);
                    seek += sizeof(int);
                    int treeLen = ReadBinary<int>(reader);
                    seek += sizeof(int);

                    
                    // FlattenArr
                    if (id.HasValue)
                    {
                        Progress.Report(id.Value, i / (float)treeCount, $"Flatten Array {i}");
                    }
                    tree.FlattenArr = new NativeArray<NativeOctNode>(flattenLen, Allocator.Persistent);
                    BatchReadExecute<NativeOctNode>(cpuCoreCount, flattenLen, ref seek, Path, (data, index, loopBatch) =>
                    {
                        tree.FlattenArr[index] = data;
                    });

                    // IndexArr
                    if (id.HasValue)
                    {
                        Progress.Report(id.Value, i / (float)treeCount, $"Index Array {i}");
                    }
                    tree.IndexArr = new NativeArray<int>(indexLen, Allocator.Persistent);
                    BatchReadExecute<int>(cpuCoreCount, indexLen, ref seek, Path, (data, index, loopBatch) =>
                    {
                        tree.IndexArr[index] = data;
                    });
                    
                    // TreeArr
                    if (id.HasValue)
                    {
                        Progress.Report(id.Value, i / (float)treeCount, $"Tree Array {i}");
                    }
                    tree.TreeArr = new NativeArray<int>(treeLen, Allocator.Persistent);
                    BatchReadExecute<int>(cpuCoreCount, treeLen, ref seek, Path, (data, index, loopBatch) =>
                    {
                        tree.TreeArr[index] = data;
                    });

                    tree.FlattenPtr = (NativeOctNode*)tree.FlattenArr.GetUnsafePtr();
                    tree.IndexPtr = (int*)tree.IndexArr.GetUnsafePtr();
                    tree.TreePtr = (int*)tree.FlattenArr.GetUnsafePtr();
                    trees[i] = tree;
                    
                    stream.Seek(seek, SeekOrigin.Begin);
                }

                tree3d = new NativeOctTree3D(rootPosition, treeScale, trees, treeSize, treeCount);
                
                s.Stop();
                Debug.Log(s.ElapsedMilliseconds);

                if (id.HasValue)
                {
                    Progress.Finish(id.Value, Progress.Status.Succeeded);
                }
            }
        }

        public void BakeGraph(NativeOctGraph graph, bool displayProgress)
        {
            if (displayProgress)
            {
                EditorUtility.DisplayProgressBar("Baking Graph...", "Start", 0f);
            }

            // 파일 스트림 생성
            using (FileStream stream = new FileStream(Path, FileMode.Create, FileAccess.Write))
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                unsafe
                {
                    Debug.Assert(graph.Edge2PtrLength == graph.EdgeLen.Length);

                    WriteBinary(graph.Edge2PtrLength, writer);
                    
                    if (displayProgress)
                    {
                        EditorUtility.DisplayProgressBar("Baking Graph...", "Offsets", 0f);
                    }

                    WriteBinary(graph.EdgeTreeOffset.Length, writer);
                    for (int i = 0; i < graph.EdgeTreeOffset.Length; i++)
                    {
                        WriteBinary(graph.EdgeTreeOffset[i], writer);
                    }

                    if (displayProgress)
                    {
                        EditorUtility.DisplayProgressBar("Baking Graph...", "Edge Length", 0f);
                    }

                    for (int i = 0; i < graph.Edge2PtrLength; i++)
                    {
                        WriteBinary(graph.EdgeLen[i], writer);
                    }
                    
                    for (int i = 0; i < graph.Edge2PtrLength; i++)
                    {
                        if (displayProgress)
                        {
                            EditorUtility.DisplayProgressBar("Baking Graph...", "Edges",
                                i / (float)graph.Edge2PtrLength);
                        }

                        for (int j = 0; j < graph.EdgeLen[i]; j++)
                        {
                            WriteBinary(graph.Edge2Ptr[i][j], writer);
                        }
                    }
                }
            }

            if (displayProgress)
            {
                EditorUtility.ClearProgressBar();
                EditorUtility.DisplayDialog("Baking Done", "Baking Complete", "OK");
            }
        }

        public void LoadGraph(out NativeOctGraph graph, int? id)
        {
            Stopwatch s = new();
            s.Start();
            FileStream stream = new FileStream(Path, FileMode.Open, FileAccess.Read, FileShare.Read);
            BinaryReader reader = new BinaryReader(stream);
            
            graph = default;

            long fileIndex = 0;
            int cpuCoreCount = 16;

            unsafe
            {
                // 1. Edge2PtrLength
                graph.Edge2PtrLength = ReadBinary<int>(reader);
                fileIndex += sizeof(int);
                

                // 4. EdgeTreeOffset
                int offsetLength = ReadBinary<int>(reader);
                fileIndex += sizeof(int);
                graph.EdgeTreeOffset = new NativeArray<int>(offsetLength, Allocator.Persistent);
                for (int i = 0; i < offsetLength; i++)
                {
                    if (id.HasValue)
                    {
                        Progress.Report(id.Value, i / (float)offsetLength, $"Processing offset length");
                    }

                    graph.EdgeTreeOffset[i] = ReadBinary<int>(reader);
                    fileIndex += sizeof(int);
                }

                // 2. EdgeLen
                graph.EdgeLen = new NativeList<int>(graph.Edge2PtrLength, Allocator.Persistent);
                for (int i = 0; i < graph.Edge2PtrLength; i++)
                {
                    int len = ReadBinary<int>(reader);
                    graph.EdgeLen.Add(len);
                    fileIndex += sizeof(int);
                }
                
                stream.DisposeAsync();

                // 3. Edge2Ptr
                graph.Edge2Ptr = (NativeEdge**)UnsafeUtility.Malloc(sizeof(NativeEdge*) * graph.Edge2PtrLength,
                    OctGraph.GetNativeEdgeAlign(), Allocator.Persistent);

                var tempGraph = graph;
                List<Task> tasks = Batch(cpuCoreCount, graph.Edge2PtrLength, (start, end, loopBatch) =>
                {
                    int ids = Progress.Start($"core {loopBatch}");
                    
                    long coverageStart = 0;
                    for (int i = 0; i < start; i++)
                    {
                        if(tempGraph.EdgeLen[i] >= 0)
                            coverageStart += tempGraph.EdgeLen[i];
                    }
                    FileStream st = new FileStream(Path, FileMode.Open, FileAccess.Read, FileShare.Read);
                    BinaryReader r = new BinaryReader(st);
                   st.Seek(fileIndex + coverageStart * sizeof(NativeEdge), SeekOrigin.Begin);
                    
                    for (int i = start; i < end; i++)
                    {
                        int len = tempGraph.EdgeLen[i];
                        if (len < 0) len = 0;
                        NativeEdge* edgeArray = (NativeEdge*)UnsafeUtility.Malloc(sizeof(NativeEdge) * len,
                            UnsafeUtility.AlignOf<NativeEdge>(), Allocator.Persistent);
                        
                        for (int j = 0; j < len; j++)
                        {
                            edgeArray[j] = ReadBinary<NativeEdge>(r);
                        }

                        if (id.HasValue && (i % 1000 == 0 || i - 1 == end))
                        {
                            Progress.Report(ids, i - start, end - start);
                        }

                        tempGraph.Edge2Ptr[i] = edgeArray;
                    }
                    
                    Progress.Finish(ids, Progress.Status.Succeeded);
                    st.DisposeAsync();
                });
                
                Task.WhenAll(tasks).Wait();
            }
            
            s.Stop();
            Debug.Log(s.ElapsedMilliseconds);

            if (id.HasValue)
            {
                Progress.Finish(id.Value, Progress.Status.Succeeded);
            }
        }

        private static List<Task> Batch(int cpuCoreCount, int totalLoopCount, Action<int, int, int> callback)
        {
            int[] eachCount = new int[cpuCoreCount];
            int sum = 0;
            for (int i = 0; i < cpuCoreCount; i++)
            {
                sum += totalLoopCount / cpuCoreCount;
                eachCount[i] += sum; 
            }
            eachCount[cpuCoreCount - 1] += totalLoopCount % cpuCoreCount;
            List<Task> tasks = new(cpuCoreCount);
            for (int loopBatch = 0; loopBatch < cpuCoreCount; loopBatch++)
            {
                int start = loopBatch == 0 ? 0 : eachCount[loopBatch - 1];
                int end = eachCount[loopBatch];

                int iter = loopBatch;

                Task task = Task.Run(() =>
                {
                    try
                    {
                        callback?.Invoke(start, end, iter);
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                    }
                });
                tasks.Add(task);
            }

            return tasks;
        }
        
        private static unsafe void BatchReadExecute<T>(int cpuCoreCount, int totalLoopCount, ref long seek, string path, Action<T, int, int> callback)
            where T : unmanaged
        {
            int[] eachCount = new int[cpuCoreCount];
            int sum = 0;
            for (int i = 0; i < cpuCoreCount; i++)
            {
                sum += totalLoopCount / cpuCoreCount;
                eachCount[i] += sum; 
            }
            eachCount[cpuCoreCount - 1] += totalLoopCount % cpuCoreCount;
            List<Task> tasks = new(cpuCoreCount);
            long currentSeek = seek;
            for (int loopBatch = 0; loopBatch < cpuCoreCount; loopBatch++)
            {
                FileStream st = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
                BinaryReader r = new BinaryReader(st);
                
                int start = loopBatch == 0 ? 0 : eachCount[loopBatch - 1];
                int end = eachCount[loopBatch];

                int iter = loopBatch;
                
                //var accessor = mmf.CreateViewAccessor(currentSeek + start * sizeof(T),
                //    (end - start) * sizeof(T), MemoryMappedFileAccess.Read);

                st.Seek(currentSeek + start * sizeof(T), SeekOrigin.Begin);
                
                Task task = Task.Run(() =>
                {
                    for (int i = start; i < end; i++)
                    {
                        T data = ReadBinary<T>(r);
                        callback?.Invoke(data, i, iter);
                    }
                    st.DisposeAsync();
                });
                tasks.Add(task);
            }
            
            Task.WhenAll(tasks).Wait();

            seek = seek + sizeof(T) * totalLoopCount;
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
        private static T ReadBinaryMMF<T>(MemoryMappedViewAccessor accessor, ref long index) where T : unmanaged
        {
            unsafe
            {
                int size = sizeof(T);
                byte* tempBuf = stackalloc byte[size];

                for (int i = 0; i < size; i++)
                {
                    if (IsLittleEndian)
                    {
                        tempBuf[size - i - 1] = accessor.ReadByte(index);
                    }
                    else
                    {
                        tempBuf[i] = accessor.ReadByte(index);
                    }
                    index += 1;
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
#endif
}