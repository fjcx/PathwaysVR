using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using HTC.UnityPlugin.Multimedia;
using UnityEngine.Events;

public class GameController : MonoBehaviour {

    //private BlinkEffect blinkEffect;
    //private GradualGrayScaleEffect gradGrayEffect;
    private FadeEffect fadeEffect;
    private bool canBlink = true;
    private bool cancelSelection = true;
    private bool cancelBlink = false;

    private GameObject currVidPlayer;
    private int vidSphereDistance = 25;
    private int currVidIndex = 0;
    private IVideoPlayerController[] videoPlayerControllers;
    private AudioSource camAudioSource;
    private float gvrTargetVolume = 0;
    private int currAmbiAudioTrack = 0;

    public LobbyController lobbyController;
    public Image reticleDot;
    public Image reticleSelection;
    public Image reticleBackground;
    public GameObject mainCamera;
    public GameObject desktopVidPlayerPrefab;
    public GameObject androidVidPlayerPrefab;
    public GameObject osxVidPlayerPrefab;
    public GameObject imageSamplerObject;

    //public string[] movieNames;

    public string blinkState = "none";
    public float fadeDelayBetweenVideos = 3f;
    public VideoScene[] videoScenes;

    // Use this for initialization
    void Start() {
        //blinkEffect = mainCamera.GetComponent<BlinkEffect>();
        //gradGrayEffect = mainCamera.GetComponent<GradualGrayScaleEffect>();
        fadeEffect = mainCamera.GetComponent<FadeEffect>();
        camAudioSource = mainCamera.GetComponent<AudioSource>();
        ShowReticleDot();  // disables outer rectile elements
        HideReticleDot(true);
        gvrTargetVolume = videoScenes[currAmbiAudioTrack].soundField.volume;

#if (UNITY_ANDROID && !UNITY_EDITOR)
        if (videoScenes.Length > 0) {
            videoPlayerControllers = new IVideoPlayerController[1];     // Android only supports creating one controller/player
            // only 1 player created for android !!
            currVidPlayer = Instantiate(androidVidPlayerPrefab, new Vector3(0, 0, 0), Quaternion.identity);
            Debug.Log("Instantiate ! :VidIndex: 0, In location: " + currVidPlayer.transform.position);
            Debug.Log("Instantiate ! , currind: " + currVidIndex + ", Camera Location: " + mainCamera.transform.position);
            videoPlayerControllers[0] = currVidPlayer.GetComponent<IVideoPlayerController>();
        }
#else
        videoPlayerControllers = new IVideoPlayerController[videoScenes.Length];
#endif

        for (int i = 0; i < videoScenes.Length; i++) {
#if (UNITY_ANDROID && !UNITY_EDITOR)
            // moved to above
#elif (UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX)
            currVidPlayer = Instantiate(osxVidPlayerPrefab, new Vector3(i * vidSphereDistance, 0, 0), Quaternion.identity);
            videoPlayerControllers[i] = currVidPlayer.GetComponent<IVideoPlayerController>();
            videoPlayerControllers[i].PrepareVideo(videoScenes[i].videoName);
#else
            currVidPlayer = Instantiate(desktopVidPlayerPrefab, new Vector3(i * vidSphereDistance, 0, 0), Quaternion.identity);
            videoPlayerControllers[i] = currVidPlayer.GetComponent<IVideoPlayerController>();
            videoPlayerControllers[i].PrepareVideo(videoScenes[i].videoName);
#endif
        }

#if (UNITY_ANDROID && !UNITY_EDITOR)
        string[] movieNames = new string[videoScenes.Length];
        for (int i = 0; i < videoScenes.Length; i++) {
            movieNames[i] = videoScenes[i].videoName;
        }
        videoPlayerControllers[0].PrepareVideos(movieNames);
#endif
        videoScenes[currAmbiAudioTrack].soundField.PlayScheduled(0);
    }

    public void VidInitCompleted(string vidName) {
        // when a vidsphere is completed it calls here !!!
        if (videoPlayerControllers[0].GetVidName() == vidName) {
            videoPlayerControllers[0].PlayVideo();
        }
    }

    public void ShowReticleDot() {
        reticleSelection.enabled = false;
        reticleBackground.enabled = false;
        reticleDot.enabled = true;
    }

