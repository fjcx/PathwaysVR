using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using VRStandardAssets.Utils;

public class LobbyController : MonoBehaviour {

    //public Image playImage;
    public Image infoImage;
    //public Image exitImage;
    public Image backImage;
    public GameObject lobbyObject;
    public GameObject creditsObject;
    public GameObject lobbyCeliing;
    public GameObject imageSamplerObject;
    public GameController gameController;

    public VRInteractiveItem interactPlay;
    public VRInteractiveItem interactInfo;
    public VRInteractiveItem interactExit;
    public VRInteractiveItem interactBack;
    public VRInteractiveItem interactCredits;
    public VRInteractiveItem interactFrontWall;

    [SerializeField] private float m_SelectionDuration = 2f;

    private Coroutine m_SelectionFillRoutine;
    public float SelectionDuration { get { return m_SelectionDuration; } }

    private void Start() {
        imageSamplerObject.SetActive(true);
    }

    private void OnEnable() {
        //interactPlay.OnOver += HandleOverPlay;
        //interactPlay.OnOut += HandleOutPlay;
        interactInfo.OnOver += HandleOverInfo;
        interactInfo.OnOut += HandleOutInfo;
        //interactExit.OnOver += HandleOverExit;
        //interactExit.OnOut += HandleOutExit;
        interactBack.OnOver += HandleOverBack;
        interactBack.OnOut += HandleOutBack;
        interactCredits.OnOver += HandleOverCredits;
        interactCredits.OnOut += HandleOutCredits;
        interactFrontWall.OnOver += HandleOverFrontWall;
        interactFrontWall.OnOut += HandleOutFrontWall;
    }

    private void OnDisable() {
        //interactPlay.OnOver -= HandleOverPlay;
        //interactPlay.OnOut -= HandleOutPlay;
        interactInfo.OnOver -= HandleOverInfo;
        interactInfo.OnOut -= HandleOutInfo;
        //interactExit.OnOver -= HandleOverExit;
        //interactExit.OnOut -= HandleOutExit;
        interactBack.OnOver -= HandleOverBack;
        interactBack.OnOut -= HandleOutBack;
        interactCredits.OnOver -= HandleOverCredits;
        interactCredits.OnOut -= HandleOutCredits;
        interactFrontWall.OnOver -= HandleOverFrontWall;
        interactFrontWall.OnOut -= HandleOutFrontWall;
    }

    /*private void HandleOverPlay() {
        m_SelectionFillRoutine = StartCoroutine(FillSelectionRadial(playImage, "play"));
    }

    private void HandleOutPlay() {
        HandleUp(playImage);
    }*/

    private void HandleOverInfo() {
        m_SelectionFillRoutine = StartCoroutine(FillSelectionRadial(infoImage, "info"));
    }

    private void HandleOutInfo() {
        HandleUp(infoImage);
    }

    /*private void HandleOverExit() {
        m_SelectionFillRoutine = StartCoroutine(FillSelectionRadial(exitImage, "exit"));
    }

    private void HandleOutExit() {
        HandleUp(exitImage);
    }*/

    private void HandleOverBack() {
        m_SelectionFillRoutine = StartCoroutine(FillSelectionRadial(backImage, "back"));
    }

    private void HandleOutBack() {
        HandleUp(backImage);
    }

    private void HandleOverCredits() {
        gameController.HideReticleDot(false);
    }

    private void HandleOutCredits() {
        // do nothing
    }

    private void HandleOverFrontWall() {
        gameController.HideReticleDot(false);
    }

    private void HandleOutFrontWall() {
        // do nothing
    }

    private IEnumerator FillSelectionRadial(Image radialToFill, string actionName) {
        float timer = 0f;
        radialToFill.fillAmount = 0f;

        // This loop is executed once per frame until the timer exceeds the duration.
        while (timer < m_SelectionDuration) {
            // The image's fill amount requires a value from 0 to 1 so we normalise the time.
            radialToFill.fillAmount = timer / m_SelectionDuration;

            // Increase the timer by the time between frames and wait for the next frame.
            timer += Time.deltaTime;
            yield return null;
        }

        radialToFill.fillAmount = 1f;
        OnSelectionComplete(actionName);
    }

    private void HandleUp(Image radialToEmpty) {
        if (m_SelectionFillRoutine != null) {
            StopCoroutine(m_SelectionFillRoutine);
        }
        radialToEmpty.fillAmount = 0f;
    }

    private void OnSelectionComplete(string actionName) {
        switch (actionName) {
            case "play":
                PlayAction();
                break;
            case "info":
                InfoAction();
                break;
            case "exit":
                ExitAction();
                break;
            case "back":
                BackAction();
                break;
        }
    }

    private void PlayAction() {
        gameController.PlayBlinkEffect();
    }

    private void InfoAction() {
        creditsObject.SetActive(true);
    }

    private void ExitAction() {
        Application.Quit();
    }

    private void BackAction() {
        creditsObject.SetActive(false);
    }

    public void HideLobby() {
        lobbyObject.SetActive(false);
        imageSamplerObject.SetActive(true);
        gameController.HideReticleDot(true);
    }

    public void ShowLobby() {
        imageSamplerObject.SetActive(true);
        lobbyObject.SetActive(true);
        gameController.HideReticleDot(false);
    }

    public void ShowCredits() {
        ShowLobby();
        creditsObject.SetActive(true);
        gameController.HideReticleDot(false);
    }
}
