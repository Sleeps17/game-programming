using System.Collections.Generic;
using UnityEngine;

public class LightSourceRegistry : MonoBehaviour
{
    public static LightSourceRegistry Instance { get; private set; }

    private readonly List<RegisteredLight> _lights = new();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void Register(RegisteredLight light) => _lights.Add(light);
    public void Unregister(RegisteredLight light) => _lights.Remove(light);

    public List<LightSourceData> GetActiveSources()
    {
        var result = new List<LightSourceData>(_lights.Count);
        foreach (var rl in _lights)
        {
            if (rl != null)
                result.Add(rl.GetData());
        }
        return result;
    }
}
