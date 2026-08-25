/// <summary>
/// 피해를 받을 수 있는 대상의 위치 및 히트 반경을 제공하고 데미지를 처리
/// </summary>
public interface IDamageable
{
    bool IsDead { get; }
    void TakeDamage(float damage);

    WorldPosition Position { get; }

    float HitRadius { get; }

    float HitHeight { get; }
}