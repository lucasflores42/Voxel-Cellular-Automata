using UnityEngine;

public enum MaterialType
{
    Air,
    Sand,
    Water,
    Stone,
    Lava
}

[System.Serializable]
public class MaterialProperties
{
    public MaterialType type;
    public Color color;
    public float density;
    public bool isFluid;
    public int spreadRate;

    public static MaterialProperties GetDefaults(MaterialType type)
    {
        MaterialProperties props = new MaterialProperties();
        props.type = type;

        switch (type)
        {
            case MaterialType.Air:
                props.color = Color.clear;
                props.density = 0f;
                props.isFluid = true;
                props.spreadRate = 10;
                break;

            case MaterialType.Sand:
                props.color = new Color(0.76f, 0.7f, 0.5f);
                props.density = 1.6f;
                props.isFluid = false;
                props.spreadRate = 1;
                break;

            case MaterialType.Water:
                props.color = new Color(0.3f, 0.5f, 1f, 0.7f);
                props.density = 1.0f;
                props.isFluid = true;
                props.spreadRate = 3;
                break;

            case MaterialType.Stone:
                props.color = new Color(0.5f, 0.5f, 0.5f);
                props.density = 2.5f;
                props.isFluid = false;
                props.spreadRate = 0;
                break;

            case MaterialType.Lava:
                props.color = new Color(1f, 0.3f, 0.1f);
                props.density = 2.1f;
                props.isFluid = true;
                props.spreadRate = 2;
                break;

            default:
                // Fallback for any unhandled types
                props.color = Color.magenta;
                props.density = 1f;
                props.isFluid = false;
                props.spreadRate = 0;
                break;
        }

        return props;
    }
}