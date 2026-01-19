using UnityEngine;
using UnityEngine.Video;
using UnityEngine.InputSystem;
using FMODUnity;
using System.Collections;
using TMPro;

public class DoorController : MonoBehaviour
{
    public VideoPlayer videoPlayer;
    public CanvasGroup instructionCanvasGroup;
    public StudioEventEmitter fmodEmitter;
    public TextMeshProUGUI promptText;

    [Header("Canvas Display Links")]
    public RectTransform videoDisplayRect; 
    public UnityEngine.UI.RawImage videoRawImage; 

    [Header("Artistic Chaos Settings")]
    public string fmodParameterName = "Progress";
    public string lowNoteParameterName = "LowNoteRandom";
    public string midLowNoteParameterName = "MidLowNoteRandom";
    public string midNoteParameterName = "MidNoteRandom";
    public string glitchSmoothParameter = "GlitchSmooth";
    [Range(0, 1)] public float glitchThreshold = 0.7f; 
    [Range(0, 1)] public float impossibleThreshold = 0.98f;
    
    [Space(10)]
    [Header("Glitch Intensity (Tame these!)")]
    [Range(0, 2)] public float maxSaturation = 1.1f; 
    [Range(0, 2)] public float maxBrightness = 1.0f; 
    [Range(0, 0.5f)] public float hueShiftRange = 0.05f; 
    [Range(0, 100)] public float jitterAmount = 0f; 

    private bool hasPlayedStartSFX = false;
    private bool isGlitching = false;
    private int currentButtonIndex = 0;
    private string[] buttonNames = { "A", "B", "X", "Y" };
    private int lastButtonIndex = -1;
    private long lastGlitchFrame = -1; 

    private Vector3 originalScale;
    private Color originalColor;
    private float currentRumbleValue = 0f; // Track rumble for smooth fading

    void Start()
    {
        if(videoPlayer != null) 
        {
            videoPlayer.Pause();
            videoPlayer.sendFrameReadyEvents = true; 
        }
        
        if (videoDisplayRect != null) originalScale = videoDisplayRect.localScale;
        if (videoRawImage != null) originalColor = videoRawImage.color;

        PickRandomButton();
    }

    void Update()
    {
        if (isGlitching) return;

        float progress = (float)videoPlayer.frame / (float)videoPlayer.frameCount;
        fmodEmitter.SetParameter(fmodParameterName, progress);

        // --- GLITCH TRIGGER LOGIC ---
        if (progress > glitchThreshold)
        {
            float t = Mathf.InverseLerp(glitchThreshold, impossibleThreshold, progress);
            float currentGlitchChance = Mathf.Lerp(0.05f, 0.8f, t);

            if (Random.value < currentGlitchChance)
            {
                StartCoroutine(GlitchSequence());
                return;
            }
        }

        bool isPressed = CheckCurrentButton();
        
        // --- SFX & RUMBLE PROGRESSION ---
        if (isPressed && !hasPlayedStartSFX)
        {
            FMODUnity.RuntimeManager.PlayOneShot("event:/StartSFX");
            hasPlayedStartSFX = true;
            Debug.Log("<color=yellow>SFX:</color> Action Button Identified!");
        }

        // Fading Rumble: If pressed, target is progress-based. If let go, target is 0.
        float targetRumble = isPressed ? Mathf.Lerp(0f, 0.25f, progress) : 0f;
        currentRumbleValue = Mathf.MoveTowards(currentRumbleValue, targetRumble, Time.deltaTime * 2f);
        SetRumble(currentRumbleValue, currentRumbleValue * 0.5f);

        // --- VIDEO PLAYBACK CONTROL ---
        if (isPressed && (ulong)videoPlayer.frame < videoPlayer.frameCount - 10)
        {
            if (!videoPlayer.isPlaying) videoPlayer.Play();
        }
        else
        {
            if (videoPlayer.isPlaying) videoPlayer.Pause();
        }

        instructionCanvasGroup.alpha = (progress < 0.05f) ? 1 : Mathf.Lerp(instructionCanvasGroup.alpha, 0, Time.deltaTime * 5);
    }

