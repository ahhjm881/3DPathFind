using System;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Profiling;

namespace Candy.Pathfind3D
{
    public class Test : MonoBehaviour
    {
        public Vector3Int Size;
        public bool IsDraw;

        [SerializeField] private OctTree.InitParameter _param;

        private OctTree[,,] _trees;
        private void Start()
        {
            _trees = new OctTree[Size.x, Size.y, Size.z];

            Profiler.BeginSample("Create Space");
            
            Profiler.BeginSample("Init Physics Buffer");
            (NativeList<OverlapBoxCommand> overlapBoxCommands, NativeList<ColliderHit> results) = OctTree.CreatePhysicsBuffer(_param);
            Profiler.EndSample();
            
            
            Profiler.BeginSample("Create");
            for (int i = 0; i < Size.x; i++)
            {
                for (int j = 0; j < Size.y; j++)
                {
                    for (int k = 0; k < Size.z; k++)
                    {
                        _param.WorldPosition = transform.position + new Vector3(i, j, k) * _param.Scale +
                                               Vector3.one * _param.Scale * 0.5f;
                        var tree = new OctTree(_param);
                        tree.CreateSpace(overlapBoxCommands, results);
                        _trees[i, j, k] = tree;
                    }
                }
            }
            Profiler.EndSample();
            
            Profiler.BeginSample("Release Physics Buffer");
            overlapBoxCommands.Dispose();
            results.Dispose();
            Profiler.EndSample();
            
            Profiler.EndSample();
        }

        private void OnDestroy()
        {
            if (_trees is null) return;
            
            for (int i = 0; i < Size.x; i++)
            {
                for (int j = 0; j < Size.y; j++)
                {
                    for (int k = 0; k < Size.z; k++)
                    {
                        _trees[i, j, k].Dispose();
                    }
                }
            }

            _trees = null;
        }

        private void OnDrawGizmos()
        {
            Gizmos.DrawWireCube(transform.position + _param.Scale * (Vector3)Size * 0.5f, _param.Scale * (Vector3)Size);

            if (IsDraw is false) return;
            if (_trees is null) return;

            for (int x = 0; x < Size.x; x++)
            {
                for (int y = 0; y < Size.y; y++)
                {
                    for (int z = 0; z < Size.z; z++)
                    {
                        OctTree tree = _trees[x, y, z];
                        if (tree.OctNodes.IsCreated is false) return;

                        for (int i = 0; i < tree.OctNodes.Length; i++)
                        {
                            OctNode node = tree.OctNodes[i];
                            if (node.IsGenerated is false) continue;

                            /*
                            bool flag = false;
                            for (int j = 0; j < 8; j++)
                            {
                                int childIndex = 8 * node.Index + (j + 1);
                                if (childIndex >= tree.OctNodes.Length)
                                {
                                    break;
                                }
                                flag |= tree.OctNodes[childIndex].IsGenerated;
                            }

                            if (flag)
                                continue;*/
                            
                            Gizmos.color = node.IsObstacle ? Color.yellow : Color.blue;
                
                            Gizmos.DrawWireCube(node.WorldPosition, Vector3.one * node.Scale);
                        }
                    }
                }
            }

        }
    }
}