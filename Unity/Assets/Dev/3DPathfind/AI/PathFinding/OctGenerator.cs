using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;

public class OctGenerator : MonoBehaviour
{
    static private OctGenerator _inst;


    [SerializeField] private Vector3Int size;

    [SerializeField] private float scale = 10f;

    [Range(1, 4)]
    [SerializeField] private int depth = 3;
    [SerializeField] private uint defaultDepth = 0;
    [SerializeField] private string[] layers = { "Default" };
    [SerializeField] private int PathThreadSize = 1000;

    [SerializeField] private bool reset, createGraph;
    [SerializeField] private bool drawEmptySpace, drawNotUseSpace, drawPathLine, hiddeWireCube;

    private OctNode rootNode;
    private List<OctTree> octs;

    //Getter & setter
    static public bool existPath
    {
        get
        {
            if (get.octs.Count == 0) return false;
            if (get.octs[0].graph == null) return false;
            if (get.octs[0].graph.Count == 0) return false;
            if (get.octs[0].graph[0] == null) return false;

            return true;
        }
    }
    static public OctGenerator get
    {
        get
        {
            if (!_inst)
            {
                //throw new System.NullReferenceException("OctGenerator singleton instance is null");
                _inst = GameObject.FindWithTag("MainOctGenerator").GetComponent<OctGenerator>();
            }

            return _inst;
        }
    }
    private Vector3 origin { get { return transform.position + (scale * (Vector3)size) * 0.5f; } }
    private Vector3 scaledSize { get { return scale * (Vector3)size; } }
    public Vector3Int Size { get { return size; } set { size = value; } }
    public float Scale { get { return scale; } set { if (value < 0f) scale = 1f; else scale = value; } }
    public int Depth { get { return depth; } set { if (value < 1 || value > 4) depth = 1; else depth = value; } }
    public uint DefaultDepth { get { return defaultDepth; } set { if (value < 1 || value > 4) defaultDepth = 1; else defaultDepth = value; } }

    //Unity Callback
    private void Awake()
    {
        //_inst = this;
        octs = new List<OctTree>();

        var field = typeof(OctNode).GetField
            ("thread", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.SetField);

        field.SetValue(new OctNode(), PathThreadSize);

        CreateOcTree();
    }
    public void Update()
    {
        if (defaultDepth >= depth) defaultDepth--;

        Stopwatch w = new Stopwatch();

        foreach (var i in octs)
        {
            i.scale = scale;
            i.chunk = depth;
            i.defaultChunk = defaultDepth;
        }

        if (reset)
        {
            w.Start();
            reset = false;

            CreateSpace();

            w.Stop();
            print(w.ElapsedMilliseconds / 1000f);
        }

        if (createGraph)
        {
            w.Start();
            createGraph = false;

            CreatePath();

            w.Stop();
            print(w.ElapsedMilliseconds / 1000f);
        }
    }

    //Method
    public PathGraphNode GetAnyNode()
    {
        if (octs != null)
            return octs[0].graph[0];
        else
            return null;
    }
    public PathGraphNode PositionToNode(Vector3 position, Vector3 overlapSize)
    {
        if (rootNode == null) return null;
        if (!AABB.CheckCollision(rootNode.data.pos, rootNode.data.size, position, Vector3.zero)) return null;
        else if (rootNode.child == null) return null;

        OctNode current = rootNode;
        OctNode child = null;

        int k = 0;

        int testCount = 0;

        while (current.child != null)
        {
            if (k >= current.child.Length) break;

            child = current.child[k++];

            if (child == null)
                break;

            if (AABB.CheckCollision(child.data.pos, child.data.size, position, Vector3.zero))
            {
                testCount++;
                current = child;
                k = 0;
            }

        }

        if (current.pathNode == null) return null;
        if (!current.pathNode.use) return null;
        if (current.pathNode.size.sqrMagnitude < overlapSize.sqrMagnitude) return null;

        return current.pathNode;
    }
    public void CreateMapData()
    {
        CreateSpace();
        CreateOcTree();
        CreatePath();
    }

