using System;
using System.Collections.Generic;

/// <summary>
/// 타워 모델의 공통 속성 및 무기 관리를 제공하는 기본 추상 클래스
/// </summary>
public abstract class ATowerBase : ISellable, ITickable
{
    private Func<WorldPosition, float, AMonsterBase> _targetProvider;
    private readonly Dictionary<ATowerWeaponBase, Func<WorldPosition>> _weaponPositionProviders;

    public IReadOnlyList<ATowerWeaponBase> WeaponList { get { return _weaponList; } }
    private readonly List<ATowerWeaponBase> _weaponList;

    public int SellPrice { get; protected set; }
    public float AttackRange { get; protected set; }
    public WorldPosition Position { get; private set; }
    public bool IsSold { get; private set; }

    public const int MaxLevel = 27;

    public int Level { get; protected set; } = 1;    

    public event Action<ATowerBase> Sold;
    public event Action<ATowerWeaponBase> WeaponAdded;

    protected ATowerBase(WorldPosition position, int level, float attackRange, int sellPrice)
    {
        Position = position;
        Level = Math.Max(0, level);

        if (Level > MaxLevel)
        {
            Level = MaxLevel;
        }

        AttackRange = Math.Max(0f, attackRange);
        SellPrice = Math.Max(0, sellPrice);
        _weaponList = new List<ATowerWeaponBase>();
        _weaponPositionProviders = new Dictionary<ATowerWeaponBase, Func<WorldPosition>>();
    }

    public void AddWeapon(ATowerWeaponBase weapon)
    {
        AddWeapon(weapon, null);
    }

    public void AddWeapon(ATowerWeaponBase weapon, Func<WorldPosition> positionProvider)
    {
        if (weapon == null)
        {
            throw new ArgumentNullException(nameof(weapon));
        }

        _weaponList.Add(weapon);
        _weaponPositionProviders[weapon] = positionProvider;
        BindWeapon(weapon);
        WeaponAdded?.Invoke(weapon);
    }

    internal void SetTargetProvider(Func<WorldPosition, float, AMonsterBase> targetProvider)
    {
        _targetProvider = targetProvider;
        for (int i = 0; i < _weaponList.Count; i++)
        {
            BindWeapon(_weaponList[i]);
        }
    }

    public virtual void Sell()
    {
        if (IsSold)
        {
            return;
        }

        IsSold = true;
        Sold?.Invoke(this);
    }

    public virtual void Tick(float deltaTime)
    {
        if (IsSold)
        {
            return;
        }

        for (int i = 0; i < _weaponList.Count; i++)
        {
            _weaponList[i].Tick(deltaTime);
        }
    }

    private void BindWeapon(ATowerWeaponBase weapon)
    {
        Func<WorldPosition> positionProvider;
        if (!_weaponPositionProviders.TryGetValue(weapon, out positionProvider) || positionProvider == null)
        {
            positionProvider = () => Position;
        }

        weapon.Bind(positionProvider, () => AttackRange, _targetProvider);
    }
}
