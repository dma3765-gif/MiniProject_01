using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class UIGameMenuManager : MonoBehaviour
{
    #region 인스펙터
    [Header("UI Control")]
    [SerializeField] private GameObject uiCanvas;
    #endregion

    #region 내부 변수
    private GameObject _pnlLeft;
    private GameObject _pnlBottom;

    private GameObject _btnBuildTower;
    private GameObject _btnStartWave;

    private GameObject _lblGold;
    private GameObject _lblStage;
    private GameObject _lblLife;
    #endregion

    private void Awake()
    {
        if (uiCanvas == null)
        {
            CPrint.Error("UI Canvas null => 인스펙터 확인");
            return;
        }

        _pnlLeft = uiCanvas.transform.Find("pnlLeft")?.gameObject;
        _pnlBottom = uiCanvas.transform.Find("pnlBottom")?.gameObject;
        _btnBuildTower = uiCanvas.transform.Find("pnlLeft/btnBuildTower")?.gameObject;
        _btnStartWave = uiCanvas.transform.Find("pnlLeft/btnStartWave")?.gameObject;

        _lblGold = uiCanvas.transform.Find("pnlBottom/lblGold")?.gameObject;
        _lblStage = uiCanvas.transform.Find("pnlBottom/lblStage")?.gameObject;
        _lblLife = uiCanvas.transform.Find("pnlBottom/lblLife")?.gameObject;

        if (_pnlLeft == null || 
            _pnlBottom == null || 
            _btnBuildTower == null || 
            _btnStartWave == null || 
            _lblGold == null || 
            _lblStage == null || 
            _lblLife == null)
        {
            CPrint.Error("UI Canvas 하위 오브젝트 null => 인스펙터 확인");
            return;
        }

        _lblGold.GetComponent<TMPro.TextMeshProUGUI>().text = "Gold: 0";
        _lblStage.GetComponent<TMPro.TextMeshProUGUI>().text = "Stage: 1";
        _lblLife.GetComponent<TMPro.TextMeshProUGUI>().text = "Life: 10";
    }

    public void ClickBuild()
    {
        SetUI_EditMode();
    }

    public void ClickStartWave()
    {

    }

    private void SetUI_EditMode()
    {
        _pnlBottom.SetActive(true);
        _pnlLeft.SetActive(false);
    }

    private void SetUI_ReadyMode()
    {
        _pnlBottom.SetActive(true);
        _pnlLeft.SetActive(true);
    }

    private void SetUI_RunMode()
    {

    }
}
