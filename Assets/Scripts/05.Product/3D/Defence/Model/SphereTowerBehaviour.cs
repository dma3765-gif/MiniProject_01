using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 타워 프리팹의 뷰와 도메인 `SphereTower` 모델을 연결하고 무기(프리팹)를 생성/관리
/// </summary>
public sealed class SphereTowerBehaviour : MonoBehaviour
{
    private const int MaxWeaponCount = 10;
    private const string MaterialFolder = "Assets/Resource/Tower/Materials";

    [Header("Tower")]
    [SerializeField, Min(0f)] private float _attackRange = 5f;
    [SerializeField, Min(0)] private int _sellPrice = 50;
    [SerializeField, Range(0, ATowerBase.MaxLevel)] private int _level;
    [SerializeField, HideInInspector] private Material[] _levelMaterials;

    [Header("Orbit")]
    [SerializeField, Min(0f)] private float _orbitRadius = 1.5f;
    [SerializeField, Min(0f)] private float _orbitClearance = 0.5f;
    [SerializeField] private bool _fitOrbitToTowerBase = true;
    [SerializeField] private float _rotationSpeed = 60f;

    [Header("Weapons (1 - 10)")]
    [SerializeField] private GameObject[] _weaponPrefabs;
    [SerializeField, Min(0f)] private float _damage = 10f;
    [SerializeField, Min(0.01f)] private float _attackInterval = 1f;
    [SerializeField, Min(0.01f)] private float _projectileSpeed = 10f;
    [SerializeField, Min(1f)] private float _arcHeight = 1f;
    [SerializeField, Min(0f)] private float _boostTime = 0.5f;
    [SerializeField, Min(0f)] private float _boostVerticalSpeed = 10f;
    [SerializeField] private ProjectileBehaviour _projectilePrefab;

    public SphereTower Model { get; private set; }

    private readonly List<WeaponBinding> _weaponBindings = new List<WeaponBinding>();
    private Transform _middlePivot;
    private Transform _towerBase;
    private Transform _towerBottom;
    private Transform _weaponsRoot;
    private Material _levelMaterial;

    private sealed class WeaponBinding
    {
        public TowerWeapon Model;
        public Transform View;
        public Action<Projectile> ProjectileHandler;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        CacheLevelMaterialsInEditor();
        if (CacheHierarchy())
        {
            ApplyLevelMaterial();
        }
    }
#endif

    private void Awake()
    {
        CacheHierarchy();
        ApplyLevelMaterial();
    }

    private void Start()
    {
        if (MGameManager.Instance == null)
        {
            CPrint.Error("MGameManager 인스턴스 확인", this);
            enabled = false;
            return;
        }

        if (!CacheHierarchy())
        {
            enabled = false;
            return;
        }

        Vector3 position = transform.position;
        Model = new SphereTower(
            new WorldPosition(position.x, position.y, position.z),
            _level,
            _attackRange,
            _sellPrice);

        CreateWeapons();
        MGameManager.Instance.DefenceManager.AddTower(Model);
    }

    private void Update()
    {
        if (_middlePivot != null)
        {
            _middlePivot.Rotate(0f, _rotationSpeed * Time.deltaTime, 0f, Space.Self);
        }
    }

    public void Sell()
    {
        if (Model == null)
        {
            return;
        }

        Model.Sell();
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        for (int i = 0; i < _weaponBindings.Count; i++)
        {
            WeaponBinding binding = _weaponBindings[i];
            binding.Model.ProjectileCreated -= binding.ProjectileHandler;
        }

        _weaponBindings.Clear();
    }

    private bool CacheHierarchy()
    {
        _middlePivot = transform.Find("MiddlePivot");
        _towerBottom = transform.Find("TowerBottom");
        _towerBase = _middlePivot != null ? _middlePivot.Find("TowerBase") : null;
        _weaponsRoot = _middlePivot != null ? _middlePivot.Find("Weapons") : null;

        if (_middlePivot == null || _towerBottom == null || _towerBase == null || _weaponsRoot == null)
        {
            CPrint.Error("TowerBottom, MiddlePivot/TowerBase, MiddlePivot/Weapons 확인", this);
            return false;
        }

        return true;
    }

