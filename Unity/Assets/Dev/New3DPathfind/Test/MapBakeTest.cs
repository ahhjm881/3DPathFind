using System;
using Unity.Collections;
using UnityEngine;

namespace Candy.Pathfind3D
{
    public class MapBakeTest : MonoBehaviour
    {
        public bool _save;
        public bool _load;

        public OctreeGraphGenerator _generator;
        private void Update()
        {
            if (_save)
            {
                _save = false;

                var handler = new MapBakeHandler();
                handler.BakeGraph(_generator.NativeOctGraph);
            }

            if (_load)
            {
                _load = false;
                
                var handler = new MapBakeHandler();
                handler.LoadGraph(out NativeOctGraph graph, Allocator.Persistent);
                _generator.SetGraph(graph);
            }
        }
    }
}