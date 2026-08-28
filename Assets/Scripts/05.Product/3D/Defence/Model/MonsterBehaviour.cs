using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed class MonsterBehaviour : MonoBehaviour
{
    [Header("Monster Stats")]
    [SerializeField, Min(1f)] private float _maxHp = 100f;
    [SerializeField, Min(0f)] private float _moveSpeed = 2f;
    [SerializeField, Min(0)] private int _reward = 10;
    [SerializeField] private EnumMonsterType _type = EnumMonsterType.Normal;
    [SerializeField] private EnumMonsterMoveType _moveType = EnumMonsterMoveType.Ground;

    [Header("Animation")]
    [SerializeField] private string _runTrigger = "Run";
    [SerializeField] private string _dieTrigger = "Die";
    [SerializeField] private float _dieDestroyDelay = 1.2f;

    [Header("Move Rotation")]
    [SerializeField] private float _rotationSpeed = 10f;
    [SerializeField] private float _rotationOffsetY = 0f;

    [Header("HP Bar")]
    [SerializeField] private float _normalHpBarWidth = 3.0f;
    [SerializeField] private float _normalHpBarHeight = 0.58f;
    [SerializeField] private float _bossHpBarWidth = 15.0f;
    [SerializeField] private float _bossHpBarHeight = 1.0f;

    [SerializeField] private Transform[] _waypoints;

    private Canvas _hpCanvas;
    private Image _hpFill;
    private RectTransform _hpFillRect;
    private Camera _targetCamera;
    private Animator _animator;
    private bool _setupCompleted;
    private bool _deathStarted;
    private float _deathTime;
    private Vector3 _lastPosition;
    private bool _goalReported;

    public Monster Model { get; private set; }

    public void Setup(MonsterWaveData data, Transform[] waypoints)
    {
        _maxHp = data.Hp;
        _moveSpeed = data.MoveSpeed;
        _reward = data.Reward;
        _type = data.MonsterType;
        _waypoints = waypoints;
        _setupCompleted = true;               

        if (data.IsBoss)
        {
            transform.localScale = transform.localScale * 3.0f;
            _moveSpeed += 15;
        } 
        else
        {
            _moveSpeed += 5;
            switch (data.WaveInLevel)
            {
                case 2:
                    transform.localScale = transform.localScale * 1.2f;
                    break;
                case 3:
                    transform.localScale = transform.localScale * 1.4f;
                    break;
                case 4:
                    transform.localScale = transform.localScale * 1.6f;
                    break;
                default:
                    break;
            }
        }
    }

    private void Start()
    {
        if (!_setupCompleted)
        {
            CPrint.Error("MonsterBehaviour.Setup 이 호출되지 않았습니다", this);
            enabled = false;
            return;
        }

        if (MGameManager.Instance == null || _waypoints == null || _waypoints.Length == 0)
        {
            CPrint.Error("MGameManager 또는 waypoints 확인", this);
            enabled = false;
            return;
        }

        List<WorldPosition> path = new List<WorldPosition>(_waypoints.Length + 1) { ToWorldPosition(transform.position) };
        for (int i = 0; i < _waypoints.Length; i++)
        {
            if (_waypoints[i] != null)
            {
                path.Add(ToWorldPosition(_waypoints[i].position));
            }
        }

        Model = new Monster(_maxHp, _moveSpeed, _reward, _moveType, _type, path);

        Renderer rend = GetComponentInChildren<Renderer>();
        if (rend != null)
        {
            float estimate = Mathf.Max(rend.bounds.extents.x, rend.bounds.extents.z);
            Model.HitRadius = Mathf.Max(0.1f, estimate);
            Model.HitHeight = rend.bounds.max.y;
        }

        _animator = GetComponentInChildren<Animator>();
        if (_animator != null)
        {
            _animator.ResetTrigger(_dieTrigger);
            _animator.SetTrigger(_runTrigger);
        }

        MGameManager.Instance.DefenceManager.AddMonster(Model);
        CreateHpBar();
        UpdateHpBar();

        _lastPosition = transform.position;
    }

    private void LateUpdate()
    {
        if (Model == null)
        {
            return;
        }

        Vector3 nextPosition = ToVector3(Model.Position);
        Vector3 moveDirection = nextPosition - _lastPosition;

        transform.position = nextPosition;

        if (moveDirection.sqrMagnitude > 0.0001f)
        {
            moveDirection.y = 0f;

            Quaternion targetRotation = Quaternion.LookRotation(moveDirection.normalized);
            targetRotation *= Quaternion.Euler(0f, _rotationOffsetY, 0f);

            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, _rotationSpeed * Time.deltaTime);
        }

        _lastPosition = nextPosition;

        UpdateHpBar();
        UpdateHpBarRotation();
        //UpdateHpBarPosition();

        if (Model.IsDead)
        {
            if (!_deathStarted)
            {
                _deathStarted = true;
                _deathTime = Time.time;

                if (_hpCanvas != null)
                {
                    _hpCanvas.gameObject.SetActive(false);
                }

                if (_animator != null)
                {
                    _animator.ResetTrigger(_runTrigger);
                    _animator.SetTrigger(_dieTrigger);
                }
            }

            if (Time.time - _deathTime >= _dieDestroyDelay)
            {
                Destroy(gameObject);
            }

            return;
        }

        if (Model.HasReachedGoal && !_goalReported)
        {
            _goalReported = true;
            Destroy(gameObject);
        }
    }

    private void CreateHpBar()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
        {
            return;
        }

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        GameObject canvasObj = new GameObject("HpCanvas");
        canvasObj.transform.SetParent(transform);

        _hpCanvas = canvasObj.AddComponent<Canvas>();
        _hpCanvas.renderMode = RenderMode.WorldSpace;
        _hpCanvas.sortingOrder = 100;

        RectTransform canvasRect = canvasObj.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(100f, 10f);

        float barWidth;
        float barHeight;

        if (_type == EnumMonsterType.Boss)
        {
            barWidth = _bossHpBarWidth;
            barHeight = _bossHpBarHeight;
        }
        else
        {
            barWidth = _normalHpBarWidth;
            barHeight = _normalHpBarHeight;
        }

        canvasRect.localScale = new Vector3(
            barWidth / canvasRect.sizeDelta.x,
            barHeight / canvasRect.sizeDelta.y,
            1f
        );

        canvasRect.position = new Vector3(bounds.center.x, bounds.max.y + bounds.size.y * 0.05f, bounds.center.z);

        GameObject backgroundObj = new GameObject("Background");
        backgroundObj.transform.SetParent(canvasObj.transform, false);
        Image background = backgroundObj.AddComponent<Image>();
        background.color = new Color(0f, 0f, 0f, 0.8f);

        RectTransform backgroundRect = backgroundObj.GetComponent<RectTransform>();
        backgroundRect.anchorMin = Vector2.zero;
        backgroundRect.anchorMax = Vector2.one;
        backgroundRect.offsetMin = Vector2.zero;
        backgroundRect.offsetMax = Vector2.zero;

        GameObject fillObj = new GameObject("Fill");
        fillObj.transform.SetParent(backgroundObj.transform, false);
        _hpFill = fillObj.AddComponent<Image>();
        _hpFill.color = Color.green;

        _hpFillRect = fillObj.GetComponent<RectTransform>();
        _hpFillRect.anchorMin = Vector2.zero;
        _hpFillRect.anchorMax = Vector2.one;
        _hpFillRect.offsetMin = new Vector2(2f, 2f);
        _hpFillRect.offsetMax = new Vector2(-2f, -2f);
    }

    private void UpdateHpBar()
    {
        if (_hpFill == null || _hpFillRect == null || Model == null)
        {
            return;
        }

        float hpRate = Mathf.Clamp01(Model.Hp / Model.MaxHp);
        _hpFillRect.anchorMax = new Vector2(hpRate, 1f);

        if (hpRate > 0.6f) _hpFill.color = Color.green;
        else if (hpRate > 0.3f) _hpFill.color = Color.yellow;
        else _hpFill.color = Color.red;
    }

    private void UpdateHpBarRotation()
    {
        if (_hpCanvas == null)
        {
            return;
        }

        if (_targetCamera == null || !_targetCamera.isActiveAndEnabled)
        {
            Camera[] cameras = Camera.allCameras;
            for (int i = 0; i < cameras.Length; i++)
            {
                if (cameras[i].isActiveAndEnabled)
                {
                    _targetCamera = cameras[i];
                    break;
                }
            }
        }

        if (_targetCamera != null)
        {
            _hpCanvas.transform.rotation = _targetCamera.transform.rotation;
        }
    }

    private void UpdateHpBarPosition()
    {
        if (_hpCanvas == null || _targetCamera == null)
        {
            return;
        }

        Renderer rend = GetComponentInChildren<Renderer>();
        if (rend == null)
        {
            return;
        }

        Bounds bounds = rend.bounds;

        Vector3 position = new Vector3(
            bounds.center.x,
            bounds.max.y + bounds.size.y * 0.05f,
            bounds.center.z
        );

        Vector3 viewport = _targetCamera.WorldToViewportPoint(position);

        if (viewport.y > 0.95f)
        {
            viewport.y = 0.95f;
            position = _targetCamera.ViewportToWorldPoint(viewport);
        }

        _hpCanvas.transform.position = position;
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
