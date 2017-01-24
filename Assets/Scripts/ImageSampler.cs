using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ImageSampler : MonoBehaviour {

    public RenderTexture rendTex;
    public GameController gameController;

    public enum Thresholds { HSV, RGB, RGBdiff};

    public Thresholds thresholdType = Thresholds.HSV;
    public float hueThreshold = 0.036f;
    public float saturationThreshold = 0.6f;

    public float redLowerThreshold = 0.3f;
    public float greenUpperThreshold = 0.1f;
    public float blueUpperThreshold = 0.1f;
    
    public float diffThreshold = 0.1f;
    public float sampleRateInSec = 1f;

    private bool doSampleTexture = false;

    // Starting in 2 seconds.
    // sample renderTexture every 1 seconds
    private void Start () {
        InvokeRepeating("SampleImage", 2.0f, sampleRateInSec);
    }

    private void SampleImage() {
        doSampleTexture = true;
    }

    private void OnPostRender() {
        if (doSampleTexture) {
            RenderTexture.active = rendTex;

            // texturize rendering
            Texture2D screenshot = new Texture2D(24, 24, TextureFormat.RGB24, false);
            screenshot.ReadPixels(new Rect(0, 0, 24, 24), 0, 0);
            screenshot.Apply();

            /*byte[] bytes = screenshot.EncodeToPNG();
            string filename = ScreenShotName(24, 24);
            System.IO.File.WriteAllBytes(filename, bytes);
            Debug.Log(string.Format("Took screenshot to: {0}", filename));*/

            Color avgColor = AverageColor(screenshot);
            if (isAboveThreshold(avgColor)) {
                Debug.Log("Mainly Red!: avgColor" + (Color32)avgColor);
                HandleOver();
            } else {
                HandleOut();
            }

            /*Color pixColor0 = screenshot.GetPixel(12, 12);
            Color pixColor1 = screenshot.GetPixel(9, 9);
            Color pixColor2 = screenshot.GetPixel(18, 18);
            if (pixColor0.r > 0.3 && pixColor0.g < 0.1 && pixColor0.b < 0.1 &&
               pixColor1.r > 0.3 && pixColor1.g < 0.1 && pixColor1.b < 0.1 &&
               pixColor2.r > 0.3 && pixColor2.g < 0.1 && pixColor2.b < 0.1) {
                Debug.Log("Mainly Red!: pixColor0" + pixColor0 + "pixColor1" + pixColor1 + "pixColor2" + pixColor2);
            }*/

            RenderTexture.active = null;
            doSampleTexture = false;
        }
    }

    private bool isAboveThreshold(Color testColor) {
        switch (thresholdType) {
            case Thresholds.HSV:
                float hue;
                float saturation;
                float value;
                Color.RGBToHSV(testColor, out hue, out saturation, out value);
                if (hue < hueThreshold && saturation > saturationThreshold) {
                    Debug.Log("Mainly Red!: HSV: hue:" + hue + "saturation:" + saturation + "brightness:" + value);
                    return true;
                }
                break;
            case Thresholds.RGB:
                // red - green > 0.1 && r - b > 0.1
                if (testColor.r - testColor.g > diffThreshold && testColor.r - testColor.b > diffThreshold) {
                    return true;
                }
                break;
            case Thresholds.RGBdiff:
                if (testColor.r > redLowerThreshold && testColor.g < greenUpperThreshold && testColor.b < blueUpperThreshold) {
                    return true;
                }
                break;
        }
        return false;
    }

    private Color AverageColor(Texture2D tex) {
        Color[] texColors = tex.GetPixels();
        int total = texColors.Length;
        float r = 0;
        float g = 0;
        float b = 0;
        for (var i = 0; i < total; i++) {
            r += texColors[i].r;
            g += texColors[i].g;
            b += texColors[i].b;
        }
        return new Color(r/total, g/total, b/total, 0);
    }

    private void HandleOver() {
        Debug.Log("Over RedSurface: ");
        gameController.PlayBlinkEffect("sampler");
    }

    private void HandleOut() {
        gameController.CancelBlinkTransit("sampler");
    }

    public static string ScreenShotName(int width, int height) {
        return string.Format("{0}/screenshots/screen_{1}x{2}_{3}.png",
                             Application.dataPath,
                             width, height,
                             System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss"));
    }

}
