using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using HTC.UnityPlugin.Multimedia;

public class GameController : MonoBehaviour {

    public GameObject mainCamera;
    public VideoSourceController vidController;

    public Image reticleDot;
    public Image reticleSelection;
    public Image reticleBackground;

    private BlinkEffect blinkEffect;
    private bool canBlink = true;
    private bool cancelSelection = true;

    private bool cancelBlink = false;

    // Use this for initialization
    void Start() {
        blinkEffect = mainCamera.GetComponent<BlinkEffect>();
        showRecticleDot();
    }

    public void showRecticleDot() {
        reticleSelection.enabled = false;
        reticleBackground.enabled = false;
        reticleDot.enabled = true;
    }

    public void CancelSelectionBar() {
        cancelSelection = true;
    }


    public void FillSelectionBar(float selectionTime) {
        Debug.Log("FillSelectionBar");
        reticleSelection.fillAmount = 0f;
        reticleBackground.enabled = true;
        reticleSelection.enabled = true;

        reticleDot.enabled = false;
        cancelSelection = false;
        StartCoroutine(FillReticleSelection(selectionTime));
    }

    private IEnumerator FillReticleSelection(float selectionWait) {
        //Debug.Log("FillReticleSelection");
        float minSel = 0.0f;
        float maxSel = 1.0f;
        float currSel = minSel;

        reticleSelection.fillAmount = minSel;

        while (minSel < maxSel && cancelSelection == false) {
            reticleSelection.fillAmount = Mathf.Lerp(minSel, maxSel, currSel);
            currSel += selectionWait * Time.deltaTime;
            yield return null;
        }

        reticleSelection.fillAmount = maxSel;
        if (cancelSelection == false) {
            PlayBlinkEffect();
        } else {
            showRecticleDot();
        }
    }

    public void CancelBlinkTransit() {
        if (canBlink == false) {
            cancelBlink = true;
        }
    }

    public void PlayBlinkEffect() {
        Debug.Log("Trying to Blink");
        if (canBlink && cancelBlink == false) {
            canBlink = false;   // don't allow any blinking commands while in motion!!
            blinkEffect.enabled = true;
            //blinkEffect.enabled = evt.enable;
            //StartCoroutine(CloseEyes(evt.moveTo, evt.closeTimeSpreader, evt.openTimeSpreader, evt.blinkWait));
            // StartCoroutine(CloseEyes(vidSphere2.transform, 1.1f, 6f, 0f));

            StartCoroutine(CloseEyes(3f, 6f, 0f));
            // TODO: disable blink effect when not in use ??
        }
    }

    private IEnumerator CloseEyes(float closeTimeSpreader, float openTimeSpreader, float blinkWait) {
        Debug.Log("CloseEyes!");
        float minMask = 0.0f;
        float maxMask = 1.3f;
        float currMask = minMask;

        while (currMask < maxMask && cancelBlink == false) {
            blinkEffect.maskValue = Mathf.Lerp(minMask, maxMask, currMask);
            currMask += closeTimeSpreader * Time.deltaTime;
            yield return null;
        }

        //blinkEffect.maskValue = maxMask;

        if (cancelBlink == false) {
            //EventController.Instance.Publish(new TeleportPlayerEvent(moveTo));
            //movePlayerTo(moveTo);
            //clearSphere();
            //movePlayerToNextVidSphere(true);        // TODO: can set to dark world
            vidController.nextVideo();
        }

        yield return new WaitForSeconds(blinkWait);
        StartCoroutine(OpenEyes(openTimeSpreader));
    }

    private IEnumerator OpenEyes(float openTimeSpreader) {
        Debug.Log("OpenEyes!");
        float minMask = 0.0f;
        float maxMask = 1.3f;
        float currMask = blinkEffect.maskValue;

        while (currMask > minMask) {
            blinkEffect.maskValue = Mathf.Lerp(minMask, maxMask, currMask);
            currMask -= openTimeSpreader * Time.deltaTime;
            yield return null;
        }

        blinkEffect.maskValue = minMask;
        canBlink = true;
        cancelBlink = false;
    }
}
