using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneLoader : MonoBehaviour
{
    public static SceneLoader Instance { get; private set; }

    [SerializeField] private float fadeDuration = 0.5f;

    private Image _fadeImage;
    private bool _isFading;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        CreateFadeCanvas();
    }

    private void CreateFadeCanvas()
    {
        var canvasGO = new GameObject("FadeCanvas");
        canvasGO.transform.SetParent(transform);

        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;
        canvasGO.AddComponent<CanvasScaler>();

        var imageGO = new GameObject("FadeImage");
        imageGO.transform.SetParent(canvasGO.transform, false);

        _fadeImage = imageGO.AddComponent<Image>();
        _fadeImage.color = Color.black;
        _fadeImage.raycastTarget = false;

        var rect = _fadeImage.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        SetFadeAlpha(0f);
    }

    public void LoadScene(int index)
    {
        if (_isFading) return;
        StartCoroutine(FadeAndLoad(index));
    }

    private IEnumerator FadeAndLoad(int index)
    {
        _isFading = true;

        yield return StartCoroutine(Fade(0f, 1f));

        var op = SceneManager.LoadSceneAsync(index);
        while (!op.isDone)
            yield return null;

        yield return StartCoroutine(Fade(1f, 0f));

        _isFading = false;
    }

    private IEnumerator Fade(float from, float to)
    {
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            SetFadeAlpha(Mathf.Lerp(from, to, elapsed / fadeDuration));
            yield return null;
        }
        SetFadeAlpha(to);
    }

    private void SetFadeAlpha(float alpha)
    {
        if (_fadeImage == null) return;
        var c = _fadeImage.color;
        c.a = alpha;
        _fadeImage.color = c;
    }
}
