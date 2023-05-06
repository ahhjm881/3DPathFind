using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Diagnostics;

public class PathFind3DSingle : MonoBehaviour
{
    public float speed;

    public bool move, drawPath, drawNonUse, allowCurve;

    private AStar a;
    private List<PathGraphNode> path;
    private List<SplineNote> notes;
    private new Transform transform;
    private int index = 0;
    private int key;
    private PathGraphNode currentPath;
    private Vector3 destiniation;
    private bool async;

    private Stopwatch w;

    public Vector3 overlapSize { get { return a.overlapSize; } set { a.overlapSize = value; } }
    public AStarMessage message { get { return a.message; } }
    public int failTimeout { get { return a.failTimeout; } set { a.failTimeout = value; } }

    void Awake()
    {
        a = new AStar(0);

        a.allowCurve = allowCurve;
        a.allowTimeout = false;
        a.timeout = 10;
        a.failTimeout = 10;

        StartCoroutine("Co");

        this.transform = base.transform;

        w = new Stopwatch();
    }

    private void Update()
    {
        FollowPath();
    }

    private void FollowPath()
    {
        if (!OctGenerator.existPath) return;
        if (currentPath == null || !move || path == null) return;

        if (Vector3.Distance(transform.position, currentPath.position) <= 0f)
        {
            if (index + 1 < path.Count)
            {
                currentPath = path[++index];
            }
            return;
        }
        else
        {
            transform.position = Vector3.MoveTowards(transform.position, currentPath.position, speed * Time.deltaTime);
        }
    }

    PathGraphNode current, targets;
    IEnumerator Co()
    {
        while(true)
        {
            if (move && OctGenerator.existPath && transform && a != null && !async)
            {
                bool cc = false, ct = false;

                current = OctGenerator.get.PositionToNode(transform.position, overlapSize);
                targets = OctGenerator.get.PositionToNode(destiniation, overlapSize);


                if(path != null)
                {
                    if (path[0] == current) cc = true;
                    if (path[path.Count-1] == targets) ct = true;
                }

                if ((current != null && targets != null) && (!cc || !ct))
                {
                    w.Start();

                    a.allowCurve = allowCurve;
                    path = a.Start(current, targets, overlapSize);

                    if (path != null)
                    {
                        if (currentPath == null || !cc)
                            currentPath = path[0];
                        index = 0;
                    }

                    a.Release();

                    w.Stop();
                    w.Reset();

                }
            }

            yield return new WaitForSeconds(0.1f);
        }
    }

    public void SetDestination(Vector3 position)
    {
        destiniation = position;
        move = true;
        async = false;
    }

    public void SetDestination(List<PathGraphNode> path)
    {
        this.path = path;
        currentPath = path[0];
        index = 0;
        async = true;
        move = true;
    }

    public void Stop()
    {
        destiniation = Vector3.zero;
        move = false;
    }

    public void Release()
    {
        a.Release(true);
    }

#if UNITY_EDITOR
    HashSet<PathGraphNode> set = new HashSet<PathGraphNode>();
    private void OnDrawGizmos()
    {
        if (path == null || !move) return;


        if (!drawPath) return;
        for (int i = 0; i < path.Count; i++)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireCube(path[i].position, path[i].size);
        }

        if (!drawNonUse) return;

        Gizmos.color = Color.yellow;
        for (int i = 0; i < path.Count; i++)
        {
            for (int j = 0; j < path[i].paths.Count; j++)
            {
                if (!path[i].paths[j].node.use && !set.Contains(path[i].paths[j].node))
                {
                    Gizmos.DrawWireCube(path[i].paths[j].node.position, path[i].paths[j].node.size);
                    set.Add(path[i].paths[j].node);
                }
            }
        }

        set.Clear();


    }
#endif
}
