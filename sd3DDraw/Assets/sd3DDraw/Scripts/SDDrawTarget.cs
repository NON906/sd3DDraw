using RemoveBackground;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace SD3DDraw
{
    public class SDDrawTarget : MonoBehaviour
    {
        const string ADD_PROMPT = "simple background";
        const int SHRINK_PIXELS = 1;

        [TextArea(1, 10)]
        public string Prompt = "";
        [TextArea(1, 10)]
        public string NegativePrompt = "";

        public Texture2D GeneratedTexture
        {
            get;
            private set;
        } = null;

        SDManager sdManager_;
        Material getDepthMaterial_;
        Texture2D depthTexture_;
        Material getNormalMaterial_;
        Texture2D normalTexture_;
        Material maskMaterial_;
        Texture2D sdOutputTexture_;
        RunModel runModel_;
        Renderer[] renderers_ = null;
        List<int> defaultLayers_ = null;

        void Awake()
        {
            sdManager_ = FindObjectOfType<SDManager>();
            sdManager_.AddDrawTarget(this);

            GeneratedTexture = new Texture2D(sdManager_.CaptureSize.x, sdManager_.CaptureSize.y);
            getDepthMaterial_ = new Material(Shader.Find("Hidden/SD3DDraw/GetDepth"));
            depthTexture_ = new Texture2D(sdManager_.CaptureSize.x, sdManager_.CaptureSize.y);
            getNormalMaterial_ = new Material(Shader.Find("Hidden/SD3DDraw/GetNormal"));
            normalTexture_ = new Texture2D(sdManager_.CaptureSize.x, sdManager_.CaptureSize.y);
            maskMaterial_ = new Material(Shader.Find("Hidden/SD3DDraw/CalcMask"));
            sdOutputTexture_ = new Texture2D(sdManager_.CaptureSize.x, sdManager_.CaptureSize.y);

            runModel_ = FindObjectOfType<RunModel>();
        }

        public void Hide()
        {
            if (renderers_ != null || defaultLayers_ != null)
            {
                return;
            }

            defaultLayers_ = new List<int>();
            renderers_ = GetComponentsInChildren<Renderer>();
            for (int loop = 0; loop < renderers_.Length; loop++)
            {
                defaultLayers_.Add(renderers_[loop].gameObject.layer);
                renderers_[loop].gameObject.layer = LayerMask.NameToLayer("SDTarget");
            }
        }

        public void Show()
        {
            if (renderers_ == null || defaultLayers_ == null)
            {
                return;
            }

            for (int loop = renderers_.Length - 1; loop >= 0; loop--)
            {
                renderers_[loop].gameObject.layer = defaultLayers_[loop];
            }
            renderers_ = null;
            defaultLayers_ = null;
        }

        public IEnumerator Generate(RenderTexture depthAllTexture)
        {
            var defaultMask = sdManager_.CaptureCamera.cullingMask;
            sdManager_.CaptureCamera.cullingMask = 1 << LayerMask.NameToLayer("SDTarget");

            Hide();

            sdManager_.CaptureCamera.Render();

            RenderTexture.active = RenderTexture.GetTemporary(sdManager_.CaptureSize.x, sdManager_.CaptureSize.y);

            Graphics.Blit(sdManager_.CaptureCamera.targetTexture, RenderTexture.active, getDepthMaterial_);
            depthTexture_.ReadPixels(new Rect(0, 0, depthTexture_.width, depthTexture_.height), 0, 0);
            depthTexture_.Apply();
            //byte[] bytes = depthTexture_.EncodeToPNG();
            //File.WriteAllBytes(@"depth.png", bytes);

            Graphics.Blit(sdManager_.CaptureCamera.targetTexture, RenderTexture.active, getNormalMaterial_);
            normalTexture_.ReadPixels(new Rect(0, 0, normalTexture_.width, normalTexture_.height), 0, 0);
            normalTexture_.Apply();
            //bytes = normalTexture_.EncodeToPNG();
            //File.WriteAllBytes(@"normal.png", bytes);

            RenderTexture.ReleaseTemporary(RenderTexture.active);

            Show();

            sdManager_.CaptureCamera.cullingMask = defaultMask;

            var request = new Txt2ImgRequest();
            request.prompt = sdManager_.DefaultPrompt + ", " + ADD_PROMPT + ", " + Prompt;
            request.negative_prompt = sdManager_.DefaultNegativePrompt + ", " + NegativePrompt;
            request.width = GeneratedTexture.width;
            request.height = GeneratedTexture.height;
            request.alwayson_scripts = new Txt2ImgRequestScripts();
            request.alwayson_scripts.controlnet = new Txt2ImgRequestScriptsControlNet();

            List<Txt2ImgRequestScriptsControlNetArgs> args = new List<Txt2ImgRequestScriptsControlNetArgs>();
            {
                var arg = new Txt2ImgRequestScriptsControlNetArgs();
                arg.model = "depth";
                arg.image = Convert.ToBase64String(depthTexture_.EncodeToPNG());
                args.Add(arg);
            }
            {
                var arg = new Txt2ImgRequestScriptsControlNetArgs();
                arg.model = "normalbae";
                arg.image = Convert.ToBase64String(normalTexture_.EncodeToPNG());
                args.Add(arg);
            }
            request.alwayson_scripts.controlnet.args = args.ToArray();

            var jsonRequest = JsonUtility.ToJson(request);

            using var webRequest = new UnityWebRequest(sdManager_.ApiUrl + "/sdapi/v1/txt2img", "POST")
            {
                uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(jsonRequest)),
                downloadHandler = new DownloadHandlerBuffer(),
            };

            webRequest.SetRequestHeader("Content-Type", "application/json; charset=utf-8");

            yield return webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.ConnectionError || webRequest.result == UnityWebRequest.Result.ProtocolError)
            {
                UnityEngine.Debug.LogError(webRequest.error);
                yield break;
            }

            var responseString = webRequest.downloadHandler.text;
            var response = JsonUtility.FromJson<Txt2ImgResponse>(responseString);

            string baseImageOnBase64 = response.images[0];
            byte[] baseImage = Convert.FromBase64String(response.images[0]);
            sdOutputTexture_.LoadImage(baseImage);

            RenderTexture.active = RenderTexture.GetTemporary(sdManager_.CaptureSize.x, sdManager_.CaptureSize.y);

            var maskTexture = runModel_.Execute(sdOutputTexture_);
            maskMaterial_.SetTexture("_AllTex", depthAllTexture);
            maskMaterial_.SetTexture("_TargetTex", depthTexture_);
            maskMaterial_.SetTexture("_MaskTex", maskTexture);
            Graphics.Blit(sdOutputTexture_, RenderTexture.active, maskMaterial_);
            GeneratedTexture.ReadPixels(new Rect(0, 0, GeneratedTexture.width, GeneratedTexture.height), 0, 0);

            var pixels = GeneratedTexture.GetPixels32();
            for (int loop = 0; loop < SHRINK_PIXELS; loop++)
            {
                var newPixels = pixels.ToArray();
                for (int x = 1; x < GeneratedTexture.width - 1; x++)
                {
                    for (int y = 1; y < GeneratedTexture.height - 1; y++)
                    {
                        if (pixels[x + y * GeneratedTexture.width].a <= 0)
                        {
                            newPixels[(x + 1) + y * GeneratedTexture.width].a = 0;
                            newPixels[(x - 1) + y * GeneratedTexture.width].a = 0;
                            newPixels[x + (y + 1) * GeneratedTexture.width].a = 0;
                            newPixels[x + (y - 1) * GeneratedTexture.width].a = 0;
                        }
                    }
                }
                pixels = newPixels;
            }
            for (int loop = 0; loop < SHRINK_PIXELS; loop++)
            {
                var newPixels = pixels.ToArray();
                for (int x = 1; x < GeneratedTexture.width - 1; x++)
                {
                    for (int y = 1; y < GeneratedTexture.height - 1; y++)
                    {
                        if (pixels[x + y * GeneratedTexture.width].a >= 255)
                        {
                            newPixels[(x + 1) + y * GeneratedTexture.width].a = 255;
                            newPixels[(x - 1) + y * GeneratedTexture.width].a = 255;
                            newPixels[x + (y + 1) * GeneratedTexture.width].a = 255;
                            newPixels[x + (y - 1) * GeneratedTexture.width].a = 255;
                        }
                    }
                }
                pixels = newPixels;
            }
            GeneratedTexture.SetPixels32(pixels);

            GeneratedTexture.Apply();

            RenderTexture.ReleaseTemporary(RenderTexture.active);
        }
    }
}
