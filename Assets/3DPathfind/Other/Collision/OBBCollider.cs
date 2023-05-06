using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OBBCollider : MonoBehaviour
{
    public OBB obb;

    private void Awake()
    {
        //obb.data = OBBData.CreateData(transform, data.size, data.offset);
        //obb.data = new OBBData(Vector3.zero, data.size, transform.lossyScale, data.offset, data.forward, Vector3.right, Vector3.up);
        obb.position = transform.position;
        obb.forward = transform.forward;
        obb.right = transform.right;
        obb.up = transform.up;
        obb.lossyScale = transform.lossyScale;
    }

    public bool CheckCollision(OBB obb)
    {
        return this.obb.CheckCollision(obb);
    }

    private void OnDrawGizmosSelected()
    {
        DebugDraw();
    }

    public void DebugDraw()
    {
        Matrix4x4 rotationMatrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
        Gizmos.matrix = rotationMatrix;

        Gizmos.color = Color.green;

        Gizmos.DrawWireCube(obb.offset, obb.size);
    }
}


[System.Serializable]
public class OBB
{
    [SerializeField]
    public Vector3 size, offset;

    [HideInInspector]
    public Vector3 forward, right, up;

    [HideInInspector]
    public Vector3 position, lossyScale;

    [HideInInspector]
    public Matrix4x4 localToWorldMatrix;

    [HideInInspector]
    public bool useMatrix;

    public OBB(Vector3 _position, Vector3 _size, Vector3 _lossyScale, Vector3 _offset, Vector3 _forward, Vector3 _right, Vector3 _up, Matrix4x4 _localToWorld)
    {
        position = _position;
        size = _size;
        forward = _forward;
        right = _right;
        up = _up;
        offset = _offset;
        localToWorldMatrix = _localToWorld;
        lossyScale = _lossyScale;

        useMatrix = false; // true
    }

    public OBB(Vector3 _position, Vector3 _size, Vector3 _lossyScale, Vector3 _offset, Vector3 _forward, Vector3 _right, Vector3 _up)
    {
        position = _position;
        size = _size;
        forward = _forward;
        right = _right;
        up = _up;
        offset = _offset;
        localToWorldMatrix = Matrix4x4.identity;
        lossyScale = _lossyScale;
        useMatrix = false;
    }

    public OBB() { }

    public OBB(OBB obb)
    {
        OBB temp = new OBB();
        position = obb.position;
        size = obb.size;
        forward = obb.forward;
        right = obb.right;
        up = obb.up;
        offset = obb.offset;
        localToWorldMatrix = obb.localToWorldMatrix;
        lossyScale = obb.lossyScale;
    }

    static public OBB CreateFromTransform(Transform transform, Vector3 size, Vector3 offset)
    {
        return new OBB(transform.position, size, transform.lossyScale, offset, transform.forward, transform.right, transform.up, transform.localToWorldMatrix);
    }

    public OBB Clone()
    {
        return new OBB(this);
    }

    private Vector3 rsize
    {
        get
        {
            return (new Vector3(size.x * lossyScale.x, size.y * lossyScale.y, size.z * lossyScale.z)) * 0.5f;
        }
    }

    static float abs(float x)
    {
        if (x < 0) return -x;

        return x;
    }

