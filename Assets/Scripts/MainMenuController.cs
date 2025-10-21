using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MainMenuController : MonoBehaviour {
    [Header("UI Panels")]
    public GameObject mainMenuPanel;
    public GameObject singlePlayerPanel;
    public GameObject pvpPanel;

    [Header("Controller References")]
    public GameOfLifeController singlePlayerController;
    public PvPTerritorialController pvpController;

    void Start() {
        ShowMainMenu();
    }

    public void ShowMainMenu() {
        mainMenuPanel.SetActive(true);
        singlePlayerPanel.SetActive(false);
        pvpPanel.SetActive(false);
        
        if (singlePlayerController != null) {
            singlePlayerController.PauseSimulation();
            singlePlayerController.ForceResetAllCells();
            singlePlayerController.gameObject.SetActive(false);
        }
        if (pvpController != null) {
            pvpController.PauseSimulation();
            pvpController.ForceResetAllCells();
            pvpController.gameObject.SetActive(false);
        }
    }

    public void StartSinglePlayer() {
        if (pvpController != null) {
            pvpController.PauseSimulation();
            pvpController.ForceResetAllCells();
            pvpController.gameObject.SetActive(false);
        }
        
        mainMenuPanel.SetActive(false);
        singlePlayerPanel.SetActive(true);
        pvpPanel.SetActive(false);
        
        if (singlePlayerController != null) {
            singlePlayerController.gameObject.SetActive(true);
            singlePlayerController.ClearGrid();
        }
    }

    public void StartPvPMode() {
        if (singlePlayerController != null) {
            singlePlayerController.PauseSimulation();
            singlePlayerController.ForceResetAllCells();
            singlePlayerController.gameObject.SetActive(false);
        }
        
        mainMenuPanel.SetActive(false);
        singlePlayerPanel.SetActive(false);
        pvpPanel.SetActive(true);
        
        if (pvpController != null) {
            pvpController.gameObject.SetActive(true);
            pvpController.ResetGame();
        }
    }

    public void ReturnToMain() {
        ShowMainMenu();
    }

    public void ExitGame() {
        Application.Quit();
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }
}