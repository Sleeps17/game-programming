using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(ShadowHealth))]
public class LightDetector : MonoBehaviour
{
    [Header("Detection")]
    [SerializeField] private LayerMask occlusionMask;
    [SerializeField] private float checkRadius = 0.4f;

    private ShadowHealth _health;
    private readonly Vector3[] _sampleOffsets = {
        Vector3.zero,
        Vector3.up * 0.5f,
        Vector3.forward * 0.3f,
        Vector3.back * 0.3f,
        Vector3.left * 0.3f,
        Vector3.right * 0.3f,
    };

    private void Awake() => _health = GetComponent<ShadowHealth>();

    private void Update()
    {
        if (GameManager.Instance.CurrentState != GameManager.GameState.Playing) return;
        _health.SetInLight(IsIlluminated());
    }

    private bool IsIlluminated()
    {
        var sources = LightSourceRegistry.Instance.GetActiveSources();

        foreach (var source in sources)
        {
            if (source == null || !source.IsActive) continue;
            if (HitsAnyPoint(source)) return true;
        }
        return false;
    }

    private bool HitsAnyPoint(LightSourceData source)
    {
        foreach (var offset in _sampleOffsets)
        {
            Vector3 point = transform.position + offset;
            if (IsPointIlluminatedBySource(point, source))
                return true;
        }
        return false;
    }

    private bool IsPointIlluminatedBySource(Vector3 point, LightSourceData source)
    {
        Vector3 toSource = source.Position - point;
        float distance = toSource.magnitude;

        if (distance > source.Range) return false;

        if (source.Type == LightType.Spot)
        {
            float angle = Vector3.Angle(source.Direction, -toSource.normalized);
            if (angle > source.SpotAngle * 0.5f) return false;
        }

        // Стартуем в 0.3 м от точки в сторону света, чтобы не попасть в свой коллайдер.
        Vector3 start = point + toSource.normalized * 0.3f;
        if (Physics.Raycast(start, toSource.normalized, distance - 0.3f,
                occlusionMask, QueryTriggerInteraction.Ignore))
            return false;

        return true;
    }
}
