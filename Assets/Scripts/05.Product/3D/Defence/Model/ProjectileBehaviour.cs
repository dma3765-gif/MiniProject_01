using UnityEngine;

/// <summary>
/// `Projectile` 모델의 위치를 Unity Transform에 동기화하고 히트 이펙트를 재생하는 뷰
/// </summary>
public sealed class ProjectileBehaviour : MonoBehaviour
{
    private Projectile _model;
    [SerializeField] private GameObject _hitEffectPrefab;
    [SerializeField, Min(0.1f)] private float _hitEffectDuration = 2f;
    private bool _playedHitEffect = false;
    private Vector3 _lastModelPosition;

    public void Bind(Projectile model)
    {
        _model = model;
        SyncPosition();
        _lastModelPosition = transform.position;
    }

    private void LateUpdate()
    {
        if (_model == null)
        {
            return;
        }

        WorldPosition wpBefore = _model.Position;
        Vector3 currentPos = new Vector3(wpBefore.X, wpBefore.Y, wpBefore.Z);
        Vector3 delta = currentPos - _lastModelPosition;
        if (delta.sqrMagnitude > 1e-6f)
        {
            transform.forward = delta.normalized;
        }

        transform.position = currentPos;
        _lastModelPosition = currentPos;
        if (_model.IsCompleted)
        {
            if (!_playedHitEffect)
            {
                PlayHitEffect();
                _playedHitEffect = true;
            }

            Destroy(gameObject);
        }
    }

    private void SyncPosition()
    {
        WorldPosition position = _model.Position;
        transform.position = new Vector3(position.X, position.Y, position.Z);
    }

    private void PlayHitEffect()
    {        
        WorldPosition wp = _model.Position;
        Vector3 spawn = new Vector3(wp.X, wp.Y, wp.Z);

        if (_hitEffectPrefab != null)
        {
            GameObject fx = Instantiate(_hitEffectPrefab, spawn, Quaternion.identity);
            if (_hitEffectDuration > 0f)
            {
                Destroy(fx, _hitEffectDuration);
            }
        }
    }
}