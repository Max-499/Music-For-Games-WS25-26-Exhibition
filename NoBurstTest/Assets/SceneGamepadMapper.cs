using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Scene-owned, portable gamepad mappings + runtime rebinding + on-screen debug.
/// - No PlayerPrefs. Bindings are stored on this component (serialized) so they travel with the scene/prefab.
/// - In the Editor, successful rebinds mark the scene dirty so you can save.
/// - In a build, rebinds work for the current session but cannot persist to the scene asset (by design).
///
/// Supports only:
/// LeftStick, RightStick, Dpad, South/East/West/North buttons, Start. No triggers.
/// </summary>
public class SceneGamepadMapper : MonoBehaviour
{
    [Header("Overlay")]
    public bool showOverlay = true;
    public Key toggleOverlayKey = Key.F1;
    public bool allowStartToToggleOverlay = false;
    public float overlayRefreshHz = 20f;

    [Header("Rebind Panel")]
    public bool showRebindPanel = true;
    public Key toggleRebindPanelKey = Key.F2;

    [Header("Scene-Owned Binding Overrides (auto)")]
    [TextArea(3, 12)]
    [SerializeField] private string bindingOverridesJson = ""; // saved with scene/prefab

    [Header("Deadzone (optional)")]
    [Range(0f, 0.5f)] public float stickDeadzone = 0.12f;
    [Range(0f, 0.5f)] public float dpadDeadzone = 0.05f;

    // Public values
    public Vector2 LeftStick { get; private set; }
    public Vector2 RightStick { get; private set; }
    public Vector2 Dpad { get; private set; }

    public bool South { get; private set; } // physical south button
    public bool East  { get; private set; }
    public bool West  { get; private set; }
    public bool North { get; private set; }
    public bool StartPressedThisFrame { get; private set; }

    public event Action OnSouth;
    public event Action OnEast;
    public event Action OnWest;
    public event Action OnNorth;
    public event Action OnStart;

    InputAction leftStickAction;
    InputAction rightStickAction;
    InputAction dpadAction;

    InputAction southAction;
    InputAction eastAction;
    InputAction westAction;
    InputAction northAction;

    InputAction startAction;

    readonly List<InputAction> allActions = new();
    InputActionRebindingExtensions.RebindingOperation rebindOp;

    string rebindStatus = "";
    float nextOverlayUpdateTime = 0f;
    string overlayTextCached = "";

    struct Entry
    {
        public string label;
        public InputAction action;
        public bool isVector2;
    }
    Entry[] entries;

    void Awake()
    {
        // Build a tiny, self-contained action set (no asset required).
        leftStickAction  = new InputAction("LeftStick",  InputActionType.Value,  "<Gamepad>/leftStick");
        rightStickAction = new InputAction("RightStick", InputActionType.Value,  "<Gamepad>/rightStick");
        dpadAction       = new InputAction("Dpad",       InputActionType.Value,  "<Gamepad>/dpad");

        // Use physical positions, not printed letters.
        southAction      = new InputAction("South",      InputActionType.Button, "<Gamepad>/buttonSouth");
        eastAction       = new InputAction("East",       InputActionType.Button, "<Gamepad>/buttonEast");
        westAction       = new InputAction("West",       InputActionType.Button, "<Gamepad>/buttonWest");
        northAction      = new InputAction("North",      InputActionType.Button, "<Gamepad>/buttonNorth");

        startAction = new InputAction("Start", InputActionType.Button);
        startAction.AddBinding("<Gamepad>/menu");        // Switch Pro (+ button)
        startAction.AddBinding("<Gamepad>/startButton"); // Xbox-style pads
        startAction.AddBinding("<Gamepad>/start");       // legacy alias

        allActions.Add(leftStickAction);
        allActions.Add(rightStickAction);
        allActions.Add(dpadAction);
        allActions.Add(southAction);
        allActions.Add(eastAction);
        allActions.Add(westAction);
        allActions.Add(northAction);
        allActions.Add(startAction);

        entries = new[]
        {
            new Entry { label = "Left Stick",  action = leftStickAction,  isVector2 = true  },
            new Entry { label = "Right Stick", action = rightStickAction, isVector2 = true  },
            new Entry { label = "D-pad",       action = dpadAction,       isVector2 = true  },
            new Entry { label = "Button South", action = southAction,     isVector2 = false },
            new Entry { label = "Button East",  action = eastAction,      isVector2 = false },
            new Entry { label = "Button West",  action = westAction,      isVector2 = false },
            new Entry { label = "Button North", action = northAction,     isVector2 = false },
            new Entry { label = "Start",        action = startAction,     isVector2 = false },
        };

        // Load overrides stored in this component (scene-owned / prefab-owned).
        ApplyOverridesFromSerializedJson();
    }

