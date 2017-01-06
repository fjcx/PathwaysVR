using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IVideoPlayerController {

    void PlayVideo();
    void PauseVideo();
    void StopVideo();
    void PrepareVideo(string vidName);
    string GetVidName();
    void MoveTo(Vector3 newPos);
}
