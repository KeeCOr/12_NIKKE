using System;
using UnityEngine;

[Serializable]
public class BossPartConfig {
    public string id;
    public int hp;
    public float damageMult;
    public Vector2 offset;
    public Vector2 size;
    public bool activeOnStart;
}
