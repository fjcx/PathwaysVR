using UnityEngine;
using System.Collections;
using System;
using UnityStandardAssets.ImageEffects;

[ExecuteInEditMode]
[RequireComponent(typeof(Camera))]
public class GradualGrayScaleEffect : ImageEffectBase {

    [Range(0f, 1.0f)]
    public float rampOffset;
    public float hueThreshold;
    public float satThreshold;

    // Called by camera to apply image effect
    void OnRenderImage(RenderTexture source, RenderTexture destination) {
        material.SetFloat("_RampOffset", rampOffset);
        material.SetFloat("_HueThreshold", hueThreshold);
        material.SetFloat("_SatThreshold", satThreshold);
        Graphics.Blit(source, destination, material);
    }
}
