using UnityEngine;

public class AnimatorDebugger : MonoBehaviour
{
    private Animator animator;
    private string lastStateName = "";

    void Start()
    {
        animator = GetComponent<Animator>();
        if (animator == null)
        {
            Debug.LogError("[AnimatorDebugger] No Animator found on this GameObject.");
        }
    }

    void Update()
    {
        if (animator == null) return;

        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        if (stateInfo.IsName(lastStateName)) return;

        lastStateName = stateInfo.IsName("") ? "<Unknown>" : stateInfo.shortNameHash.ToString();
        Debug.Log($"[AnimatorDebugger] Current state: {stateInfo.fullPathHash} ({stateInfo.normalizedTime:0.00})");
    }
}