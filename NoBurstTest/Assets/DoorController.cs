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
        if (isPressed && (ulong)videoPlayer.frame < videoPlayer.frameCount - 5)
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
            promptText.text = "Identify and hold Action Button to grow the bubble.\nWould be a shame if it were to burst! ;)";
        
        hasPlayedStartSFX = false;
        currentRumbleValue = 0f; // Reset fade tracker
        Debug.Log($"<color=cyan>NEW TARGET:</color> Machine requires {buttonNames[currentButtonIndex]}");
    }

    IEnumerator GlitchSequence()
    {
        isGlitching = true;
        Debug.Log("<color=red>!!! GLITCH STATE ACTIVE !!!</color>");
        
        videoPlayer.Pause();
        fmodEmitter.SetParameter("Glitch", 1f);

        int jumpCount = Random.Range(4, 8);
        long minF = (long)(videoPlayer.frameCount * 0.15f);
        long maxF = (long)(videoPlayer.frameCount * 0.95f);
        long minD = (long)(videoPlayer.frameCount * 0.15f);

        for (int i = 0; i < jumpCount; i++)
        {
            long rFrame;
            int safety = 0;
            do {
                rFrame = (long)Random.Range(minF, maxF);
                safety++;
            } while (Mathf.Abs(rFrame - lastGlitchFrame) < minD && safety < 10);

            lastGlitchFrame = rFrame;
            videoPlayer.frame = rFrame;

            while (videoPlayer.frame != rFrame) yield return null;

            // --- STRONK ASYMMETRIC RUMBLE ---
            // Randomize left and right motors independently so it feels "broken"
            SetRumble(Random.Range(0.4f, 0.9f), Random.Range(0.4f, 0.9f));

            if (videoDisplayRect != null)
            {
                // 20% Chance for mirroring
                float flipX = (Random.value < 0.2f) ? -originalScale.x : originalScale.x;
                float flipY = (Random.value < 0.2f) ? -originalScale.y : originalScale.y;
                videoDisplayRect.localScale = new Vector3(flipX, flipY, originalScale.z);
                
                videoDisplayRect.anchoredPosition = new Vector2(Random.Range(-jitterAmount, jitterAmount), Random.Range(-jitterAmount, jitterAmount));
            }

            if (videoRawImage != null)
            {
                float h, s, v;
                Color.RGBToHSV(originalColor, out h, out s, out v);

                float roll = UnityEngine.Random.value;
                if (roll < 0.33f) 
                {
                    videoRawImage.color = Color.HSVToRGB(h, 0.4f, v);
                }
                else if (roll < 0.66f) 
                {
                    videoRawImage.color = Color.HSVToRGB(h, maxSaturation, maxBrightness);
                }
                else 
                {
                    float shiftedH = (h + UnityEngine.Random.Range(-hueShiftRange, hueShiftRange)) % 1f;
                    videoRawImage.color = Color.HSVToRGB(shiftedH, 1.0f, 1.0f);
                }
            }

            fmodEmitter.SetParameter(fmodParameterName, (float)videoPlayer.frame / videoPlayer.frameCount);
            Debug.Log($"Glitch Jump {i+1}/{jumpCount}");
            yield return new WaitForSeconds(Random.Range(0.06f, 0.2f));
        }

        // --- RESET ---
        videoPlayer.frame = 0;
        while (videoPlayer.frame != 0) yield return null;
        videoPlayer.Pause();
        
        if (videoDisplayRect != null) 
        {
            videoDisplayRect.localScale = originalScale;
            videoDisplayRect.anchoredPosition = Vector2.zero;
        }
        if (videoRawImage != null) videoRawImage.color = originalColor;

        fmodEmitter.SetParameter(fmodParameterName, 0f);
        fmodEmitter.SetParameter("Glitch", 0f);
        
        SetRumble(0, 0); // Stop rumble immediately
        PickRandomButton();
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
}