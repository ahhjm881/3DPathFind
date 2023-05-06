using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Diagnostics;

public class AStar
{
    public bool allowTimeout;
    public bool allowCurve;
    public int timeout = 5, failTimeout = 5;

    private OpenSetHeap open;
    private HashSet<PathGraphNode> close;

    private PathGraphNode current;
    private PathGraphNode endNode;
    private PathGraphNode temp = null;
    private PathGraphNode min = null;

    private Vector3 agentSize = Vector3.zero;

    private OBB obb1, obb2;

    private bool check = false;
    private bool progressing = false;
    private bool disableEndNodeException = false;

    private int openHeapSize;
    private int id;

    private Stopwatch stopwatch;

    private AStarMessage msg;

    //Getter & Setter
    public Vector3 overlapSize { get { return agentSize; } set { agentSize = value; } }
    public AStarMessage message { get { return msg; } }
    
    //Constructor
    public AStar(int id, int heapSize = 100000)
    {
        openHeapSize = heapSize;

        this.id = id;

        open = new OpenSetHeap(id, openHeapSize);

        close = new HashSet<PathGraphNode>();

        allowTimeout = false;

        obb1 = new OBB();
        obb2 = new OBB();
        stopwatch = new Stopwatch();
    }

    //Method
    public List<PathGraphNode> Start(PathGraphNode start, PathGraphNode end, Vector3 _agentSize)
    {
        if (start == null)
            throw new System.ArgumentException("Parameter is null", "start");

        if (end == null && !disableEndNodeException)
            throw new System.ArgumentException("Parameter is null", "end");

        if(OctNode.Thread - 1 < id)
            throw new System.ArgumentException("Not valid id : " + id.ToString(), "id");

        if (!progressing)
        {
            current = start;
            endNode = end;
        }

        this.agentSize = _agentSize;

        return Search();
    }

    public void Release(bool completelyRelease = false)
    {
        if (completelyRelease)
        {
            open = null;
            close = null;

            stopwatch = null;
            obb1 = obb2 = null;
        }
        else
        {
            open.Clear();
            close.Clear();
        }

        current = null;
        endNode = null;

        progressing = false;
        disableEndNodeException = false;
        msg = AStarMessage.None;
    }

    //Inside Method
    private List<PathGraphNode> ConstructPath(PathGraphNode node)
    {
        List<PathGraphNode> path = new List<PathGraphNode>();

        if (endNode == null)
        {
            path.Add(node);
            return path;
        }

        do
        {
            path.Add(node);
            node = node.parent[id];

        } while (node != null);


        path.Reverse();

        msg = AStarMessage.Complete;

        return path;
    }
    private bool CheckCurve(PathGraphNode current, PathGraphNode min)
    {
        bool check = false;
        bool minIsCurrentPath = false;

        Vector3 minMinusCurrent = (min.position - current.position).normalized;

        float v1 = Vector3.Dot(minMinusCurrent, Vector3.up);
        float v2 = Vector3.Dot(minMinusCurrent, Vector3.forward);
        float v3 = Vector3.Dot(minMinusCurrent, Vector3.right);

        PathGraphNode tempNev = null;


        for (int i = 0; i < current.paths.Count; i++)
        {
            if (current.paths[i].node == min)
            {
                minIsCurrentPath = true;
                break;
            }
        }

        if (minIsCurrentPath && !(v1 == 1 || v1 == -1) && !(v2 == 1 || v2 == -1) && !(v3 == 1 || v3 == -1))
        {
            Vector3 up = Vector3.Cross(current.size - current.position, minMinusCurrent).normalized;
            Vector3 right = Vector3.Cross(minMinusCurrent, up).normalized;
            Vector3 forward = Vector3.Cross(up, right).normalized;


            for (int k = 0; k < current.paths.Count; k++)
            {
                tempNev = current.paths[k].node;
                if (tempNev.use) continue;
                obb1.position = current.position + minMinusCurrent * 50f;
                obb1.size = new Vector3(agentSize.x, agentSize.y, 100f);
                obb1.lossyScale = Vector3.one;
                obb1.offset = Vector3.zero;
                obb1.forward = forward;
                obb1.right = right;
                obb1.up = up;

                obb2.position = tempNev.position;
                obb2.size = tempNev.size;
                obb2.lossyScale = Vector3.one;
                obb2.offset = Vector3.zero;
                obb2.forward = Vector3.forward;
                obb2.right = Vector3.right;
                obb2.up = Vector3.up;

                if (obb1.CheckCollision(obb2))
                {
                    check = true;
                    break;
                }
            }

        }

        return check;
    }
    private List<PathGraphNode> Search()
    {
        if (!progressing)
        {
            current.parent[id] = null;
            current.weight[id] = 0;

            open.Add(current);

            progressing = true;
        }

        if (allowTimeout)
        {
            stopwatch.Reset();
            stopwatch.Start();
        }

        float currentTimeout = 0f;

        while (!open.IsEmpty())
        {
            if (allowTimeout)
                currentTimeout = timeout;
            else
                currentTimeout = failTimeout;

            if (stopwatch.ElapsedMilliseconds >= currentTimeout)
            {
                stopwatch.Stop();

                if (allowTimeout)
                    msg = AStarMessage.TimeOut;
                else
                    msg = AStarMessage.Failed;

                return null;
            }

            min = open.Pop();

            //target find check
            if(min == endNode)
            {
                msg = AStarMessage.Complete;
                return ConstructPath(min);
            }


            if (min != null)
            {
                close.Add(min);
                current = min;
            }

            for (int i = 0; i < current.paths.Count; i++)
            {
                temp = current.paths[i].node;

                check = false;

                //Curve detect
                //if(!allowCurve)
                //{
                //    if (current.paths[i].collisionTable.ContainsKey(agentSize))
                //        check = current.paths[i].collisionTable[agentSize];
                //    else
                //        current.paths[i].collisionTable.Add(agentSize, check = CheckCurve(current, temp));
                //}


                if (check) continue;
                if (!temp.use) continue;
                if (close.Contains(temp)) continue;
                if (agentSize.sqrMagnitude > temp.size.sqrMagnitude) continue;

                if (open.HasKey(temp))
                {
                    if (temp.weight[id] > current.weight[id] + current.paths[i].weight)
                    {
                        temp.parent[id] = current;
                        temp.weight[id] = current.weight[id] + current.paths[i].weight;
                    }
                }
                else
                {
                    temp.parent[id] = current;
                    temp.weight[id] = current.paths[i].weight + current.weight[id];


                    temp.f[id] = temp.weight[id] + temp.GetHeuristics(endNode);

                    open.Add(temp);
                }
            }

        }

        msg = AStarMessage.Failed;

        return null;
    }
}

