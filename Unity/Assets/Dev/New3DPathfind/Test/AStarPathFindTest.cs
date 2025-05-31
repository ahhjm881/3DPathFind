using System.Linq;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using Unity.Mathematics;

namespace Candy.Pathfind3D
{
    public class AStarJobRunner : MonoBehaviour
    {
        public Transform StartTransform;
        public Transform EndTransform;

        public bool DoFind;

        public OctreeGraphGenerator GraphGenerator;

        private NativeList<AStarNodeKey> _path;

        private int? si, ei, sti, eti;

        private void Update()
        {
            if (DoFind)
            {
                DoFind = false;
                ExecuteAStar();
            }
        }
        private unsafe void ExecuteAStar()
        {
            NativeOctTree3D octTree3D = GraphGenerator.NativeOctTree3D;
            si = ei = sti = eti = null;
            
            // 실제로는 가장 가까운 트리, 노드를 찾는 로직이 필요
            int startTreeIndex = octTree3D.FindTreeToIncludePoint(StartTransform.position);
            if (startTreeIndex == -1) return;
            sti = startTreeIndex;
            NativeFlattenOctTree nativeFlattenTree = octTree3D.GetTree(startTreeIndex);
            NativeList<int> temp0 = new NativeList<int>(10, Allocator.Temp); 
            NativeList<int> temp1 = new NativeList<int>(10, Allocator.Temp); 
            int startFlattenIndex = NativeFlattenOctTree.FindLeafNodeIndex(StartTransform.position, float3.zero, 
                nativeFlattenTree.FlattenArr, nativeFlattenTree.TreeArr, nativeFlattenTree.IndexArr,
                temp0, temp1
                );
            if (startFlattenIndex == -1) return;
            si = startFlattenIndex;
            
            int endTreeIndex = octTree3D.FindTreeToIncludePoint(EndTransform.position);
            if (endTreeIndex == -1) return;
            eti = endTreeIndex;
            nativeFlattenTree = octTree3D.GetTree(endTreeIndex);
            int endFlattenIndex = NativeFlattenOctTree.FindLeafNodeIndex(EndTransform.position, float3.zero, 
                nativeFlattenTree.FlattenArr, nativeFlattenTree.TreeArr, nativeFlattenTree.IndexArr,
                temp0, temp1
            );
            if (endFlattenIndex == -1) return;
            ei = endFlattenIndex;

            temp0.Dispose();
            temp1.Dispose();
            
            

            // 컨테이너 초기화
            var openSet = new MinHeap<AStarNodeKey>(256, Allocator.TempJob);
            var closedSet = new NativeList<AStarNodeKey>(256, Allocator.TempJob);
            var costFromStart = new NativeHashMap<AStarNodeKey, float>(512, Allocator.TempJob);
            var estimatedTotalCost = new NativeHashMap<AStarNodeKey, float>(512, Allocator.TempJob);
            var cameFrom = new NativeHashMap<AStarNodeKey, AStarNodeKey>(512, Allocator.TempJob);

            // Job 구조체 설정
            var job = new AStarJob
            {
                Edge2Ptr = GraphGenerator.NativeOctGraph.Edge2Ptr,
                EdgeLen = GraphGenerator.NativeOctGraph.EdgeLen,
                Edge2PtrLength = GraphGenerator.NativeOctGraph.Edge2PtrLength,
                EdgeTreeOffset = GraphGenerator.NativeOctGraph.EdgeTreeOffset,

                Trees = GraphGenerator.NativeOctTree3D.Trees,
                TreesLength = GraphGenerator.NativeOctTree3D.TreeCount,

                StartTreeIndex = startTreeIndex,
                StartFlattenIndex = startFlattenIndex,
                EndTreeIndex = endTreeIndex,
                EndFlattenIndex = endFlattenIndex,

                OpenSet = openSet,
                ClosedSet = closedSet,
                CostFromStart = costFromStart,
                EstimatedTotalCost = estimatedTotalCost,
                CameFrom = cameFrom
            };

            // Job 실행 (메인 쓰레드에서 동기 실행)
            //job.Run();
            job.Execute();

            // 경로 역추적
            if (_path.IsCreated) _path.Dispose();
            _path = new NativeList<AStarNodeKey>(Allocator.Persistent);

            var current = new AStarNodeKey(endTreeIndex, endFlattenIndex);
            if (cameFrom.ContainsKey(current))
            {
                while (cameFrom.ContainsKey(current))
                {
                    _path.Add(current);
                    current = cameFrom[current];
                }

                _path.Add(new AStarNodeKey(startTreeIndex, startFlattenIndex));
                _path.Reverse();
            }
            else
            {
                Debug.LogWarning("No Path found!");
            }

            // 임시 컨테이너 정리
            openSet.Dispose();
            closedSet.Dispose();
            costFromStart.Dispose();
            estimatedTotalCost.Dispose();
            cameFrom.Dispose();
        }

        private void OnDrawGizmos()
        {

            Gizmos.color = Color.red;
            if (si.HasValue && sti.HasValue)
            {
                var a = GraphGenerator.NativeOctTree3D.GetTree(sti.Value).GetNode(si.Value);
                Gizmos.DrawSphere(a.WorldPosition, 3f);
            }
            if (ei.HasValue && eti.HasValue)
            {
                var a = GraphGenerator.NativeOctTree3D.GetTree(eti.Value).GetNode(ei.Value);
                Gizmos.DrawSphere(a.WorldPosition, 3f);
            }

            if (!_path.IsCreated || _path.Length < 2)
                return;

            Gizmos.color = Color.green;

            for (int i = 0; i < _path.Length - 1; i++)
            {
                var nodeA = GraphGenerator.NativeOctTree3D.GetTree(_path[i].TreeIndex).GetNode(_path[i].FlattenIndex);
                var nodeB = GraphGenerator.NativeOctTree3D.GetTree(_path[i + 1].TreeIndex).GetNode(_path[i + 1].FlattenIndex);

                Gizmos.DrawLine(nodeA.WorldPosition, nodeB.WorldPosition);
            }
        }

        private void OnDestroy()
        {
            if (_path.IsCreated)
                _path.Dispose();
        }
    }
}
