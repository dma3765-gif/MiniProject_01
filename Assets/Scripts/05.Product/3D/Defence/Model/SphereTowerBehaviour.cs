using UnityEngine;

public sealed class SphereTowerBehaviour : MonoBehaviour
{
    #region 인스펙터
    [Header("Tower")]
    [SerializeField, Min(0f)] private float _attackRange = 5f;
    [SerializeField, Min(0)] private int _sellPrice = 50;

    [Header("Weapon")]
    [SerializeField, Min(0f)] private float _damage = 10f;
    [SerializeField, Min(0.01f)] private float _attackInterval = 1f;
    [SerializeField, Min(0.01f)] private float _projectileSpeed = 10f;
    [SerializeField] private ProjectileBehaviour _projectilePrefab; 
    #endregion

    public SphereTower Model { get; private set; }

    private TowerWeapon _weapon;

    private void Start()
    {
        if (MGameManager.Instance == null)
        {
            Debug.LogError("MGameManager 인스턴스 확인", this);
            enabled = false;
            return;
        }

        Vector3 position = transform.position;
        Model = new SphereTower(
            new WorldPosition(position.x, position.y, position.z),
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