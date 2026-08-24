using System;
using System.Collections.Generic;

public class Monster : AMonsterBase
{
    private readonly List<WorldPosition> _path;
    private int _nextWaypointIndex;

    public Monster(float maxHp, float moveSpeed, int reward, EnumMonsterMoveType moveType, EnumMonsterType type, IList<WorldPosition> path)
        : base(maxHp, moveSpeed, reward, moveType, type, GetStartPosition(path))
    {
        _path = new List<WorldPosition>(path);
        _nextWaypointIndex = 1;
    }

    public override void Tick(float deltaTime)
    {
        if (IsDead || HasReachedGoal || deltaTime <= 0f)
        {
            return;
        }

        float remainingDistance = MoveSpeed * deltaTime;
        while (remainingDistance > 0f && _nextWaypointIndex < _path.Count)
        {
            WorldPosition destination = _path[_nextWaypointIndex];
            float distance = WorldPosition.Distance(Position, destination);
            Position = WorldPosition.MoveTowards(Position, destination, remainingDistance);

            if (distance > remainingDistance)
            {
                break;
            }

            remainingDistance -= distance;
            _nextWaypointIndex++;
        }

        if (_nextWaypointIndex >= _path.Count)
        {
            CompletePath();
        }
    }

    private static WorldPosition GetStartPosition(IList<WorldPosition> path)
    {
        if (path == null || path.Count < 2)
        {
            throw new ArgumentException("몬스터 경로 에러", nameof(path));
        }

        return path[0];
    }
}