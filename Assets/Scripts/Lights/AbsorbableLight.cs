using System.Collections;
using UnityEngine;

// Требует Light + Collider (Trigger) с радиусом взаимодействия.
[RequireComponent(typeof(Light))]
[RequireComponent(typeof(RegisteredLight))]
public class AbsorbableLight : MonoBehaviour
{
    [Header("Absorption")]
    [SerializeField] private float absorbTransitionTime = 0.3f;

    [Header("Visual Feedback")]
    [SerializeField] private GameObject absorbIndicator;

    public bool IsAbsorbed { get; private set; }

    private Light _light;
    private float _originalIntensity;
    private Coroutine _transitionCoroutine;

    private void Awake()
    {
        _light = GetComponent<Light>();
        _originalIntensity = _light.intensity;
    }

    public void Absorb()
    {
        if (IsAbsorbed) return;
        IsAbsorbed = true;
        RestartCoroutine(TransitionIntensity(_light.intensity, 0f));
        if (absorbIndicator) absorbIndicator.SetActive(false);
    }

    public void Release()
    {
        if (!IsAbsorbed) return;
        IsAbsorbed = false;
        _light.enabled = true;
        RestartCoroutine(TransitionIntensity(_light.intensity, _originalIntensity));
        if (absorbIndicator) absorbIndicator.SetActive(true);
    }

    private void RestartCoroutine(IEnumerator routine)
    {
        if (_transitionCoroutine != null) StopCoroutine(_transitionCoroutine);
        _transitionCoroutine = StartCoroutine(routine);
    }

    private IEnumerator TransitionIntensity(float from, float to)
    {
        float elapsed = 0f;
        while (elapsed < absorbTransitionTime)
        {
            elapsed += Time.deltaTime;
            _light.intensity = Mathf.Lerp(from, to, elapsed / absorbTransitionTime);
            yield return null;
        }
        _light.intensity = to;
        if (to <= 0f) _light.enabled = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (absorbIndicator && other.CompareTag("Player"))
            absorbIndicator.SetActive(true);
    }

    private void OnTriggerExit(Collider other)
    {
        if (absorbIndicator && other.CompareTag("Player") && !IsAbsorbed)
            absorbIndicator.SetActive(false);
    }
}
