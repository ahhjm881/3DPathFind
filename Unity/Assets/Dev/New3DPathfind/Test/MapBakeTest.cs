using System;
using System.Threading;
using System.Threading.Tasks;
using Unity.Collections;
using UnityEngine;


namespace Candy.Pathfind3D
{
    #if UNITY_EDITOR
    using Editor;
    using UnityEditor;
    
    public class MapBakeTest : MonoBehaviour
    {
        public bool _saveTree;
        public bool _loadTree;
        public bool _saveGraph;
        public bool _loadGraph;

        public OctreeGraphGenerator _generator;
        private void LoadTree(SynchronizationContext mainCtx)
        {
            var handler = new MapBakeHandler();
            int id = Progress.Start("Load Tree", "Processing nodes", Progress.Options.Managed);
            handler.LoadTree(out NativeOctTree3D tree3d, id);
            
            mainCtx.Post(_ =>
            {
                _generator.SetTree(tree3d);
            }, null);
        }

        private void LoadGraph(SynchronizationContext mainCtx)
        {
            var handler = new MapBakeHandler();
            int id = Progress.Start("Load Graph", "Processing nodes", Progress.Options.Managed);
            handler.LoadGraph(out NativeOctGraph graph, id);
            
            mainCtx.Post(_ =>
            {
                _generator.SetGraph(graph);
            }, null);
        }
        
        private void Update()
        {
            if (!_generator) return;
            
            if (_saveTree)
            {
                _saveTree = false;

                var handler = new MapBakeHandler();
                handler.BakeTree(_generator.NativeOctTree3D, true);
            }
            if (_loadTree)
            {
                _loadTree = false;

                var ctx = SynchronizationContext.Current;

                Task.Run(() =>
                {
                    try
                    {
                        LoadTree(ctx);
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                    }
                });
            }
            if (_saveGraph)
            {
                _saveGraph = false;

                var handler = new MapBakeHandler();
                handler.BakeGraph(_generator.NativeOctGraph, true);
            }

            if (_loadGraph)
            {
                _loadGraph = false;
                var ctx = SynchronizationContext.Current;
                Task.Run(() =>
                {
                    try
                    {
                        LoadGraph(ctx);
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                    }
                });
            }
        }
    }
    #endif
}