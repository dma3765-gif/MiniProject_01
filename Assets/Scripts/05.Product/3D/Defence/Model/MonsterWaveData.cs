using System;
using System.Collections.Generic;

[Serializable]
public class MonsterWaveData
{
    public int TotalWave;
    public int Level;
    public int WaveInLevel;
    public EnumMonsterType MonsterType;
    public string PrefabName;
    public int SpawnCount;
    public float Hp;
    public float MoveSpeed;
    public int Reward;

    public bool IsBoss { get { return MonsterType == EnumMonsterType.Boss; } }
}

public static class MonsterDataStore
{
    public const int MaxLevel = 7;
    public const int NormalWaveCount = 4;
    public const int NormalMonsterCount = 20;

    private static readonly List<MonsterWaveData> _waveList = CreateWaveList();
    public static IReadOnlyList<MonsterWaveData> WaveList { get { return _waveList; } }
    private static List<MonsterWaveData> CreateWaveList()
    {
        List<MonsterWaveData> list = new List<MonsterWaveData>();
        int totalWave = 1;

        for (int level = 1; level <= MaxLevel; level++)
        {
            float levelHp = 100f * (float)Math.Pow(1.55f, level - 1);
            float levelSpeed = 5.8f * (1f + ((level - 1) * 0.035f));
            int levelReward = 5 + ((level - 1) * 2);

            for (int wave = 1; wave <= NormalWaveCount; wave++)
            {
                list.Add(new MonsterWaveData
                {
                    TotalWave = totalWave++,
                    Level = level,
                    WaveInLevel = wave,
                    MonsterType = EnumMonsterType.Normal,
                    PrefabName = $"MOB_LV_{level:00}",
                    SpawnCount = NormalMonsterCount,
                    Hp = levelHp * (1f + ((wave - 1) * 0.12f)),
                    MoveSpeed = levelSpeed * (1f + ((wave - 1) * 0.02f)),
                    Reward = levelReward
                });
            }

            list.Add(new MonsterWaveData
            {
                TotalWave = totalWave++,
                Level = level,
                WaveInLevel = 5,
                MonsterType = EnumMonsterType.Boss,
                PrefabName = $"MOB_BOSS_LV_{level:00}",
                SpawnCount = 1,
                Hp = levelHp * 12f,
                MoveSpeed = levelSpeed * 0.82f,
                Reward = levelReward * 20
            });
        }

        return list;
    }
}
