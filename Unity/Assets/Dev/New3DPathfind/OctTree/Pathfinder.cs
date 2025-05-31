using UnityEngine;

namespace Candy.Pathfind3D
{
    public class Pathfinder
    {
        
        public Pathfinder(OctreeGraphGenerator generator)
        {
            _generator = generator;
        }

        public void Request(Vector3 start, Vector3 end)
        {
            
        }
        
        private readonly OctreeGraphGenerator _generator;
        
    }
}