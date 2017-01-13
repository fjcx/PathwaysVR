/************************************************************************************

Copyright   :   Copyright 2014 Oculus VR, LLC. All Rights reserved.

Licensed under the Oculus VR Rift SDK License Version 3.3 (the "License");
you may not use the Oculus VR Rift SDK except in compliance with the License,
which is provided at the time of installation or download, or which
otherwise accompanies this software in either electronic or hard copy form.

You may obtain a copy of the License at

http://www.oculus.com/licenses/LICENSE-3.3

Unless required by applicable law or agreed to in writing, the Oculus VR SDK
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.

************************************************************************************/

using UnityEngine;
using System.Collections;					// required for Coroutines
using System.Runtime.InteropServices;		// required for DllImport
using System;								// requred for IntPtr
using System.IO;							// required for File

/************************************************************************************
Usage:

	Place a simple textured quad surface with the correct aspect ratio in your scene.

	Add the MoviePlayerSample.cs script to the surface object.

	Supply the name of the media file to play:
	This sample assumes the media file is placed in "Assets/StreamingAssets", ie
	"ProjectName/Assets/StreamingAssets/MovieName.mp4".

	On Desktop, Unity MovieTexture functionality is used. Note: the media file
	is loaded at runtime, and therefore expected to be converted to Ogg Theora
	beforehand.

Implementation:

	In the MoviePlayerSample Awake() call, GetNativeTexturePtr() is called on 
	renderer.material.mainTexture.
	
	When the MediaSurface plugin gets the initialization event on the render thread, 
	it creates a new Android SurfaceTexture and Surface object in preparation 
	for receiving media. 

	When the game wants to start the video playing, it calls the StartVideoPlayerOnTextureId()
	script call, which creates an Android MediaPlayer java object, issues a 
	native plugin call to tell the native code to set up the target texture to
	render the video to and return the Android Surface object to pass to MediaPlayer,
	then sets up the media stream and starts it.
	
	Every frame, the SurfaceTexture object is checked for updates.  If there 
	is one, the target texId is re-created at the correct dimensions and format
	if it is the first frame, then the video image is rendered to it and mipmapped.  
	The following frame, instead of Unity drawing the image that was placed 
	on the surface in the Unity editor, it will draw the current video frame.

************************************************************************************/

public class MovieTexturePlayer : MonoBehaviour, IVideoPlayerController {
    //public string movieName = string.Empty;
    public bool videoPaused = false;
    private bool videoPausedBeforeAppPause = false;

    private string mediaFullPath = string.Empty;
    private bool startedVideo = false;

    private GameController gameController;
    private string videoName;

    private Boolean mediaSufaceInit = false;

    private MovieTexture movieTexture = null;
    private AudioSource audioEmitter = null;
    private Renderer mediaRenderer = null;

    private enum MediaSurfaceEventType {
        Initialize = 0,
        Shutdown = 1,
        Update = 2,
        Max_EventType
    };

    /// <summary>
    /// The start of the numeric range used by event IDs.
    /// </summary>
    /// <description>
    /// If multiple native rundering plugins are in use, the Oculus Media Surface plugin's event IDs
    /// can be re-mapped to avoid conflicts.
    /// 
    /// Set this value so that it is higher than the highest event ID number used by your plugin.
    /// Oculus Media Surface plugin event IDs start at eventBase and end at eventBase plus the highest
    /// value in MediaSurfaceEventType.
    /// </description>
    public static int eventBase {
        get { return _eventBase; }
        set {
            _eventBase = value;
        }
    }
    private static int _eventBase = 0;

    private static void IssuePluginEvent(MediaSurfaceEventType eventType) {
        GL.IssuePluginEvent((int)eventType + eventBase);
    }

    /// <summary>
    /// Initialization of the movie surface
    /// </summary>
    void Awake() {
        gameController = FindObjectOfType<GameController>();
        Debug.Log("MovieSample Awake");
        mediaRenderer = GetComponent<Renderer>();
        audioEmitter = GetComponent<AudioSource>();
    }

