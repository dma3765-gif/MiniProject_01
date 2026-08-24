using System;

public enum EnumMonsterType
{
    Normal,
    Boss
}

public enum EnumMonsterState
{
    Idle,
    Moving,
    Dead
}

public enum EnumMonsterMoveType
{
    Ground,
    Flying
}

public abstract class AMonsterBase : ITickable, IDamageable
{
    public EnumMonsterType MonsterType { get; set; }
    public EnumMonsterState MonsterState { get; set; }
    public EnumMonsterMoveType MonsterMoveType { get; set; }

    public float MaxHp { get; protected set; }
    public float Hp { get; protected set; }
    public float MoveSpeed { get; protected set; }
    public bool IsDead { get; protected set; }
    public bool HasReachedGoal { get; protected set; }
    public int Reward { get; protected set; }
    public WorldPosition Position { get; protected set; }

    public event Action<AMonsterBase> Died;
    public event Action<AMonsterBase> ReachedGoal;

    protected AMonsterBase(float maxHp, float moveSpeed, int reward, EnumMonsterMoveType moveType, EnumMonsterType type, WorldPosition startPosition)
    {
        MaxHp = Math.Max(1f, maxHp);
        Hp = MaxHp;
        MoveSpeed = Math.Max(0f, moveSpeed);
        Reward = Math.Max(0, reward);
        Position = startPosition;
        MonsterState = EnumMonsterState.Idle;
        MonsterMoveType = moveType;
        MonsterType = type;
    }

    public virtual void TakeDamage(float damage)
    {
        if (IsDead || HasReachedGoal || damage <= 0f)
        {
            return;
        }

        Hp -= damage;

        if (Hp <= 0f)
        {
            Hp = 0f;
            Die();
        }
    }

    protected virtual void Die()
    {
        IsDead = true;
        Died?.Invoke(this);
    }

    protected void CompletePath()
    {
        if (IsDead || HasReachedGoal)
        {
            return;
        }

        HasReachedGoal = true;
        ReachedGoal?.Invoke(this);
    }

    public abstract void Tick(float deltaTime);
}