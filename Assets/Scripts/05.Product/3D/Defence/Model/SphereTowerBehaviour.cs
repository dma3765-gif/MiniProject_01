using Unity.VisualScripting.Dependencies.Sqlite;
using UnityEngine;

public sealed class SphereTowerBehaviour : MonoBehaviour
{
    #region 인스펙터
    [Header("Tower")]
    [SerializeField, Min(0f)] private float _attackRange = 5f;
    [SerializeField, Min(0)] private int _sellPrice = 50;
    [SerializeField, Min(0)] private int _level = 0;

    [Header("Weapon")]
    [SerializeField, Min(0f)] private float _damage = 10f;
    [SerializeField, Min(0.01f)] private float _attackInterval = 1f;
    [SerializeField, Min(0.01f)] private float _projectileSpeed = 10f;
    [SerializeField] private ProjectileBehaviour _projectilePrefab;
    #endregion

    #region 내부 변수
    public SphereTower Model { get; private set; }

    private TowerWeapon _weapon;

    private Transform _trTowerBase;
    private Transform _trTowerBottom;
    #endregion

    private void Awake()
    {
        _trTowerBase = transform.Find("MiddelPivot/TowerBase");
        _trTowerBottom = transform.Find("TowerBottom");

        if (_trTowerBase == null || _trTowerBottom == null)
        {
            CPrint.Error("TowerBase or TowerBottom null => 인스펙터 확인");
            return;
        }
    }

    private void Start()
    {
        if (MGameManager.Instance == null)
        {
            CPrint.Error("MGameManager 인스턴스 확인", this);
            enabled = false;
            return;
        }

        Vector3 position = transform.position;
        Model = new SphereTower(
            new WorldPosition(position.x, position.y, position.z),
            _level,
            _attackRange,
            _sellPrice);

        _weapon = new TowerWeapon(_damage, _attackInterval, _projectileSpeed);
        _weapon.ProjectileCreated += OnProjectileCreated;
        Model.AddWeapon(_weapon);
        MGameManager.Instance.DefenceManager.AddTower(Model);
    }

    public void Sell()
    {
        if (Model != null)
        {
            Model.Sell();
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        if (_weapon != null)
        {
            _weapon.ProjectileCreated -= OnProjectileCreated;
        }
    }

    private void OnProjectileCreated(Projectile projectile)
    {
        if (_projectilePrefab == null)
        {
            return;
        }

        ProjectileBehaviour projectileBehaviour = Instantiate(_projectilePrefab, transform.position, Quaternion.identity);
        projectileBehaviour.Bind(projectile);
    }
}