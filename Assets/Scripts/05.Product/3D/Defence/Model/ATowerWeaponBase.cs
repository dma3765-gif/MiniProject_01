using System;

public abstract class ATowerWeaponBase : ITickable
{
    private Func<WorldPosition> _positionProvider;
    private Func<float> _rangeProvider;
    private Func<WorldPosition, float, AMonsterBase> _targetProvider;

    public float AttackInterval { get; private set; }
    public float CooldownRemaining { get; private set; }

    protected ATowerWeaponBase(float attackInterval)
    {
        AttackInterval = Math.Max(0.01f, attackInterval);
    }

    internal void Bind(Func<WorldPosition> positionProvider, Func<float> rangeProvider, Func<WorldPosition, float, AMonsterBase> targetProvider)
    {
        _positionProvider = positionProvider;
        _rangeProvider = rangeProvider;
        _targetProvider = targetProvider;
    }

    public void Tick(float deltaTime)
    {
        if (_targetProvider == null || deltaTime <= 0f)
        {
            return;
        }

        CooldownRemaining = Math.Max(0f, CooldownRemaining - deltaTime);
        if (CooldownRemaining > 0f)
        {
            return;
        }

        AMonsterBase target = _targetProvider(_positionProvider(), _rangeProvider());
        if (target == null)
        {
            return;
        }

        Fire(target, _positionProvider());
        CooldownRemaining = AttackInterval;
    }

    protected abstract void Fire(AMonsterBase target, WorldPosition origin);
}
