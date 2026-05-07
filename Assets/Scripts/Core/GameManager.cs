using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public enum GameState { Menu, Playing, Paused, GameOver, LevelComplete, GameComplete }

    public GameState CurrentState { get; private set; }

    [Header("Scene Indices")]
    [SerializeField] private int introSceneIndex = 0;
    [SerializeField] private int firstLevelIndex = 1;
    [SerializeField] private int totalLevels = 3;
    [SerializeField] private int outroSceneIndex = 4;

    private int _currentLevelIndex;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void StartGame()
    {
        _currentLevelIndex = firstLevelIndex;
        SetState(GameState.Playing);
        SceneLoader.Instance.LoadScene(_currentLevelIndex);
    }

    public void PlayerDied()
    {
        if (CurrentState != GameState.Playing) return;
        SetState(GameState.GameOver);
        UIManager.Instance.ShowDeathScreen();
    }

    // Вызывается LevelGoal'ом при касании выхода: только меняет состояние и показывает экран.
    // Переход на следующий уровень происходит по кнопке CONTINUE.
    public void LevelReached()
    {
        if (CurrentState != GameState.Playing) return;
        SetState(GameState.LevelComplete);
    }

    public void LevelCompleted()
    {
        if (CurrentState != GameState.LevelComplete &&
            CurrentState != GameState.Playing) return;

        Time.timeScale = 1f;

        int nextLevel = _currentLevelIndex + 1;
        if (nextLevel > firstLevelIndex + totalLevels - 1)
        {
            SetState(GameState.GameComplete);
            SceneLoader.Instance.LoadScene(outroSceneIndex);
        }
        else
        {
            _currentLevelIndex = nextLevel;
            SetState(GameState.Playing);
            SceneLoader.Instance.LoadScene(_currentLevelIndex);
        }
    }

    public void RestartLevel()
    {
        Time.timeScale = 1f;
        SetState(GameState.Playing);
        SceneLoader.Instance.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void GoToMenu()
    {
        Time.timeScale = 1f;
        SetState(GameState.Menu);
        SceneLoader.Instance.LoadScene(introSceneIndex);
    }

    public void PauseGame()
    {
        if (CurrentState != GameState.Playing) return;
        SetState(GameState.Paused);
        Time.timeScale = 0f;
        UIManager.Instance?.ShowPauseScreen();
    }

    public void ResumeGame()
    {
        if (CurrentState != GameState.Paused) return;
        SetState(GameState.Playing);
        Time.timeScale = 1f;
        UIManager.Instance?.HidePauseScreen();
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void EnterPlayingState() => SetState(GameState.Playing);

    private void SetState(GameState state) => CurrentState = state;
}
