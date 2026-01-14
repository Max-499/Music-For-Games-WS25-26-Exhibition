using System.Collections.Generic;
using UnityEngine;

public enum KioskPlayMode { Linear, Random, Shuffle }

[CreateAssetMenu(menuName = "Kiosk/Kiosk Config")]
public class KioskConfig : ScriptableObject
{
    [Header("Scenes")]
    public string attractScene = "Attract";
    public List<string> contentScenes = new List<string>();

    [Header("Play mode (set before build)")]
    public KioskPlayMode playMode = KioskPlayMode.Shuffle;

    [Header("Inactivity (seconds)")]
    [Min(0f)] public float softInactivitySeconds = 30f;  // advance to next content
    [Min(0f)] public float hardInactivitySeconds = 120f; // go to attract (must be > soft)

    [Header("Attract start key")]
    public KeyCode startKey = KeyCode.Return; // "Start" while in Attract
}