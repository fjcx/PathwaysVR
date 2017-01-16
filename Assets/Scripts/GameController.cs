using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using HTC.UnityPlugin.Multimedia;
using UnityEngine.Events;

public class GameController : MonoBehaviour {

    private BlinkEffect blinkEffect;
    private GradualGrayScaleEffect gradGrayEffect;
    private FadeEffect fadeEffect;
    private bool canBlink = true;
    private bool cancelSelection = true;
    private bool cancelBlink = false;

    private GameObject currVidPlayer;
    private int vidSphereDistance = 25;
    private int currVidIndex = 0;
    private IVideoPlayerController[] videoPlayerControllers;

    public Image reticleDot;
    public Image reticleSelection;
    public Image reticleBackground;
    public GameObject mainCamera;
    public GameObject desktopVidPlayerPrefab;
    public GameObject androidVidPlayerPrefab;
    public GameObject osxVidPlayerPrefab;
    public GvrAudioSoundfield gvrAudioSoundfield;

    public string[] movieNames;

    // Use this for initialization
    void Start() {
        blinkEffect = mainCamera.GetComponent<BlinkEffect>();
        gradGrayEffect = mainCamera.GetComponent<GradualGrayScaleEffect>();
        fadeEffect = mainCamera.GetComponent<FadeEffect>();
        showRecticleDot();  // disables outer rectile elements
        hideRecticleDot(true);
        videoPlayerControllers = new IVideoPlayerController[movieNames.Length];

#if (UNITY_ANDROID && !UNITY_EDITOR)
        if (movieNames.Length > 0) {
            // only 1 player created for android !!
            currVidPlayer = Instantiate(androidVidPlayerPrefab, new Vector3(0, 0, 0), Quaternion.identity);
            Debug.Log("Instantiate ! :VidIndex: 0, In location: " + currVidPlayer.transform.position);
            Debug.Log("Instantiate ! , currind: " + currVidIndex + ", Camera Location: " + mainCamera.transform.position);
        }
#endif

        for (int i = 0; i < movieNames.Length; i++) {
#if (UNITY_ANDROID && !UNITY_EDITOR)
            // moved to above
#elif (UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX)
            currVidPlayer = Instantiate(osxVidPlayerPrefab, new Vector3(i * vidSphereDistance, 0, 0), Quaternion.identity);
#else
            currVidPlayer = Instantiate(desktopVidPlayerPrefab, new Vector3(i * vidSphereDistance, 0, 0), Quaternion.identity);
#endif
            videoPlayerControllers[i] = currVidPlayer.GetComponent<IVideoPlayerController>();

            videoPlayerControllers[i].PrepareVideo(movieNames[i]);
        }

#if (UNITY_ANDROID && !UNITY_EDITOR)
        videoPlayerControllers[0].PrepareVideos(movieNames);
#endif
         gvrAudioSoundfield.PlayScheduled(0);
    }

    public void VidInitCompleted(string vidName) {
        // when a vidsphere is completed it calls here !!!
        if (videoPlayerControllers[0].GetVidName() == vidName) {
            videoPlayerControllers[0].PlayVideo();
        }
    }

    public void showRecticleDot() {
        reticleSelection.enabled = false;
        reticleBackground.enabled = false;
        reticleDot.enabled = true;
    }

