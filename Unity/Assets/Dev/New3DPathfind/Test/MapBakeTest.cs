using System;
using Unity.Collections;
using UnityEngine;

namespace Candy.Pathfind3D
{
    public class MapBakeTest : MonoBehaviour
    {
        public bool _saveTree;
        public bool _loadTree;
        public bool _saveGraph;
        public bool _loadGraph;

        public OctreeGraphGenerator _generator;
        private void Update()
        {
            if (!_generator) return;
            
            if (_saveTree)
            {
                _saveTree = false;

                var handler = new MapBakeHandler();
                handler.BakeTree(_generator.NativeOctTree3D);
            }
            if (_loadTree)
            {
                _loadTree = false;

                var handler = new MapBakeHandler();
                handler.LoadTree(out NativeOctTree3D tree3d);
                _generator.SetTree(tree3d);
            }
            if (_saveGraph)
            {
                _saveGraph = false;

                var handler = new MapBakeHandler();
                handler.BakeGraph(_generator.NativeOctGraph);
            }

            if (_loadGraph)
            {
                _loadGraph = false;
                
                var handler = new MapBakeHandler();
                handler.LoadGraph(out NativeOctGraph graph);
                _generator.SetGraph(graph);
            }
        }
    }
}