    void OnEnable()
    {
        foreach (var a in allActions) a.Enable();

        southAction.performed += _ => OnSouth?.Invoke();
        eastAction.performed  += _ => OnEast?.Invoke();
        westAction.performed  += _ => OnWest?.Invoke();
        northAction.performed += _ => OnNorth?.Invoke();
        startAction.performed += _ => OnStart?.Invoke();
    }

    void OnDisable()
    {
        CancelRebind();

        southAction.performed -= _ => OnSouth?.Invoke();
        eastAction.performed  -= _ => OnEast?.Invoke();
        westAction.performed  -= _ => OnWest?.Invoke();
        northAction.performed -= _ => OnNorth?.Invoke();
        startAction.performed -= _ => OnStart?.Invoke();

        foreach (var a in allActions) a.Disable();
    }

    void Update()
    {
        // Poll values (robust across devices and avoids event timing surprises).
        LeftStick = ApplyDeadzone(leftStickAction.ReadValue<Vector2>(), stickDeadzone);
        RightStick = ApplyDeadzone(rightStickAction.ReadValue<Vector2>(), stickDeadzone);
        Dpad = ApplyDeadzone(dpadAction.ReadValue<Vector2>(), dpadDeadzone);

        South = southAction.IsPressed();
        East  = eastAction.IsPressed();
        West  = westAction.IsPressed();
        North = northAction.IsPressed();
        StartPressedThisFrame = startAction.WasPressedThisFrame();

        // Panel toggles
        if (Keyboard.current != null && Keyboard.current[toggleOverlayKey].wasPressedThisFrame)
            showOverlay = !showOverlay;

        if (Keyboard.current != null && Keyboard.current[toggleRebindPanelKey].wasPressedThisFrame)
            showRebindPanel = !showRebindPanel;

        if (allowStartToToggleOverlay && startAction.WasPressedThisFrame())
            showOverlay = !showOverlay;

        // Cache overlay string at limited rate to reduce allocations.
        if (Time.unscaledTime >= nextOverlayUpdateTime)
        {
            nextOverlayUpdateTime = Time.unscaledTime + (overlayRefreshHz <= 0 ? 0.05f : 1f / overlayRefreshHz);
            overlayTextCached = BuildOverlayText();
        }
    }

    static Vector2 ApplyDeadzone(Vector2 v, float dz)
    {
        if (dz <= 0f) return v;
        float mag = v.magnitude;
        if (mag < dz) return Vector2.zero;

        // Rescale so it ramps smoothly from 0 at dz to 1 at 1.
        float scaled = (mag - dz) / (1f - dz);
        return v.normalized * Mathf.Clamp01(scaled);
    }

    string BuildOverlayText()
    {
        var sb = new StringBuilder(700);

        sb.AppendLine("SceneGamepadMapper");
        sb.Append("Gamepads detected: ").AppendLine(Gamepad.all.Count.ToString());

        if (Gamepad.current != null)
        {
            sb.Append("Current: ").Append(Gamepad.current.displayName).Append(" | ")
              .Append(Gamepad.current.description.manufacturer).Append(" | ")
              .AppendLine(Gamepad.current.description.product);
        }
        else
        {
            sb.AppendLine("Current: (none)");
        }

        sb.AppendLine();
        sb.Append("LeftStick:  ").AppendLine(LeftStick.ToString("F3"));
        sb.Append("RightStick: ").AppendLine(RightStick.ToString("F3"));
        sb.Append("Dpad:       ").AppendLine(Dpad.ToString("F3"));
        sb.AppendLine();

        sb.Append("South: ").Append(South ? "1" : "0").Append("  ");
        sb.Append("East: ").Append(East ? "1" : "0").Append("  ");
        sb.Append("West: ").Append(West ? "1" : "0").Append("  ");
        sb.Append("North: ").AppendLine(North ? "1" : "0");
        sb.Append("Start: ").AppendLine(StartPressedThisFrame ? "pressed" : "0");

        sb.AppendLine();
        sb.AppendLine("Effective bindings:");
        foreach (var e in entries)
            sb.Append(" - ").Append(e.label).Append(": ").AppendLine(GetBindingDisplay(e.action));

        if (!string.IsNullOrWhiteSpace(rebindStatus))
        {
            sb.AppendLine();
            sb.AppendLine(rebindStatus);
        }

        sb.AppendLine();
        sb.AppendLine("Toggle overlay: F1 | Toggle rebind panel: F2");

        return sb.ToString();
    }

