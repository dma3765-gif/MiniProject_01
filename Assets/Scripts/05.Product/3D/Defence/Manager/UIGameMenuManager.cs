using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class UIGameMenuManager : MonoBehaviour
{
    [Header("UI Control")]
    [SerializeField] private GameObject uiCanvas;

    private GameObject _pnlLeft;
    private GameObject _pnlBottom;
    private Button _btnBuildTower;
    private Button _btnStartWave;
    private TextMeshProUGUI _lblGold;
    private TextMeshProUGUI _lblStage;
    private TextMeshProUGUI _lblKill;
    private TextMeshProUGUI _lblLife;
    
    private void Awake()
    {
        if (uiCanvas == null)
        {
            uiCanvas = GameObject.Find("UI/Canvas");
        }

        if (uiCanvas == null)
        {
            CPrint.Error("UI Canvas 를 찾을 수 없습니다", this);
            return;
        }

        _pnlLeft = uiCanvas.transform.Find("pnlLeft")?.gameObject;
        _pnlBottom = uiCanvas.transform.Find("pnlBottom")?.gameObject;
        _btnBuildTower = uiCanvas.transform.Find("pnlLeft/btnBuildTower")?.GetComponent<Button>();
        _btnStartWave = uiCanvas.transform.Find("pnlLeft/btnStartWave")?.GetComponent<Button>();
        _lblGold = uiCanvas.transform.Find("pnlBottom/lblGold")?.GetComponent<TextMeshProUGUI>();
        _lblStage = uiCanvas.transform.Find("pnlBottom/lblStage")?.GetComponent<TextMeshProUGUI>();
        _lblKill = uiCanvas.transform.Find("pnlBottom/lblKill")?.GetComponent<TextMeshProUGUI>();
        _lblLife = uiCanvas.transform.Find("pnlBottom/lblLife")?.GetComponent<TextMeshProUGUI>();

        if (_btnBuildTower != null) _btnBuildTower.onClick.AddListener(ClickBuild);
        if (_btnStartWave != null) _btnStartWave.onClick.AddListener(ClickStartWave);
    }

    private void Start()
    {
        if (MGameManager.Instance == null)
        {
            return;
        }

        DefenceManager defence = MGameManager.Instance.DefenceManager;
        defence.GoldChanged += SetGold;
        defence.LivesChanged += SetLife;

        MGameManager.Instance.WaveManager.WaveStarted += OnWaveStarted;
        MGameManager.Instance.WaveManager.WaveCleared += OnWaveCleared;
        MGameManager.Instance.WaveManager.GameCleared += OnGameCleared;
        MGameManager.Instance.WaveManager.MonsterProgressChanged += OnMonsterProgressChanged;

        SetGold(defence.Gold);
        SetLife(defence.Lives);
        if (_lblStage != null) _lblStage.text = "Stage: 1-1 / 35";
        if (_lblKill != null) _lblKill.text = "Kill: 0";
    }

    private void OnDestroy()
    {
        if (MGameManager.Instance == null)
        {
            return;
        }

        MGameManager.Instance.DefenceManager.GoldChanged -= SetGold;
        MGameManager.Instance.DefenceManager.LivesChanged -= SetLife;
        MGameManager.Instance.WaveManager.WaveStarted -= OnWaveStarted;
        MGameManager.Instance.WaveManager.WaveCleared -= OnWaveCleared;
        MGameManager.Instance.WaveManager.GameCleared -= OnGameCleared;
        MGameManager.Instance.WaveManager.MonsterProgressChanged -= OnMonsterProgressChanged;
    }

    public void ClickBuild()
    {
        if (MGameManager.Instance == null || MGameManager.Instance.PlacementManager == null)
        {
            return;
        }

        MGameManager.Instance.PlacementManager.BeginPlacement();
    }

    public void ClickStartWave()
    {
        if (MGameManager.Instance == null)
        {
            return;
        }

        MGameManager.Instance.WaveManager.StartGame();
    }

    private void OnWaveStarted(MonsterWaveData data)
    {
        if (_btnStartWave != null) _btnStartWave.interactable = false;
        if (_lblStage != null)
        {
            string waveText = data.IsBoss ? "BOSS" : data.WaveInLevel.ToString();
            _lblStage.text = $"Stage: {data.Level}-{waveText} / {data.TotalWave}:35";
        }
        SetUI_RunMode();
    }

    private void OnWaveCleared(MonsterWaveData data)
    {
        if (!MGameManager.Instance.WaveManager.IsRunning && _btnStartWave != null)
        {
            _btnStartWave.interactable = true;
        }
    }

    private void OnMonsterProgressChanged(int total, int killed, int escaped)
    {
        if (_lblKill == null)
        {
            return;
        }

        _lblKill.text = $"Kill: {killed} / {total}  Miss: {escaped}";
    }

    private void OnGameCleared()
    {
        if (_lblStage != null) _lblStage.text = "Stage: CLEAR";
        if (_btnStartWave != null) _btnStartWave.interactable = false;
        SetUI_ReadyMode();
    }

    private void SetGold(int gold)
    {
        if (_lblGold != null) _lblGold.text = $"Gold: {gold}";
    }

    private void SetLife(int life)
    {
        if (_lblLife != null) _lblLife.text = $"Life: {life}";
    }

    private void SetUI_EditMode()
    {
        if (_pnlBottom != null) _pnlBottom.SetActive(true);
        if (_pnlLeft != null) _pnlLeft.SetActive(false);
    }

    private void SetUI_ReadyMode()
    {
        if (_pnlBottom != null) _pnlBottom.SetActive(true);
        if (_pnlLeft != null) _pnlLeft.SetActive(true);
    }

    private void SetUI_RunMode()
    {
        if (_pnlBottom != null) _pnlBottom.SetActive(true);
        if (_pnlLeft != null) _pnlLeft.SetActive(true);
    }
}
