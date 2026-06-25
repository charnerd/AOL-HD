using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class AudioToggle : MonoBehaviour
{
    [Header("Audio Settings")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private Button toggleButton;
    [SerializeField] private Sprite soundOnIcon;
    [SerializeField] private Sprite soundOffIcon;
    [SerializeField] private Image buttonImage;
    
    [Header("Optional Settings")]
    [SerializeField] private bool startMuted = false;
    [SerializeField] private Text statusText;
    
    private bool isSoundOn = true;
    private Coroutine fadeCoroutine;
    private float previousVolume = 1f;
    
    void Start()
    {
        // Set initial state based on startMuted
        isSoundOn = !startMuted;
        
        // Ensure audio source exists
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                Debug.LogError("No AudioSource found! Please assign one in the Inspector.");
                return;
            }
        }
        
        // Store initial volume
        previousVolume = audioSource.volume;
        
        // Start audio based on initial state
        if (isSoundOn)
        {
            EnableSound();
        }
        else
        {
            DisableSound();
        }
        
        UpdateButtonVisuals();
        UpdateStatusText();
        
        // Add listener to the button
        if (toggleButton != null)
        {
            toggleButton.onClick.AddListener(ToggleSound);
        }
        else
        {
            // Try to get button component if not assigned
            toggleButton = GetComponent<Button>();
            if (toggleButton != null)
            {
                toggleButton.onClick.AddListener(ToggleSound);
            }
            else
            {
                Debug.LogWarning("No Button assigned or found! Please assign a Button.");
            }
        }
        
        // If using a Toggle instead of Button
        Toggle toggle = GetComponent<Toggle>();
        if (toggle != null)
        {
            toggle.onValueChanged.AddListener(OnToggleValueChanged);
        }
    }
    
    void ToggleSound()
    {
        isSoundOn = !isSoundOn;
        
        if (isSoundOn)
        {
            EnableSound();
        }
        else
        {
            DisableSound();
        }
        
        UpdateButtonVisuals();
        UpdateStatusText();
    }
    
    void EnableSound()
    {
        if (audioSource != null)
        {
            // Unmute
            audioSource.mute = false;
            
            // IMPORTANT: Resume playing if it was stopped
            if (!audioSource.isPlaying)
            {
                audioSource.Play();
            }
            
            // Restore previous volume
            audioSource.volume = previousVolume;
            
            // Optional: Fade in sound
            if (fadeCoroutine != null)
            {
                StopCoroutine(fadeCoroutine);
            }
            fadeCoroutine = StartCoroutine(FadeVolume(0f, previousVolume, 0.5f));
        }
        
        Debug.Log("Sound Enabled");
    }
    
    void DisableSound()
    {
        if (audioSource != null)
        {
            // Store current volume before muting
            previousVolume = audioSource.volume;
            
            // Optional: Fade out sound
            if (fadeCoroutine != null)
            {
                StopCoroutine(fadeCoroutine);
            }
            fadeCoroutine = StartCoroutine(FadeVolume(audioSource.volume, 0f, 0.5f, () => {
                audioSource.mute = true;
                // IMPORTANT: Don't stop the audio, just mute it
                // This way it will resume from the same position when unmuted
            }));
        }
        
        Debug.Log("Sound Disabled");
    }
    
    IEnumerator FadeVolume(float startVolume, float endVolume, float duration, System.Action onComplete = null)
    {
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            if (audioSource != null)
            {
                audioSource.volume = Mathf.Lerp(startVolume, endVolume, t);
            }
            
            yield return null;
        }
        
        if (audioSource != null)
        {
            audioSource.volume = endVolume;
        }
        
        if (onComplete != null)
        {
            onComplete.Invoke();
        }
    }
    
    // For Toggle component support
    void OnToggleValueChanged(bool value)
    {
        isSoundOn = value;
        
        if (isSoundOn)
        {
            EnableSound();
        }
        else
        {
            DisableSound();
        }
        
        UpdateButtonVisuals();
        UpdateStatusText();
    }
    
    void UpdateButtonVisuals()
    {
        // Update button image
        if (buttonImage != null)
        {
            if (isSoundOn && soundOnIcon != null)
            {
                buttonImage.sprite = soundOnIcon;
                buttonImage.color = Color.white;
            }
            else if (!isSoundOn && soundOffIcon != null)
            {
                buttonImage.sprite = soundOffIcon;
                buttonImage.color = Color.gray;
            }
        }
        else
        {
            // Try to get Image component if not assigned
            Image image = GetComponent<Image>();
            if (image != null)
            {
                if (isSoundOn && soundOnIcon != null)
                {
                    image.sprite = soundOnIcon;
                    image.color = Color.white;
                }
                else if (!isSoundOn && soundOffIcon != null)
                {
                    image.sprite = soundOffIcon;
                    image.color = Color.gray;
                }
            }
        }
    }
    
    void UpdateStatusText()
    {
        if (statusText != null)
        {
            statusText.text = isSoundOn ? "🔊 ON" : "🔇 OFF";
            statusText.color = isSoundOn ? Color.green : Color.red;
        }
    }
    
    // Public methods for external control
    public void Toggle()
    {
        ToggleSound();
    }
    
    public void SetSoundState(bool state)
    {
        isSoundOn = state;
        
        if (isSoundOn)
        {
            EnableSound();
        }
        else
        {
            DisableSound();
        }
        
        UpdateButtonVisuals();
        UpdateStatusText();
    }
    
    public bool IsSoundOn()
    {
        return isSoundOn;
    }
    
    public void SetVolume(float volume)
    {
        if (audioSource != null)
        {
            previousVolume = Mathf.Clamp01(volume);
            if (isSoundOn)
            {
                audioSource.volume = previousVolume;
            }
        }
    }
    
    // Clean up listeners when disabled
    void OnDisable()
    {
        if (toggleButton != null)
        {
            toggleButton.onClick.RemoveListener(ToggleSound);
        }
        
        Toggle toggle = GetComponent<Toggle>();
        if (toggle != null)
        {
            toggle.onValueChanged.RemoveListener(OnToggleValueChanged);
        }
    }
}