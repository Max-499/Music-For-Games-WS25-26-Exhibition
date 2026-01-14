using System;
using UnityEngine;

[Serializable]
public class KioskScenePolicy
{
    public string sceneName;

    [Header("Soft inactivity (advance content)")]
    public bool enableSoft = false;
    [Min(0f)] public float softSeconds = 30f;

    [Header("Hard inactivity (go to attract)")]
    public bool enableHard = false;
    [Min(0f)] public float hardSeconds = 120f;
}