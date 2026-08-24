using System;

public sealed class Projectile : ITickable
{
    private readonly IDamageable _target;
    private readonly Func<WorldPosition> _targetPositionProvider;

    public WorldPosition Position { get; private set; }
    public float Speed { get; private set; }
    public float Damage { get; private set; }
    public bool IsCompleted { get; private set; }

    public Projectile(WorldPosition origin, IDamageable target, Func<WorldPosition> targetPositionProvider, float speed, float damage)
    {
        Position = origin;
        _target = target;
        _targetPositionProvider = targetPositionProvider;
        Speed = Math.Max(0.01f, speed);
        Damage = Math.Max(0f, damage);
    }

    public void Tick(float deltaTime)
    {
        if (IsCompleted || deltaTime <= 0f)
        {
            return;
        }

        AMonsterBase monster = _target as AMonsterBase;



        if (_target == null || _target.IsDead || (monster != null && monster.HasReachedGoal))
        {
            IsCompleted = true;
            return;
        }

        WorldPosition targetPosition = _targetPositionProvider();
        float moveDistance = Speed * deltaTime;
        if (WorldPosition.Distance(Position, targetPosition) <= moveDistance)
        {
            Position = targetPosition;
            _target.TakeDamage(Damage);
            IsCompleted = true;
            return;
        }

        Position = WorldPosition.MoveTowards(Position, targetPosition, moveDistance);
    }
}