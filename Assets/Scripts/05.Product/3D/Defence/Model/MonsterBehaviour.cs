using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Unity 씬의 몬스터 GameObject와 도메인 모델 `Monster`를 연결하고 위치를 동기화
/// </summary>
public sealed class MonsterBehaviour : MonoBehaviour
{
    [SerializeField, Min(1f)] private float _maxHp = 100f;
    [SerializeField, Min(0f)] private float _moveSpeed = 2f;
    [SerializeField, Min(0)] private int _reward = 10;
    [SerializeField] private EnumMonsterType _type = EnumMonsterType.Normal;
    [SerializeField] private EnumMonsterMoveType _moveType = EnumMonsterMoveType.Ground;
    [SerializeField] private Transform[] _waypoints;
    [SerializeField] private bool _destroyWhenFinished = true;

    public Monster Model { get; private set; }

    private void Start()
    {
        if (MGameManager.Instance == null || _waypoints == null || _waypoints.Length == 0)
        {
            CPrint.Error("MGameManager or waypoints null 인스펙터 확인", this);
            enabled = false;
            return;
        }

        List<WorldPosition> path = new List<WorldPosition>(_waypoints.Length + 1)
        {
            ToWorldPosition(transform.position)
        };

        for (int i = 0; i < _waypoints.Length; i++)
        {
            if (_waypoints[i] != null)
            {
                path.Add(ToWorldPosition(_waypoints[i].position));
            }
        }

        if (path.Count < 2)
        {
            CPrint.Error("MonsterBehaviour waypoints 는 2 이상 필요", this);
            enabled = false;
            return;
        }

        Model = new Monster(_maxHp, _moveSpeed, _reward, _moveType, _type, path);
        
        Renderer rend = GetComponentInChildren<Renderer>();
        if (rend != null)
        {
            float estimate = Mathf.Max(rend.bounds.extents.x, rend.bounds.extents.z);
            Model.HitRadius = Mathf.Max(0.1f, estimate);
            Model.HitHeight = rend.bounds.max.y;
        }

        MGameManager.Instance.DefenceManager.AddMonster(Model);
    }

    private void LateUpdate()
    {
        if (Model == null)
        {
            return;
        }

        transform.position = ToVector3(Model.Position);
        if (_destroyWhenFinished && (Model.IsDead || Model.HasReachedGoal))
        {
            Destroy(gameObject);
        }
    }

    private static WorldPosition ToWorldPosition(Vector3 position)
    {
        return new WorldPosition(position.x, position.y, position.z);
    }

    private static Vector3 ToVector3(WorldPosition position)
    {
        return new Vector3(position.X, position.Y, position.Z);
    }
}