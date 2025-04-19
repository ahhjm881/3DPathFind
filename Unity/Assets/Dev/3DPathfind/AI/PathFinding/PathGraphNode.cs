using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class PathGraphNode
{
    public Vector3 position;
    public Vector3 size;
    public List<PathData> paths;
    public uint height;
    public bool use;

    //Getter & setter
    public float[] weight { get; private set; }
    public float[] f{ get; private set; }
    public PathGraphNode[] parent { get; private set; }

    //Constructor
    public PathGraphNode(Vector3 pos, Vector3 size, int thread, float weight = 0f)
    {
        paths = new List<PathData>(16);

        position = pos;
        this.size = size;
        this.weight = new float[thread];
        f = new float[thread];
        parent = new PathGraphNode[thread];

        if(weight > 0f)
        {
            for (int i = 0; i < this.weight.Length; i++)
            {
                this.weight[i] = weight;
            }
        }
    }

    //Heuristics Method
    public float GetHeuristics(PathGraphNode end)
    {
        return (end.position - position).sqrMagnitude;
    }
    public float GetHeuristicsDown(PathGraphNode end)
    {
        return (end.position - position).sqrMagnitude * (position.y <= 5f ? 0.75f : 1.25f);
    }
    public float GetHeuristics(Vector3 endPoint)
    {
        return (endPoint - position).sqrMagnitude;
    }

    //Method
    public bool CompareMin(PathGraphNode y, int id)
    {
        return f[id] < y.f[id];
    }

    //Inside struct
    public struct PathData
    {
        public float weight;
        public PathGraphNode node;

        public Dictionary<Vector3, bool> collisionTable { get; private set; }

        public PathData(PathGraphNode path, float weight)
        {
            this.weight = weight;
            this.node = path;

            collisionTable = new Dictionary<Vector3, bool>();
        }
    }
}