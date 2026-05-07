using System;
using UnityEngine;

public class LightAbsorber : MonoBehaviour
{
    [Header("Absorption")]
    [Tooltip("Сколько секунд свет остаётся поглощённым до автоматического отпускания.")]
    [SerializeField] private float absorbDuration = 6f;

    public float AbsorbDuration => absorbDuration;
    public AbsorbableLight Current { get; private set; }
    public float TimeRemaining { get; private set; }
    public float NormalizedTimeRemaining =>
        absorbDuration > 0f ? Mathf.Clamp01(TimeRemaining / absorbDuration) : 0f;

    public event Action<AbsorbableLight> OnAbsorbStarted;
    public event Action OnAbsorbEnded;

    public void ToggleAbsorb(AbsorbableLight light)
    {
        if (Current == light)
        {
            Release();
            return;
        }
        if (Current != null) Release();
        Absorb(light);
    }

    private void Absorb(AbsorbableLight light)
    {
        light.Absorb();
        Current = light;
        TimeRemaining = absorbDuration;
        OnAbsorbStarted?.Invoke(light);
    }

    public void Release()
    {
        if (Current == null) return;
        Current.Release();
        Current = null;
        TimeRemaining = 0f;
        OnAbsorbEnded?.Invoke();
    }

    private void Update()
    {
        if (Current == null) return;
        TimeRemaining -= Time.deltaTime;
        if (TimeRemaining <= 0f) Release();
    }

    private void OnDestroy()
    {
        if (Current != null) Current.Release();
    }
}
