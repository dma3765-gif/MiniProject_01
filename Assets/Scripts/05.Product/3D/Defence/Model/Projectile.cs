using System;

/// <summary>
/// 투사체의 이동(호밍, 포물선), 충돌 판정 및 피해 처리를 담당
/// </summary>
public sealed class Projectile : ITickable
{
    private readonly IDamageable _target;
    private readonly Func<WorldPosition> _targetPositionProvider;
    private static readonly System.Random _rng = new System.Random();
    private float _turningSpeedDeg = 360f;
    private float _dirX;
    private float _dirY;
    private float _dirZ;
    private float _arcHeight = 0f;
    private float _gravity = 9.81f;
    private float _velX;
    private float _velY;
    private float _velZ;
    private bool _isBoosting;
    private float _boostRemaining;
    private float _boostSpeed;
    

    public WorldPosition Position { get; private set; }
    public float Speed { get; private set; }
    public float Damage { get; private set; }
    public bool IsCompleted { get; private set; }
    public IDamageable Target { get { return _target; } }

    public Projectile(WorldPosition origin, IDamageable target, Func<WorldPosition> targetPositionProvider, float speed, float damage, float turningSpeedDeg = 360f, float arcHeight = 1f, float boostTime = 0f, float boostVerticalSpeed = 0f)
    {
        Position = origin;
        _target = target;
        _targetPositionProvider = targetPositionProvider;
        Speed = Math.Max(0.01f, speed);
        Damage = Math.Max(0f, damage);
        _turningSpeedDeg = Math.Max(0f, turningSpeedDeg);
        _arcHeight = Math.Max(0f, arcHeight);
        _isBoosting = boostTime > 0f;
        _boostRemaining = Math.Max(0f, boostTime);
        _boostSpeed = Math.Max(0f, boostVerticalSpeed);

        try
        {
            WorldPosition tp = _targetPositionProvider();
            float dx = tp.X - Position.X;
            float dy = tp.Y - Position.Y;
            float dz = tp.Z - Position.Z;
            dy += _arcHeight;
            float len = (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);
            if (len <= 1e-6f)
            {
                _dirX = 0f; _dirY = 0f; _dirZ = 1f;
            }
            else
            {
                _dirX = dx / len; _dirY = dy / len; _dirZ = dz / len;
            }

            _velX = _dirX * Speed;
            _velY = _dirY * Speed;
            _velZ = _dirZ * Speed;
        }
        catch
        {
            _dirX = 0f; _dirY = 0f; _dirZ = 1f;
            _velX = _dirX * Speed;
            _velY = _dirY * Speed;
            _velZ = _dirZ * Speed;
        }
    }

