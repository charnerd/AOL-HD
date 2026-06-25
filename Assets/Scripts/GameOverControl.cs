using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class GameOverControl : MonoBehaviour
{
    [Header("Score Display")]
    [SerializeField] private TextMeshProUGUI finalScoreText;
    [SerializeField] private TextMeshProUGUI survivalTimeText;
    [SerializeField] private TextMeshProUGUI eventsSurvivedText;
    [SerializeField] private TextMeshProUGUI objectsCollectedText;

    void Start()
    {
        DisplayScore();
    }

    void DisplayScore()
    {
        // Get saved score data
        float finalScore = PlayerPrefs.GetFloat("FinalScore", 0f);
        float survivalTime = PlayerPrefs.GetFloat("SurvivalTime", 0f);
        int eventsSurvived = PlayerPrefs.GetInt("EventsSurvived", 0);
        int objectsCollected = PlayerPrefs.GetInt("ObjectsCollected", 0);

        // Format time
        int minutes = Mathf.FloorToInt(survivalTime / 60);
        int seconds = Mathf.FloorToInt(survivalTime % 60);
        string timeString = $"{minutes:00}:{seconds:00}";

        // Display all stats
        if (finalScoreText != null)
        {
            finalScoreText.text = $"Final Score: {Mathf.FloorToInt(finalScore)}";
        }

        if (survivalTimeText != null)
        {
            survivalTimeText.text = $"Survival Time: {timeString}";
        }

        if (eventsSurvivedText != null)
        {
            eventsSurvivedText.text = $"Events Survived: {eventsSurvived}";
        }

        if (objectsCollectedText != null)
        {
            objectsCollectedText.text = $"Objects Collected: {objectsCollected}";
        }

        Debug.Log($"Game Over - Score: {finalScore}, Time: {timeString}, Events: {eventsSurvived}, Objects: {objectsCollected}");
    }

    public void MainMenu() 
    {
        SceneManager.LoadScene("MainMenu");
    }

    public void ReplayGame() 
    {
        SceneManager.LoadScene("Gameplay");
    }
}