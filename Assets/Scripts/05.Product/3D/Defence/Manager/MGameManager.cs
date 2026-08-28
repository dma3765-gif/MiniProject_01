using UnityEngine;

public class MGameManager : MonoBehaviour
{
    [SerializeField] private int _initialGold = 100;
    [SerializeField] private int _initialLives = 10;

    public static MGameManager Instance { get; private set; }
    public DefenceManager DefenceManager { get; private set; }
    public MonsterWaveManager WaveManager { get; private set; }

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