    //Inside Method
    private void CreateSpace()
    {
        rootNode = new OctNode(null, new OctNodeData(origin, scaledSize), 0);
        rootNode.child = new OctNode[octs.Count];

        for (int i = 0; i < octs.Count; i++)
        {
            octs[i].Generate();
            rootNode.child[i] = octs[i].rootNode;
        }

    }
    private void CreatePath()
    {
        foreach (var i in octs)
        {
            i.Tree2Graph();
        }
    }
    private void CreateOcTree()
    {
        OctTree[][][] test = new OctTree[size.x][][];

        for (int x = 0; x < size.x; x++)
        {
            test[x] = new OctTree[size.y][];
            for (int y = 0; y < size.y; y++)
            {
                test[x][y] = new OctTree[size.z];
                for (int z = 0; z < size.z; z++)
                {
                    var oct = new OctTree(transform.position + new Vector3(x, y, z) * scale + Vector3.one * scale * 0.5f, Vector3.one * scale, layers);

                    oct.scale = scale;
                    oct.chunk = depth;
                    oct.defaultChunk = defaultDepth;

                    octs.Add(oct);
                    test[x][y][z] = oct;
                }
            }
        }



        for (int x = 0; x < size.x; x++)
        {
            for (int y = 0; y < size.y; y++)
            {
                for (int z = 0; z < size.z; z++)
                {
                    test[x][y][z].SetNeighbor(this, new OctTree[26]);

                    if (z + 1 < size.z)
                        test[x][y][z].GetNeighbor(this)[(int)CubeDirection.Front] = test[x][y][z + 1];

                    if (x + 1 < size.x)
                    {
                        test[x][y][z].GetNeighbor(this)[(int)CubeDirection.Right] = test[x + 1][y][z];

                        if (y + 1 < size.y)
                            test[x][y][z].GetNeighbor(this)[(int)CubeDirection.RightUp] = test[x + 1][y + 1][z];

                        if (y - 1 >= 0)
                            test[x][y][z].GetNeighbor(this)[(int)CubeDirection.RightDown] = test[x + 1][y - 1][z];
                    }

                    if (z - 1 >= 0)
                        test[x][y][z].GetNeighbor(this)[(int)CubeDirection.Back] = test[x][y][z - 1];

                    if (x - 1 >= 0)
                    {
                        test[x][y][z].GetNeighbor(this)[(int)CubeDirection.Left] = test[x - 1][y][z];

                        if (y + 1 < size.y)
                            test[x][y][z].GetNeighbor(this)[(int)CubeDirection.LeftUp] = test[x - 1][y + 1][z];

                        if (y - 1 >= 0)
                            test[x][y][z].GetNeighbor(this)[(int)CubeDirection.LeftDown] = test[x - 1][y - 1][z];

                    }

                    if (y + 1 < size.y)
                        test[x][y][z].GetNeighbor(this)[(int)CubeDirection.Up] = test[x][y + 1][z];

                    if (y - 1 >= 0)
                        test[x][y][z].GetNeighbor(this)[(int)CubeDirection.Down] = test[x][y - 1][z];


                    if (z + 1 < size.z)
                    {
                        if (y + 1 < size.y && x - 1 >= 0)
                            test[x][y][z].GetNeighbor(this)[(int)CubeDirection.FrontLeftUp] = test[x - 1][y + 1][z + 1];

                        if (x - 1 >= 0)
                            test[x][y][z].GetNeighbor(this)[(int)CubeDirection.FrontLeft] = test[x - 1][y][z + 1];

                        if (y - 1 >= 0 && x - 1 >= 0)
                            test[x][y][z].GetNeighbor(this)[(int)CubeDirection.FrontLeftDown] = test[x - 1][y - 1][z + 1];

                        if (y + 1 < size.y && x + 1 < size.x)
                            test[x][y][z].GetNeighbor(this)[(int)CubeDirection.FrontRightUp] = test[x + 1][y + 1][z + 1];

                        if (x + 1 < size.x)
                            test[x][y][z].GetNeighbor(this)[(int)CubeDirection.FrontRight] = test[x + 1][y][z + 1];

                        if (y - 1 >= 0 && x + 1 < size.x)
                            test[x][y][z].GetNeighbor(this)[(int)CubeDirection.FrontRightDown] = test[x + 1][y - 1][z + 1];

                        if (y + 1 < size.y)
                            test[x][y][z].GetNeighbor(this)[(int)CubeDirection.FrontUp] = test[x][y + 1][z + 1];

                        if (y - 1 >= 0)
                            test[x][y][z].GetNeighbor(this)[(int)CubeDirection.FrontDown] = test[x][y - 1][z + 1];
                    }

                    if (z - 1 >= 0)
                    {
                        if (y + 1 < size.y && x - 1 >= 0)
                            test[x][y][z].GetNeighbor(this)[(int)CubeDirection.BackLeftUp] = test[x - 1][y + 1][z - 1];

                        if (x - 1 >= 0)
                            test[x][y][z].GetNeighbor(this)[(int)CubeDirection.BackLeft] = test[x - 1][y][z - 1];

                        if (y - 1 >= 0 && x - 1 >= 0)
                            test[x][y][z].GetNeighbor(this)[(int)CubeDirection.BackLeftDown] = test[x - 1][y - 1][z - 1];
                                                                      
                        if (y + 1 < size.y && x + 1 < size.x)        
                            test[x][y][z].GetNeighbor(this)[(int)CubeDirection.BackRightUp] = test[x + 1][y + 1][z - 1];
                                                                     
                        if (x + 1 < size.x)                           
                            test[x][y][z].GetNeighbor(this)[(int)CubeDirection.BackRight] = test[x + 1][y][z - 1];
                                                                      
                        if (y - 1 >= 0 && x + 1 < size.x)             
                            test[x][y][z].GetNeighbor(this)[(int)CubeDirection.BackRightDown] = test[x + 1][y - 1][z - 1];

                        if (y + 1 < size.y)
                            test[x][y][z].GetNeighbor(this)[(int)CubeDirection.BackUp] = test[x][y + 1][z - 1];

                        if (y - 1 >= 0)
                            test[x][y][z].GetNeighbor(this)[(int)CubeDirection.BackDown] = test[x][y - 1][z - 1];
                    }


                }
            }
        }

    }


#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;

        if(!hiddeWireCube)
            Gizmos.DrawWireCube(origin, scaledSize);

        if(drawEmptySpace || drawNotUseSpace)
        {
            foreach (var oct in octs)
            {
                //Gizmos.DrawWireCube(oct.treeObb.data.position, oct.treeObb.data.size);

                for (int i = 0; i < oct.graph.Count; i++)
                {
                    if (oct.graph[i].use && drawEmptySpace)
                    {
                        Gizmos.color = Color.blue;
                        Gizmos.DrawWireCube(oct.graph[i].position, oct.graph[i].size);
                    }
                    if(!oct.graph[i].use && drawNotUseSpace)
                    {
                        Gizmos.color = Color.yellow;
                        Gizmos.DrawWireCube(oct.graph[i].position, oct.graph[i].size);
                    }
                }   
            }
        }

        if(drawPathLine)
        {
            foreach (var oct in octs)
            {
                Gizmos.color = Color.white;

                foreach(var i in oct.graph)
                {
                    foreach(var j in i.paths)
                    {
                        if (!j.node.use || !i.use) continue;
                        Gizmos.DrawLine(i.position, j.node.position);
                    }
                }
            }
            
        }
    }
#endif
}