    public void HideReticleDot(bool isHidden) {
        //Debug.Log("Hide reticle:" + isHidden);
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
            PlayBlinkEffect("fill");
        } else {
            ShowReticleDot();
        }
    }

    public void CancelBlinkTransit(string state) {
        if (canBlink == false) {
            if (this.blinkState != "forced" && (this.blinkState != "transit" || (this.blinkState == "transit" && state == "transit"))) {
                cancelBlink = true;
            }
        }
    }

    private void NextVideo() {
        Debug.Log("GameController: NextVideo()");
        int testIndex = 0;
#if (UNITY_ANDROID && !UNITY_EDITOR)
        Debug.Log("Switching Video to");
        testIndex = videoPlayerControllers[0].SwitchVideo();
#else
        Debug.Log("VidIndex: " + currVidIndex + ", In location: " + mainCamera.transform.position);
        videoPlayerControllers[currVidIndex].PauseVideo();
        videoPlayerControllers[currVidIndex].StopVideo();

        // tmp
        videoPlayerControllers[currVidIndex].MoveTo(new Vector3(-vidSphereDistance, 0, 0));

        currVidIndex++; // need null index check !!
        if (currVidIndex >= videoPlayerControllers.Length) {
            currVidIndex = 0;
        }
        testIndex = currVidIndex;

        // tmp
        videoPlayerControllers[currVidIndex].MoveTo(mainCamera.transform.position);
        // move camera to next sphere locataion
        //mainCamera.transform.position = new Vector3(currVidIndex * vidSphereDistance, 0, 0);

        Debug.Log("VidIndex: " + currVidIndex + ", Moving to location: " + mainCamera.transform.position);
        videoPlayerControllers[currVidIndex].PlayVideo();
#endif
        currAmbiAudioTrack = testIndex;
        videoScenes[currAmbiAudioTrack].soundField.PlayScheduled(0);
        StartCoroutine(FadeInAmbiAudio(videoScenes[currAmbiAudioTrack].soundField, 6f));

        if (testIndex == 0) {
            lobbyController.ShowCredits();
        } else if (testIndex == 1) {
            lobbyController.HideLobby();
        }
        // do null array length checks !!
        // disable all transit areas
        for (int i = 0; i < videoScenes.Length; i++) {
            videoScenes[i].reticleVisibleArea.SetActive(false);
        }
        // enable the transit area for video after given delay
        StartCoroutine(EnableAfterDelay(videoScenes[testIndex].reticleVisibleArea, videoScenes[testIndex].interactDelay));
        resetCameraPos(testIndex);
        VidIndexAction(testIndex);
    }

    // put this in better place !!
    private void VidIndexAction(int vidIndex) {
        switch (vidIndex) {
            case 0:
                imageSamplerObject.SetActive(true);
                break;
            case 1:
                imageSamplerObject.SetActive(false);
                break;
            case 2:
                imageSamplerObject.SetActive(true);
                break;
            case 3:
                imageSamplerObject.SetActive(false);
                break;
            case 4:
                imageSamplerObject.SetActive(false);
                break;
            case 5:
                imageSamplerObject.SetActive(false);
                break;
            case 6:
                imageSamplerObject.SetActive(false);
                break;
        }
    }

    public void PlayBlinkEffect(string state) {
        //reticleDot.enabled = true;
        Debug.Log("Trying to Blink");
        if (canBlink && cancelBlink == false) {
            canBlink = false;   // don't allow any blinking commands while in motion!!
            //blinkEffect.enabled = true;
            //blinkEffect.enabled = evt.enable;
            //StartCoroutine(CloseEyes(evt.moveTo, evt.closeTimeSpreader, evt.openTimeSpreader, evt.blinkWait));
            // StartCoroutine(CloseEyes(vidSphere2.transform, 1.1f, 6f, 0f));

            //StartCoroutine(GradualGrayScale());
            //StartCoroutine(CloseEyes(3f, 6f, 1f));
            this.blinkState = state;
            StartCoroutine(FadeOut(3f, 6f, 7f));
            StartCoroutine(FadeOutAmbiAudio(videoScenes[currAmbiAudioTrack].soundField, 3f));
            // TODO: disable blink effect when not in use ??
        }
    }

    private IEnumerator FadeOutAmbiAudio(GvrAudioSoundfield audioSoundField, float closeTimeSpreader) {
        float minMask = 0.0f;
        float maxMask = gvrTargetVolume;
        float currMask = audioSoundField.volume;

        while (currMask > minMask && cancelBlink == false) {
            audioSoundField.volume = Mathf.Lerp(minMask, maxMask, currMask);
            currMask -= closeTimeSpreader * Time.deltaTime;
            yield return null;
        }

        audioSoundField.volume = minMask;
        audioSoundField.Stop();
    }

    private IEnumerator FadeInAmbiAudio(GvrAudioSoundfield audioSoundField, float openTimeSpreader) {
        Debug.Log("FadeIn!");
        float minMask = 0.0f;
        float maxMask = gvrTargetVolume;
        float currMask = minMask;

        while (currMask < maxMask) {
            audioSoundField.volume = Mathf.Lerp(minMask, maxMask, currMask);
            currMask += openTimeSpreader * Time.deltaTime;
            yield return null;
        }
        audioSoundField.volume = gvrTargetVolume;
    }

   

    private IEnumerator FadeOut(float closeTimeSpreader, float openTimeSpreader, float fadeWait) {

        fadeEffect.enabled = true;
        Debug.Log("FadeOut!");
        camAudioSource.Play();
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
        fadeEffect.enabled = false;
        HideReticleDot(true);
        this.blinkState = "none";
    }

    private void resetCameraPos(int vidIndex) {
        mainCamera.transform.rotation = Quaternion.Euler(mainCamera.transform.localEulerAngles.x, videoScenes[vidIndex].cameraYRotation, mainCamera.transform.localEulerAngles.z);
    }

    IEnumerator EnableAfterDelay(GameObject toEnable, float delayInSeconds) {
        yield return new WaitForSeconds(delayInSeconds);
        toEnable.SetActive(true);
    }

    /* private IEnumerator CloseEyes(float closeTimeSpreader, float openTimeSpreader, float blinkWait) {

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
     }*/

    /* private IEnumerator GradualGrayScale() {
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
    }*/
}