    bool CheckCurrentButton()
    {
        if (Gamepad.current == null) return Keyboard.current.spaceKey.isPressed;

        return currentButtonIndex switch
        {
            0 => Gamepad.current.buttonSouth.isPressed,
            1 => Gamepad.current.buttonEast.isPressed, 
            2 => Gamepad.current.buttonWest.isPressed, 
            3 => Gamepad.current.buttonNorth.isPressed,
            _ => false
        };
    }

    void PickRandomButton()
    {
        int newButtonIndex;
        do { newButtonIndex = Random.Range(0, 4); } 
        while (newButtonIndex == lastButtonIndex); 

        currentButtonIndex = newButtonIndex;
        lastButtonIndex = currentButtonIndex;

        if (promptText != null) 
            promptText.text = "Identify and hold Action Button to grow the bubble.\nWould be a shame if it were to burst!";
        
        hasPlayedStartSFX = false;
        currentRumbleValue = 0f; // Reset fade tracker
        Debug.Log($"<color=cyan>NEW TARGET:</color> Machine requires {buttonNames[currentButtonIndex]}");
        
        // ROLL FOR NEW LOW NOTE
        int randomLowNote = Random.Range(0, 3); // Results in 0, 1, or 2
        int randomMidLowNote = Random.Range(0, 3);
        int randomMidNote = Random.Range(0, 3);
        fmodEmitter.SetParameter(lowNoteParameterName, (float)randomLowNote);
        fmodEmitter.SetParameter(midLowNoteParameterName, (float)randomMidLowNote);
        fmodEmitter.SetParameter(midNoteParameterName, (float)randomMidNote);
    
        Debug.Log($"<color=orange>FMOD:</color> LowNoteRandom set to {randomLowNote}\n MidLowNoteRandom set to {randomMidLowNote}\n MidNoteRandom set to {randomMidNote}");
    }

