using System;
using System.Collections.Generic;

public class DefenceManager : AManagerBase
{
    private readonly List<ATowerBase> _playerTowerList = new List<ATowerBase>();
    private readonly List<AMonsterBase> _monsterList = new List<AMonsterBase>();
    private readonly List<Projectile> _projectileList = new List<Projectile>();

    public IReadOnlyList<ATowerBase> PlayerTowerList { get { return _playerTowerList; } }
    public IReadOnlyList<AMonsterBase> MonsterList { get { return _monsterList; } }
    public IReadOnlyList<Projectile> ProjectileList { get { return _projectileList; } }
    public int Gold { get; private set; }
    public int Lives { get; private set; }

    public event Action<int> GoldChanged;
    public event Action<int> LivesChanged;
    public event Action<AMonsterBase> MonsterDied;
    public event Action<AMonsterBase> MonsterReachedGoal;

    public DefenceManager(int initialGold = 100, int initialLives = 20)
    {
        Gold = Math.Max(0, initialGold);
        Lives = Math.Max(0, initialLives);
    }

    protected override void OnInit()
    {

    }

    public void AddTower(ATowerBase tower)
    {
        if (tower == null || _playerTowerList.Contains(tower))
        {
            return;
        }

        _playerTowerList.Add(tower);
        tower.SetTargetProvider(FindNearestMonster);
        tower.Sold += OnTowerSold;
        tower.WeaponAdded += OnWeaponAdded;

        for (int i = 0; i < tower.WeaponList.Count; i++)
        {
            SubscribeWeapon(tower.WeaponList[i]);
        }
    }

    public void AddMonster(AMonsterBase monster)
    {
        if (monster == null || _monsterList.Contains(monster))
        {
            return;
        }

        _monsterList.Add(monster);
        monster.Died += OnMonsterDied;
        monster.ReachedGoal += OnMonsterReachedGoal;
    }

    protected override void OnTick(float deltaTime)
    {
        for (int i = _monsterList.Count - 1; i >= 0; i--)
        {
            AMonsterBase monster = _monsterList[i];
            monster.Tick(deltaTime);
            if (monster.IsDead || monster.HasReachedGoal)
            {
                RemoveMonsterAt(i, monster);
            }
        }

        for (int i = _playerTowerList.Count - 1; i >= 0; i--)
        {
            ATowerBase tower = _playerTowerList[i];
            tower.Tick(deltaTime);
            if (tower.IsSold)
            {
                RemoveTowerAt(i, tower);
            }
        }

        for (int i = _projectileList.Count - 1; i >= 0; i--)
        {
            _projectileList[i].Tick(deltaTime);
            if (_projectileList[i].IsCompleted)
            {
                _projectileList.RemoveAt(i);
            }
        }
    }

    private AMonsterBase FindNearestMonster(WorldPosition origin, float range)
    {
        AMonsterBase nearest = null;
        float nearestDistance = range;

        for (int i = 0; i < _monsterList.Count; i++)
        {
            AMonsterBase monster = _monsterList[i];
            if (monster.IsDead || monster.HasReachedGoal)
            {
                continue;
            }

            float distance = WorldPosition.Distance(origin, monster.Position);
            if (distance <= nearestDistance)
            {
                nearest = monster;
                nearestDistance = distance;
            }
        }

        return nearest;
    }

    private void OnMonsterDied(AMonsterBase monster)
    {
        Gold += monster.Reward;
        GoldChanged?.Invoke(Gold);
        MonsterDied?.Invoke(monster);
    }

    private void OnMonsterReachedGoal(AMonsterBase monster)
    {
        Lives = Math.Max(0, Lives - 1);
        LivesChanged?.Invoke(Lives);
        MonsterReachedGoal?.Invoke(monster);
    }

    private void OnTowerSold(ATowerBase tower)
    {
        Gold += tower.SellPrice;
        GoldChanged?.Invoke(Gold);
    }

    private void OnWeaponAdded(ATowerWeaponBase weapon)
    {
        SubscribeWeapon(weapon);
    }

    private void SubscribeWeapon(ATowerWeaponBase weapon)
    {
        TowerWeapon towerWeapon = weapon as TowerWeapon;
        if (towerWeapon != null)
        {
            towerWeapon.ProjectileCreated -= OnProjectileCreated;
            towerWeapon.ProjectileCreated += OnProjectileCreated;
        }
    }

    private void OnProjectileCreated(Projectile projectile)
    {
        if (projectile != null)
        {
            _projectileList.Add(projectile);
        }
    }

    private void RemoveMonsterAt(int index, AMonsterBase monster)
    {
        monster.Died -= OnMonsterDied;
        monster.ReachedGoal -= OnMonsterReachedGoal;
        _monsterList.RemoveAt(index);
    }

    private void RemoveTowerAt(int index, ATowerBase tower)
    {
        tower.Sold -= OnTowerSold;
        tower.WeaponAdded -= OnWeaponAdded;
        for (int i = 0; i < tower.WeaponList.Count; i++)
        {
            TowerWeapon weapon = tower.WeaponList[i] as TowerWeapon;
            if (weapon != null)
            {
                weapon.ProjectileCreated -= OnProjectileCreated;
            }
        }
        _playerTowerList.RemoveAt(index);
    }
}
