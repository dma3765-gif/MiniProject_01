using System;

/// <summary>
/// 타워의 무기 속성(데미지, 발사 속도 등)과 발사 로직을 정의
/// </summary>
public class TowerWeapon : ATowerWeaponBase
{
    public float Damage { get; private set; }
    public float ProjectileSpeed { get; private set; }
    public float TurningSpeed { get; private set; }
    public float ArcHeight { get; private set; }
    public float BoostTime { get; private set; }
    public float BoostVerticalSpeed { get; private set; }

    public event Action<Projectile> ProjectileCreated;

    public TowerWeapon(float damage = 10f, float attackInterval = 1f, float projectileSpeed = 10f, float turningSpeed = 360f, float arcHeight = 1f, float boostTime = 0.5f, float boostVerticalSpeed = 10f)
        : base(attackInterval)
    {
        Damage = Math.Max(0f, damage);
        ProjectileSpeed = Math.Max(0.01f, projectileSpeed);
        TurningSpeed = Math.Max(0f, turningSpeed);
        ArcHeight = Math.Max(0f, arcHeight);
        BoostTime = Math.Max(0f, boostTime);
        BoostVerticalSpeed = Math.Max(0f, boostVerticalSpeed);
    }

    protected override void Fire(AMonsterBase target, WorldPosition origin)
    {
        Projectile projectile = new Projectile(origin, target, () => target.Position, ProjectileSpeed, Damage, TurningSpeed, ArcHeight, BoostTime, BoostVerticalSpeed);
        ProjectileCreated?.Invoke(projectile);
    }
}
