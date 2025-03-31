using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SplineNote
{
    public Vector3 current;

    public Vector3 preHandle;
    public Vector3 currentHandle;

    public Vector3 lookHandle;
    public bool useLooksHandle;

    [Range(0.1f, 10f)]
    public float looksPower;

    public Vector3 U
    {
        get { return 3 * (currentHandle - current); }
        set { currentHandle = value; preHandle = current + (current - currentHandle); }
    }

    public Vector3 V
    {
        get { return 3 * (current - preHandle); }
    }


    public int index;

    public SplineNote(Vector3 _current, Vector3 _preHandle, Vector3 _nextHandle, int _index)
    {
        LazyInitialize(_current, _preHandle, _nextHandle, _index);
    }

    public SplineNote(Vector3 _current, Vector3 _U, int _index)
    {
        current = _current;

        U = _U;

        index = _index;
    }

    public SplineNote(Vector3 _current, int _index)
    {
        current = _current;
        index = _index;
    }

    public SplineNote() { }

    public void LazyInitialize(Vector3 _current, Vector3 _preHandle, Vector3 _nextHandle, int _index)
    {
        current = _current;

        this.preHandle = _preHandle;
        this.currentHandle = _nextHandle;

        index = _index;
    }

    public void MoveCurrent(Vector3 _pos)
    {
        preHandle = preHandle + (_pos - current);
        currentHandle = currentHandle + (_pos - current);
        lookHandle = lookHandle + (_pos - current);
        current = _pos;
    }
}

public enum SplineType
{
    QUAD,
    CUBIC,
    Hermite,
}
