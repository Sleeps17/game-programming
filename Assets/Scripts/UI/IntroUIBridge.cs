using UnityEngine;
using UnityEngine.UI;

public class IntroUIBridge : MonoBehaviour
{
    private void Start()
    {
        var startBtn = GameObject.Find("StartButton")?.GetComponent<Button>();
        if (startBtn != null)
            startBtn.onClick.AddListener(GameManager.Instance.StartGame);
    }
}
