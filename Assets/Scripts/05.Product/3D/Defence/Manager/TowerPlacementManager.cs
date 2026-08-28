using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class TowerPlacementManager : MonoBehaviour
{
    private GameObject _towerPrefab;
    private Camera _targetCamera;
    private Transform _towerArea;
    private GameObject _previewTower;
    private Renderer[] _previewRenderers;
    private Renderer[] _previewFootprintRenderers;
    private LineRenderer _placementLine;
    private Material _placementLineMaterial;
    private MaterialPropertyBlock _propertyBlock;
    private float _towerSpacing;
    private float _heightOffset;
    private int _snapDivision;
    private float _snapStepX;
    private float _snapStepZ;
    private bool _canBuild;

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    public bool IsPlacing { get { return _previewTower != null; } }

    public void Init(GameObject towerPrefab, Camera targetCamera, float towerSpacing, float heightOffset, int snapDivision)
    {
        _propertyBlock = new MaterialPropertyBlock();
        _towerPrefab = towerPrefab;
        _targetCamera = targetCamera;
        _towerSpacing = Mathf.Max(0f, towerSpacing);
        _heightOffset = heightOffset;
        _snapDivision = Mathf.Max(1, snapDivision);
        _towerArea = GameObject.Find("GameBoundPoint/TowerArea")?.transform;

        if (_towerArea == null)
        {
            CPrint.Error("GameBoundPoint/TowerArea 를 찾을 수 없습니다", this);
        }
    }

    private void Update()
    {
        if (_previewTower == null)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1))
        {
            CancelPlacement();
            return;
        }

        UpdatePreview();

        if (Input.GetMouseButtonDown(0) && !IsPointerOverUI() && _canBuild)
        {
            BuildTower();
        }
    }

    public void BeginPlacement()
    {
        if (_previewTower != null)
        {
            CancelPlacement();
            return;
        }

        if (_towerPrefab == null)
        {
            CPrint.Error("MGameManager의 Tower Prefab을 지정하세요", this);
            return;
        }

        if (_towerArea == null)
        {
            _towerArea = GameObject.Find("GameBoundPoint/TowerArea")?.transform;
        }

        if (_towerArea == null)
        {
            CPrint.Error("GameBoundPoint/TowerArea 를 찾을 수 없습니다", this);
            return;
        }

        _previewTower = Instantiate(_towerPrefab);
        _previewTower.name = _towerPrefab.name + "_Preview";
        DisablePreviewComponents();
        _previewRenderers = _previewTower.GetComponentsInChildren<Renderer>(true);
        _previewFootprintRenderers = GetFootprintRenderers(_previewTower.transform);
        CalculateSnapStep();
        CreatePlacementLine();
        _canBuild = false;
        SetPreviewColor(Color.red);
        UpdatePreview();
    }

    public void CancelPlacement()
    {
        if (_previewTower != null)
        {
            ClearPlacementLine();
            Destroy(_previewTower);
        }

        _previewTower = null;
        _previewRenderers = null;
        _previewFootprintRenderers = null;
        _canBuild = false;
    }

    private void UpdatePreview()
    {
        Camera camera = GetTargetCamera();
        if (camera == null)
        {
            _canBuild = false;
            SetPreviewColor(Color.red);
            return;
        }

        Ray ray = camera.ScreenPointToRay(Input.mousePosition);
        RaycastHit[] hitArray = Physics.RaycastAll(ray, 1000f, ~0, QueryTriggerInteraction.Ignore);
        if (hitArray == null || hitArray.Length == 0)
        {
            _canBuild = false;
            SetPreviewColor(Color.red);
            return;
        }

        RaycastHit nearestHit = hitArray[0];
        RaycastHit buildAreaHit = default(RaycastHit);
        bool hasBuildAreaHit = false;
        for (int i = 1; i < hitArray.Length; i++)
        {
            if (hitArray[i].distance < nearestHit.distance)
            {
                nearestHit = hitArray[i];
            }
        }

        for (int i = 0; i < hitArray.Length; i++)
        {
            if (hitArray[i].collider.GetComponentInParent<TowerBuildArea>() == null)
            {
                continue;
            }

            if (!hasBuildAreaHit || hitArray[i].distance < buildAreaHit.distance)
            {
                buildAreaHit = hitArray[i];
                hasBuildAreaHit = true;
            }
        }

        if (hasBuildAreaHit)
        {
            nearestHit = buildAreaHit;
        }

        Vector3 position = nearestHit.point;
        position.x = SnapPosition(position.x, _snapStepX);
        position.z = SnapPosition(position.z, _snapStepZ);

        Vector3 snappedSurfacePosition;
        hasBuildAreaHit = TryGetSnappedSurface(position, out snappedSurfacePosition);
        position.y = snappedSurfacePosition.y;
        position.y += _heightOffset;
        _previewTower.transform.position = position;

        Rect footprint;
        bool hasFootprint = TryGetFootprint(_previewFootprintRenderers, position, out footprint);
        if (hasFootprint)
        {
            UpdatePlacementLine(footprint, position.y + 0.08f);
        }

        _canBuild = hasBuildAreaHit && hasFootprint && IsFootprintInsideBuildArea(footprint, position.y) && !IsOverlappingBuiltTower(footprint);
        SetPreviewColor(_canBuild ? Color.green : Color.red);
    }

    private float SnapPosition(float value, float snapStep)
    {
        if (snapStep <= 0.001f)
        {
            return value;
        }

        return Mathf.Round(value / snapStep) * snapStep;
    }

    private bool TryGetSnappedSurface(Vector3 position, out Vector3 surfacePosition)
    {
        surfacePosition = position;
        Vector3 origin = new Vector3(position.x, position.y + 30f, position.z);
        RaycastHit[] hitArray = Physics.RaycastAll(origin, Vector3.down, 60f, ~0, QueryTriggerInteraction.Ignore);
        if (hitArray == null || hitArray.Length == 0)
        {
            return false;
        }

        RaycastHit nearestHit = hitArray[0];
        RaycastHit nearestBuildAreaHit = default(RaycastHit);
        bool hasBuildAreaHit = false;

        for (int i = 0; i < hitArray.Length; i++)
        {
            if (hitArray[i].distance < nearestHit.distance)
            {
                nearestHit = hitArray[i];
            }

            if (hitArray[i].collider.GetComponentInParent<TowerBuildArea>() != null && (!hasBuildAreaHit || hitArray[i].distance < nearestBuildAreaHit.distance))
            {
                nearestBuildAreaHit = hitArray[i];
                hasBuildAreaHit = true;
            }
        }

        RaycastHit resultHit = hasBuildAreaHit ? nearestBuildAreaHit : nearestHit;
        surfacePosition = resultHit.point;
        return hasBuildAreaHit;
    }

    private void CalculateSnapStep()
    {
        List<Renderer> bottomRendererList = new List<Renderer>();
        AddFootprintRenderers(_previewTower.transform.Find("TowerBottom"), bottomRendererList);
        Renderer[] snapRendererArray = bottomRendererList.Count > 0 ? bottomRendererList.ToArray() : _previewFootprintRenderers;

        Rect bottomFootprint;
        if (!TryGetFootprint(snapRendererArray, _previewTower.transform.position, out bottomFootprint))
        {
            _snapStepX = 0f;
            _snapStepZ = 0f;
            return;
        }

        _snapStepX = Mathf.Max(0.05f, bottomFootprint.width / _snapDivision);
        _snapStepZ = Mathf.Max(0.05f, bottomFootprint.height / _snapDivision);
    }

    private void BuildTower()
    {
        Vector3 position = _previewTower.transform.position;
        Quaternion rotation = _previewTower.transform.rotation;

        GameObject tower = Instantiate(_towerPrefab, position, rotation, _towerArea);
        tower.name = _towerPrefab.name + "_" + (_towerArea.childCount).ToString("00");

        ClearPlacementLine();
        Destroy(_previewTower);
        _previewTower = null;
        _previewRenderers = null;
        _previewFootprintRenderers = null;
        _canBuild = false;
    }

    private bool IsFootprintInsideBuildArea(Rect footprint, float towerY)
    {
        const float checkStep = 0.2f;
        const float edgePadding = 0.06f;
        float minX = footprint.xMin - edgePadding;
        float maxX = footprint.xMax + edgePadding;
        float minZ = footprint.yMin - edgePadding;
        float maxZ = footprint.yMax + edgePadding;
        int countX = Mathf.Clamp(Mathf.CeilToInt((maxX - minX) / checkStep), 2, 16);
        int countZ = Mathf.Clamp(Mathf.CeilToInt((maxZ - minZ) / checkStep), 2, 16);

        for (int x = 0; x <= countX; x++)
        {
            float checkX = Mathf.Lerp(minX, maxX, x / (float)countX);
            for (int z = 0; z <= countZ; z++)
            {
                float checkZ = Mathf.Lerp(minZ, maxZ, z / (float)countZ);
                Vector3 origin = new Vector3(checkX, towerY + 20f, checkZ);
                RaycastHit hit;
                if (!Physics.Raycast(origin, Vector3.down, out hit, 50f, ~0, QueryTriggerInteraction.Ignore))
                {
                    return false;
                }

                if (hit.collider.GetComponentInParent<TowerBuildArea>() == null)
                {
                    return false;
                }
            }
        }

        return true;
    }

    private bool IsOverlappingBuiltTower(Rect previewFootprint)
    {
        if (_towerArea != null)
        {
            for (int i = 0; i < _towerArea.childCount; i++)
            {
                if (DoesTowerOverlap(_towerArea.GetChild(i), previewFootprint))
                {
                    return true;
                }
            }
        }

        SphereTowerBehaviour[] sceneTowerArray = FindObjectsOfType<SphereTowerBehaviour>();
        for (int i = 0; i < sceneTowerArray.Length; i++)
        {
            Transform tower = sceneTowerArray[i].transform;
            if (_previewTower != null && tower == _previewTower.transform)
            {
                continue;
            }

            if (DoesTowerOverlap(tower, previewFootprint))
            {
                return true;
            }
        }

        return false;
    }

    private bool DoesTowerOverlap(Transform tower, Rect previewFootprint)
    {
        const float towerGap = 0.08f;
        Renderer[] towerRenderers = GetFootprintRenderers(tower);
        Rect towerFootprint;
        if (!TryGetFootprint(towerRenderers, tower.position, out towerFootprint))
        {
            return false;
        }

        bool overlapX = previewFootprint.xMin < towerFootprint.xMax + towerGap && previewFootprint.xMax > towerFootprint.xMin - towerGap;
        bool overlapZ = previewFootprint.yMin < towerFootprint.yMax + towerGap && previewFootprint.yMax > towerFootprint.yMin - towerGap;
        return overlapX && overlapZ;
    }

    private Renderer[] GetFootprintRenderers(Transform towerRoot)
    {
        List<Renderer> rendererList = new List<Renderer>();
        Transform towerBottom = towerRoot.Find("TowerBottom");
        Transform towerBase = towerRoot.Find("MiddlePivot/TowerBase");
        AddFootprintRenderers(towerBottom, rendererList);
        AddFootprintRenderers(towerBase, rendererList);

        if (rendererList.Count == 0)
        {
            Renderer[] rendererArray = towerRoot.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < rendererArray.Length; i++)
            {
                if (!(rendererArray[i] is ParticleSystemRenderer) && !(rendererArray[i] is LineRenderer))
                {
                    rendererList.Add(rendererArray[i]);
                }
            }
        }

        return rendererList.ToArray();
    }

    private void AddFootprintRenderers(Transform root, List<Renderer> rendererList)
    {
        if (root == null)
        {
            return;
        }

        Renderer[] rendererArray = root.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < rendererArray.Length; i++)
        {
            if (!(rendererArray[i] is ParticleSystemRenderer) && !(rendererArray[i] is LineRenderer))
            {
                rendererList.Add(rendererArray[i]);
            }
        }
    }

    private bool TryGetFootprint(Renderer[] rendererArray, Vector3 fallbackPosition, out Rect footprint)
    {
        bool hasBounds = false;
        float minX = 0f;
        float maxX = 0f;
        float minZ = 0f;
        float maxZ = 0f;

        if (rendererArray != null)
        {
            for (int i = 0; i < rendererArray.Length; i++)
            {
                Renderer targetRenderer = rendererArray[i];
                if (targetRenderer == null)
                {
                    continue;
                }

                Bounds bounds = targetRenderer.bounds;
                if (!hasBounds)
                {
                    minX = bounds.min.x;
                    maxX = bounds.max.x;
                    minZ = bounds.min.z;
                    maxZ = bounds.max.z;
                    hasBounds = true;
                }
                else
                {
                    minX = Mathf.Min(minX, bounds.min.x);
                    maxX = Mathf.Max(maxX, bounds.max.x);
                    minZ = Mathf.Min(minZ, bounds.min.z);
                    maxZ = Mathf.Max(maxZ, bounds.max.z);
                }
            }
        }

        if (!hasBounds)
        {
            float fallbackSize = Mathf.Max(0.5f, _towerSpacing);
            footprint = new Rect(fallbackPosition.x - fallbackSize * 0.5f, fallbackPosition.z - fallbackSize * 0.5f, fallbackSize, fallbackSize);
            return true;
        }

        footprint = Rect.MinMaxRect(minX, minZ, maxX, maxZ);
        return true;
    }

    private void DisablePreviewComponents()
    {
        Collider[] colliders = _previewTower.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            colliders[i].enabled = false;
        }

        MonoBehaviour[] behaviours = _previewTower.GetComponentsInChildren<MonoBehaviour>(true);
        for (int i = 0; i < behaviours.Length; i++)
        {
            behaviours[i].enabled = false;
        }
    }

    private void SetPreviewColor(Color color)
    {
        if (_previewRenderers == null)
        {
            return;
        }

        if (_propertyBlock == null)
        {
            _propertyBlock = new MaterialPropertyBlock();
        }

        float pulse = (Mathf.Sin(Time.unscaledTime * 8f) + 1f) * 0.5f;
        bool isBuildableColor = color.g >= color.r;
        float modelIntensity = 1.25f + pulse * 0.35f;
        Color modelColor = isBuildableColor ? new Color(0.12f, modelIntensity, 0.12f, 1f) : new Color(modelIntensity, 0.12f, 0.12f, 1f);
        float lineIntensity = 4f + pulse * 2f;
        Color lineColor = isBuildableColor ? new Color(0f, lineIntensity, 0f, 1f) : new Color(lineIntensity, 0f, 0f, 1f);

        for (int i = 0; i < _previewRenderers.Length; i++)
        {
            Renderer targetRenderer = _previewRenderers[i];
            if (targetRenderer == null || !targetRenderer.enabled)
            {
                continue;
            }

            targetRenderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetColor(BaseColorId, modelColor);
            _propertyBlock.SetColor(ColorId, modelColor);
            _propertyBlock.SetColor("_EmissionColor", modelColor * 0.18f);
            targetRenderer.SetPropertyBlock(_propertyBlock);
            _propertyBlock.Clear();
        }

        if (_placementLine != null)
        {
            _placementLine.startColor = lineColor;
            _placementLine.endColor = lineColor;
            _placementLine.widthMultiplier = 0.12f + pulse * 0.06f;
        }

        if (_placementLineMaterial != null)
        {
            if (_placementLineMaterial.HasProperty(BaseColorId))
            {
                _placementLineMaterial.SetColor(BaseColorId, lineColor);
            }

            if (_placementLineMaterial.HasProperty(ColorId))
            {
                _placementLineMaterial.SetColor(ColorId, lineColor);
            }
        }
    }

    private void CreatePlacementLine()
    {
        GameObject lineObject = new GameObject("PlacementStatusRing");
        lineObject.layer = LayerMask.NameToLayer("Ignore Raycast");
        lineObject.transform.SetParent(_previewTower.transform, false);
        lineObject.transform.localPosition = new Vector3(0f, 0.08f, 0f);

        _placementLine = lineObject.AddComponent<LineRenderer>();
        _placementLine.useWorldSpace = true;
        _placementLine.loop = true;
        _placementLine.positionCount = 4;
        _placementLine.textureMode = LineTextureMode.Stretch;
        _placementLine.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        _placementLine.receiveShadows = false;

        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
        {
            shader = Shader.Find("Unlit/Color");
        }
        if (shader == null)
        {
            shader = Shader.Find("Sprites/Default");
        }

        if (shader != null)
        {
            _placementLineMaterial = new Material(shader);
            _placementLine.material = _placementLineMaterial;
        }

    }

    private void UpdatePlacementLine(Rect footprint, float y)
    {
        if (_placementLine == null)
        {
            return;
        }

        _placementLine.SetPosition(0, new Vector3(footprint.xMin, y, footprint.yMin));
        _placementLine.SetPosition(1, new Vector3(footprint.xMin, y, footprint.yMax));
        _placementLine.SetPosition(2, new Vector3(footprint.xMax, y, footprint.yMax));
        _placementLine.SetPosition(3, new Vector3(footprint.xMax, y, footprint.yMin));
    }

    private void ClearPlacementLine()
    {
        if (_placementLineMaterial != null)
        {
            Destroy(_placementLineMaterial);
        }

        _placementLineMaterial = null;
        _placementLine = null;
    }

    private Camera GetTargetCamera()
    {
        if (_targetCamera != null && _targetCamera.isActiveAndEnabled)
        {
            return _targetCamera;
        }

        if (Camera.main != null && Camera.main.isActiveAndEnabled)
        {
            return Camera.main;
        }

        Camera[] cameraArray = Camera.allCameras;
        Camera result = null;
        for (int i = 0; i < cameraArray.Length; i++)
        {
            if (!cameraArray[i].isActiveAndEnabled)
            {
                continue;
            }

            if (result == null || cameraArray[i].depth > result.depth)
            {
                result = cameraArray[i];
            }
        }

        return result;
    }

    private bool IsPointerOverUI()
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }

    private void OnDisable()
    {
        CancelPlacement();
    }
}
