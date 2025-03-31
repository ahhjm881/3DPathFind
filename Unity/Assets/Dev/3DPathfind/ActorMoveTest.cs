using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PathFind3DSingle))]
public class ActorMoveTest : MonoBehaviour
{
    public Transform target;
    public bool move, allowTimeout;
    public int timeout = 1, failTimeout = 100;

    PathFind3DSingle agent;

    private void Awake()
    {
        agent = GetComponent<PathFind3DSingle>();
    }

    private void Update()
    {
        agent.overlapSize = transform.lossyScale;

        if(!allowTimeout)
        {
            if (target && move)
                agent.SetDestination(target.position);
            else
                agent.Stop();
        }
        else
        {
            if(move && target)
            {
                AStarMessage message;
                var path = PathRequest.AsyncTryGetPath(1, out message, transform.position, target.position, transform.lossyScale, timeout, failTimeout, false);

                if(path != null)
                {
                    agent.SetDestination(path);
                }
            }
        }
    }
}
