using UnityEngine;
using System.Collections.Generic;
using TMPro;
using System.Collections;
using UnityEngine.SceneManagement;

public class TimeHealthDrained : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float drainRate = 0.5f;
    [SerializeField] private float addHealth = 5f;

    [Header("Interaction Settings")]
    [SerializeField] private Transform detectionPoint;
    [SerializeField] private float detectionRadius = 1f;
    [SerializeField] private LayerMask detectionLayer;
    [SerializeField] private int maxInteractions = 3;

    [Header("Non-Water Settings")]
    [SerializeField] private LayerMask nonWaterLayer;
    [SerializeField] private float nonWaterHeal = 2f;
    [SerializeField] private int nonWaterMaxInteractions = 2;

    [Header("Score Layer Settings")]
    [SerializeField] private LayerMask scoreLayer; // Layer for objects that give score
    // Note: Score objects give NO healing, just score points

    [Header("Castle Layer Settings")]
    [SerializeField] private LayerMask castleLayer; // Layer for castle items
    [SerializeField] private float castleScore = 10000f; // Points for castle items

    [Header("UI Components")]
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private TextMeshProUGUI eventStatusText;
    [SerializeField] private UnityEngine.UI.Image uiImage;

    [Header("Color Feedback Settings")]
    [SerializeField] private Color healColor = Color.green;
    [SerializeField] private Color damageColor = Color.red;
    [SerializeField] private float interactionColorDuration = 4f;

    [Header("Random Event Settings")]
    [SerializeField] private float eventInterval = 40f;
    [SerializeField] private Color heatwaveColor = new Color(1f, 0.5f, 0f);
    [SerializeField] private Color thunderstormColor = new Color(0.5f, 0f, 1f);
    [SerializeField] private float heatwaveDrainMultiplier = 1.5f;
    [SerializeField] private float thunderstormHealPerSecond = 1.5f;

    [Header("Death Settings")]
    [SerializeField] private string gameOverSceneName = "GameOver";
    [SerializeField] private float deathDelay = 5f;

    [Header("Player Reference")]
    [SerializeField] private PlayerMovement playerMovement;

    [Header("Score Settings")]
    [SerializeField] private TextMeshProUGUI scoreText; // Display current score
    [SerializeField] private TextMeshProUGUI scoreCollectionText; // Separate text for score popups
    [SerializeField] private float scoreMultiplier = 2f; // Multiplier for collected objects
    [SerializeField] private float survivalScoreRate = 10f; // Points per second for survival
    [SerializeField] private float scoreMessageDuration = 2f;
    [SerializeField] private int scoreMultiplierInterval = 5; // Multiply score every 5 interactions
    
    // Score tracking variables
    private float totalScore = 0f;
    private float survivalTime = 0f;
    private int eventsSurvived = 0;
    private int objectsCollected = 0;
    private int castleItemsCollected = 0;
    private int currentObjectMultiplier = 1;
    private bool isScoreInitialized = false;
    private float scoreTimer = 0f; // For tracking survival score

    private float currentHealth;
    private bool uiNeedsUpdate = false;
    private Color defaultColor;
    private bool isColorActive = false;
    private Color currentActiveColor;
    private float eventTimer = 0f;
    private bool isEventActive = false;
    private bool isDrainPaused = false;
    private float currentDrainRateMultiplier = 1f;
    private string currentEventName = "";
    private float currentEventTimeLeft = 0f;
    private bool isEventTimeDisplayActive = false;
    private float interactionMessageTimer = 0f;
    private string interactionMessage = "";
    private bool areInteractionsBlocked = false;
    private bool isDead = false;
    
    // Separate text for score collection messages
    private float scoreMessageTimer = 0f;
    
    private Dictionary<GameObject, int> interactionCounts = new Dictionary<GameObject, int>();

    void Start()
    {
        currentHealth = maxHealth;
        isDead = false;
        isScoreInitialized = true;
        totalScore = 0f;
        survivalTime = 0f;
        scoreTimer = 0f;
        
        // Auto-find player movement if not assigned
        if (playerMovement == null)
        {
            playerMovement = FindObjectOfType<PlayerMovement>();
            if (playerMovement != null)
            {
                Debug.Log("PlayerMovement automatically found and assigned");
            }
            else
            {
                Debug.LogWarning("PlayerMovement not found in scene! Please assign it manually.");
            }
        }
        
        if (healthText != null)
        {
            defaultColor = healthText.color;
        }
        
        UpdateEventStatus("Waiting for next event...", false);
        
        Debug.Log("Health initialized to: " + currentHealth);
        UpdateHealthUI();
        UpdateScoreUI();
        
        eventTimer = eventInterval;
    }

    void Update()
    {
        if (isDead) return;
        
        // Track survival time for score
        survivalTime += Time.deltaTime;
        
        // Add survival score every second
        scoreTimer += Time.deltaTime;
        if (scoreTimer >= 1f) // Every second
        {
            scoreTimer = 0f;
            AddScore(survivalScoreRate, $"Survival (+{survivalScoreRate}/sec)");
        }
        
        // 1. Handle Passive Health Drain
        if (currentHealth > 0 && !isDrainPaused)
        {
            float effectiveDrainRate = drainRate * currentDrainRateMultiplier;
            currentHealth -= effectiveDrainRate * Time.deltaTime;
            
            if (currentHealth <= 0)
            {
                currentHealth = 0;
                OnPlayerDeath();
                return;
            }
            uiNeedsUpdate = true;
        }
        else if (currentHealth > 0 && isDrainPaused)
        {
            if (currentHealth <= 0)
            {
                currentHealth = 0;
                OnPlayerDeath();
                return;
            }
        }

        // 2. Handle Object Interaction
        if (Input.GetKeyDown(KeyCode.E) && !isDead)
        {
            GameObject detectedObject = DetectObject();
            if (detectedObject != null)
            {
                InteractWithObject(detectedObject);
            }
        }

        // 3. Handle interaction message timer
        if (interactionMessageTimer > 0)
        {
            interactionMessageTimer -= Time.deltaTime;
            if (interactionMessageTimer <= 0)
            {
                interactionMessage = "";
                UpdateEventStatus(currentEventName, isEventActive);
            }
        }

        // 4. Handle score collection message timer
        if (scoreMessageTimer > 0)
        {
            scoreMessageTimer -= Time.deltaTime;
            if (scoreMessageTimer <= 0 && scoreCollectionText != null)
            {
                scoreCollectionText.text = "";
            }
        }

        // 5. Update event status with timer
        if (isEventActive && isEventTimeDisplayActive && !isDead)
        {
            currentEventTimeLeft -= Time.deltaTime;
            if (currentEventTimeLeft <= 0)
            {
                currentEventTimeLeft = 0;
            }
            
            string eventDisplay = $"{currentEventName}: {Mathf.CeilToInt(currentEventTimeLeft)}s left";
            if (eventStatusText != null && interactionMessageTimer <= 0)
            {
                eventStatusText.text = eventDisplay;
            }
        }

        // 6. Update UI
        if (uiNeedsUpdate && !isDead)
        {
            UpdateHealthUI();
            uiNeedsUpdate = false;
        }

        // 7. Handle Random Events
        if (!isEventActive && !isDead)
        {
            eventTimer -= Time.deltaTime;
            if (eventTimer <= 0)
            {
                TriggerRandomEvent();
                eventTimer = eventInterval;
            }
            
            if (eventStatusText != null && !isEventActive && interactionMessageTimer <= 0)
            {
                eventStatusText.text = $"Next event in: {Mathf.CeilToInt(eventTimer)}s";
            }
        }
    }

    void UpdateEventStatus(string message, bool isEventActive)
    {
        currentEventName = message;
        this.isEventActive = isEventActive;
        
        if (eventStatusText != null && interactionMessageTimer <= 0 && !isDead)
        {
            if (isEventActive)
            {
                eventStatusText.text = $"{message}: {Mathf.CeilToInt(currentEventTimeLeft)}s left";
            }
            else
            {
                eventStatusText.text = message;
            }
        }
    }

    void ShowInteractionMessage(string message, float duration = 2f)
    {
        if (isDead) return;
        
        interactionMessage = message;
        interactionMessageTimer = duration;
        
        if (eventStatusText != null)
        {
            eventStatusText.text = message;
        }
    }

    void ShowScoreCollectionMessage(string message, float duration = 2f)
    {
        if (isDead || scoreCollectionText == null) return;
        
        scoreCollectionText.text = message;
        scoreMessageTimer = duration;
        Debug.Log($"Score collection message: {message}");
    }

    void TriggerRandomEvent()
    {
        if (isEventActive || isDead) return;
        
        int eventType = Random.Range(0, 2);
        isEventActive = true;
        
        switch (eventType)
        {
            case 0:
                StartCoroutine(ExtremeHeatwave());
                break;
            case 1:
                StartCoroutine(Thunderstorm());
                break;
        }
    }

    IEnumerator ExtremeHeatwave()
    {
        currentEventName = "HEATWAVE";
        currentEventTimeLeft = 10f;
        isEventTimeDisplayActive = true;
        // MODIFIED: Only block water and non-natural layer interactions
        areInteractionsBlocked = true;
        
        Debug.Log($"EXTREME HEATWAVE ACTIVATED! Water/Non-natural interactions blocked! Score layer interactions still allowed!");
        
        currentDrainRateMultiplier = heatwaveDrainMultiplier;
        float effectiveDrainRate = drainRate * currentDrainRateMultiplier;
        Debug.Log($"Current drain rate: {drainRate} -> {effectiveDrainRate} (multiplier: {currentDrainRateMultiplier}x)");
        
        SetEventColor(heatwaveColor);
        UpdateEventStatus("HEATWAVE", true);
        
        float elapsed = 0f;
        while (elapsed < 10f)
        {
            if (isDead) yield break;
            
            currentEventTimeLeft = 10f - elapsed;
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        // Event survived - add score
        eventsSurvived++;
        AddScore(100f, "Event Survived");
        
        currentDrainRateMultiplier = 1f;
        Debug.Log($"Heatwave ended. Drain rate restored to {drainRate} (multiplier: {currentDrainRateMultiplier}x)");
        
        ResetEventColor();
        isEventActive = false;
        isEventTimeDisplayActive = false;
        areInteractionsBlocked = false;
        currentEventName = "";
        
        ShowInteractionMessage("Event ended! +100 Score!", 2f);
    }

    IEnumerator Thunderstorm()
    {
        currentEventName = "THUNDERSTORM";
        currentEventTimeLeft = 8f;
        isEventTimeDisplayActive = true;
        // MODIFIED: Only block water and non-natural layer interactions
        areInteractionsBlocked = true;
        
        Debug.Log($"THUNDERSTORM ACTIVATED! Water/Non-natural interactions blocked! Score layer interactions still allowed!");
        
        isDrainPaused = true;
        Debug.Log("Drain rate paused during thunderstorm");
        
        SetEventColor(thunderstormColor);
        UpdateEventStatus("THUNDERSTORM", true);
        
        float duration = 8f;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            if (isDead) yield break;
            
            currentHealth += thunderstormHealPerSecond * Time.deltaTime;
            if (currentHealth > maxHealth)
            {
                currentHealth = maxHealth;
            }
            uiNeedsUpdate = true;
            
            currentEventTimeLeft = duration - elapsed;
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        // Event survived - add score
        eventsSurvived++;
        AddScore(100f, "Event Survived");
        
        isDrainPaused = false;
        Debug.Log($"Thunderstorm ended. Drain rate resumed to {drainRate} (multiplier: {currentDrainRateMultiplier}x)");
        
        ResetEventColor();
        isEventActive = false;
        isEventTimeDisplayActive = false;
        areInteractionsBlocked = false;
        currentEventName = "";
        
        ShowInteractionMessage("Event ended! +100 Score!", 2f);
    }

    void SetEventColor(Color eventColor)
    {
        if (isDead) return;
        
        if (healthText != null)
        {
            if (defaultColor == default(Color))
            {
                defaultColor = healthText.color;
            }
            healthText.color = eventColor;
            currentActiveColor = eventColor;
            isColorActive = true;
        }
        
        if (uiImage != null)
        {
            if (defaultColor == default(Color))
            {
                defaultColor = uiImage.color;
            }
            uiImage.color = eventColor;
        }
        
        Debug.Log($"Event color set to: {eventColor} (will last for entire event duration)");
    }

    void ResetEventColor()
    {
        if (isDead) return;
        
        if (healthText != null)
        {
            healthText.color = defaultColor;
        }
        if (uiImage != null)
        {
            uiImage.color = defaultColor;
        }
        isColorActive = false;
        Debug.Log("Event color reset to default");
    }

    void SetInteractionColor(Color newColor)
    {
        if (isDead) return;
        
        if (healthText != null)
        {
            if (defaultColor == default(Color))
            {
                defaultColor = healthText.color;
            }
            
            healthText.color = newColor;
            currentActiveColor = newColor;
            isColorActive = true;
            
            StartCoroutine(ResetInteractionColorAfterDelay(interactionColorDuration));
            Debug.Log($"Interaction color set to: {newColor} for {interactionColorDuration} seconds");
        }
        
        if (uiImage != null)
        {
            if (defaultColor == default(Color))
            {
                defaultColor = uiImage.color;
            }
            uiImage.color = newColor;
        }
    }

    IEnumerator ResetInteractionColorAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        if (!isEventActive && !isDead)
        {
            if (healthText != null)
            {
                healthText.color = defaultColor;
            }
            if (uiImage != null)
            {
                uiImage.color = defaultColor;
            }
            isColorActive = false;
            Debug.Log("Interaction color reverted to default");
        }
        else if (isEventActive && !isDead)
        {
            Debug.Log("Event is active, keeping event color");
        }
    }

    void InteractWithObject(GameObject obj)
    {
        if (isDead) return;
        
        // MODIFIED: Check if interaction is blocked by event
        // Allow score layer interactions even during events
        bool isScoreLayer = ((1 << obj.layer) & scoreLayer) != 0;
        bool isCastleLayer = ((1 << obj.layer) & castleLayer) != 0;
        
        // Only block if not on score layer or castle layer
        if (areInteractionsBlocked && !isScoreLayer && !isCastleLayer)
        {
            ShowInteractionMessage("Cannot interact during event!", 2f);
            Debug.Log($"Interaction blocked during event: {obj.name} (Layer: {LayerMask.LayerToName(obj.layer)})");
            return;
        }

        if (!interactionCounts.ContainsKey(obj))
        {
            interactionCounts[obj] = 0;
        }

        interactionCounts[obj]++;
        
        // Check if object is on the Castle Layer
        if (isCastleLayer)
        {
            ShowInteractionMessage("Castle Treasure!", 2f);
            Debug.Log($"Castle item collected: {obj.name}");
            
            SetInteractionColor(healColor);
            
            // Award 10,000 points for castle item
            castleItemsCollected++;
            float castleScoreAmount = castleScore;
            AddScore(castleScoreAmount, $"Castle Treasure #{castleItemsCollected} (+{castleScoreAmount})");
            
            // Castle items disappear after 1 interaction
            obj.SetActive(false);
            Debug.Log($"{obj.name} (Castle Item) has disappeared!");
            interactionCounts.Remove(obj);
        }
        // Check if object is on the Water/Non-Water layer (original behavior)
        else if (((1 << obj.layer) & nonWaterLayer) != 0)
        {
            currentHealth += nonWaterHeal;
            if (currentHealth > maxHealth)
            {
                currentHealth = maxHealth;
            }
            
            ShowInteractionMessage("Water Source!", 2f);
            Debug.Log($"Hydrated! Health increased to: {currentHealth}");
            
            SetInteractionColor(healColor);
            
            if (interactionCounts[obj] >= nonWaterMaxInteractions)
            {
                obj.SetActive(false);
                Debug.Log($"{obj.name} (Hydrated) has disappeared after {nonWaterMaxInteractions} interactions!");
                interactionCounts.Remove(obj);
            }
        }
        // Check if object is on the Score Layer - NO HEALING, just score
        else if (isScoreLayer)
        {
            // NO HEALING - just score points
            
            ShowInteractionMessage("Score Item Collected!", 2f);
            Debug.Log($"Score item collected! No healing given.");
            
            SetInteractionColor(healColor);
            
            // Add score for collecting this object
            objectsCollected++;
            
            // MODIFIED: Multiply score every 5 interactions
            if (objectsCollected % scoreMultiplierInterval == 0 && objectsCollected > 0)
            {
                currentObjectMultiplier *= 2;
                Debug.Log($"Score multiplier increased to x{currentObjectMultiplier} after {objectsCollected} collections!");
            }
            
            float objectScore = currentObjectMultiplier;
            AddScore(objectScore, $"Collected Object #{objectsCollected} (x{currentObjectMultiplier})");
            
            // Score objects disappear after 1 interaction (maxInteractions = 1)
            obj.SetActive(false);
            Debug.Log($"{obj.name} (Score Item) has disappeared after 1 interaction!");
            interactionCounts.Remove(obj);
        }
        // Default interaction (original behavior)
        else
        {
            Debug.Log($"Interacted with {obj.name}. Count: {interactionCounts[obj]}/{maxInteractions}");

            currentHealth += addHealth;
            if (currentHealth > maxHealth)
            {
                currentHealth = maxHealth;
            }
            
            ShowInteractionMessage("Water Source!", 2f);
            Debug.Log("Health increased to: " + currentHealth);
            
            SetInteractionColor(healColor);
            
            if (interactionCounts[obj] >= maxInteractions)
            {
                obj.SetActive(false);
                Debug.Log($"{obj.name} has disappeared after {maxInteractions} interactions!");
                interactionCounts.Remove(obj);
            }
        }
        
        uiNeedsUpdate = true;
        UpdateHealthUI();
        UpdateScoreUI();
    }

    // Add score method
    void AddScore(float amount, string reason = "")
    {
        if (!isScoreInitialized || isDead) return;
        
        totalScore += amount;
        UpdateScoreUI();
        
        // Show collection message (only for events and objects, not for survival)
        if (!reason.Contains("Survival"))
        {
            string message = $"+{amount:F0} Score: {reason}";
            ShowScoreCollectionMessage(message, scoreMessageDuration);
        }
        Debug.Log($"Score added: +{amount} ({reason}). Total: {totalScore}");
    }

    // Update score UI
    void UpdateScoreUI()
    {
        if (scoreText != null && !isDead)
        {
            // Show score with time bonus indicator
            scoreText.text = $"Score: {Mathf.FloorToInt(totalScore)}";
        }
    }

    GameObject DetectObject()
    {
        if (isDead) return null;
        
        // Combine all layers for detection
        LayerMask combinedLayers = detectionLayer | nonWaterLayer | scoreLayer | castleLayer;
        Collider2D[] colliders = Physics2D.OverlapCircleAll(detectionPoint.position, detectionRadius, combinedLayers);
        
        if (colliders.Length > 0)
        {
            foreach (Collider2D col in colliders)
            {
                if (col.gameObject.activeInHierarchy)
                {
                    Debug.Log("Detected: " + col.gameObject.name + " (Layer: " + LayerMask.LayerToName(col.gameObject.layer) + ")");
                    return col.gameObject;
                }
            }
        }
        return null;
    }

    void UpdateHealthUI()
    {
        if (isDead) return;
        
        if (healthText != null)
        {
            int displayHealth = Mathf.CeilToInt(currentHealth);
            string newText = "Health: " + displayHealth.ToString();
            
            if (healthText.text != newText)
            {
                healthText.text = newText;
                Debug.Log("UI Updated to: " + newText);
            }
        }
        else
        {
            Debug.LogError("healthText is NULL! Please assign it in the Inspector.");
        }
    }

    void OnPlayerDeath()
    {
        if (isDead) return;
        
        isDead = true;
        
        // Add any remaining partial second as bonus
        float timeBonus = scoreTimer; // Partial second
        totalScore += timeBonus;
        Debug.Log($"Final survival time bonus: +{timeBonus} points");
        
        Debug.Log($"Player has died! Final Score: {totalScore}. Disabling movement and loading GameOver scene in 5 seconds...");
        
        // Save final score to PlayerPrefs for GameOver scene
        PlayerPrefs.SetFloat("FinalScore", totalScore);
        PlayerPrefs.SetInt("ObjectsCollected", objectsCollected);
        PlayerPrefs.SetInt("EventsSurvived", eventsSurvived);
        PlayerPrefs.SetFloat("SurvivalTime", survivalTime);
        PlayerPrefs.SetInt("CastleItemsCollected", castleItemsCollected);
        PlayerPrefs.Save();
        
        // Disable player movement
        if (playerMovement != null)
        {
            playerMovement.DisableMovement();
            Debug.Log("Player movement disabled");
        }
        else
        {
            Debug.LogWarning("PlayerMovement reference is null! Movement not disabled.");
        }
        
        // Update UI to show death
        if (healthText != null)
        {
            healthText.text = "0 - DEAD";
            healthText.color = damageColor;
        }
        if (uiImage != null)
        {
            uiImage.color = damageColor;
        }
        if (eventStatusText != null)
        {
            eventStatusText.text = "YOU DIED!";
        }
        if (scoreText != null)
        {
            scoreText.text = $"Final Score: {Mathf.FloorToInt(totalScore)}";
        }
        
        // Start death sequence with 5 second delay
        StartCoroutine(DeathSequence());
    }

    IEnumerator DeathSequence()
    {
        // Wait for 5 seconds
        yield return new WaitForSeconds(deathDelay);
        
        // Check if the scene exists in build settings before loading
        if (IsSceneInBuildSettings(gameOverSceneName))
        {
            Debug.Log($"Loading scene: {gameOverSceneName}");
            SceneManager.LoadScene(gameOverSceneName);
        }
        else
        {
            Debug.LogError($"Scene '{gameOverSceneName}' is not in Build Settings! Please add it via File -> Build Settings.");
            // Fallback: Try to load anyway (might work in editor)
            try
            {
                SceneManager.LoadScene(gameOverSceneName);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to load scene: {e.Message}");
            }
        }
    }

    // Helper method to check if a scene is in build settings
    private bool IsSceneInBuildSettings(string sceneName)
    {
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
            string sceneNameFromPath = System.IO.Path.GetFileNameWithoutExtension(scenePath);
            if (sceneNameFromPath == sceneName)
            {
                return true;
            }
        }
        return false;
    }

    // Optional: Reset all interaction counts
    public void ResetInteractionCounts()
    {
        interactionCounts.Clear();
        Debug.Log("Interaction counts reset");
    }

    // Optional: Visualize detection radius in editor
    void OnDrawGizmosSelected()
    {
        if (detectionPoint != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(detectionPoint.position, detectionRadius);
        }
    }

    // Public methods for debugging
    public bool IsDrainPaused()
    {
        return isDrainPaused;
    }

    public float GetCurrentDrainMultiplier()
    {
        return currentDrainRateMultiplier;
    }

    public float GetEffectiveDrainRate()
    {
        return drainRate * currentDrainRateMultiplier;
    }

    public bool AreInteractionsBlocked()
    {
        return areInteractionsBlocked;
    }

    public bool IsDead()
    {
        return isDead;
    }

    // Public method to get final score (for other scripts)
    public float GetFinalScore()
    {
        return totalScore;
    }
}