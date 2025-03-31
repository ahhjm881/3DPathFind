using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AABB
{
    static public bool CheckCollision(Vector3 pos1, Vector3 size1, Vector3 pos2, Vector3 size2)
    {
        size1 *= 0.5f;
        size2 *= 0.5f;

        if(
            pos1.x - size1.x <= pos2.x + size2.x && pos1.x + size1.x >= pos2.x - size2.x  &&
            pos1.y - size1.y <= pos2.y + size2.y && pos1.y + size1.y >= pos2.y - size2.y &&
            pos1.z - size1.z <= pos2.z + size2.z && pos1.z + size1.z >= pos2.z - size2.z
            )
        return true;

        return false;
    }
}
