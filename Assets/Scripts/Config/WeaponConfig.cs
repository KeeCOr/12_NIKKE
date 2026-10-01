using System;

[Serializable]
public class WeaponConfig {
    public string name;
    public int    magazineSize;
    public float  damage;
    public float  fireRate;
    public float  reloadTime;
    public float  bulletSpeed;
    public BulletType bulletType;
    public int    pellets    = 1;
    public float  spread       = 0f;
    public float  splashRadius = 0f;
    public float  maxRange     = 0f;  // world units; 0 = unlimited
}
