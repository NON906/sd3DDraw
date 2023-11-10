using RemoveBackground;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace SD3DDraw
{
    public class SDDrawTarget : MonoBehaviour
    {
        const string ADD_PROMPT = "simple background, no background, solid color background";

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

        public IEnumerator Generate(RenderTexture depthAllTexture)
        {
            var defaultMask = sdManager_.CaptureCamera.cullingMask;
            sdManager_.CaptureCamera.cullingMask = 1 << LayerMask.NameToLayer("SDTarget");

            var defaultLayers = new List<int>();
            var renderers = GetComponentsInChildren<Renderer>();
            for (int loop = 0; loop < renderers.Length; loop++)
            {
                defaultLayers.Add(renderers[loop].gameObject.layer);
                renderers[loop].gameObject.layer = LayerMask.NameToLayer("SDTarget");
            }

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

            for (int loop = 0; loop < renderers.Length; loop++)
            {
                renderers[loop].gameObject.layer = defaultLayers[loop];
            }

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
            GeneratedTexture.Apply();

            RenderTexture.ReleaseTemporary(RenderTexture.active);
        }
    }
}
