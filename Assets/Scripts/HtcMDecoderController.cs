//========= Copyright 2015-2016, HTC Corporation. All rights reserved. ===========

using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using System.IO;
using System;

namespace HTC.UnityPlugin.Multimedia {
    [RequireComponent(typeof(MediaDecoder))]
    public class HtcMDecoderController : MonoBehaviour, IVideoPlayerController {
        protected string LOG_TAG = "[VideoSourceController]";

        public string folderPath;
        public string filter;
        public bool isAdaptToResolution;
        //public UnityEvent onInitComplete;
        //public UnityEvent onChangeVideo;
        protected bool isInitialized = true; //false;
        //protected FileSeeker fileSeeker;

        protected MediaDecoder decoder;
        protected Vector3 oriScale;

        private GameController gameController;
        private string videoName;

        protected virtual void Awake() {
            decoder = GetComponent<MediaDecoder>();
            gameController = FindObjectOfType<GameController>();

            //isInitialized = false;
            oriScale = transform.localScale;
            isInitialized = true;
            //onInitComplete.Invoke();
            if (!isInitialized) {
                Debug.Log(LOG_TAG + " not initialized.");
                return;
            }

            if (isAdaptToResolution) {
                decoder.onInitComplete.AddListener(adaptResolution);
            }
            decoder.onInitComplete.AddListener(decoder.startDecoding);
            // forcing pause after init !!!
            decoder.onInitComplete.AddListener(decoder.setPause);
            decoder.onInitComplete.AddListener(delegate { gameController.VidInitCompleted(videoName); });
            decoder.onInitComplete.AddListener(decoder.onInitComplete.RemoveAllListeners);
        }

        public void stopVideo() {
            if (!isInitialized) {
                Debug.Log(LOG_TAG + " not initialized.");
                return;
            }
            decoder.stopDecoding();
        }

        protected virtual void adaptResolution() {
            int width = 1;
            int height = 1;
            decoder.getVideoResolution(ref width, ref height);
            Vector3 adaptReso = oriScale;
            adaptReso.x *= ((float)width / height);
            transform.localScale = adaptReso;
        }

        public void nextVideo() {
            if (!isInitialized) {
                Debug.Log(LOG_TAG + " not initialized.");
                return;
            }

            decoder.stopDecoding();
            //fileSeeker.toNext();

            //onChangeVideo.Invoke();
        }

        public void prevVideo() {
            if (!isInitialized) {
                Debug.Log(LOG_TAG + " not initialized.");
                return;
            }

            decoder.stopDecoding();
            //fileSeeker.toPrev();

            //onChangeVideo.Invoke();
        }

        // Interace methods
        public void PrepareVideo(string vidName) {
            this.videoName = vidName;
            string defilepath = Application.streamingAssetsPath + "/" + vidName;
            Debug.Log("decoding path: " + defilepath);
            decoder.initDecoder(defilepath);

            Debug.Log("setting Pause!");
            decoder.setPause();
        }

        public void PlayVideo() {
            // may need to do check for video initialization/decoding?
            decoder.setResume();
        }

        public void PauseVideo() {
            decoder.setPause();
        }

        public void StopVideo() {
            
        }

        public string GetVidName() {
            return videoName;
        }
    }
}