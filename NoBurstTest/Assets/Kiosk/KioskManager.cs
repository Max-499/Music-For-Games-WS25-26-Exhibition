// Assets/Kiosk/KioskManager.cs
// New Input System, event-driven activity detection via PlayerInput.onActionTriggered
// Kiosk owns per-scene inactivity policies (no per-scene SceneInactivitySettings GO)

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;



public class KioskManager : MonoBehaviour
{
    public static KioskManager Instance { get; private set; }

    [Header("Scenes")]
    public string kioskSceneName = "Kiosk";
    public string attractSceneName = "Attract";
    public List<string> contentScenes = new List<string>();

    [Header("Boot")]
    public bool autoStartFromKiosk = true;

    [Header("Play mode")]
    public KioskPlayMode playMode = KioskPlayMode.Shuffle;

    [Header("Per-scene inactivity policies (configured here)")]
    public KioskScenePolicy defaultPolicy = new KioskScenePolicy { enableSoft = false, enableHard = false };
    public List<KioskScenePolicy> scenePolicies = new List<KioskScenePolicy>();

    [Header("Input (New Input System)")]
    public PlayerInput playerInput;

    [Tooltip("Action name that starts the experience from Attract.")]
    public string startActionName = "Start";

    [Tooltip("Actions that count as activity while in content scenes (exact action names).")]
    public List<string> activityActionNames = new List<string>
    {
        "Start", "South", "East", "West", "North", "LeftStick", "RightStick", "Dpad"
    };

    [Header("Vector2 activity filtering")]
    [Tooltip("Deadzone magnitude for Vector2 actions (sticks/dpad). Activity is edge-triggered across this threshold.")]
    public float vector2Deadzone = 0.35f;

    [Header("Grace window")]
    [Min(0f)] public float inputGraceSeconds = 0.35f;

    [Header("Debug")]
    public bool debugLogs = true;
    public bool debugIdleOncePerSecond = true;
    public bool debugActionResets = false;

    float lastInputTime;
    float ignoreInputUntil;
    float nextPrintTime;

    int linearIndex = -1;
    readonly List<int> shuffleBag = new List<int>();
    readonly System.Random rng = new System.Random();

    // Edge-triggered memory for Vector2 actions so held sticks don't reset forever
    readonly Dictionary<string, bool> vec2WasActive = new Dictionary<string, bool>();

    bool InAttract => SceneManager.GetActiveScene().name == attractSceneName;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        BuildShuffleBag();
        ResetInputTimer("boot");
        ApplyGraceWindow();
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;

        if (!playerInput)
            playerInput = FindObjectOfType<PlayerInput>(); // compatible across Unity versions

        if (playerInput)
            playerInput.onActionTriggered += OnActionTriggered;
        else if (debugLogs)
            Debug.LogWarning("[KIOSK] No PlayerInput found. Activity will not be detected via actions.");
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;

        if (playerInput)
            playerInput.onActionTriggered -= OnActionTriggered;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ResetInputTimer("sceneLoaded");
        ApplyGraceWindow();
        nextPrintTime = 0f;

        if (debugLogs)
            Debug.Log($"[KIOSK] Loaded scene: {scene.name} | Mode={(InAttract ? "Attract" : "Content")}");

