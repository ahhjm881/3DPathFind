using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

// Sets
#region
[System.Serializable]
public struct PlayerKeySets
{
    public KeyCode front, back, left, right;
    public KeyCode up, down;
}

[System.Serializable]
public struct TagContainerSet
{
    public string group;
    public List<int> tags;
}

public struct OctNodeData
{
    public Vector3 pos;
    public Vector3 size;

    public OctNodeData(Vector3 p, Vector3 s) { pos = p; size = s; }
}
#endregion


// Enum
#region
[System.Serializable]
public enum AStarMessage
{
    None = 0,
    TimeOut = 1,
    Complete = 2,
    Failed = 3
}
public enum CubeDirection : int
{
    Front,
    Right,
    Back,
    Left,
    Up,
    Down,
    FrontLeftUp,
    FrontRightUp,
    FrontLeft,
    FrontRight,
    FrontLeftDown,
    FrontRightDown,
    FrontUp,
    FrontDown,
    LeftUp,
    RightUp,
    LeftDown,
    RightDown,
    BackLeftUp,
    BackRightUp,
    BackLeft,
    BackRight,
    BackLeftDown,
    BackRightDown,
    BackUp,
    BackDown
}
#endregion