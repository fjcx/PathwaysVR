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

public class AndroidMoviePlayer : MonoBehaviour, IVideoPlayerController {
    //public string movieName = string.Empty;
    public bool videoPaused = false;
    private bool videoPausedBeforeAppPause = false;

    //private string mediaFullPath = string.Empty;
    private bool startedVideo = false;

    private GameController gameController;
    //private string videoName;
    private int currentVidIndex = 0;

    private string[] videoNames = null;
    private string[] mediaPaths = null;

    private Boolean mediaSufaceInit = false;

	private Texture2D nativeTexture = null;
	private IntPtr	  nativeTexId = IntPtr.Zero;
    private int		  textureWidth = 2880;
    private int 	  textureHeight = 1440;
    //private int         textureWidth = 3840;
    //private int 	    textureHeight = 1920;

    private int durationMs = 0;
    
    private AndroidJavaObject 	mediaPlayer = null;

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
			OVR_Media_Surface_SetEventBase(_eventBase);
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

		OVR_Media_Surface_Init();
        mediaRenderer = GetComponent<Renderer>();

    }

    public void PrepareVideos(string[] vidNames) {
        this.mediaPaths = new string[vidNames.Length];
        this.videoNames = new string[vidNames.Length];

        for (int i = 0; i < vidNames.Length; i++) {
            Debug.Log("Prep vid");
            this.videoNames[i] = vidNames[i];

            if (mediaRenderer.material == null || mediaRenderer.material.mainTexture == null) {
                Debug.LogError("No material for movie surface");
            }

            if (this.videoNames[i] != string.Empty) {
                StartCoroutine(RetrieveStreamingAsset(this.videoNames[i], i));
            } else {
                Debug.LogError("No media file name provided");
            }
        }
    }

    public void PrepareVideo(string vidName) {
        // Not Implemented here !!!
    }

    /// <summary>
    /// Construct the streaming asset path.
    /// Note: For Android, we need to retrieve the data from the apk.
    /// </summary>
    IEnumerator RetrieveStreamingAsset(string mediaFileName, int mpIndex) {
        string streamingMediaPath = Application.streamingAssetsPath + "/" + mediaFileName;
        string persistentPath = Application.persistentDataPath + "/" + mediaFileName;
        if (!File.Exists(persistentPath)) {
            WWW wwwReader = new WWW(streamingMediaPath);
            yield return wwwReader;

            if (wwwReader.error != null) {
                Debug.LogError("wwwReader error: " + wwwReader.error);
            }
            System.IO.File.WriteAllBytes(persistentPath, wwwReader.bytes);
        }
        //mediaFullPath = persistentPath;
        mediaPaths[mpIndex] = persistentPath;
        Debug.Log("Movie FullPath: " + mediaPaths[mpIndex]);

        // Moved to PlayVideo
        SetPaused(true);
        // Video must start only after mediaFullPath is filled in
        //Debug.Log("MovieSample Start");
        //StartCoroutine(DelayedStartVideo());
        gameController.VidInitCompleted(videoNames[mpIndex]);
    }

    public void PlayVideo() {
        Debug.Log("Play Video");
        Debug.Log("PlayVideo: texWidth: " + textureWidth + ", texheight: " + textureHeight);
        nativeTexture = Texture2D.CreateExternalTexture(textureWidth, textureHeight, TextureFormat.RGBA32, true, false, IntPtr.Zero);

        IssuePluginEvent(MediaSurfaceEventType.Initialize);
        mediaSufaceInit = true;

        // Video must start only after mediaFullPath is filled in
        Debug.Log("PlayVid: " + this.videoNames[currentVidIndex] + ", In location: " + this.transform.position);
        StartCoroutine(DelayedStartVideo());
    }

    /// <summary>
    /// Auto-starts video playback
    /// </summary>
    IEnumerator DelayedStartVideo() {
        yield return null; // delay 1 frame to allow MediaSurfaceInit from the render thread.

        if (!startedVideo) {
            Debug.Log("Mediasurface DelayedStartVideo");

            startedVideo = true;
            Debug.Log("DelayedStart: texWidth: "+ textureWidth + ", texheight: " + textureHeight);
			mediaPlayer = StartVideoPlayerOnTextureId(textureWidth, textureHeight, mediaPaths[currentVidIndex]);
			mediaRenderer.material.mainTexture = nativeTexture;
        }
    }

    void Update() {
        if (mediaSufaceInit == true) {
            if (!videoPaused) {
                IntPtr currTexId = OVR_Media_Surface_GetNativeTexture();
                if (currTexId != nativeTexId) {
                    nativeTexId = currTexId;
                    nativeTexture.UpdateExternalTexture(currTexId);
                }

                IssuePluginEvent(MediaSurfaceEventType.Update);

                try {
                    // inefficient !! -> use JNI callback instead!
                    if (durationMs != 0)  {
                        int positionMs = mediaPlayer.Call<int>("getCurrentPosition");
                        // are we at end of video ?
                        if (positionMs >= (durationMs - 1000)) {
                            if (currentVidIndex != 0) {
                                Debug.Log("Force transition");
                                gameController.PlayBlinkEffect("forced");
                            } else {
                                // jump back 9 seconds
                                /*int backToMs = durationMs - 9000;
                                if (backToMs < 0) {
                                    backToMs = 0;
                                }*/
                                mediaPlayer.Call("seekTo", 0);
                            }
                        }
                    }
                } catch (Exception e) {
                    Debug.Log("Failed to loop 10sec back with message " + e.Message);
                }
            }
        }
    }

    public void Rewind() {
        if (mediaPlayer != null) {
            try {
				mediaPlayer.Call("seekTo", 0);
			} catch (Exception e) {
				Debug.Log("Failed to stop mediaPlayer with message " + e.Message);
			}
        }
    }

    public void PauseVideo() {
        SetPaused(true);
    }

    public void SetPaused(bool wasPaused) {
        Debug.Log("SetPaused: " + wasPaused);
		if (mediaPlayer != null) {
			videoPaused = wasPaused;
			try {
				mediaPlayer.Call((videoPaused) ? "pause" : "start");
			} catch (Exception e) {
				Debug.Log("Failed to start/pause mediaPlayer with message " + e.Message);
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
        Debug.Log("Shutting down video");
        try {
            // This will trigger the shutdown on the render thread
            IssuePluginEvent(MediaSurfaceEventType.Shutdown);
            mediaPlayer.Call("stop");
            mediaPlayer.Call("release");
            mediaPlayer = null;
        } catch (Exception e) {
            Debug.Log("Failed to shutdown cleanly: " + e.Message);
        }
    }

    

    public void StopVideo() {
        mediaSufaceInit = false;
        Debug.Log("Shutting down video");
		// This will trigger the shutdown on the render thread
		IssuePluginEvent(MediaSurfaceEventType.Shutdown);
        mediaPlayer.Call("stop");
        mediaPlayer.Call("release");   

        //mediaPlayer = null;
        //DestroyObject(this.gameObject);
    }

    public void MoveTo(Vector3 newPos) {
        this.transform.position = newPos;
    }

    public string GetVidName() {
        return this.videoNames[currentVidIndex];
    }

    public int SwitchVideo() {
        mediaPlayer.Call("stop");
        mediaPlayer.Call("reset");

        currentVidIndex++;
        if (currentVidIndex >= mediaPaths.Length) {
            currentVidIndex = 0;
        }
        string mPath = mediaPaths[currentVidIndex];

        mediaPlayer.Call("setDataSource", mPath);
		mediaPlayer.Call("prepare");
		mediaPlayer.Call("setLooping", false);
		mediaPlayer.Call("start");

        Debug.Log("Getting duration");
        durationMs = mediaPlayer.Call<int>("getDuration");
        Debug.Log("duration: " + durationMs);
        return currentVidIndex;
    }

	/// <summary>
	/// Set up the video player with the movie surface texture id.
	/// </summary>
	AndroidJavaObject StartVideoPlayerOnTextureId(int texWidth, int texHeight, string mediaPath)
	{
		Debug.Log("MoviePlayer: StartVideoPlayerOnTextureId");

        Debug.Log("StartVideoPlayerOnTextureId: texWidth: " + textureWidth + ", texheight: " + textureHeight);
        OVR_Media_Surface_SetTextureParms(textureWidth, textureHeight);

		IntPtr androidSurface = OVR_Media_Surface_GetObject();
		AndroidJavaObject mediaPlayer = new AndroidJavaObject("android/media/MediaPlayer");

		// Can't use AndroidJavaObject.Call() with a jobject, must use low level interface
		//mediaPlayer.Call("setSurface", androidSurface);
		IntPtr setSurfaceMethodId = AndroidJNI.GetMethodID(mediaPlayer.GetRawClass(),"setSurface","(Landroid/view/Surface;)V");
		jvalue[] parms = new jvalue[1];
		parms[0] = new jvalue();
		parms[0].l = androidSurface;
		AndroidJNI.CallVoidMethod(mediaPlayer.GetRawObject(), setSurfaceMethodId, parms);

		try {
			mediaPlayer.Call("setDataSource", mediaPath);
			mediaPlayer.Call("prepare");
			mediaPlayer.Call("setLooping", true);
			mediaPlayer.Call("start");

            Debug.Log("Getting duration");
            durationMs = mediaPlayer.Call<int>("getDuration");
            Debug.Log("duration: " + durationMs);
        } catch (Exception e) {
			Debug.Log("Failed to start mediaPlayer with message " + e.Message);
		}

		return mediaPlayer;
	}

	[DllImport("OculusMediaSurface")]
	private static extern void OVR_Media_Surface_Init();

	[DllImport("OculusMediaSurface")]
	private static extern void OVR_Media_Surface_SetEventBase(int eventBase);

	// This function returns an Android Surface object that is
	// bound to a SurfaceTexture object on an independent OpenGL texture id.
	// Each frame, before the TimeWarp processing, the SurfaceTexture is checked
	// for updates, and if one is present, the contents of the SurfaceTexture
	// will be copied over to the provided surfaceTexId and mipmaps will be 
	// generated so normal Unity rendering can use it.
	[DllImport("OculusMediaSurface")]
	private static extern IntPtr OVR_Media_Surface_GetObject();

	[DllImport("OculusMediaSurface")]
	private static extern IntPtr OVR_Media_Surface_GetNativeTexture();

	[DllImport("OculusMediaSurface")]
	private static extern void OVR_Media_Surface_SetTextureParms(int texWidth, int texHeight);
}