    private void CreateWeapons()
    {
        if (_weaponPrefabs == null || _weaponPrefabs.Length == 0)
        {
            CPrint.Error("Weapon Prefabs 확인", this);
            return;
        }

        int weaponCount = 0;
        for (int i = 0; i < _weaponPrefabs.Length && weaponCount < MaxWeaponCount; i++)
        {
            if (_weaponPrefabs[i] != null)
            {
                weaponCount++;
            }
        }

        int weaponIndex = 0;
        float orbitDistance = GetOrbitDistance();
        for (int i = 0; i < _weaponPrefabs.Length && weaponIndex < weaponCount; i++)
        {
            GameObject prefab = _weaponPrefabs[i];
            if (prefab == null)
            {
                continue;
            }

            float angle = 360f * weaponIndex / weaponCount;
            float radians = angle * Mathf.Deg2Rad;
            Vector3 pivotLocalPosition = new Vector3(
                Mathf.Cos(radians) * orbitDistance,
                0f,
                Mathf.Sin(radians) * orbitDistance);
            Vector3 worldPosition = _middlePivot.TransformPoint(pivotLocalPosition);
            Vector3 weaponRootLocalPosition = _weaponsRoot.InverseTransformPoint(worldPosition);

            GameObject weaponObject = Instantiate(prefab, _weaponsRoot);
            weaponObject.name = prefab.name + "_" + (weaponIndex + 1).ToString("00");
            weaponObject.transform.localPosition = weaponRootLocalPosition;
            weaponObject.transform.localRotation = Quaternion.Euler(0f, -angle, 0f);
            ApplyMaterial(weaponObject.transform, _levelMaterial);

            TowerWeapon weaponModel = new TowerWeapon(_damage, _attackInterval, _projectileSpeed, 360, _arcHeight, _boostTime, _boostVerticalSpeed);
            Transform weaponTransform = weaponObject.transform;
            Action<Projectile> handler = projectile => OnProjectileCreated(projectile, weaponTransform);
            weaponModel.ProjectileCreated += handler;

            _weaponBindings.Add(new WeaponBinding
            {
                Model = weaponModel,
                View = weaponTransform,
                ProjectileHandler = handler
            });

            Model.AddWeapon(weaponModel, () => ToWorldPosition(weaponTransform.position));
            weaponIndex++;
        }
    }

    private float GetOrbitDistance()
    {
        if (!_fitOrbitToTowerBase)
        {
            return _orbitRadius + _orbitClearance;
        }

        float towerBaseRadius = 0f;
        Renderer[] renderers = _towerBase.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Bounds bounds = renderers[i].bounds;
            Vector3 min = bounds.min;
            Vector3 max = bounds.max;

            for (int x = 0; x < 2; x++)
            {
                for (int y = 0; y < 2; y++)
                {
                    for (int z = 0; z < 2; z++)
                    {
                        Vector3 corner = new Vector3(
                            x == 0 ? min.x : max.x,
                            y == 0 ? min.y : max.y,
                            z == 0 ? min.z : max.z);
                        Vector3 pivotLocalCorner = _middlePivot.InverseTransformPoint(corner);
                        float radius = new Vector2(pivotLocalCorner.x, pivotLocalCorner.z).magnitude;
                        towerBaseRadius = Mathf.Max(towerBaseRadius, radius);
                    }
                }
            }
        }

        return Mathf.Max(_orbitRadius, towerBaseRadius) + _orbitClearance;
    }

    private void OnProjectileCreated(Projectile projectile, Transform weaponTransform)
    {
        if (_projectilePrefab == null)
        {
            return;
        }

        ProjectileBehaviour projectileBehaviour = Instantiate(
            _projectilePrefab,
            weaponTransform.position,
            Quaternion.identity);
        projectileBehaviour.Bind(projectile);
    }

    private void ApplyLevelMaterial()
    {
        _level = Mathf.Clamp(_level, 0, ATowerBase.MaxLevel);
        _levelMaterial = FindLevelMaterial(_level);
        if (_levelMaterial == null)
        {
            CPrint.Error("레벨 " + _level + "에 해당하는 타워 Material을 못찾음", this);
            return;
        }

        ApplyMaterial(_towerBottom, _levelMaterial);
        ApplyMaterial(_towerBase, _levelMaterial);
    }

    private static void ApplyMaterial(Transform root, Material material)
    {
        if (root == null || material == null)
        {
            return;
        }

        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].sharedMaterial = material;
        }
    }

    private static WorldPosition ToWorldPosition(Vector3 position)
    {
        return new WorldPosition(position.x, position.y, position.z);
    }

    private Material FindLevelMaterial(int level)
    {
        if (_levelMaterials != null && level < _levelMaterials.Length && _levelMaterials[level] != null)
        {
            return _levelMaterials[level];
        }

        string prefix = "M_Lv" + level.ToString("00") + "_";

#if UNITY_EDITOR
        string[] guids = UnityEditor.AssetDatabase.FindAssets("t:Material", new[] { MaterialFolder });
        for (int i = 0; i < guids.Length; i++)
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[i]);
            Material material = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null && material.name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return material;
            }
        }
#endif

        Material[] runtimeMaterials = Resources.LoadAll<Material>("Tower/Materials");
        for (int i = 0; i < runtimeMaterials.Length; i++)
        {
            if (runtimeMaterials[i].name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return runtimeMaterials[i];
            }
        }

        return null;
    }

#if UNITY_EDITOR
    private void CacheLevelMaterialsInEditor()
    {
        if (_levelMaterials == null || _levelMaterials.Length != ATowerBase.MaxLevel + 1)
        {
            _levelMaterials = new Material[ATowerBase.MaxLevel + 1];
        }

        string[] guids = UnityEditor.AssetDatabase.FindAssets("t:Material", new[] { MaterialFolder });
        for (int i = 0; i < guids.Length; i++)
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[i]);
            Material material = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                continue;
            }

            for (int level = 0; level <= ATowerBase.MaxLevel; level++)
            {
                string prefix = "M_Lv" + level.ToString("00") + "_";
                if (material.name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    _levelMaterials[level] = material;
                    break;
                }
            }
        }
    }
#endif
}