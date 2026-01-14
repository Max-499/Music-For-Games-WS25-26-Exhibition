using UnityEngine;

public enum SoftTimeoutAction { None, NextContent }
public enum HardTimeoutAction { None, Attract }

public class SceneInactivitySettings : MonoBehaviour
{
    [Header("Soft inactivity (advance content)")]
    public bool enableSoft = false;
    [Min(0f)] public float softSeconds = 30f;
    public SoftTimeoutAction softAction = SoftTimeoutAction.NextContent;

    [Header("Hard inactivity (go to attract)")]
    public bool enableHard = false;
    [Min(0f)] public float hardSeconds = 120f;
    public HardTimeoutAction hardAction = HardTimeoutAction.Attract;

    void OnValidate()
    {
        if (enableSoft && enableHard &&
            hardSeconds > 0f &&
            softSeconds > 0f &&
            hardSeconds <= softSeconds)
        {
            hardSeconds = softSeconds + 1f;
        }
    }
}