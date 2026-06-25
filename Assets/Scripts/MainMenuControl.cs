using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuControl : MonoBehaviour
{
    
    public void PlayGame() {
        SceneManager.LoadScene("Gameplay");
    }

} // class
