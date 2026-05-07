using UnityEngine;

[RequireComponent(typeof(Light))]
public class RegisteredLight : MonoBehaviour
{
    private Light _light;

    private void Awake() => _light = GetComponent<Light>();

    private void OnEnable()  => LightSourceRegistry.Instance?.Register(this);
    private void OnDisable() => LightSourceRegistry.Instance?.Unregister(this);

    public LightSourceData GetData() => LightSourceData.FromLight(_light);
}
