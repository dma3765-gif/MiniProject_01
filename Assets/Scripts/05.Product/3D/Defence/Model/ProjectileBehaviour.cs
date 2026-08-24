using UnityEngine;

public sealed class ProjectileBehaviour : MonoBehaviour
{
    private Projectile _model;

    public void Bind(Projectile model)
    {
        _model = model;
        SyncPosition();
    }

    private void LateUpdate()
    {
        if (_model == null)
        {
            return;
        }

        SyncPosition();
        if (_model.IsCompleted)
        {
            Destroy(gameObject);
        }
    }

    private void SyncPosition()
    {
        WorldPosition position = _model.Position;
        transform.position = new Vector3(position.X, position.Y, position.Z);
    }
}