using System;

[Serializable]
public struct WorldPosition
{
    public float X;
    public float Y;
    public float Z;

    public WorldPosition(float x, float y, float z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    public static float Distance(WorldPosition a, WorldPosition b)
    {
        float x = b.X - a.X;
        float y = b.Y - a.Y;
        float z = b.Z - a.Z;
        return (float)Math.Sqrt((x * x) + (y * y) + (z * z));
    }

    public static WorldPosition MoveTowards(WorldPosition current, WorldPosition target, float maxDistanceDelta)
    {
        float distance = Distance(current, target);
        if (distance <= maxDistanceDelta || distance <= 0f)
        {
            return target;
        }

        float ratio = maxDistanceDelta / distance;
        return new WorldPosition(
            current.X + ((target.X - current.X) * ratio),
            current.Y + ((target.Y - current.Y) * ratio),
            current.Z + ((target.Z - current.Z) * ratio));
    }
}