    /// <summary>
    /// Construct the streaming asset path.
    /// Note: For Android, we need to retrieve the data from the apk.
    /// </summary>
    IEnumerator RetrieveStreamingAsset(string mediaFileName) {
        //string mediaFileNameOgv = Path.GetFileNameWithoutExtension(mediaFileName) + ".ogv";
        string mediaFileNameOgv = Path.GetFileNameWithoutExtension(mediaFileName) + ".ogg";
        string streamingMediaPath = "file:///" + Application.streamingAssetsPath + "/" + mediaFileNameOgv;
        WWW wwwReader = new WWW(streamingMediaPath);
        yield return wwwReader;

        if (wwwReader.error != null) {
            Debug.LogError("wwwReader error: " + wwwReader.error);
        }

        movieTexture = wwwReader.movie;
        mediaRenderer.material.mainTexture = movieTexture;
        audioEmitter.clip = movieTexture.audioClip;
        mediaFullPath = streamingMediaPath;
        Debug.Log("Movie FullPath: " + mediaFullPath);

        // Moved to PlayVideo
        SetPaused(true);
        // Video must start only after mediaFullPath is filled in
        //Debug.Log("MovieSample Start");
        //StartCoroutine(DelayedStartVideo());
        gameController.VidInitCompleted(videoName); 
    }

    /// <summary>
    /// Auto-starts video playback
    /// </summary>
    IEnumerator DelayedStartVideo() {
        yield return null; // delay 1 frame to allow MediaSurfaceInit from the render thread.

        if (!startedVideo) {
            Debug.Log("Mediasurface DelayedStartVideo");

            startedVideo = true;
            if (movieTexture != null && movieTexture.isReadyToPlay) {
                movieTexture.Play();
                if (audioEmitter != null) {
                    audioEmitter.Play();
                }
            }
        }
    }

    void Update() {
        if (mediaSufaceInit == true) {
            if (movieTexture != null) {
                if (movieTexture.isReadyToPlay != movieTexture.isPlaying && !videoPaused) {
                    movieTexture.Play();
                    if (audioEmitter != null) {
                        audioEmitter.Play();
                    }
                }
            }
        }
    }

    public void Rewind() {
        if (movieTexture != null) {
            movieTexture.Stop();
            if (audioEmitter != null) {
                audioEmitter.Stop();
            }
        }
    }

    public void SetPaused(bool wasPaused) {
        Debug.Log("SetPaused: " + wasPaused);
        if (movieTexture != null) {
            videoPaused = wasPaused;
            if (videoPaused) {
                movieTexture.Pause();
                if (audioEmitter != null) {
                    audioEmitter.Pause();
                }
            } else {
                movieTexture.Play();
                if (audioEmitter != null) {
                    audioEmitter.Play();
                }
            }
        }
    }

    /// <summary>
    /// Pauses video playback when the app loses or gains focus
    /// </summary>
    void OnApplicationPause(bool appWasPaused) {
        Debug.Log("OnApplicationPause: " + appWasPaused);
        if (appWasPaused) {
            videoPausedBeforeAppPause = videoPaused;
        }

        // Pause/unpause the video only if it had been playing prior to app pause
        if (!videoPausedBeforeAppPause) {
            SetPaused(appWasPaused);
        }
    }

    private void OnDestroy() {

    }

    public void PlayVideo() {
        Debug.Log("Play Video");
        mediaSufaceInit = true;
        
        // Video must start only after mediaFullPath is filled in
        Debug.Log("MovieSample Start");
        Debug.Log("PlayVid: " + this.videoName + ", In location: " + this.transform.position);
        Debug.Log("PlayVid ! , Camera Location: " + gameController.mainCamera.transform.position);
        StartCoroutine(DelayedStartVideo());
    }

    public void StopVideo() {
        mediaSufaceInit = false;

        //mediaPlayer = null;
        //DestroyObject(this.gameObject);
    }

    public void PauseVideo() {
        SetPaused(true);
    }

    public void MoveTo(Vector3 newPos) {
        this.transform.position = newPos;
    }

    public void PrepareVideo(string vidName) {
        Debug.Log("Prep vid");
        this.videoName = vidName;
        Debug.Log("PrepareVid: " + this.videoName + ", In location: " + this.transform.position);
        Debug.Log("PrepareVid ! , Camera Location: " + gameController.mainCamera.transform.position);
        if (mediaRenderer.material == null || mediaRenderer.material.mainTexture == null) {
            Debug.LogError("No material for movie surface");
        }

        if (vidName != string.Empty) {
            StartCoroutine(RetrieveStreamingAsset(vidName));
        } else {
            Debug.LogError("No media file name provided");
        }
    }

    public string GetVidName() {
        return this.videoName;
    }

    public void SwitchVideo() {
        
    }

    public void PrepareVideos(string[] vidNames) {
        throw new NotImplementedException();
    }

}