    IEnumerator GlitchSequence()
    {
        isGlitching = true;
        Debug.Log("<color=red>!!! GLITCH STATE ACTIVE !!!</color>");
        
        videoPlayer.Pause();
        SetRumble(0, 0);
        currentRumbleValue = 0f;
        // --- SMOOTH SLIDE UP (0 to 1 in 0.2s) ---
        StartCoroutine(SmoothFMODParameter(glitchSmoothParameter, 1f, 0.2f));
        yield return new WaitForSeconds(1.2f); // The 2-second "Frozen" suspense
    
        int jumpCount = Random.Range(7, 10);
        long minD = (long)(videoPlayer.frameCount * 0.05f);
        //long curF = (long)(videoPlayer.frameCount * 0.8 / jumpCount);
        long curF = (long)(videoPlayer.frameCount * 0.45 / jumpCount);
    
        for (int i = 0; i < jumpCount; i++)
        {
            // 1. Alternating Rumble Burst
            if (i % 2 == 0) SetRumble(Random.Range(0.6f, 0.9f), 0f);
            else SetRumble(0f, Random.Range(0.6f, 0.9f));
    
            // 2. Logic: Find the frame
            long rFrame;
            int safety = 0;
            do {
                // CLAMP: Never allow the glitch to target the last 10 frames of the video
                long maxSafeFrame = (long)videoPlayer.frameCount - 10;
                rFrame = (long)Random.Range((jumpCount - i) * curF + minD, (jumpCount - i) * curF + curF + minD);
                rFrame = (long)Mathf.Clamp(rFrame, 0, maxSafeFrame); 
    
                safety++;
            } while (Mathf.Abs(rFrame - lastGlitchFrame) < minD && safety < 10);

            lastGlitchFrame = rFrame;
            videoPlayer.frame = rFrame;

// --- THE FIX: SAFETY TIMEOUT ---
            float seekTimeout = 0.15f; // 150ms max wait
            while (videoPlayer.frame != rFrame && seekTimeout > 0) 
            {
                seekTimeout -= Time.deltaTime;
                yield return null; // If it takes too long, we just move on
            }
    
            // 3. Visuals (Mirroring/Jitter)
            if (videoDisplayRect != null)
            {
                float flipX = (Random.value < 0.4f) ? -originalScale.x : originalScale.x;
                float flipY = (Random.value < 0.4f) ? -originalScale.y : originalScale.y;
                videoDisplayRect.localScale = new Vector3(flipX, flipY, originalScale.z);
                videoDisplayRect.anchoredPosition = new Vector2(Random.Range(-jitterAmount, jitterAmount), Random.Range(-jitterAmount, jitterAmount));
            }
    
            // 4. Visuals (Color) - The Golden AI Glitch
            if (videoRawImage != null)
            {
                float roll = Random.value;
                float h, s, v;
                Color.RGBToHSV(originalColor, out h, out s, out v);

                float safeBrightness = Mathf.Clamp(maxBrightness, 0.6f, 1.3f); // Keep it bright
                float safeSaturation = Mathf.Clamp(maxSaturation, 0.5f, 1.5f); // Keep it vivid

                // Target Yellow Hue is ~0.15. We'll allow a "Warm Zone" between 0.05 (Red/Orange) and 0.18 (Yellow)
                float goldenHue = Random.Range(0.13f, 0.18f);

                if (roll < 0.3f) 
                {
                    // MODE A: Sepia-Tone (The "Archive" look)
                    // High saturation yellow/orange but with lower brightness
                    videoRawImage.color = Color.HSVToRGB(0.12f, 0.8f, 0.7f);
                }
                else if (roll < 0.6f) 
                {
                    // MODE B: The "Latent Shift"
                    // Blend original hue 20% with Golden Hue 80%
                    float finalH = Mathf.Lerp(h, goldenHue, 0.8f);
                    videoRawImage.color = Color.HSVToRGB(finalH, safeSaturation, safeBrightness);
                }
                else if (roll < 0.9f)
                {
                    // MODE C: Overexposed Yellow
                    // Force pure yellow-gold
                    videoRawImage.color = Color.HSVToRGB(0.15f, 1.2f, 1.2f);
                }
                else 
                {
                    // MODE D: High-Key Contrast
                    // Instead of inverting to blue, we invert and then shift BACK to yellow
                    videoRawImage.color = Color.HSVToRGB(goldenHue, 0.4f, 1.5f);
                }
            }
    
            fmodEmitter.SetParameter(fmodParameterName, (float)videoPlayer.frame / videoPlayer.frameCount);
            
            // 5. Short Burst Duration
            yield return new WaitForSeconds(0.03f); 
            SetRumble(0, 0); // Kill rumble so it's a "clunk" not a "buzz"
            yield return new WaitForSeconds(Random.Range(0.05f, 0.2f)); 
        }
    
        // --- RESET SEQUENCE ---
        videoPlayer.Stop(); // Force the buffer to clear
        videoPlayer.frame = 0;
        videoPlayer.Prepare(); // Re-arm the player
        float resetTimeout = 0.2f;
        while (videoPlayer.frame != 0 && resetTimeout > 0) 
        {
            resetTimeout -= Time.deltaTime;
            yield return null;
        }
        videoPlayer.Pause();
        
        if (videoDisplayRect != null) {
            videoDisplayRect.localScale = originalScale;
            videoDisplayRect.anchoredPosition = Vector2.zero;
        }
        if (videoRawImage != null) videoRawImage.color = originalColor;
    
        yield return StartCoroutine(SmoothFMODParameter(glitchSmoothParameter, 0f, 0.2f));
        
        fmodEmitter.SetParameter(fmodParameterName, 0f);
        fmodEmitter.SetParameter("Glitch", 0f);
        
        SetRumble(0, 0);
        PickRandomButton(); // THIS CALLS THE NEW LOW NOTE LOGIC
        instructionCanvasGroup.alpha = 1f;
        isGlitching = false;
        Debug.Log("<color=green>GLITCH STATE DEACTIVATED.</color>");
    }
    
    void SetRumble(float low, float high)
    {
        if (Gamepad.current != null)
        {
            Gamepad.current.SetMotorSpeeds(low, high);
        }
    }
    
    IEnumerator SmoothFMODParameter(string paramName, float targetValue, float duration)
    {
        float startValue;
        // Get the current value from the emitter so the transition is seamless
        fmodEmitter.EventInstance.getParameterByName(paramName, out startValue);
    
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float newValue = Mathf.Lerp(startValue, targetValue, elapsed / duration);
            fmodEmitter.SetParameter(paramName, newValue);
            yield return null;
        }
        fmodEmitter.SetParameter(paramName, targetValue);
    }
    
}

