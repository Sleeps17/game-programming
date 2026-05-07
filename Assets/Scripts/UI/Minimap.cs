using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

// Схематичная миникарта сверху: читает рендереры на слое Wall как прямоугольники,
// проецирует их на UI-панель в плоскости XZ; игрок и LevelGoal — отдельные точки.
public class Minimap : MonoBehaviour
{
    [Header("Style")]
    [SerializeField] private Color frameColor   = new Color(0f,    0f,    0f,    0.7f);
    [SerializeField] private Color contentColor = new Color(0.04f, 0.05f, 0.10f, 0.85f);
    [SerializeField] private Color borderColor  = new Color(0.45f, 0.50f, 0.75f, 0.6f);
    [SerializeField] private Color wallColor    = new Color(0.78f, 0.82f, 1f,    0.95f);
    [SerializeField] private Color playerColor  = new Color(0.4f,  0.95f, 1f,    1f);
    [SerializeField] private Color goalColor    = new Color(0.4f,  1f,    0.55f, 1f);

    [Header("Padding (world units)")]
    [SerializeField] private float worldPadding = 1.5f;

    private RectTransform _contentRT;
    private GameObject    _wallContainer;
    private RectTransform _playerDot;
    private RectTransform _goalDot;

    private Transform _player;
    private Transform _goal;
    private Bounds    _worldBounds;
    private float     _scale;
    private Vector2   _drawOffset;
    private bool      _ready;

