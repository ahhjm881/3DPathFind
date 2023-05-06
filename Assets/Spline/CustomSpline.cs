using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class CustomSpline : MonoBehaviour
{
    private CustomSpline() { }

    static public Vector3 MoveToWard(SplineNote n1, SplineNote n2, float _t, SplineType type = SplineType.CUBIC, float _tension = 0f)
    {
        switch(type)
        {
            case SplineType.QUAD:
                return Quad(n1, n2, _t);
            case SplineType.CUBIC:
                return Cubic(n1, n2, _t);
            case SplineType.Hermite:
                return Hermite(n1, n2, _t, _tension);
            default:
                return Quad(n1, n2, _t);
        }
    }

    static public Vector3 Cubic(SplineNote n1, SplineNote n2, float _t)
    {
        return Cubic(n1.current, n1.currentHandle, n2.preHandle, n2.current, _t);
    }

    static public Vector3 Cubic(Vector3 _current, Vector3 _currentHandle, Vector3 _preHandle, Vector3 _next, float _t)
    {
        Vector3 e = Vector3.Lerp(_current , _currentHandle, _t);
        Vector3 f = Vector3.Lerp(_currentHandle, _preHandle, _t);
        Vector3 g = Vector3.Lerp(_preHandle, _next, _t);
        Vector3 q = Vector3.Lerp(e, f, _t);
        Vector3 r = Vector3.Lerp(f, g, _t);
        Vector3 p = Vector3.Lerp(q, r, _t);

        return p;
    }

    static public Vector3 Quad(SplineNote n1, SplineNote n2, float _t)
    {
        return Quad(n1.current, n1.currentHandle, n2.current, _t);
    }

    static public Vector3 Quad(Vector3 _current, Vector3 _currentHandle, Vector3 _next, float _t)
    {
        Vector3 d = Vector3.Lerp(_current, _currentHandle, _t);
        Vector3 e = Vector3.Lerp(_currentHandle, _next, _t);
        Vector3 f = Vector3.Lerp(d, e, _t);

        return f;
    }

    static public Vector3 Hermite(SplineNote n1, SplineNote n2, float _t, float _tension = 0f)
    {
        return Cubic(
            n1.current,
            n1.current + n1.U * (1 - _tension),
            n2.current - n2.V * (1 - _tension),
            n2.current,
            _t
            );
    }

    static public void TransCatmullRom(ref List<SplineNote> notes, float handleLength = 1f)
    {
        if (notes.Count > 0)
        {
            notes[0].currentHandle = notes[0].current;
            notes[0].preHandle = notes[0].current;

            notes[notes.Count - 1].currentHandle = notes[notes.Count - 1].current;
            notes[notes.Count - 1].preHandle = notes[notes.Count - 1].current;
        }

        for (int i = 0; i < notes.Count; i++)
        {
            if (i + 2 >= notes.Count) break;

            notes[i + 1].U = notes[i + 1].current + (notes[i + 2].current - notes[i].current) * 0.5f * handleLength;
        }
    }
}
