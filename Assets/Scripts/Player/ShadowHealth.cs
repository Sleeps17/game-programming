using System;
using UnityEngine;

public class ShadowHealth : MonoBehaviour
{
    public static ShadowHealth Instance { get; private set; }

    [Header("Settings")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float damagePerSecond = 30f;
    [SerializeField] private float regenPerSecond = 15f;
    [SerializeField] private float deathDelay = 0.3f;

    public float CurrentHealth { get; private set; }
    public float MaxHealth => maxHealth;
    public bool IsInLight { get; private set; }

    public event Action<float> OnHealthChanged;
    public event Action OnDeath;

    private bool _isDead;

    private void Awake()
    {
        Instance = this;
        CurrentHealth = maxHealth;
    }

    public void SetInLight(bool inLight)
    {
        IsInLight = inLight;
    }

    private void Update()
    {
        if (_isDead || GameManager.Instance.CurrentState != GameManager.GameState.Playing) return;

        if (IsInLight)
        {
            CurrentHealth -= damagePerSecond * Time.deltaTime;
            CurrentHealth = Mathf.Max(0f, CurrentHealth);
            OnHealthChanged?.Invoke(CurrentHealth);

            if (CurrentHealth <= 0f)
                Die();
        }
        else
        {
            CurrentHealth += regenPerSecond * Time.deltaTime;
            CurrentHealth = Mathf.Min(maxHealth, CurrentHealth);
            OnHealthChanged?.Invoke(CurrentHealth);
        }
    }

    private void Die()
    {
        if (_isDead) return;
        _isDead = true;
        OnDeath?.Invoke();
        Invoke(nameof(NotifyManager), deathDelay);
    }

    private void NotifyManager() => GameManager.Instance.PlayerDied();
}