    public void Tick(float deltaTime)
    {
        if (IsCompleted || deltaTime <= 0f)
        {
            return;
        }

        AMonsterBase monster = _target as AMonsterBase;



        if (_target == null || _target.IsDead || (monster != null && monster.HasReachedGoal))
        {
            IsCompleted = true;
            return;
        }

        if (_isBoosting)
        {
            float ascend = _boostSpeed * deltaTime;
            Position = new WorldPosition(Position.X, Position.Y + ascend, Position.Z);
            _boostRemaining -= deltaTime;
            if (_boostRemaining <= 0f)
            {
                _isBoosting = false;
                try
                {
                    WorldPosition tpInit = _targetPositionProvider();
                    float dxInit = tpInit.X - Position.X;
                    float dyInit = tpInit.Y - Position.Y;
                    float dzInit = tpInit.Z - Position.Z;
                    dyInit += _arcHeight;
                    float lenInit = (float)Math.Sqrt(dxInit * dxInit + dyInit * dyInit + dzInit * dzInit);
                    if (lenInit <= 1e-6f)
                    {
                        _dirX = 0f; _dirY = 0f; _dirZ = 1f;
                    }
                    else
                    {
                        _dirX = dxInit / lenInit; _dirY = dyInit / lenInit; _dirZ = dzInit / lenInit;
                    }

                    _velX = _dirX * Speed;
                    _velY = _dirY * Speed;
                    _velZ = _dirZ * Speed;
                }
                catch
                {
                    _velX = 0f; _velY = Speed; _velZ = 0f;
                }
            }
            return;
        }

        WorldPosition targetPosition = _targetPositionProvider();
        float moveDistance = Speed * deltaTime;
        float distanceToTarget = WorldPosition.Distance(Position, targetPosition);

        float desiredX = targetPosition.X - Position.X;
        float desiredY = targetPosition.Y - Position.Y;
        float desiredZ = targetPosition.Z - Position.Z;
        float desiredLen = (float)Math.Sqrt(desiredX * desiredX + desiredY * desiredY + desiredZ * desiredZ);
        if (desiredLen > 1e-6f)
        {
            desiredX /= desiredLen; desiredY /= desiredLen; desiredZ /= desiredLen;
        }

        float vlen = (float)Math.Sqrt(_velX * _velX + _velY * _velY + _velZ * _velZ);
        float curX = _velX / Math.Max(1e-6f, vlen);
        float curY = _velY / Math.Max(1e-6f, vlen);
        float curZ = _velZ / Math.Max(1e-6f, vlen);

        float dot = curX * desiredX + curY * desiredY + curZ * desiredZ;
        dot = Math.Max(-1f, Math.Min(1f, dot));
        float angleBetween = (float)Math.Acos(dot);
        float maxTurn = _turningSpeedDeg * (float)Math.PI / 180f * deltaTime;
        float newDirX = curX, newDirY = curY, newDirZ = curZ;

        if (angleBetween <= maxTurn || angleBetween <= 1e-6f)
        {
            newDirX = desiredX; newDirY = desiredY; newDirZ = desiredZ;
        }
        else
        {
            float t = maxTurn / angleBetween;
            float sinTotal = (float)Math.Sin(angleBetween);
            if (sinTotal > 1e-6f)
            {
                float s1 = (float)Math.Sin((1 - t) * angleBetween) / sinTotal;
                float s2 = (float)Math.Sin(t * angleBetween) / sinTotal;
                newDirX = s1 * curX + s2 * desiredX;
                newDirY = s1 * curY + s2 * desiredY;
                newDirZ = s1 * curZ + s2 * desiredZ;
                float nl = (float)Math.Sqrt(newDirX * newDirX + newDirY * newDirY + newDirZ * newDirZ);
                if (nl > 1e-6f) { newDirX /= nl; newDirY /= nl; newDirZ /= nl; }
            }
            else
            {
                newDirX = desiredX; newDirY = desiredY; newDirZ = desiredZ;
            }
        }

        _dirX = newDirX; _dirY = newDirY; _dirZ = newDirZ;

        float speedMag = vlen;
        _velX = _dirX * speedMag;
        _velY = _dirY * speedMag;
        _velZ = _dirZ * speedMag;

        _velY -= _gravity * deltaTime;

        WorldPosition prevPos = Position;
        float nextX = Position.X + _velX * deltaTime;
        float nextY = Position.Y + _velY * deltaTime;
        float nextZ = Position.Z + _velZ * deltaTime;
        float moveDistThisTick = (float)Math.Sqrt((_velX * deltaTime) * (_velX * deltaTime) + (_velY * deltaTime) * (_velY * deltaTime) + (_velZ * deltaTime) * (_velZ * deltaTime));

        if (distanceToTarget <= moveDistThisTick)
        {
            
            try
            {
                if (_target != null)
                {
                    WorldPosition center = _target.Position;
                    float radius = 0.5f;
                    try { radius = _target.HitRadius; } catch { }

                    
                    float px = prevPos.X; float py = prevPos.Y; float pz = prevPos.Z;
                    float cx = center.X; float cy = center.Y; float cz = center.Z;
                    float sx = nextX - px; float sy = nextY - py; float sz = nextZ - pz;

                    float ox = px - cx; float oy = py - cy; float oz = pz - cz;

                    float a = sx * sx + sy * sy + sz * sz;
                    float b = 2f * (sx * ox + sy * oy + sz * oz);
                    float c = ox * ox + oy * oy + oz * oz - radius * radius;
                    float disc = b * b - 4f * a * c;
                    float chosenT = float.NaN;
                    if (disc >= 0f && a > 1e-6f)
                    {
                        float sqrtD = (float)Math.Sqrt(disc);
                        float t1 = (-b - sqrtD) / (2f * a);
                        float t2 = (-b + sqrtD) / (2f * a);
                        if (t1 >= 0f && t1 <= 1f) chosenT = t1;
                        else if (t2 >= 0f && t2 <= 1f) chosenT = t2;
                    }

                    if (float.IsNaN(chosenT))
                    {
                        Position = new WorldPosition(center.X, center.Y, center.Z);
                    }
                    else
                    {
                        Position = new WorldPosition(px + sx * chosenT, py + sy * chosenT, pz + sz * chosenT);
                    }
                }
                else
                {
                    Position = targetPosition;
                }
            }
            catch
            {
                Position = targetPosition;
            }

            if (_target != null)
            {
                _target.TakeDamage(Damage);
            }

            IsCompleted = true;
            return;
        }

        else
        {
            Position = new WorldPosition(nextX, nextY, nextZ);
        }
    }
}