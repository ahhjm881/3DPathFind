using System;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Profiling;

namespace Candy.Pathfind3D
{
    public class Test : MonoBehaviour
    {
        public bool IsDraw;

        [SerializeField] private OctTree.InitParameter _param;

        private OctTree _tree;
        private void Start()
        {
            _param.WorldPosition = transform.position;
            _tree = new OctTree(_param);
            
            Profiler.BeginSample("Create Space");
            _tree.Divide();
            Profiler.EndSample();
        }

        private void OnDestroy()
        {
            _tree?.Dispose();
        }

        private void OnDrawGizmos()
        {
            Gizmos.DrawWireCube(transform.position, Vector3.one * _param.Scale);

            if (IsDraw is false) return;
            
            if (_tree is null) return;

            for (int i = 0; i < _tree.OctNodes.Length; i++)
            {
                OctNode node = _tree.OctNodes[i];
                if (node.IsGenerated is false) continue;

                Gizmos.color = node.IsObstacle ? Color.yellow : Color.blue;
                
                Gizmos.DrawWireCube(node.WorldPosition, Vector3.one * node.Scale);
            }
        }
    }
}