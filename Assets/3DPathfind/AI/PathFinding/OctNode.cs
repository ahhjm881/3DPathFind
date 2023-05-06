using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class OctNode
{
    public OctNode parent;
    public OctNode[] child;
    public OctNodeData data;
    public bool use;
    public uint height;
    public PathGraphNode pathNode;

    //Getter & Setter
    static private int thread;
    static public int Thread { get { return thread; } }
    
    //operator
    public OctNode this[int indexer]
    {
        get
        {
            return child[indexer];
        }

            set
        {
            child[indexer] = value;
        }
    }


    //Constructor
    public OctNode(OctNode parent, OctNodeData d, uint h)
    {
        child = new OctNode[8];

        data = d;
        height = h;
    }
    public OctNode()
    {
        child = new OctNode[8];

        parent = null;
    }

    //Method
    public void GetGraph(List<PathGraphNode> graph, uint maximumChunk)
    {
        int count = 0;

        if (child != null && height <= maximumChunk)
        {
            for (int i = 0; i < child.Length; i++)
            {
                if (child[i] != null)
                {
                    child[i].GetGraph(graph, maximumChunk);
                    count++;
                }
            }
        }

        if (count == 0)
        {
            var g = new PathGraphNode(data.pos, data.size, thread);
            g.height = height;
            graph.Add(g);
            g.use = use;
            pathNode = g;
        }
    }
    public void Release()
    {
        if (child == null) return;

        for (int i = 0; i < 8; i++)
        {
            if (child[i] != null)
                child[i].Release();
        }
    }

}