    static string GetBindingDisplay(InputAction action)
    {
        if (action.bindings.Count == 0) return "(none)";
        var b = action.bindings[0];
        return string.IsNullOrEmpty(b.effectivePath) ? b.path : b.effectivePath;
    }

    void OnGUI()
    {
        if (showOverlay)
        {
            var rect = new Rect(10, 10, 600, 460);
            GUI.Box(rect, "");
            GUI.Label(new Rect(20, 20, 580, 440), overlayTextCached);
        }

        if (showRebindPanel)
        {
            var rect = new Rect(10, 480, 600, 320);
            GUI.Box(rect, "Rebind Panel (scene-owned)");

            float y = 510;
            GUI.Label(new Rect(20, y, 560, 20),
                "Click Rebind then press a gamepad control. Esc cancels. Reset clears overrides stored on this component.");
            y += 28;

            for (int i = 0; i < entries.Length; i++)
            {
                var e = entries[i];

                GUI.Label(new Rect(20, y, 230, 22), e.label);
                GUI.Label(new Rect(250, y, 250, 22), GetBindingDisplay(e.action));

                if (GUI.Button(new Rect(510, y, 70, 22), "Rebind"))
                {
                    StartRebind(e.action, e.isVector2);
                }

                y += 26;
            }

            y += 6;

            if (GUI.Button(new Rect(20, y, 120, 24), "Reset All"))
                ResetAllBindings();

            if (GUI.Button(new Rect(150, y, 140, 24), "Copy JSON"))
                CopyOverridesJsonToClipboard();

            if (GUI.Button(new Rect(300, y, 140, 24), "Paste JSON"))
                PasteOverridesJsonFromClipboard();

            if (GUI.Button(new Rect(450, y, 130, 24), "Clear Status"))
                rebindStatus = "";
        }
    }

    public void StartRebind(InputAction action, bool isVector2)
    {
        CancelRebind();

        // Disable action during rebind to avoid accidental firing.
        action.Disable();

        rebindStatus = $"Rebinding {action.name}... press a control (Esc cancels).";

        rebindOp = action.PerformInteractiveRebinding(0)
            .WithCancelingThrough("<Keyboard>/escape")
            // Important: exclude trackpad/mouse so it cannot steal the binding.
            .WithControlsExcluding("<Mouse>")
            .WithControlsExcluding("<Pointer>")
            .WithControlsExcluding("<Touchscreen>")
            .OnMatchWaitForAnother(0.08f);

        if (isVector2) rebindOp.WithExpectedControlType("Vector2");
        else rebindOp.WithExpectedControlType("Button");

        rebindOp.OnCancel(op =>
        {
            rebindStatus = "Rebind cancelled.";
            CleanupAfterRebind(action);
        });

        rebindOp.OnComplete(op =>
        {
            rebindStatus = $"Rebind complete: {GetBindingDisplay(action)}";
            CleanupAfterRebind(action);

            // Save overrides into this component so they travel with the scene/prefab.
            UpdateSerializedOverridesFromActions();
            MarkDirtyForSave();
        });

        rebindOp.Start();
    }

    void CleanupAfterRebind(InputAction action)
    {
        rebindOp?.Dispose();
        rebindOp = null;
        action.Enable();
    }

    public void CancelRebind()
    {
        if (rebindOp != null)
        {
            rebindOp.Cancel();
            rebindOp.Dispose();
            rebindOp = null;
        }
        rebindStatus = "";
    }

    public void ResetAllBindings()
    {
        CancelRebind();

        foreach (var a in allActions)
            a.RemoveAllBindingOverrides();

        bindingOverridesJson = "";
        rebindStatus = "All bindings reset to defaults (scene-owned overrides cleared).";
        MarkDirtyForSave();
    }

    void ApplyOverridesFromSerializedJson()
    {
        if (string.IsNullOrWhiteSpace(bindingOverridesJson))
            return;

        try
        {
            foreach (var a in allActions)
                a.LoadBindingOverridesFromJson(bindingOverridesJson);

            rebindStatus = "Loaded scene-owned binding overrides.";
        }
        catch
        {
            rebindStatus = "Failed to load overrides JSON (ignored).";
        }
    }

