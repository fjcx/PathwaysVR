using UnityEngine;
using System.Collections;
using System;
using UnityStandardAssets.ImageEffects;

[ExecuteInEditMode]
[RequireComponent(typeof(Camera))]
public class FadeEffect : ImageEffectBase {

    [Range(0f, 1.0f)]
    public float rampOffset;

    // Called by camera to apply image effect
    void OnRenderImage(RenderTexture source, RenderTexture destination) {
        material.SetFloat("_RampOffset", rampOffset);
        Graphics.Blit(source, destination, material);
    }
}
