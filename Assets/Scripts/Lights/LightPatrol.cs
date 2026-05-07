using UnityEngine;

public class LightPatrol : MonoBehaviour
{
    [Tooltip("Дистанция по X в каждую сторону от исходной позиции.")]
    [SerializeField] private float halfRange = 4f;

    [Tooltip("Время прохода от одного конца пути к другому.")]
    [SerializeField] private float travelTime = 3f;

    [Tooltip("Длительность отключения света на концах пути.")]
    [SerializeField] private float blinkDuration = 0.6f;

    [Tooltip("Перед отключением свет мерцает — предупреждение игроку.")]
    [SerializeField] private float warningTime = 0.4f;

    private Vector3 _origin;
    private Light _light;
    private AbsorbableLight _absorbable;
    private float _baseIntensity;

    private void Awake()
    {
        _origin = transform.position;
        _light  = GetComponent<Light>();
        _absorbable = GetComponent<AbsorbableLight>();
        if (_light != null) _baseIntensity = _light.intensity;
    }

    private void Update()
    {
        float cycle = 2f * (travelTime + blinkDuration);
        float t = Mathf.Repeat(Time.time, cycle);

        float xOffset;
        bool blinkOff = false;
        bool warning  = false;

        if (t < travelTime)
        {
            float p = Mathf.SmoothStep(0f, 1f, t / travelTime);
            xOffset = Mathf.Lerp(-halfRange, halfRange, p);
            warning = (travelTime - t) < warningTime;
        }
        else if (t < travelTime + blinkDuration)
        {
            xOffset = halfRange;
            blinkOff = true;
        }
        else if (t < 2f * travelTime + blinkDuration)
        {
            float local = t - travelTime - blinkDuration;
            float p = Mathf.SmoothStep(0f, 1f, local / travelTime);
            xOffset = Mathf.Lerp(halfRange, -halfRange, p);
            warning = (travelTime - local) < warningTime;
        }
        else
        {
            xOffset = -halfRange;
            blinkOff = true;
        }

        transform.position = _origin + new Vector3(xOffset, 0f, 0f);

        // Не перебиваем анимацию затухания при поглощении.
        if (_light == null || (_absorbable != null && _absorbable.IsAbsorbed)) return;

        if (blinkOff)
            _light.intensity = 0f;
        else if (warning)
            _light.intensity = _baseIntensity * (0.35f + 0.65f * Mathf.PingPong(Time.time * 9f, 1f));
        else
            _light.intensity = _baseIntensity;
    }
}
