using System.Collections;
using System.Collections.Generic;
using UnityEngine;

static public class PathRequest
{
    static private Dictionary<int, AStar> stars = new Dictionary<int, AStar>();

    static public List<PathGraphNode> AsyncTryGetPath(int id, out AStarMessage message, Vector3 current, Vector3 target, Vector3 agentSize, int timeout, int failTimeout, bool allowCurve)
    {
        AStar a = null;

        if (id == 0) throw new System.ArgumentException("id value can't 0", "id");

        if (stars.ContainsKey(id))
            a = stars[id];
        else
            stars.Add(id, a = new AStar(id));

        a.timeout = timeout;
        a.allowCurve = allowCurve;
        a.failTimeout = failTimeout;
        a.allowTimeout = true;

        var start = OctGenerator.get.PositionToNode(current, agentSize);
        var end = OctGenerator.get.PositionToNode(target, agentSize);

        if(start == null || end == null)
        {
            message =  AStarMessage.Failed;
            return null;
        }

        var path = a.Start(start, end, agentSize);
        message = a.message;

        if(message == AStarMessage.Complete || message == AStarMessage.Failed)
        {
            a.Release();
        }


        return path;
    }

    static public bool DestroyAsyncPath(int id)
    {
        return stars.Remove(id);
    }

    static public List<PathGraphNode> Find(Vector3 start, Vector3 end, Vector3 overlapSize, OctGenerator generator, int failTimeout = 5, bool allowCurve = false)
    {
        var n1 = generator.PositionToNode(start, overlapSize);
        var n2 = generator.PositionToNode(end, Vector3.zero);

        if (n1 == null || n2 == null) return null;

        AStar a = new AStar(0);
        a.failTimeout = failTimeout;
        a.allowCurve = allowCurve;

        var path = a.Start(n1, n2, overlapSize);

        a.Release(true);

        return path;
    }
}
