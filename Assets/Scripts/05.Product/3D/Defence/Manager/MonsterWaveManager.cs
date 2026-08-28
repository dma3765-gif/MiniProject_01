using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MonsterWaveManager : MonoBehaviour
{
    #region 인스펙터
    [SerializeField] private float _normalSpawnInterval = 0.35f;
    [SerializeField] private bool _autoStartNextWave = false;
    [SerializeField] private float _nextWaveDelay = 2f;
    #endregion

    #region 내부 변수
    private Transform _startPos;
    private Transform[] _waypoints;
    private readonly Dictionary<string, GameObject> _prefabMap = new Dictionary<string, GameObject>();
    private int _waveIndex;
    private bool _isRunning;
    private int _currentMonsterTotal;
    private int _currentMonsterKilled;
    private int _currentMonsterEscaped;

    public bool IsRunning { get { return _isRunning; } }
    public int CurrentWaveNumber { get { return Mathf.Clamp(_waveIndex + 1, 1, MonsterDataStore.WaveList.Count); } }
    public int MaxWaveCount { get { return MonsterDataStore.WaveList.Count; } }

    public int CurrentMonsterTotal { get { return _currentMonsterTotal; } }
    public int CurrentMonsterKilled { get { return _currentMonsterKilled; } }
    public int CurrentMonsterEscaped { get { return _currentMonsterEscaped; } }

    public event Action<MonsterWaveData> WaveStarted;
    public event Action<MonsterWaveData> WaveCleared;
    public event Action GameCleared;
    public event Action<int, int, int> MonsterProgressChanged;
    #endregion

    public void Init()
    {
        Transform wavePoint = GameObject.Find("GameBoundPoint/WavePoint")?.transform;
        if (wavePoint == null)
        {
            CPrint.Error("GameBoundPoint/WavePoint 를 찾을 수 없습니다", this);
            return;
        }

        _startPos = wavePoint.Find("StartPos");
        Transform waypointRoot = wavePoint.Find("Waypoints");
        Transform endPos = wavePoint.Find("EndPos");

        if (_startPos == null || waypointRoot == null || endPos == null)
        {
            CPrint.Error("StartPos, Waypoints, EndPos 계층을 확인하세요", this);
            return;
        }

        List<Transform> path = new List<Transform>();
        for (int i = 0; i < waypointRoot.childCount; i++)
        {
            path.Add(waypointRoot.GetChild(i));
        }
        path.Add(endPos);
        _waypoints = path.ToArray();

        LoadMonsterPrefabs();
        MGameManager.Instance.DefenceManager.MonsterDied += OnMonsterDied;
        MGameManager.Instance.DefenceManager.MonsterReachedGoal += OnMonsterReachedGoal;
    }

    public void StartGame()
    {
        if (_isRunning || _waveIndex >= MonsterDataStore.WaveList.Count)
        {
            return;
        }

        StartCoroutine(RunWaveLoop());
    }

    private IEnumerator RunWaveLoop()
    {
        _isRunning = true;

        while (_waveIndex < MonsterDataStore.WaveList.Count)
        {
            MonsterWaveData data = MonsterDataStore.WaveList[_waveIndex];

            _currentMonsterTotal = data.SpawnCount;
            _currentMonsterKilled = 0;
            _currentMonsterEscaped = 0;

            MonsterProgressChanged?.Invoke(_currentMonsterTotal, _currentMonsterKilled, _currentMonsterEscaped);
            WaveStarted?.Invoke(data);

            yield return SpawnWave(data);
            yield return new WaitUntil(() => MGameManager.Instance.DefenceManager.MonsterList.Count == 0);

            _waveIndex++;

            if (_waveIndex >= MonsterDataStore.WaveList.Count)
            {
                _isRunning = false;
                WaveCleared?.Invoke(data);
                GameCleared?.Invoke();
                yield break;
            }

            if (!_autoStartNextWave)
            {
                _isRunning = false;
                WaveCleared?.Invoke(data);
                yield break;
            }

            WaveCleared?.Invoke(data);

            yield return new WaitForSeconds(_nextWaveDelay);
        }
    }

    private IEnumerator SpawnWave(MonsterWaveData data)
    {
        if (!_prefabMap.TryGetValue(data.PrefabName, out GameObject prefab) || prefab == null)
        {
            CPrint.Error($"몬스터 프리팹을 찾을 수 없습니다: {data.PrefabName}", this);
            yield break;
        }

        for (int i = 0; i < data.SpawnCount; i++)
        {
            GameObject monsterObj = Instantiate(prefab, _startPos.position, _startPos.rotation);
            monsterObj.name = $"{data.PrefabName}_{i + 1:00}";

            MonsterBehaviour behaviour = monsterObj.GetComponent<MonsterBehaviour>();
            if (behaviour == null)
            {
                behaviour = monsterObj.AddComponent<MonsterBehaviour>();
            }

            behaviour.Setup(data, _waypoints);

            if (!data.IsBoss && i < data.SpawnCount - 1)
            {
                yield return new WaitForSeconds(_normalSpawnInterval);
            }
        }
    }

    private void LoadMonsterPrefabs()
    {
        _prefabMap.Clear();

        GameObject[] prefabs = Resources.LoadAll<GameObject>("Monster/Level");
        for (int i = 0; i < prefabs.Length; i++)
        {
            if (!_prefabMap.ContainsKey(prefabs[i].name))
            {
                _prefabMap.Add(prefabs[i].name, prefabs[i]);
            }
        }

        if (_prefabMap.Count == 0)
        {
            CPrint.Error("Resources/Monster/Level 에 몬스터 프리팹이 없습니다", this);
        }
    }

    private void OnMonsterDied(AMonsterBase monster)
    {
        if (!_isRunning)
        {
            return;
        }

        _currentMonsterKilled++;
        MonsterProgressChanged?.Invoke(_currentMonsterTotal, _currentMonsterKilled, _currentMonsterEscaped);
    }

    private void OnMonsterReachedGoal(AMonsterBase monster)
    {
        if (!_isRunning)
        {
            return;
        }

        _currentMonsterEscaped++;
        MonsterProgressChanged?.Invoke(_currentMonsterTotal, _currentMonsterKilled, _currentMonsterEscaped);
    }
}
