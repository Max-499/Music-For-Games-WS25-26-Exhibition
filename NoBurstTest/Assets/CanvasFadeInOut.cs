using UnityEngine;

public class CanvasFadeInOut : MonoBehaviour
{
    [Header("Timing (seconds, can be zero)")]
    public float fadeInTime = 0f;
    public float holdTime = 2f;
    public float fadeOutTime = 1f;

    [Header("End behavior")]
    public bool disableOnEnd = true;

    [Header("Wiring")]
    public CanvasGroup canvasGroup;

    [Header("Debug")]
    public bool debugLogs = true;
    public bool debugOnScreen = true;
    public float logEverySeconds = 0.25f;

    float timer = 0f;
    float logTimer = 0f;

    enum State { FadeIn, Hold, FadeOut, Done }
    State state = State.FadeIn;

    void Awake()
    {
        if (!canvasGroup)
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (!canvasGroup) canvasGroup = GetComponentInChildren<CanvasGroup>(true);
        }

        if (!canvasGroup)
        {
            Debug.LogError("CanvasFadeInOut: No CanvasGroup found.");
            enabled = false;
            return;
        }
    }

    void OnEnable()
    {
        timer = 0f;
        logTimer = 0f;
        state = State.FadeIn;

        // Force correct starting alpha
        canvasGroup.alpha = (fadeInTime > 0f) ? 0f : 1f;

        if (debugLogs) Debug.Log($"[Fade] ENABLED state={state} alpha={canvasGroup.alpha}");
    }

    void Update()
    {
        timer += Time.unscaledDeltaTime;
        logTimer += Time.unscaledDeltaTime;

        switch (state)
        {
            case State.FadeIn:
                if (fadeInTime <= 0f)
                {
                    canvasGroup.alpha = 1f;
                    Next(State.Hold);
                }
                else
                {
                    float t = Mathf.Clamp01(timer / fadeInTime);
canvasGroup.alpha = Mathf.SmoothStep(0f, 1f, t);
                    if (timer >= fadeInTime) Next(State.Hold);
                }
                break;

            case State.Hold:
                if (holdTime <= 0f || timer >= holdTime) Next(State.FadeOut);
                break;

            case State.FadeOut:
                if (fadeOutTime <= 0f)
                {
                    canvasGroup.alpha = 0f;
                    Finish();
                }
                else
                {
                    canvasGroup.alpha = 1f - Mathf.Clamp01(timer / fadeOutTime);
                    if (timer >= fadeOutTime) Finish();
                }
                break;
        }

        // Throttled logging so the Console doesn't melt
        if (debugLogs && logEverySeconds > 0f && logTimer >= logEverySeconds)
        {
            logTimer = 0f;
            Debug.Log($"[Fade] state={state} timer={timer:F2} alpha={canvasGroup.alpha:F2} " +
                      $"unscaledDt={Time.unscaledDeltaTime:F4} timeScale={Time.timeScale:F2}");
        }
    }

    void Next(State next)
    {
        if (debugLogs) Debug.Log($"[Fade] {state} -> {next} (timer={timer:F2}, alpha={canvasGroup.alpha:F2})");
        state = next;
        timer = 0f;
    }

    void Finish()
    {
        state = State.Done;
        canvasGroup.alpha = 0f;

        if (debugLogs) Debug.Log("[Fade] DONE");

        if (disableOnEnd) gameObject.SetActive(false);
        else enabled = false;
    }

    void OnGUI()
    {
        if (!debugOnScreen) return;

        GUI.Label(new Rect(10, 10, 900, 60),
            $"Fade Debug | state={state}  timer={timer:F2}  alpha={canvasGroup.alpha:F2}\n" +
            $"unscaledTime={Time.unscaledTime:F2}  unscaledDt={Time.unscaledDeltaTime:F4}  timeScale={Time.timeScale:F2}");
    }
}