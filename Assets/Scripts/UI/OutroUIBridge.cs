using UnityEngine;
using UnityEngine.UI;

public class OutroUIBridge : MonoBehaviour
{
    private void Start()
    {
        var playAgain = GameObject.Find("PlayAgainBtn")?.GetComponent<Button>();
        if (playAgain != null)
            playAgain.onClick.AddListener(GameManager.Instance.GoToMenu);

        var quit = GameObject.Find("QuitBtn")?.GetComponent<Button>();
        if (quit != null)
            quit.onClick.AddListener(GameManager.Instance.QuitGame);
    }
}
