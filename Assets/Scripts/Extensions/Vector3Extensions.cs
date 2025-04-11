using UnityEngine;

public static class Vector3Extensions
{
    public static Vector2 ToVector2(this Vector3 v)
    {
        return new(v.x, v.y);
    }
}
