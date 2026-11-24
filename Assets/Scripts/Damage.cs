using System.Collections.Generic;
using UnityEngine;
public static class EnemyEvents
{
    public static event System.Action<Vector2> Died;

    public static void RaiseDied(Vector2 position)
    {
        Died?.Invoke(position);
    }
}
public static class Damage
{
    private static readonly Dictionary<EnemyChase, EnemyChase> links = new Dictionary<EnemyChase, EnemyChase>();
    static bool sharing;

    public static void Hit(EnemyChase target, float amount)
    {
        if (target == null || target.Health == null || target.Health.Current <= 0f) return;
        target.Health.TakeDamage(amount);

        if (sharing) return;
        if (links.TryGetValue(target, out EnemyChase other) && other != null &&
            other.Health != null && other.Health.Current > 0f)
        {
            sharing = true;
            other.Health.TakeDamage(amount);
            sharing = false;
        }
    }

    public static bool IsLinked(EnemyChase e)
    {
        return links.ContainsKey(e);
    }
    public static void Link(EnemyChase a, EnemyChase b)
    {
        links[a] = b;
        links[b] = a;
    }

    public static void Unlink(EnemyChase a, EnemyChase b)
    {
        if (!object.ReferenceEquals(a, null)) links.Remove(a);
        if (!object.ReferenceEquals(b, null)) links.Remove(b);
    }
    public static void ClearLinks()
    {
        links.Clear();
        sharing = false;
    }
}


public static class EnemyView
{
    public static EnemyChase[] List = new EnemyChase[256];
    public static Vector2[] Pos = new Vector2[256];
    public static float[] Rad = new float[256];
    public static int Count;

    private static int frame = -1;

    public static void Refresh()
    {
        if (frame == Time.frameCount) return;
        frame = Time.frameCount;

        int n = EnemyChase.Active.Count;
        if (n > List.Length)
        {
            int size = Mathf.NextPowerOfTwo(n);
            System.Array.Resize(ref List, size);
            System.Array.Resize(ref Pos, size);
            System.Array.Resize(ref Rad, size);
        }

        int i = 0;
        foreach (EnemyChase e in EnemyChase.Active)
        {
            List[i] = e;
            Pos[i] = e.Position;
            Rad[i] = e.HitRadius;
            i++;
        }
        Count = i;
    }

    public static bool IsAlive(int index)
    {
        EnemyChase e = List[index];
        return e != null && e.Health != null && e.Health.Current > 0f;
    }

    public static int Nearest(Vector2 from, float range)
    {
        Refresh();
        int best = -1;
        float bestSqr = range * range;

        for (int k = 0; k < Count; k++)
        {
            if (!IsAlive(k)) continue;
            float sqr = (Pos[k] - from).sqrMagnitude;
            if (sqr < bestSqr)
            {
                bestSqr = sqr;
                best = k;
            }
        }
        return best;
    }

    public static int RandomIn(Vector2 from, float range)
    {
        Refresh();
        int pick = -1;
        int seen = 0;
        float rangeSqr = range * range;

        for (int k = 0; k < Count; k++)
        {
            if (!IsAlive(k)) continue;
            if ((Pos[k] - from).sqrMagnitude > rangeSqr) continue;


            seen++;
            if (Random.Range(0, seen) == 0) pick = k;
        }
        return pick;
    }
}