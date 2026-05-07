using UnityEngine;

public class LightSourceData
{
    public Vector3 Position;
    public Vector3 Direction;
    public float Range;
    public float SpotAngle;
    public LightType Type;
    public bool IsActive;

    public static LightSourceData FromLight(Light light)
    {
        return new LightSourceData
        {
            Position  = light.transform.position,
            Direction = light.transform.forward,
            Range     = light.range,
            SpotAngle = light.spotAngle,
            Type      = light.type,
            IsActive  = light.isActiveAndEnabled,
        };
    }
}