    public void hideRecticleDot(bool isHidden) {
        this.reticleDot.enabled = !isHidden;
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

    private void NextVideo() {
        Debug.Log("GameController: NextVideo()");
#if (UNITY_ANDROID && !UNITY_EDITOR)
        Debug.Log("Switching Video to");
        videoPlayerControllers[currVidIndex].SwitchVideo();
#else
        Debug.Log("VidIndex: " + currVidIndex + ", In location: " + mainCamera.transform.position);
        videoPlayerControllers[currVidIndex].PauseVideo();
        videoPlayerControllers[currVidIndex].StopVideo();

        // tmp
        videoPlayerControllers[currVidIndex].MoveTo(new Vector3(-vidSphereDistance, 0, 0));

        currVidIndex++; // need null index check !!
        // tmp
        videoPlayerControllers[currVidIndex].MoveTo(mainCamera.transform.position);
        // move camera to next sphere locataion
        //mainCamera.transform.position = new Vector3(currVidIndex * vidSphereDistance, 0, 0);

        Debug.Log("VidIndex: " + currVidIndex + ", Moving to location: " + mainCamera.transform.position);
        videoPlayerControllers[currVidIndex].PlayVideo();
#endif
    }

    public void PlayBlinkEffect() {
        //reticleDot.enabled = true;

        Debug.Log("Trying to Blink");
        if (canBlink && cancelBlink == false) {
            canBlink = false;   // don't allow any blinking commands while in motion!!
            blinkEffect.enabled = true;
            //blinkEffect.enabled = evt.enable;
            //StartCoroutine(CloseEyes(evt.moveTo, evt.closeTimeSpreader, evt.openTimeSpreader, evt.blinkWait));
            // StartCoroutine(CloseEyes(vidSphere2.transform, 1.1f, 6f, 0f));

            //StartCoroutine(GradualGrayScale());
            //StartCoroutine(CloseEyes(3f, 6f, 1f));
            StartCoroutine(FadeOut(3f, 6f, 1f));
            
            // TODO: disable blink effect when not in use ??
        }
    }

    private IEnumerator GradualGrayScale() {
        Debug.Log("Grad Grayscale!");
        float minGray = 0.0f;
        float maxGray = 1.0f;
        float currGray = gradGrayEffect.rampOffset;
        float grayTimeSpreader = 0.7f;

        while (currGray > minGray) {
            gradGrayEffect.rampOffset = Mathf.Lerp(minGray, maxGray, currGray);
            currGray -= grayTimeSpreader * Time.deltaTime;
            yield return null;
        }

        blinkEffect.maskValue = minGray;

        //StartCoroutine(CloseEyes(3f, 6f, 1f));
    }

    private IEnumerator FadeOut(float closeTimeSpreader, float openTimeSpreader, float fadeWait) {
        Debug.Log("FadeOut!");
        float minMask = 0.0f;
        float maxMask = 1.0f;
        float currMask = fadeEffect.rampOffset;

        while (currMask > minMask && cancelBlink == false) {
            fadeEffect.rampOffset = Mathf.Lerp(minMask, maxMask, currMask);
            currMask -= closeTimeSpreader * Time.deltaTime;
            yield return null;
        }

        fadeEffect.rampOffset = minMask;

        if (cancelBlink == false) {
            NextVideo();
        }

        yield return new WaitForSeconds(fadeWait);
        StartCoroutine(FadeIn(openTimeSpreader));
    }

    private IEnumerator FadeIn(float openTimeSpreader) {
        Debug.Log("FadeIn!");
        float minMask = 0.0f;
        float maxMask = 1.0f;
        float currMask = minMask;

        while (currMask < maxMask) {
            fadeEffect.rampOffset = Mathf.Lerp(minMask, maxMask, currMask);
            currMask += openTimeSpreader * Time.deltaTime;
            yield return null;
        }

        canBlink = true;
        cancelBlink = false;
        Debug.Log("FadeIn, currind: " + currVidIndex + ", Camera Location: " + mainCamera.transform.position);
        //reticleDot.enabled = false;
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
            NextVideo();
            //EventController.Instance.Publish(new TeleportPlayerEvent(moveTo));
            //movePlayerTo(moveTo);
            //clearSphere();
            //movePlayerToNextVidSphere(true);        // TODO: can set to dark world
            //vidController.nextVideo();
        }

        yield return new WaitForSeconds(blinkWait);
        StartCoroutine(OpenEyes(openTimeSpreader));
    }

    private IEnumerator OpenEyes(float openTimeSpreader) {
        gradGrayEffect.rampOffset = 1f;

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
        Debug.Log("OpenEyes, currind: " + currVidIndex + ", Camera Location: " + mainCamera.transform.position);
        //reticleDot.enabled = false;
    }
}
