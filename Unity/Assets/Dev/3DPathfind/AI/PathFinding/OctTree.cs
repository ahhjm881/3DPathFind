using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;

public class OctTree
{
    static private Collider[] allCollider = new Collider[1000];

    readonly Quaternion q1 = Quaternion.Euler(0f, 90f, 0f);
    readonly Quaternion q2 = Quaternion.Euler(0f, 180f, 0f);
    readonly Quaternion q3 = Quaternion.Euler(0f, 270f, 0f);

    [Range(1, 4)]
    public int chunk = 4;
    public float scale = 30f;
    public uint defaultChunk = 0;
    public Vector3 position, size;

    private LayerMask mask;

    private OctTree[] neighbor;
    private OctNode rootOctNode;
    private OctNodeData[] tempOctNodeData = new OctNodeData[8];

    //Getter & Setter
    public OctNode rootNode { get { return rootOctNode; } }
    public List<PathGraphNode> graph { get; private set; }

    //Constructor
    public OctTree(Vector3 pos, Vector3 size, params string[] layers)
    {
        graph = new List<PathGraphNode>(1024);
        position = pos;
        this.size = size;

        mask = LayerMask.GetMask(layers);
    }

    //Method
    public void Generate()
    {
        //if (chunk > 6) chunk = 6;
        graph.Clear();

        if (rootOctNode != null)
            rootOctNode.Release();

        rootOctNode = new OctNode(null, new OctNodeData(position, Vector3.one * scale), 0);

        FF(position, scale, chunk, rootOctNode);
        rootOctNode.GetGraph(graph, (uint)chunk);

    }
    public void Tree2Graph()
    {
        float s = 1.01f;
        for(int i=0; i< graph.Count;i++)
        {
            for (int j=0; j<neighbor.Length; j++)
            {
                if (neighbor[j] != null && AABB.CheckCollision(graph[i].position, graph[i].size, neighbor[j].position, neighbor[j].scale * s * Vector3.one))
                {

                    for (int k = 0; k < neighbor[j].graph.Count; k++)
                    {
                        if (AABB.CheckCollision(graph[i].position, graph[i].size, neighbor[j].graph[k].position, neighbor[j].graph[k].size * s))
                        {
                            graph[i].paths.Add(new PathGraphNode.PathData(neighbor[j].graph[k], (neighbor[j].graph[k].position - graph[i].position).sqrMagnitude));
                        }
                    }
                }
            }

            for (int j=0; j< graph.Count; j++)
            {
                if(AABB.CheckCollision(graph[i].position, graph[i].size, graph[j].position, graph[j].size * s))
                {
                    graph[i].paths.Add(new PathGraphNode.PathData(graph[j], (graph[j].position - graph[i].position).sqrMagnitude));
                }
            }
        }
    }
    public OctTree[] GetNeighbor(OctGenerator og)
    {
        if (og != null)
            return neighbor;
        else
            return null;
    }
    public void SetNeighbor(OctGenerator og, OctTree[] neighbor)
    {
        this.neighbor = neighbor;
    }

    //Inner Method
    private void FF(Vector3 pos, float scale, int chunk, OctNode node)
    {
        if (chunk < 1) return;

        // 해당 공간 범위에 존재하는 Collider가 있는지 체크
        int c = Physics.OverlapBoxNonAlloc(node.data.pos, node.data.size * 0.5f, allCollider, Quaternion.identity, mask);
        bool on = c > 0 ? true : false;

        // 최소 공간 분할 깊이 체크
        if (!on && this.chunk - defaultChunk >= chunk)
        {
            //PathGraphNode tempNode = null;
            if (this.chunk == chunk)
            {
                node.data = new OctNodeData(pos, Vector3.one * scale);
                //tempNode = new PathGraphNode(node.data.pos, node.data.size, 500);
                //tempNode.use = true;
                node.use = true;
            }

            node.child = null;
            return;
        }

        // 공간 분할 (하위 노드 생성)
        DivOct(node, chunk);

        // 위에서 생성한 노드를 재귀적으로 분할
        Vector3 size = Vector3.one * scale * 0.5f;
        OctNode sender = null;
        sender = node.child[0];
        FF(pos + size, scale * 0.5f, chunk - 1, sender);
        sender = node.child[1];
        FF(pos + q1 * size, scale * 0.5f, chunk - 1, sender);
        sender = node.child[2];
        FF(pos + q2 * size, scale * 0.5f, chunk - 1, sender);
        sender = node.child[3];
        FF(pos + q3 * size, scale * 0.5f, chunk - 1, sender);
        sender = node.child[4];
        FF(pos - size, scale * 0.5f, chunk - 1, sender);
        sender = node.child[5];
        FF(pos - q1 * size, scale * 0.5f, chunk - 1, sender);
        sender = node.child[6];
        FF(pos - q2 * size, scale * 0.5f, chunk - 1, sender);
        sender = node.child[7];
        FF(pos - q3 * size, scale * 0.5f, chunk - 1, sender);

    }
    private void DivOct(OctNode node, int chunk)
    {
        Vector3 pos = node.data.pos;
        Vector3 size = node.data.size * 0.5f;

        tempOctNodeData[0] = new OctNodeData(pos + size * 0.5f, size);
        tempOctNodeData[1] = new OctNodeData(pos + q1 * (size * 0.5f), size);
        tempOctNodeData[2] = new OctNodeData(pos + q2 * (size * 0.5f), size);
        tempOctNodeData[3] = new OctNodeData(pos + q3 * (size * 0.5f), size);
        tempOctNodeData[4] = new OctNodeData(pos - size * 0.5f, size);
        tempOctNodeData[5] = new OctNodeData(pos - q1 * (size * 0.5f), size);
        tempOctNodeData[6] = new OctNodeData(pos - q2 * (size * 0.5f), size);
        tempOctNodeData[7] = new OctNodeData(pos - q3 * (size * 0.5f), size);

        for (int i = 0; i < 8; i++)
        {
            var d = tempOctNodeData[i];

            bool on = false;

            int c = Physics.OverlapBoxNonAlloc(tempOctNodeData[i].pos, tempOctNodeData[i].size * 0.5f, allCollider, Quaternion.identity, mask);

            on = c > 0 ? true : false;

            node[i] = new OctNode(node, d, (uint)(this.chunk - chunk));
            node[i].use = !on;


        }

    }

}
