using System;
using UnityEngine;

public static class GameEvents {
    public static event Action<float, float>   OnBossHpChanged;
    public static event Action<string>          OnBossPartDestroyed;
    public static event Action                  OnBossEnraged;
    public static event Action                  OnBossDefeated;
    public static event Action<BulletData>      OnFireBullet;
    public static event Action<string, int, int> OnAmmoChanged;
    public static event Action<string>           OnReloadStarted;   // fires once when reload begins
    public static event Action<string, float>   OnReloadProgress;  // fires every frame during reload; float = 0→1
    public static event Action<string>          OnReloadComplete;
    public static event Action<string>          OnMemberDied;
    public static event Action<Vector2, float>  OnBossShockwave;
    public static event Action                  OnBossAttack;
    public static event Action<float, float>    OnWallHpChanged;
    public static event Action                  OnWallDestroyed;
    public static event Action<MinionType, Vector2> OnSpawnMinion;

    public static void RaiseBossHpChanged(float hp, float max)          => OnBossHpChanged?.Invoke(hp, max);
    public static void RaiseBossPartDestroyed(string partId)            => OnBossPartDestroyed?.Invoke(partId);
    public static void RaiseBossEnraged()                               => OnBossEnraged?.Invoke();
    public static void RaiseBossDefeated()                              => OnBossDefeated?.Invoke();
    public static void RaiseFireBullet(BulletData data)                 => OnFireBullet?.Invoke(data);
    public static void RaiseAmmoChanged(string id, int cur, int max)    => OnAmmoChanged?.Invoke(id, cur, max);
    public static void RaiseReloadStarted(string id)                    => OnReloadStarted?.Invoke(id);
    public static void RaiseReloadProgress(string id, float progress)   => OnReloadProgress?.Invoke(id, progress);
    public static void RaiseReloadComplete(string id)                   => OnReloadComplete?.Invoke(id);
    public static void RaiseMemberDied(string id)                       => OnMemberDied?.Invoke(id);
    public static void RaiseBossShockwave(Vector2 pos, float damage)    => OnBossShockwave?.Invoke(pos, damage);
    public static void RaiseBossAttack()                                => OnBossAttack?.Invoke();
    public static void RaiseWallHpChanged(float hp, float max)          => OnWallHpChanged?.Invoke(hp, max);
    public static void RaiseWallDestroyed()                             => OnWallDestroyed?.Invoke();
    public static void RaiseSpawnMinion(MinionType type, Vector2 pos)   => OnSpawnMinion?.Invoke(type, pos);

    public static void ClearAllListeners() {
        OnBossHpChanged = null; OnBossPartDestroyed = null; OnBossEnraged = null;
        OnBossDefeated = null; OnFireBullet = null; OnAmmoChanged = null;
        OnReloadStarted = null; OnReloadProgress = null; OnReloadComplete = null; OnMemberDied = null;
        OnBossShockwave = null; OnBossAttack = null; OnWallHpChanged = null;
        OnWallDestroyed = null; OnSpawnMinion = null;
    }
}

public enum MinionType { Runner, Berserker, Spitter }

public struct BulletData {
    public Vector2 origin;
    public float angle;        // radians — set by SquadMemberController via Atan2
    public float speed;
    public float damage;
    public string ownerId;
    public BulletType bulletType;
    public float splashRadius;
    public int pellets;
    public float spread;       // degrees — designer-facing cone half-width, converted to rad in FireSystem
    public string targetPartId;
    public float maxRange;     // world units; 0 = unlimited travel distance
}

public enum BulletType { Single, Shotgun, Rocket }