    void UpdateSerializedOverridesFromActions()
    {
        // Save as one combined JSON blob for simplicity.
        // Any action can produce JSON containing overrides; we want a consistent blob, so we build from all actions.
        // Approach: create a temporary combined JSON by applying overrides to a new string builder format is internal,
        // but Unity supports concatenated JSON if it was produced from actions with the same set. Easiest is:
        // Pick one action and save; then load that blob into each action when applying.
        //
        // To ensure the blob includes all actions, we merge by:
        // - Start from empty blob.
        // - For each action, take its overrides json and load it into all actions cumulatively.
        // This yields a single blob that carries everything.
        //
        // Practically, Unity's SaveBindingOverridesAsJson includes only that action's overrides,
        // so we do a simple "compose then save" on a dedicated "collector" action:
        // We'll just serialize all overrides by temporarily saving and concatenating is not supported reliably.
        //
        // Therefore, the robust method is to store per-action JSON in a small wrapper.
        // We'll do that instead.

        bindingOverridesJson = BuildWrapperJsonFromActions();
    }

    // Wrapper: per action JSON in one serializable string.
    [Serializable]
    class OverridesWrapper
    {
        public List<ActionOverride> items = new();
    }

    [Serializable]
    class ActionOverride
    {
        public string actionName;
        public string json;
    }

    string BuildWrapperJsonFromActions()
    {
        var w = new OverridesWrapper();
        foreach (var a in allActions)
        {
            var j = a.SaveBindingOverridesAsJson();
            // Store even if empty so it's explicit and stable.
            w.items.Add(new ActionOverride { actionName = a.name, json = j });
        }
        return JsonUtility.ToJson(w, true);
    }

    void ApplyWrapperJsonToActions(string wrapperJson)
    {
        if (string.IsNullOrWhiteSpace(wrapperJson)) return;

        OverridesWrapper w = null;
        try { w = JsonUtility.FromJson<OverridesWrapper>(wrapperJson); }
        catch { return; }

        if (w == null || w.items == null) return;

        // Clear any existing overrides first, then apply the ones we have.
        foreach (var a in allActions) a.RemoveAllBindingOverrides();

        foreach (var item in w.items)
        {
            if (item == null || string.IsNullOrWhiteSpace(item.actionName)) continue;
            var action = allActions.Find(x => x.name == item.actionName);
            if (action == null) continue;

            if (!string.IsNullOrWhiteSpace(item.json))
            {
                try { action.LoadBindingOverridesFromJson(item.json); }
                catch { /* ignore malformed entry */ }
            }
        }
    }

    // Replace earlier direct LoadBindingOverridesFromJson with wrapper-based approach.
    void ApplyOverridesFromSerializedJson_UseWrapper()
    {
        if (string.IsNullOrWhiteSpace(bindingOverridesJson)) return;
        ApplyWrapperJsonToActions(bindingOverridesJson);
    }

    void CopyOverridesJsonToClipboard()
    {
        // Always copy what is currently stored (or rebuild if empty but overrides exist).
        if (string.IsNullOrWhiteSpace(bindingOverridesJson))
            bindingOverridesJson = BuildWrapperJsonFromActions();

        GUIUtility.systemCopyBuffer = bindingOverridesJson;
        rebindStatus = "Overrides JSON copied to clipboard.";
    }

    void PasteOverridesJsonFromClipboard()
    {
        var text = GUIUtility.systemCopyBuffer;
        if (string.IsNullOrWhiteSpace(text))
        {
            rebindStatus = "Clipboard is empty.";
            return;
        }

        // Try apply
        try
        {
            ApplyWrapperJsonToActions(text);
            bindingOverridesJson = text;
            rebindStatus = "Overrides JSON pasted from clipboard and applied.";
            MarkDirtyForSave();
        }
        catch
        {
            rebindStatus = "Clipboard text was not valid overrides JSON.";
        }
    }

    void MarkDirtyForSave()
    {
#if UNITY_EDITOR
        // Mark the component and scene as dirty so changes are saved with the scene/prefab.
        EditorUtility.SetDirty(this);

        // If this is a prefab instance, record modifications so overrides persist properly.
        PrefabUtility.RecordPrefabInstancePropertyModifications(this);

        if (!Application.isPlaying)
        {
            // In edit mode, ensure the scene knows it has changes.
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
        }
#endif
    }

    // Ensure wrapper-based load is used.
    void OnValidate()
    {
        // Keep this light; only in Editor.
#if UNITY_EDITOR
        // No automatic reload on validate to avoid fighting with play mode changes.
#endif
    }

    // Replace the old loader with wrapper-based loader.
    void Start()
    {
        // In case something else wrote bindingOverridesJson before Start, apply here.
        // (Awake already applied, but Start is a safe second chance if script order changes.)
        ApplyOverridesFromSerializedJson_UseWrapper();
    }
}