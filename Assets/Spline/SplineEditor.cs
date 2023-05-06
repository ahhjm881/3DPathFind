#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(SplineFollower))]
public class SplineEditor : Editor
{

    public override void OnInspectorGUI()
    {
        SplineFollower hs = target as SplineFollower;
        if (hs == null) return;

        base.OnInspectorGUI();

        if(GUILayout.Button("Create Note"))
        {
            Undo.RecordObject(hs, "spline");
            SplineNote based = null;

            if(hs.notes.Count < 1)
            {
                based = new SplineNote()
                {
                    current = hs.transform.position,
                    preHandle = hs.transform.position ,
                    currentHandle = hs.transform.position ,
                    lookHandle = hs.transform.position ,
                    looksPower = 1f,
                    index = 0
                };

                hs.notes.Add(based);
            }
            else
            {
                based = hs.notes[hs.notes.Count - 1];

                SplineNote note = new SplineNote()
                {
                    current = based.current + Vector3.forward * 5f,
                    preHandle = based.current + Vector3.right * 5f,
                    currentHandle = based.current + Vector3.left * 5f,
                    lookHandle = hs.transform.position + Vector3.up * 5f,
                    looksPower = 1f,
                    index = based.index + 1
                };

                hs.notes.Add(note);
            }

        }

        if (GUILayout.Button("Refesh Position") && hs.notes.Count > 0)
        {
            Undo.RecordObject(hs, "spline_Refesh_Position");

            Vector3 dir = hs.notes[0].current - hs.transform.position;

            foreach (var item in hs.notes)
            {
                item.MoveCurrent(item.current - dir);
            }
        }

        if (GUILayout.Button("Trans Cardinal") && hs.notes.Count > 0)
        {
            Undo.RecordObject(hs, "spline_Trans_Cardinal");

            CustomSpline.TransCatmullRom(ref hs.notes);
        }

        serializedObject.ApplyModifiedProperties();
    }

    public void OnSceneGUI()
    {
        SplineFollower hs = target as SplineFollower;

        if (hs == null) return;
        if (hs.notes == null) return;

        Undo.RecordObject(hs, "spline");

        SplineNote before = null;
        foreach (var item in hs.notes)
        {
            if(before != null)
            {
                Handles.color = Color.white;
                for (int i = 0; i < hs.iteration; i++)
                {
                    Handles.DrawLine(
                        CustomSpline.MoveToWard(before, item, i / (float)hs.iteration, hs.type, hs.tension),
                        CustomSpline.MoveToWard(before, item, (i + 1) / (float)hs.iteration, hs.type, hs.tension)
                        );
                }
            }
            else
            {
                before = item;
                continue;
            }

            before = item;
            
            if(hs.drawHandles)
            {
                float size = 0.25f;
                item.MoveCurrent(Handles.PositionHandle(item.current, Quaternion.identity));
                Handles.color = Color.white;
                Handles.SphereHandleCap(0, item.current, Quaternion.identity, size, EventType.Repaint);

                if(hs.type == SplineType.CUBIC || hs.type == SplineType.Hermite)
                {
                    if(hs.type != SplineType.Hermite)
                        item.preHandle = Handles.PositionHandle(item.preHandle, Quaternion.identity);

                    LineHandle(item.current, item.preHandle, size, Color.red);

                }

                if(hs.type == SplineType.Hermite)
                    item.U = Handles.PositionHandle(item.currentHandle, Quaternion.identity);
                else
                    item.currentHandle = Handles.PositionHandle(item.currentHandle, Quaternion.identity);

                LineHandle(item.current, item.currentHandle, size, Color.red);

                if(item.useLooksHandle)
                {
                    item.lookHandle = Handles.PositionHandle(item.lookHandle, Quaternion.identity);
                    LineHandle(item.current, item.lookHandle, size, Color.blue);
                }
            }

        }

        serializedObject.ApplyModifiedProperties();
    }

    private void LineHandle(Vector3 current, Vector3 handle, float size = 0.25f, Color? color=null)
    {
        if (color == null)
            color = Color.white;

        Handles.color = color.Value;
        Handles.DrawLine(handle, current);
        Handles.SphereHandleCap(0, handle, Quaternion.identity, size, EventType.Repaint);
    }
}

#endif