        // Boot behavior: immediately leave the Kiosk scene
        if (autoStartFromKiosk && scene.name == kioskSceneName)
            StartCoroutine(LoadNextFrameContent());
    }

    IEnumerator LoadNextFrameContent()
    {
        yield return null; // wait one frame so PlayerInput/objects initialize cleanly
        LoadNextContentScene();
    }

    void ApplyGraceWindow()
    {
        ignoreInputUntil = Time.unscaledTime + inputGraceSeconds;
    }

    bool InGraceWindow()
    {
        return Time.unscaledTime < ignoreInputUntil;
    }

    void ResetInputTimer(string reason)
    {
        lastInputTime = Time.unscaledTime;
        if (debugLogs && debugActionResets)
            Debug.Log($"[KIOSK] Reset timer ({reason}) in scene '{SceneManager.GetActiveScene().name}'");
    }

    KioskScenePolicy GetPolicyForActiveScene()
    {
        string active = SceneManager.GetActiveScene().name;

        for (int i = 0; i < scenePolicies.Count; i++)
        {
            var p = scenePolicies[i];
            if (p != null && p.sceneName == active)
                return p;
        }

        return defaultPolicy;
    }

    bool IsActivityAction(string actionName)
    {
        for (int i = 0; i < activityActionNames.Count; i++)
            if (activityActionNames[i] == actionName) return true;
        return false;
    }

    void OnActionTriggered(InputAction.CallbackContext ctx)
    {
        if (InGraceWindow()) return;

        string aName = ctx.action.name;

        // Attract: ONLY Start leaves Attract. Everything else ignored.
        if (InAttract)
        {
            if (ctx.performed && aName == startActionName)
            {
                if (debugLogs) Debug.Log("[KIOSK] Start in Attract -> load next content");
                LoadNextContentScene();
            }
            return;
        }

        // Content: only listed actions count as activity
        if (!IsActivityAction(aName))
            return;

        // Button actions: count only when performed
        if (ctx.action.type == InputActionType.Button)
        {
            if (ctx.performed)
                ResetInputTimer($"action:{aName}");
            return;
        }

        // Vector2 actions: count only on edge crossing deadzone
        if (ctx.performed && ctx.action.expectedControlType == "Vector2")
        {
            Vector2 v = ctx.ReadValue<Vector2>();
            bool activeNow = v.magnitude >= vector2Deadzone;

            bool wasActive = false;
            vec2WasActive.TryGetValue(aName, out wasActive);

            if (activeNow && !wasActive)
                ResetInputTimer($"vec2-edge:{aName}");

            vec2WasActive[aName] = activeNow;
        }
    }

    void Update()
    {
        // Attract never auto-advances and ignores inactivity timers.
        if (InAttract)
            return;

        var p = GetPolicyForActiveScene();

        float soft = (p != null && p.enableSoft) ? p.softSeconds : 0f;
        float hard = (p != null && p.enableHard) ? p.hardSeconds : 0f;

        // Ensure hard > soft if both enabled
        if (hard > 0f && soft > 0f && hard <= soft) hard = soft + 1f;

        float idle = Time.unscaledTime - lastInputTime;

        if (debugLogs && debugIdleOncePerSecond && Time.unscaledTime >= nextPrintTime)
        {
            nextPrintTime = Time.unscaledTime + 1f;

            string s = (soft > 0f) ? $"Soft={soft:0.0}s" : "Soft=OFF";
            string h = (hard > 0f) ? $"Hard={hard:0.0}s" : "Hard=OFF";

            Debug.Log($"[KIOSK] Scene='{SceneManager.GetActiveScene().name}' | Idle={idle:0.0}s | {s} | {h}");
        }

        if (hard > 0f && idle >= hard)
        {
            if (debugLogs) Debug.Log($"[KIOSK] HARD timeout (idle={idle:0.0}s >= {hard:0.0}s) -> Attract");
            LoadAttract();
            return;
        }

        if (soft > 0f && idle >= soft)
        {
            if (debugLogs) Debug.Log($"[KIOSK] SOFT timeout (idle={idle:0.0}s >= {soft:0.0}s) -> Next content");
            LoadNextContentScene();
            return;
        }
    }

    void BuildShuffleBag()
    {
        shuffleBag.Clear();
        for (int i = 0; i < contentScenes.Count; i++)
            shuffleBag.Add(i);
        Shuffle(shuffleBag);
    }

    void Shuffle(List<int> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    int NextIndex()
    {
        int n = contentScenes.Count;
        if (n == 0) return -1;

        switch (playMode)
        {
            case KioskPlayMode.Linear:
                linearIndex = (linearIndex + 1) % n;
                return linearIndex;

            case KioskPlayMode.Random:
                return rng.Next(0, n);

            case KioskPlayMode.Shuffle:
                if (shuffleBag.Count == 0) BuildShuffleBag();
                int idx = shuffleBag[0];
                shuffleBag.RemoveAt(0);
                return idx;

            default:
                return 0;
        }
    }

    public void LoadNextContentScene()
    {
        int idx = NextIndex();
        if (idx < 0)
        {
            if (debugLogs) Debug.LogWarning("[KIOSK] No content scenes configured.");
            return;
        }

        string next = contentScenes[idx];

        if (next == attractSceneName)
        {
            if (debugLogs) Debug.LogWarning("[KIOSK] Attract scene was in contentScenes list; skipping.");
            LoadNextContentScene();
            return;
        }

        if (debugLogs) Debug.Log($"[KIOSK] Loading content scene '{next}'");
        SceneManager.LoadScene(next);
    }

    public void LoadAttract()
    {
        if (InAttract) return; // desert cannot go to desert
        if (debugLogs) Debug.Log($"[KIOSK] Loading attract scene '{attractSceneName}'");
        SceneManager.LoadScene(attractSceneName);
    }

    // Optional: allow content scenes to request exits via kiosk
    public void RequestExitToNextContent() => LoadNextContentScene();
    public void RequestExitToAttract() => LoadAttract();
}