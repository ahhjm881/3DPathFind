using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SplineFollower3D : MonoBehaviour
{
    [Header("Trace Transform")]
    public Transform target;

    [Header("OverlapSize")]
    [SerializeField] private Vector3 size;

    [Header("Movement")]
    public float speed;
    public float angulerSpeed;
    public bool move;

    [Header("Setting")]
    [Range(0, 1f)] public float tension;
    [Range(0, 1f)] public float handleLength = 1f;
    [Range(5, 30)] public int iteration;

    [Header("Debug")]
    public bool drawPath;
    public bool drawNonUse,drawLine, drawHandleLine, drawHandle;

    private List<SplineNote> notes;
    private List<Vector3> positions;


    private List<PathGraphNode> path;

    private Vector3? currentDestination;
    private Stack<Vector3> stack;
    public void SetTarget(Vector3 pos)
    {
        path = PathRequest.Find(transform.position, pos, size, OctGenerator.get, 2);

        if(path != null)
        {
            path.Insert(0, new PathGraphNode(transform.position, Vector3.zero, 0, 0));

            path.Add(new PathGraphNode(pos, Vector3.zero, 0, 0));

            notes = PathAdapter.Path2NoteAuto(path, handleLength);
            positions = PathAdapter.NoteCapture(notes, tension, iteration);

            stack = new Stack<Vector3>();

            for (int i = positions.Count - 1; i >= 0; i--)
            {
                stack.Push(positions[i]);
            }
        }
    }

    const float sqrtCloseDistance = 0.22360679774997896964091736687313f;
    private void Update()
    {
        if (stack == null || (stack != null && stack.Count < 1)) return;
        if (!move) return;

        var v = Vector3.MoveTowards(transform.position, currentDestination.Value, speed * Time.deltaTime);
        Vector3 dir = v - transform.position;
        transform.position = v;

        if ((transform.position - currentDestination.Value).sqrMagnitude <= sqrtCloseDistance)
        {
            currentDestination = stack.Pop();
        }

        if (dir != Vector3.zero)
        {
            Quaternion q;

            q = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.Slerp(transform.rotation, q, Time.deltaTime * angulerSpeed);
        }
    }

    private void OnGUI()
    {
        if(GUILayout.Button("SetTarget"))
        {
            currentDestination = transform.position;
            SetTarget(target.position);
        }
    }

    HashSet<PathGraphNode> set = new HashSet<PathGraphNode>();
    private void OnDrawGizmos()
    {
        if (drawLine && path != null)
        {
            var notes = PathAdapter.Path2NoteAuto(path, handleLength);
            if (notes != null)
            {
                for (int i = 0; i < notes.Count - 1; i++)
                {
                    Vector3 v1 = notes[i].current;
                    if (drawHandle)
                        Gizmos.DrawSphere(notes[i].current, 0.05f);

                    if (drawHandleLine)
                    {
                        Gizmos.color = Color.red;
                        Gizmos.DrawLine(notes[i].current, notes[i].currentHandle);

                        if (drawHandle)
                            Gizmos.DrawSphere(notes[i].currentHandle, 0.05f);

                        Gizmos.color = Color.white;
                        Gizmos.DrawLine(notes[i].current, notes[i].preHandle);

                        if (drawHandle)
                            Gizmos.DrawSphere(notes[i].preHandle, 0.05f);
                        Gizmos.color = Color.yellow;
                    }

                    for (int t = 0; t < iteration; t++)
                    {
                        var v = CustomSpline.MoveToWard(notes[i], notes[i + 1], (t + 1f) / iteration, SplineType.Hermite, tension);

                        Gizmos.DrawLine(v1, v);
                        v1 = v;
                    }

                }
            }

        }


        if (!drawPath || path == null) return;
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

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireCube(Vector3.zero, size);
    }
}