    public bool CheckCollision(OBB target)
    {
        float[][] c = new float[3][];
        c[0] = new float[3];
        c[1] = new float[3];
        c[2] = new float[3];

        float[][] absC = new float[3][];
        absC[0] = new float[3];
        absC[1] = new float[3];
        absC[2] = new float[3];

        float[] d = new float[3];

        float r0, r1, r;
        int i;

        const float cutoff = 0.999999f;
        bool existsParallelPair = false;

        Vector3 diff;

        diff = position - target.position;


        Vector3[] axisDir = new Vector3[3];
        axisDir[0] = target.right;
        axisDir[1] = target.up;
        axisDir[2] = target.forward;


        for (i = 0; i < 3; ++i)
        {
            c[0][i] = Vector3.Dot(right, axisDir[i]);
            absC[0][i] = abs(c[0][i]);
            if (absC[0][i] > cutoff)
                existsParallelPair = true;
        }
        d[0] = Vector3.Dot(diff, right);
        r = abs(d[0]);
        r0 = rsize.x;
        r1 = target.rsize.x * absC[0][0] + target.rsize.y * absC[0][1] + target.rsize.z * absC[0][2];

        if (r > r0 + r1)
            return false;



        for (i = 0; i < 3; ++i)
        {
            c[1][i] = Vector3.Dot(up, axisDir[i]);
            absC[1][i] = abs(c[1][i]);
            if (absC[1][i] > cutoff)
                existsParallelPair = true;
        }
        d[1] = Vector3.Dot(diff, up);
        r = abs(d[1]);
        r0 = rsize.y;
        r1 = target.rsize.x * absC[1][0] + target.rsize.y * absC[1][1] + target.rsize.z * absC[1][2];

        if (r > r0 + r1)
            return false;



        for (i = 0; i < 3; ++i)
        {
            c[2][i] = Vector3.Dot(forward, axisDir[i]);
            absC[2][i] = abs(c[2][i]);
            if (absC[2][i] > cutoff)
                existsParallelPair = true;
        }
        d[2] = Vector3.Dot(diff, forward);
        r = abs(d[2]);
        r0 = rsize.z;
        r1 = target.rsize.x * absC[2][0] + target.rsize.y * absC[2][1] + target.rsize.z * absC[2][2];

        if (r > r0 + r1)
            return false;



        r = abs(Vector3.Dot(diff, target.right));
        r0 = rsize.x * absC[0][0] + rsize.y * absC[1][0] + rsize.z * absC[2][0];
        r1 = target.rsize.x;

        if (r > r0 + r1)
            return false;



        r = abs(Vector3.Dot(diff, target.up));
        r0 = rsize.x * absC[0][1] + rsize.y * absC[1][1] + rsize.z * absC[2][1];
        r1 = target.rsize.y;

        if (r > r0 + r1)
            return false;



        r = abs(Vector3.Dot(diff, target.forward));
        r0 = rsize.x * absC[0][2] + rsize.y * absC[1][2] + rsize.z * absC[2][2];
        r1 = target.rsize.z;

        if (r > r0 + r1)
            return false;



        if (existsParallelPair == true)
            return true;



        r = abs(d[2] * c[1][0] - d[1] * c[2][0]);
        r0 = rsize.y * absC[2][0] + rsize.z * absC[1][0];
        r1 = target.rsize.y * absC[0][2] + target.rsize.z * absC[0][1];
        if (r > r0 + r1)
            return false;



        r = abs(d[2] * c[1][1] - d[1] * c[2][1]);
        r0 = rsize.y * absC[2][1] + rsize.z * absC[1][1];
        r1 = target.rsize.x * absC[0][2] + target.rsize.z * absC[0][0];
        if (r > r0 + r1)
            return false;



        r = abs(d[2] * c[1][2] - d[1] * c[2][2]);
        r0 = rsize.y * absC[2][2] + rsize.z * absC[1][2];
        r1 = target.rsize.x * absC[0][1] + target.rsize.y * absC[0][0];
        if (r > r0 + r1)
            return false;



        r = abs(d[0] * c[2][0] - d[2] * c[0][0]);
        r0 = rsize.x * absC[2][0] + rsize.z * absC[0][0];
        r1 = target.rsize.y * absC[1][2] + target.rsize.z * absC[1][1];
        if (r > r0 + r1)
            return false;



        r = abs(d[0] * c[2][1] - d[2] * c[0][1]);
        r0 = rsize.x * absC[2][1] + rsize.z * absC[0][1];
        r1 = target.rsize.x * absC[1][2] + target.rsize.z * absC[1][0];
        if (r > r0 + r1)
            return false;



        r = abs(d[0] * c[2][2] - d[2] * c[0][2]);
        r0 = rsize.x * absC[2][2] + rsize.z * absC[0][2];
        r1 = target.rsize.x * absC[1][1] + target.rsize.y * absC[1][0];
        if (r > r0 + r1)
            return false;



        r = abs(d[1] * c[0][0] - d[0] * c[1][0]);
        r0 = rsize.x * absC[1][0] + rsize.y * absC[0][0];
        r1 = target.rsize.y * absC[2][2] + target.rsize.z * absC[2][1];
        if (r > r0 + r1)
            return false;



        r = abs(d[1] * c[0][1] - d[0] * c[1][1]);
        r0 = rsize.x * absC[1][1] + rsize.y * absC[0][1];
        r1 = target.rsize.x * absC[2][2] + target.rsize.z * absC[2][0];
        if (r > r0 + r1)
            return false;



        r = abs(d[1] * c[0][2] - d[0] * c[1][2]);
        r0 = rsize.x * absC[1][2] + rsize.y * absC[0][2];
        r1 = target.rsize.x * absC[2][1] + target.rsize.y * absC[2][0];
        if (r > r0 + r1)
            return false;



        return true;
    }
}
