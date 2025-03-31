using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PathAdapter
{
    private PathAdapter() { }

    static public List<SplineNote> Path2Note(List<PathGraphNode> paths)
    {
        List<SplineNote> notes = new List<SplineNote>();

        for (int i = 0; i < paths.Count; i++)
        {
            SplineNote n = new SplineNote(paths[i].position, paths[i].position, paths[i].position, i);
            notes.Add(n);
        }

        return notes;
    }

    static public List<SplineNote> Path2NoteAuto(List<PathGraphNode> paths, float handleLength = 1f)
    {
        var notes = Path2Note(paths);

        CustomSpline.TransCatmullRom(ref notes, handleLength);

        return notes;
    }

    static public List<Vector3> NoteCapture(List<SplineNote> notes, float tension = 0.9f, int iteration=20)
    {
        var positions = new List<Vector3>();

        if (iteration < 1) iteration = 1;

        SplineNote before = null;
        foreach (var item in notes)
        {
            if (before == null)
            {
                before = item;
                continue;
            }

            for (int i = 0; i < iteration; i++)
            {
                positions.Add(CustomSpline.MoveToWard(before, item, (i + 1f) / iteration, SplineType.Hermite, tension));
            }
            before = item;
        }

        return positions;
    }
}
