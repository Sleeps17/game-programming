using UnityEngine;

public class LightSweep : MonoBehaviour
{
    [Tooltip("Половина дуги поворота в градусах (полный размах = 2×).")]
    [SerializeField] private float halfArc = 45f;

    [Tooltip("Время прохода от одного крайнего положения к другому.")]
    [SerializeField] private float sweepTime = 2.4f;

    [Tooltip("Пауза на крайних положениях перед обратным проходом.")]
    [SerializeField] private float holdTime = 0.7f;

    private Quaternion _baseRot;

    private void Awake() => _baseRot = transform.localRotation;

    private void Update()
    {
        float cycle = 2f * (sweepTime + holdTime);
        float t = Mathf.Repeat(Time.time, cycle);

        float yaw;
        if (t < holdTime)
        {
            yaw = -halfArc;
        }
        else if (t < holdTime + sweepTime)
        {
            float p = Mathf.SmoothStep(0f, 1f, (t - holdTime) / sweepTime);
            yaw = Mathf.Lerp(-halfArc, halfArc, p);
        }
        else if (t < 2f * holdTime + sweepTime)
        {
            yaw = halfArc;
        }
        else
        {
            float p = Mathf.SmoothStep(0f, 1f, (t - 2f * holdTime - sweepTime) / sweepTime);
            yaw = Mathf.Lerp(halfArc, -halfArc, p);
        }

        transform.localRotation = _baseRot * Quaternion.Euler(0f, yaw, 0f);
    }
}
