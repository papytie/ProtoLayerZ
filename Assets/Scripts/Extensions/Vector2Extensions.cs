using UnityEngine;

public static class Vector2Extensions
{
    public static Vector3 ToVector3(this Vector2 v)
    {
        return new(v.x, v.y, 0);
    }
}
