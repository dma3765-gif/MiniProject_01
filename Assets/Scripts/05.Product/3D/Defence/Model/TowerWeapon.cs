using System;

public class TowerWeapon : ATowerWeaponBase
{
    public float Damage { get; private set; }
    public float ProjectileSpeed { get; private set; }

    public event Action<Projectile> ProjectileCreated;

    public TowerWeapon(float damage = 10f, float attackInterval = 1f, float projectileSpeed = 10f)
        : base(attackInterval)
    {
        Damage = Math.Max(0f, damage);
        ProjectileSpeed = Math.Max(0.01f, projectileSpeed);
    }

    protected override void Fire(AMonsterBase target, WorldPosition origin)
    {
        Projectile projectile = new Projectile(origin, target, () => target.Position, ProjectileSpeed, Damage);
        ProjectileCreated?.Invoke(projectile);
    }
}