    private void Awake() => BuildPanel();

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        if (isActiveAndEnabled) StartCoroutine(RefreshAfterFrame());
    }

    private void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

    private void OnSceneLoaded(Scene s, LoadSceneMode m)
    {
        // Если HUD скрыт во время перехода между сценами, этот объект может быть неактивен —
        // Unity бросит исключение на StartCoroutine. OnEnable сам запустит Refresh при возврате.
        if (isActiveAndEnabled)
            StartCoroutine(RefreshAfterFrame());
    }

    // Ждём 2 кадра: LevelManager должен заспавнить игрока/цель, а RectTransform — пройти layout.
    private IEnumerator RefreshAfterFrame()
    {
        yield return null;
        yield return null;
        Refresh();
    }

    private void Update()
    {
        if (!_ready) return;

        if (_player != null)
        {
            _playerDot.anchoredPosition = WorldToMinimap(
                new Vector2(_player.position.x, _player.position.z));
            _playerDot.localRotation = Quaternion.Euler(
                0f, 0f, -_player.eulerAngles.y);
            _playerDot.gameObject.SetActive(true);
        }
        else
        {
            _playerDot.gameObject.SetActive(false);
        }

        if (_goal != null)
        {
            _goalDot.anchoredPosition = WorldToMinimap(
                new Vector2(_goal.position.x, _goal.position.z));
            _goalDot.gameObject.SetActive(true);
        }
        else
        {
            _goalDot.gameObject.SetActive(false);
        }
    }

    private void BuildPanel()
    {
        // Image на этом GO уже создан в BuildHUD — просто перекрашиваем.
        var frame = GetComponent<Image>();
        if (frame == null) frame = gameObject.AddComponent<Image>();
        frame.color = frameColor;
        frame.raycastTarget = false;

        var border = MakeImage(transform, "Border", borderColor);
        border.raycastTarget = false;
        var brt = border.rectTransform;
        brt.anchorMin = Vector2.zero; brt.anchorMax = Vector2.one;
        brt.offsetMin = new Vector2(2f,  2f);
        brt.offsetMax = new Vector2(-2f, -22f);

        // RectMask2D режет любые UI-элементы, выходящие за границы панели.
        var content = MakeImage(transform, "Content", contentColor);
        content.raycastTarget = false;
        _contentRT = content.rectTransform;
        _contentRT.anchorMin = Vector2.zero; _contentRT.anchorMax = Vector2.one;
        _contentRT.offsetMin = new Vector2(4f,  4f);
        _contentRT.offsetMax = new Vector2(-4f, -24f);
        content.gameObject.AddComponent<RectMask2D>();

        var titleGO = new GameObject("Title");
        titleGO.transform.SetParent(transform, false);
        var titleTMP = titleGO.AddComponent<TextMeshProUGUI>();
        titleTMP.text          = "MAP";
        titleTMP.fontSize      = 13f;
        titleTMP.fontStyle     = FontStyles.Bold;
        titleTMP.color         = new Color(0.75f, 0.82f, 1f);
        titleTMP.alignment     = TextAlignmentOptions.Center;
        titleTMP.raycastTarget = false;
        var trt = titleTMP.rectTransform;
        trt.anchorMin = new Vector2(0f, 1f);
        trt.anchorMax = new Vector2(1f, 1f);
        trt.pivot = new Vector2(0.5f, 1f);
        trt.anchoredPosition = new Vector2(0f, -3f);
        trt.sizeDelta = new Vector2(0f, 18f);

        _wallContainer = new GameObject("Walls");
        _wallContainer.transform.SetParent(_contentRT, false);
        var wcImg = _wallContainer.AddComponent<Image>();
        wcImg.color = new Color(0f, 0f, 0f, 0f);
        wcImg.raycastTarget = false;
        var wcRT = wcImg.rectTransform;
        wcRT.anchorMin = Vector2.zero; wcRT.anchorMax = Vector2.one;
        wcRT.offsetMin = Vector2.zero; wcRT.offsetMax = Vector2.zero;

        // Точка цели — квадрат, повёрнутый на 45° (визуально — ромб).
        var goalGO = MakeImage(_contentRT, "GoalDot", goalColor).gameObject;
        var grt = goalGO.GetComponent<RectTransform>();
        grt.anchorMin = Vector2.zero; grt.anchorMax = Vector2.zero;
        grt.pivot = new Vector2(0.5f, 0.5f);
        grt.sizeDelta = new Vector2(10f, 10f);
        grt.localRotation = Quaternion.Euler(0f, 0f, 45f);
        _goalDot = grt;

        // Точка игрока — узкий прямоугольник, поворот = направление взгляда.
        var playerGO = MakeImage(_contentRT, "PlayerDot", playerColor).gameObject;
        var prt = playerGO.GetComponent<RectTransform>();
        prt.anchorMin = Vector2.zero; prt.anchorMax = Vector2.zero;
        prt.pivot = new Vector2(0.5f, 0.5f);
        prt.sizeDelta = new Vector2(7f, 13f);
        _playerDot = prt;
    }

    private static Image MakeImage(Transform parent, string name, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = color;
        return img;
    }

    private void Refresh()
    {
        _ready = false;

        var playerGO = GameObject.FindGameObjectWithTag("Player");
        _player = playerGO != null ? playerGO.transform : null;

        var goal = FindFirstObjectByType<LevelGoal>();
        _goal = goal != null ? goal.transform : null;

        for (int i = _wallContainer.transform.childCount - 1; i >= 0; i--)
            Destroy(_wallContainer.transform.GetChild(i).gameObject);

        if (_player == null) return;

        int wallLayer = LayerMask.NameToLayer("Wall");
        if (wallLayer < 0) return;

        var wallList = new List<Bounds>(64);
        var renderers = FindObjectsByType<Renderer>(FindObjectsSortMode.None);
        bool hasAny = false;
        Bounds total = new Bounds();
        foreach (var r in renderers)
        {
            if (r == null || r.gameObject.layer != wallLayer) continue;
            var b = r.bounds;
            if (!hasAny) { total = b; hasAny = true; }
            else         { total.Encapsulate(b); }
            wallList.Add(b);
        }
        if (!hasAny) return;

        total.Expand(new Vector3(worldPadding * 2f, 0f, worldPadding * 2f));
        _worldBounds = total;

        // rect.size валиден только после layout-pass'а — форсим, чтобы не прочитать нули.
        UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(_contentRT);
        var contentSize = _contentRT.rect.size;
        if (contentSize.x <= 1f || contentSize.y <= 1f) return;

        float worldW = total.size.x;
        float worldH = total.size.z;
        _scale = Mathf.Min(contentSize.x / worldW, contentSize.y / worldH);
        _drawOffset = new Vector2(
            (contentSize.x - worldW * _scale) * 0.5f,
            (contentSize.y - worldH * _scale) * 0.5f);

        foreach (var wb in wallList)
        {
            var img = MakeImage(_wallContainer.transform, "Wall", wallColor);
            img.raycastTarget = false;
            var rt = img.rectTransform;
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = WorldToMinimap(
                new Vector2(wb.center.x, wb.center.z));
            rt.sizeDelta = new Vector2(
                Mathf.Max(2f, wb.size.x * _scale),
                Mathf.Max(2f, wb.size.z * _scale));
        }

        // Игрок поверх цели — если точки наложатся, стрелка останется видна.
        _goalDot.SetAsLastSibling();
        _playerDot.SetAsLastSibling();

        _ready = true;
    }

    private Vector2 WorldToMinimap(Vector2 worldXZ) => new Vector2(
        (worldXZ.x - _worldBounds.min.x) * _scale + _drawOffset.x,
        (worldXZ.y - _worldBounds.min.z) * _scale + _drawOffset.y);
}
