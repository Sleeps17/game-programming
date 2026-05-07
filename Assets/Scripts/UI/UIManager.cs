using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

// Персистентный UI-менеджер: сам создаёт Canvas в Awake, без ссылок из сцены.
public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    private GameObject _canvasGO;
    private Canvas     _canvas;
    private GameObject _shadowCanvasGO;
    private Canvas     _shadowCanvas;
    private GameObject _hudRoot;
    private GameObject _shadowStatusBar;
    private Image      _shadowFill;
    private TextMeshProUGUI _shadowValueLabel;

    private GameObject _deathScreen;
    private GameObject _levelCompleteScreen;
    private GameObject _pauseScreen;
    private GameObject _warnObject;
    private GameObject _eventSystemGO;

    private float _warnTimer;
    private float _maxHealth = 100f;

    // Подписки на игрока пересоздаются в каждой сцене.
    private ShadowHealth _health;
    private LightAbsorber   _absorber;
    private PlayerController _playerController;
    private Slider _absorbBar;
    private Slider _sprintBar;
    private TextMeshProUGUI _absorbStatusLabel;

    private static readonly Color HealthyColor  = new Color(0.2f, 0.8f, 1f);
    private static readonly Color CriticalColor = new Color(1f,   0.2f, 0.1f);

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        BuildUI();
        HideAll();
    }

    private void OnEnable()  => SceneManager.sceneLoaded += OnSceneLoaded;

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        UnsubscribeFromPlayer();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // EventSystem сцены, из которой ушли, мог быть удалён — без него кнопки не реагируют.
        EnsureEventSystem();
        EnsureUI();
        HideAll();
        StartCoroutine(LateSubscribe());
    }

    // Один кадр ожидания — чтобы Player и другие объекты сцены успели инициализироваться.
    private IEnumerator LateSubscribe()
    {
        yield return null;
        EnsureEventSystem();
        EnsureUI();
        SubscribeToPlayer();
        if (GameManager.Instance != null &&
            GameManager.Instance.CurrentState == GameManager.GameState.Playing &&
            GameObject.FindGameObjectWithTag("Player") != null)
            ShowHUD();
        else
            HideAll();
    }

    private void SubscribeToPlayer()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        var nextHealth = player.GetComponent<ShadowHealth>();
        if (_health != nextHealth)
        {
            if (_health != null)
                _health.OnHealthChanged -= UpdateHealthBar;

            _health = nextHealth;

            if (_health != null)
            {
                _maxHealth = _health.MaxHealth;
                _health.OnHealthChanged += UpdateHealthBar;
            }
        }

        if (_health != null)
            UpdateHealthBar(_health.CurrentHealth);

        var nextAbsorber = player.GetComponent<LightAbsorber>();
        if (_absorber != nextAbsorber)
        {
            if (_absorber != null)
            {
                _absorber.OnAbsorbStarted -= HandleAbsorbStarted;
                _absorber.OnAbsorbEnded -= UpdateAbsorbStatus;
            }

            _absorber = nextAbsorber;

            if (_absorber != null)
            {
                _absorber.OnAbsorbStarted += HandleAbsorbStarted;
                _absorber.OnAbsorbEnded += UpdateAbsorbStatus;
            }
        }

        UpdateAbsorbStatus();
        _playerController = player.GetComponent<PlayerController>();
    }

    private void UnsubscribeFromPlayer()
    {
        if (_health != null)
        {
            _health.OnHealthChanged -= UpdateHealthBar;
            _health = null;
        }

        if (_absorber != null)
        {
            _absorber.OnAbsorbStarted -= HandleAbsorbStarted;
            _absorber.OnAbsorbEnded -= UpdateAbsorbStatus;
            _absorber = null;
        }

        _playerController = null;
    }

    private void HandleAbsorbStarted(AbsorbableLight light) => UpdateAbsorbStatus();

    private void UpdateAbsorbStatus()
    {
        if (_absorbStatusLabel == null) return;
        if (_absorber == null || _absorber.Current == null)
            _absorbStatusLabel.text = "ABSORB READY (E)";
        else
            _absorbStatusLabel.text = $"ABSORBED — {_absorber.TimeRemaining:0.0}s";
    }

    private void Update()
    {
        if (Time.frameCount % 30 == 0)
            EnsureEventSystem();

        if (Input.GetKeyDown(KeyCode.Escape) && GameManager.Instance != null)
        {
            if (GameManager.Instance.CurrentState == GameManager.GameState.Playing)
                GameManager.Instance.PauseGame();
            else if (GameManager.Instance.CurrentState == GameManager.GameState.Paused)
                GameManager.Instance.ResumeGame();
        }

        if ((_hudRoot == null || !_hudRoot.activeSelf ||
             _shadowCanvasGO == null || !_shadowCanvasGO.activeSelf ||
             _shadowStatusBar == null || !_shadowStatusBar.activeSelf) &&
            GameManager.Instance != null &&
            GameManager.Instance.CurrentState == GameManager.GameState.Playing &&
            GameObject.FindGameObjectWithTag("Player") != null)
        {
            ShowHUD();
        }
        else if (_health != null && _shadowStatusBar != null && _shadowStatusBar.activeSelf)
        {
            UpdateHealthBar(_health.CurrentHealth);
        }

        if (_warnTimer > 0f)
        {
            _warnTimer -= Time.unscaledDeltaTime;
            if (_warnTimer <= 0f) SafeSetActive(_warnObject, false);
        }

        if (_absorber != null && _absorbBar != null)
        {
            _absorbBar.value = _absorber.NormalizedTimeRemaining;
            if (_absorber.Current != null && _absorbStatusLabel != null)
                _absorbStatusLabel.text = $"ABSORBED — {_absorber.TimeRemaining:0.0}s";
        }

        if (_playerController != null && _sprintBar != null)
        {
            _sprintBar.value = _playerController.SprintStamina /
                               Mathf.Max(_playerController.MaxSprintStamina, 0.0001f);
        }
    }

    public void ShowHUD()
    {
        EnsureEventSystem();
        EnsureUI();
        HideAll();
        SubscribeToPlayer();
        UpdateHealthBar(_health != null ? _health.CurrentHealth : _maxHealth);
        _hudRoot.transform.SetAsLastSibling();
        if (_shadowCanvasGO != null) _shadowCanvasGO.transform.SetAsLastSibling();
        SafeSetActive(_shadowCanvasGO, true);
        SafeSetActive(_shadowStatusBar, true);
        SafeSetActive(_hudRoot, true);
        Time.timeScale = 1f;
    }

    public void ShowDeathScreen()
    {
        SafeSetActive(_hudRoot, false);
        SafeSetActive(_shadowCanvasGO, false);
        SafeSetActive(_deathScreen, true);
        Time.timeScale = 0f;
    }

    public void ShowLevelComplete()
    {
        SafeSetActive(_hudRoot, false);
        SafeSetActive(_shadowCanvasGO, false);
        SafeSetActive(_levelCompleteScreen, true);
        Time.timeScale = 0f;
    }

    public void ShowPauseScreen()
    {
        SafeSetActive(_hudRoot, false);
        SafeSetActive(_shadowCanvasGO, false);
        SafeSetActive(_pauseScreen, true);
    }

    public void HidePauseScreen()
    {
        SafeSetActive(_pauseScreen, false);
        if (GameObject.FindGameObjectWithTag("Player") != null)
            ShowHUD();
    }

    public void ShowAbsorbWarning()
    {
        SafeSetActive(_warnObject, true);
        _warnTimer = 2f;
    }

    private void HideAll()
    {
        SafeSetActive(_hudRoot,             false);
        SafeSetActive(_shadowCanvasGO,      false);
        SafeSetActive(_deathScreen,         false);
        SafeSetActive(_levelCompleteScreen, false);
        SafeSetActive(_pauseScreen,         false);
        SafeSetActive(_warnObject,          false);
        Time.timeScale = 1f;
    }

    private void UpdateHealthBar(float current)
    {
        float ratio = _maxHealth > 0f ? Mathf.Clamp01(current / _maxHealth) : 0f;
        if (_shadowFill != null)
        {
            var fillRect = _shadowFill.rectTransform;
            fillRect.anchorMax = new Vector2(ratio, 1f);
            fillRect.offsetMax = Vector2.zero;
            _shadowFill.color = Color.Lerp(CriticalColor, HealthyColor, ratio);
        }
        if (_shadowValueLabel != null)
            _shadowValueLabel.text = $"{Mathf.RoundToInt(ratio * 100f)}%";
    }


    private void BuildUI()
    {
        if (_canvasGO != null)
        {
            _canvasGO.SetActive(false);
            Destroy(_canvasGO);
        }
        if (_shadowCanvasGO != null)
        {
            _shadowCanvasGO.SetActive(false);
            Destroy(_shadowCanvasGO);
        }

        _canvasGO = new GameObject("UIManager_Canvas");
        _canvasGO.transform.SetParent(transform, false);

        _canvas = _canvasGO.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 900;

        var scaler = _canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        _canvasGO.AddComponent<GraphicRaycaster>();

        EnsureEventSystem();

        BuildHUD(_canvasGO.transform);
        BuildShadowStatusBar();
        BuildDeathScreen(_canvasGO.transform);
        BuildLevelCompleteScreen(_canvasGO.transform);
        BuildPauseScreen(_canvasGO.transform);
        BuildAbsorbWarning(_canvasGO.transform);
    }

    private void EnsureUI()
    {
        if (_canvasGO == null || _canvas == null || _shadowCanvasGO == null ||
            _shadowCanvas == null || _hudRoot == null || _shadowStatusBar == null)
        {
            BuildUI();
            return;
        }

        _canvasGO.SetActive(true);
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 900;
        _shadowCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _shadowCanvas.sortingOrder = 950;
    }

    // Без EventSystem + InputModule кнопки не получают клики. Сцены UmbraSetup имеют свой
    // EventSystem, который удаляется при смене сцены — поэтому держим собственный под
    // _Managers (DontDestroyOnLoad) и пересоздаём при необходимости.
    // Используется StandaloneInputModule: InputSystemUIInputModule в текущей версии
    // не имеет привязок и AssignDefaultActions() кидает исключение.
    private void EnsureEventSystem()
    {
        var systems = FindObjectsByType<UnityEngine.EventSystems.EventSystem>(FindObjectsSortMode.None);

        if (_eventSystemGO == null)
        {
            foreach (var es in systems)
            {
                if (es == null) continue;

                if (es.transform.IsChildOf(transform))
                {
                    _eventSystemGO = es.gameObject;
                    break;
                }
            }
        }

        if (_eventSystemGO == null)
        {
            foreach (var es in systems)
            {
                if (es == null) continue;

                _eventSystemGO = es.gameObject;
                _eventSystemGO.transform.SetParent(transform, true);
                break;
            }
        }

        if (_eventSystemGO == null)
        {
            _eventSystemGO = new GameObject("EventSystem");
            _eventSystemGO.transform.SetParent(transform, false);
            _eventSystemGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
            Debug.Log("[UIManager] EventSystem (re)created");
        }

        if (_eventSystemGO.GetComponent<UnityEngine.EventSystems.EventSystem>() == null)
            _eventSystemGO.AddComponent<UnityEngine.EventSystems.EventSystem>();

        if (_eventSystemGO.GetComponent<UnityEngine.EventSystems.BaseInputModule>() == null)
            _eventSystemGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

        _eventSystemGO.SetActive(true);

        systems = FindObjectsByType<UnityEngine.EventSystems.EventSystem>(FindObjectsSortMode.None);
        foreach (var es in systems)
        {
            if (es == null || es.gameObject == _eventSystemGO) continue;
            es.gameObject.SetActive(false);
            Destroy(es.gameObject);
        }
    }

    private void BuildHUD(Transform parent)
    {
        _hudRoot = new GameObject("HUD");
        _hudRoot.transform.SetParent(parent, false);
        // AddComponent<Image> автоматически конвертирует Transform → RectTransform.
        var hudBg = _hudRoot.AddComponent<Image>();
        hudBg.color = Color.clear;
        hudBg.raycastTarget = false;
        Stretch(_hudRoot);

        _absorbStatusLabel = MakeTMP(_hudRoot.transform, "ABSORB READY (E)",
            18f, FontStyles.Normal, new Color(0.6f, 0.9f, 1f));
        _absorbStatusLabel.alignment = TextAlignmentOptions.Right;
        var aRect = _absorbStatusLabel.rectTransform;
        aRect.anchorMin = new Vector2(1f, 1f);
        aRect.anchorMax = new Vector2(1f, 1f);
        aRect.anchoredPosition = new Vector2(-20f, -30f);
        aRect.sizeDelta = new Vector2(280f, 30f);

        _absorbBar = MakeBar(_hudRoot.transform, "AbsorbBar",
            new Vector2(1f, 1f), new Vector2(-20f, -64f),
            new Vector2(260f, 12f), new Color(0.4f, 0.85f, 1f));

        var sprintLbl = MakeTMP(_hudRoot.transform, "SPRINT (Shift)",
            13f, FontStyles.Normal, new Color(1f, 0.85f, 0.5f));
        sprintLbl.alignment = TextAlignmentOptions.Right;
        var sRect = sprintLbl.rectTransform;
        sRect.anchorMin = new Vector2(1f, 1f);
        sRect.anchorMax = new Vector2(1f, 1f);
        sRect.anchoredPosition = new Vector2(-20f, -86f);
        sRect.sizeDelta = new Vector2(260f, 18f);

        _sprintBar = MakeBar(_hudRoot.transform, "SprintBar",
            new Vector2(1f, 1f), new Vector2(-20f, -110f),
            new Vector2(260f, 12f), new Color(1f, 0.65f, 0.25f));

        var pauseBtn = MakeButton(_hudRoot.transform, "II",
            Vector2.zero, new Vector2(56f, 56f));
        var pauseRT = pauseBtn.GetComponent<RectTransform>();
        pauseRT.anchorMin = new Vector2(0f, 1f);
        pauseRT.anchorMax = new Vector2(0f, 1f);
        pauseRT.pivot = new Vector2(0f, 1f);
        pauseRT.anchoredPosition = new Vector2(20f, -20f);
        pauseBtn.onClick.AddListener(() => GameManager.Instance.PauseGame());

        var minimapGO = new GameObject("Minimap");
        minimapGO.transform.SetParent(_hudRoot.transform, false);
        // AddComponent<Image> сначала, чтобы получить RectTransform; Minimap.BuildPanel
        // затем перекрасит этот Image в свой frame-цвет.
        minimapGO.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.7f);
        var miniRT = minimapGO.GetComponent<RectTransform>();
        miniRT.anchorMin = new Vector2(0f, 1f);
        miniRT.anchorMax = new Vector2(0f, 1f);
        miniRT.pivot = new Vector2(0f, 1f);
        miniRT.anchoredPosition = new Vector2(20f, -88f);
        miniRT.sizeDelta = new Vector2(220f, 220f);
        minimapGO.AddComponent<Minimap>();
    }

    private void BuildPauseScreen(Transform parent)
    {
        _pauseScreen = MakeOverlay(parent, "PauseScreen",
            new Color(0f, 0f, 0f, 0.78f));

        MakeTMP(_pauseScreen.transform,
            "PAUSED",
            56f, FontStyles.Bold, new Color(0.9f, 0.9f, 1f),
            new Vector2(0, 90), new Vector2(700, 110));

        MakeTMP(_pauseScreen.transform,
            "Catch your breath, shadow.",
            22f, FontStyles.Italic, new Color(0.7f, 0.75f, 0.85f),
            new Vector2(0, 30), new Vector2(600, 45));

        var resume = MakeButton(_pauseScreen.transform, "RESUME",
            new Vector2(0f, -50f), new Vector2(220f, 52f));
        resume.onClick.AddListener(() => {
            Debug.Log("[UIManager] RESUME clicked");
            GameManager.Instance.ResumeGame();
        });

        var restart = MakeButton(_pauseScreen.transform, "RESTART",
            new Vector2(-115f, -120f), new Vector2(200f, 52f));
        restart.onClick.AddListener(() => {
            Debug.Log("[UIManager] RESTART (pause) clicked");
            Time.timeScale = 1f; GameManager.Instance.RestartLevel();
        });

        var menu = MakeButton(_pauseScreen.transform, "MENU",
            new Vector2(115f, -120f), new Vector2(200f, 52f));
        menu.onClick.AddListener(() => {
            Debug.Log("[UIManager] MENU (pause) clicked");
            Time.timeScale = 1f; GameManager.Instance.GoToMenu();
        });
    }

    private void BuildShadowStatusBar()
    {
        _shadowCanvasGO = new GameObject("ShadowHealthCanvas");
        _shadowCanvasGO.transform.SetParent(transform, false);

        _shadowCanvas = _shadowCanvasGO.AddComponent<Canvas>();
        _shadowCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _shadowCanvas.sortingOrder = 950;

        var scaler = _shadowCanvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        _shadowCanvasGO.AddComponent<GraphicRaycaster>().enabled = false;

        _shadowStatusBar = MakePanel(_shadowCanvasGO.transform, "ShadowStatusBar",
            new Color(0f, 0f, 0f, 0.95f));
        var bgImg = _shadowStatusBar.GetComponent<Image>();
        bgImg.raycastTarget = false;
        var bgRect = _shadowStatusBar.GetComponent<RectTransform>();
        bgRect.anchorMin = new Vector2(0.5f, 0f);
        bgRect.anchorMax = new Vector2(0.5f, 0f);
        bgRect.pivot = new Vector2(0.5f, 0f);
        bgRect.anchoredPosition = new Vector2(0f, 86f);
        bgRect.sizeDelta = new Vector2(720f, 72f);

        var title = MakeTMP(_shadowStatusBar.transform, "SHADOW HEALTH",
            16f, FontStyles.Bold, new Color(0.88f, 0.94f, 1f));
        title.raycastTarget = false;
        title.alignment = TextAlignmentOptions.Left;
        var titleRect = title.rectTransform;
        titleRect.anchorMin = new Vector2(0f, 0.5f);
        titleRect.anchorMax = new Vector2(0f, 0.5f);
        titleRect.pivot = new Vector2(0f, 0.5f);
        titleRect.anchoredPosition = new Vector2(20f, 0f);
        titleRect.sizeDelta = new Vector2(180f, 28f);

        var frame = MakePanel(_shadowStatusBar.transform, "ShadowHealthFrame",
            new Color(0.08f, 0.10f, 0.17f, 1f));
        frame.GetComponent<Image>().raycastTarget = false;
        var frameRect = frame.GetComponent<RectTransform>();
        frameRect.anchorMin = new Vector2(0f, 0.5f);
        frameRect.anchorMax = new Vector2(1f, 0.5f);
        frameRect.pivot = new Vector2(0.5f, 0.5f);
        frameRect.anchoredPosition = new Vector2(46f, 0f);
        frameRect.sizeDelta = new Vector2(-330f, 24f);

        var fillGO = new GameObject("ShadowHealthFill");
        fillGO.transform.SetParent(frame.transform, false);
        _shadowFill = fillGO.AddComponent<Image>();
        _shadowFill.color = HealthyColor;
        _shadowFill.raycastTarget = false;
        Stretch(fillGO);

        _shadowValueLabel = MakeTMP(_shadowStatusBar.transform, "100%",
            18f, FontStyles.Bold, new Color(0.96f, 0.98f, 1f));
        _shadowValueLabel.raycastTarget = false;
        _shadowValueLabel.alignment = TextAlignmentOptions.Right;
        var valueRect = _shadowValueLabel.rectTransform;
        valueRect.anchorMin = new Vector2(1f, 0.5f);
        valueRect.anchorMax = new Vector2(1f, 0.5f);
        valueRect.pivot = new Vector2(1f, 0.5f);
        valueRect.anchoredPosition = new Vector2(-22f, 0f);
        valueRect.sizeDelta = new Vector2(86f, 32f);
    }

    static Slider MakeBar(Transform parent, string name,
        Vector2 anchor, Vector2 anchoredPos, Vector2 size, Color fillColor)
    {
        var bgGO = new GameObject(name);
        bgGO.transform.SetParent(parent, false);
        var bgImg = bgGO.AddComponent<Image>();
        bgImg.color = new Color(0.08f, 0.08f, 0.14f, 0.7f);
        bgImg.raycastTarget = false;
        var bgRT = bgGO.GetComponent<RectTransform>();
        bgRT.anchorMin = anchor; bgRT.anchorMax = anchor;
        bgRT.pivot = new Vector2(1f, 1f);
        bgRT.anchoredPosition = anchoredPos;
        bgRT.sizeDelta = size;

        var sliderGO = new GameObject("Slider");
        sliderGO.transform.SetParent(bgGO.transform, false);
        var slider = sliderGO.AddComponent<Slider>();
        slider.minValue = 0f; slider.maxValue = 1f; slider.value = 1f;
        slider.interactable = false;
        slider.direction = Slider.Direction.LeftToRight;
        Stretch(sliderGO);

        var fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(sliderGO.transform, false);
        var faImg = fillArea.AddComponent<Image>();
        faImg.color = Color.clear;
        faImg.raycastTarget = false;
        Stretch(fillArea);
        var faRT = fillArea.GetComponent<RectTransform>();
        faRT.offsetMin = new Vector2(2f, 2f);
        faRT.offsetMax = new Vector2(-2f, -2f);

        var fillGO = new GameObject("Fill");
        fillGO.transform.SetParent(fillArea.transform, false);
        var fillImg = fillGO.AddComponent<Image>();
        fillImg.color = fillColor;
        fillImg.raycastTarget = false;
        Stretch(fillGO);
        slider.fillRect = fillImg.rectTransform;
        return slider;
    }

    private void BuildDeathScreen(Transform parent)
    {
        _deathScreen = MakeOverlay(parent, "DeathScreen",
            new Color(0f, 0f, 0f, 0.82f));

        MakeTMP(_deathScreen.transform,
            "YOU DISSOLVED IN THE LIGHT",
            40f, FontStyles.Bold, new Color(1f, 0.3f, 0.2f),
            new Vector2(0, 90), new Vector2(800, 90));

        MakeTMP(_deathScreen.transform,
            "The shadows could not protect you.",
            21f, FontStyles.Italic, new Color(0.7f, 0.5f, 0.5f),
            new Vector2(0, 20), new Vector2(600, 45));

        var retry = MakeButton(_deathScreen.transform, "RETRY",
            new Vector2(-95f, -70f), new Vector2(165f, 52f));
        retry.onClick.AddListener(() => {
            Debug.Log("[UIManager] RETRY clicked");
            Time.timeScale = 1f; GameManager.Instance.RestartLevel();
        });

        var menu = MakeButton(_deathScreen.transform, "MENU",
            new Vector2(95f, -70f), new Vector2(165f, 52f));
        menu.onClick.AddListener(() => {
            Debug.Log("[UIManager] MENU clicked");
            Time.timeScale = 1f; GameManager.Instance.GoToMenu();
        });
    }

    private void BuildLevelCompleteScreen(Transform parent)
    {
        _levelCompleteScreen = MakeOverlay(parent, "LevelCompleteScreen",
            new Color(0f, 0.04f, 0f, 0.85f));

        MakeTMP(_levelCompleteScreen.transform,
            "SHADOW PASSAGE",
            50f, FontStyles.Bold, new Color(0.3f, 1f, 0.5f),
            new Vector2(0, 90), new Vector2(700, 100));

        MakeTMP(_levelCompleteScreen.transform,
            "You slipped through unseen.",
            22f, FontStyles.Italic, new Color(0.5f, 0.8f, 0.6f),
            new Vector2(0, 20), new Vector2(600, 45));

        var next = MakeButton(_levelCompleteScreen.transform, "CONTINUE",
            new Vector2(-95f, -70f), new Vector2(165f, 52f));
        next.onClick.AddListener(() =>
            { Time.timeScale = 1f; GameManager.Instance.LevelCompleted(); });

        var menu = MakeButton(_levelCompleteScreen.transform, "MENU",
            new Vector2(95f, -70f), new Vector2(165f, 52f));
        menu.onClick.AddListener(() =>
            { Time.timeScale = 1f; GameManager.Instance.GoToMenu(); });
    }

    private void BuildAbsorbWarning(Transform parent)
    {
        _warnObject = MakePanel(parent, "AbsorbWarning",
            new Color(0.8f, 0.25f, 0.05f, 0.9f));
        var rt = _warnObject.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, -72f);
        rt.sizeDelta = new Vector2(400f, 46f);

        var t = MakeTMP(_warnObject.transform,
            "MAX ABSORBED LIGHTS REACHED",
            18f, FontStyles.Normal, Color.white);
        t.alignment = TextAlignmentOptions.Center;
        Stretch(t.gameObject);
    }

    static GameObject MakeOverlay(Transform parent, string name, Color bg)
    {
        var go = MakePanel(parent, name, bg);
        go.GetComponent<Image>().raycastTarget = true;
        Stretch(go);
        return go;
    }

    static GameObject MakePanel(Transform parent, string name, Color bg)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<Image>().color = bg;
        return go;
    }

    static TextMeshProUGUI MakeTMP(Transform parent, string text,
        float fontSize, FontStyles style, Color color,
        Vector2 anchoredPos = default, Vector2 sizeDelta = default)
    {
        var go = new GameObject("TMP_" + text.Substring(0, Mathf.Min(8, text.Length)));
        go.transform.SetParent(parent, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text      = text;
        tmp.fontSize  = fontSize;
        tmp.fontStyle = style;
        tmp.color     = color;
        tmp.alignment = TextAlignmentOptions.Center;
        if (sizeDelta != default)
        {
            var rt = tmp.rectTransform;
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = sizeDelta;
        }
        return tmp;
    }

    static Button MakeButton(Transform parent, string label,
        Vector2 anchoredPos, Vector2 size)
    {
        var go = new GameObject("Btn_" + label);
        go.transform.SetParent(parent, false);
        go.AddComponent<Image>().color = new Color(0.12f, 0.12f, 0.22f, 0.95f);
        var btn = go.AddComponent<Button>();
        var rt  = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;

        var lblGO = new GameObject("Label");
        lblGO.transform.SetParent(go.transform, false);
        var tmp = lblGO.AddComponent<TextMeshProUGUI>();
        tmp.text      = label;
        tmp.fontSize  = 20f;
        tmp.color     = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        Stretch(lblGO);
        return btn;
    }

    static void Stretch(GameObject go)
    {
        // AddComponent<RectTransform> не заменит существующий Transform — вызывающий
        // должен сначала добавить UI-компонент (Image / TMP / Slider…).
        var rt = go.GetComponent<RectTransform>();
        if (rt == null)
        {
            Debug.LogError($"[UIManager] '{go.name}' has no RectTransform. Add an Image or other UI component before calling Stretch.");
            return;
        }
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    static void SafeSetActive(GameObject go, bool active)
    {
        if (go != null) go.SetActive(active);
    }
}
