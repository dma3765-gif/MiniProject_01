using UnityEngine;

public class MGameManager : MonoBehaviour
{
    [SerializeField] private int _initialGold = 100;
    [SerializeField] private int _initialLives = 10;

    [Header("Tower Placement")]
    [SerializeField] private GameObject _towerPrefab;
    [SerializeField] private Camera _placementCamera;
    [SerializeField, Min(0f)] private float _towerSpacing = 2f;
    [SerializeField] private float _placementHeightOffset = 0f;
    [SerializeField, Min(1)] private int _placementSnapDivision = 4;

    public static MGameManager Instance { get; private set; }
    public DefenceManager DefenceManager { get; private set; }
    public MonsterWaveManager WaveManager { get; private set; }
    public TowerPlacementManager PlacementManager { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DefenceManager = new DefenceManager(_initialGold, _initialLives);
        DefenceManager.Init();

        WaveManager = GetComponent<MonsterWaveManager>();
        if (WaveManager == null)
        {
            WaveManager = gameObject.AddComponent<MonsterWaveManager>();
        }
        WaveManager.Init();

        PlacementManager = GetComponent<TowerPlacementManager>();
        if (PlacementManager == null)
        {
            PlacementManager = gameObject.AddComponent<TowerPlacementManager>();
        }
        PlacementManager.Init(_towerPrefab, _placementCamera, _towerSpacing, _placementHeightOffset, _placementSnapDivision);
    }

    private void Start()
    {
        DefenceManager.Main();
    }

    private void Update()
    {
        DefenceManager.Tick(Time.deltaTime);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
