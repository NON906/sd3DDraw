using RemoveBackground;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.Networking;

namespace SD3DDraw
{
    [ExecuteInEditMode]
    public class SDDrawTarget : MonoBehaviour
    {
        const string ADD_PROMPT = "simple background";
        const int ALPHA_THRESHOLD = 10;

        public enum ControlModeEnum
        {
            Balanced,
            MyPrompt,
            ControlNet,
        }

        [TextArea(1, 10)]
        public string Prompt = "";
        [TextArea(1, 10)]
        public string NegativePrompt = "";
        public long Seed = -1;
        [Range(0f, 2f)]
        public float DepthWeight = 1f;
        public ControlModeEnum DepthControlMode = ControlModeEnum.MyPrompt;
        [Range(0f, 2f)]
        public float NormalWeight = 1f;
        public ControlModeEnum NormalControlMode = ControlModeEnum.MyPrompt;
        [Range(0f, 2f)]
        public float LineartWeight = 0f;
        public ControlModeEnum LineartControlMode = ControlModeEnum.MyPrompt;
        public Texture2D ReferenceTexture = null;
        [Range(0f, 2f)]
        public float ReferenceWeight = 0f;
        public ControlModeEnum ReferenceControlMode = ControlModeEnum.ControlNet;
        public bool ChangeMaterials = true;
        public bool DisableBackgroundMask = false;
        public int RemovalPixels = 8;

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
        Material reflectMaskMaterial_;
        Texture2D imageTexture_;
        Texture2D sdOutputTexture_;
        RunModel runModel_;
        Renderer[] renderers_ = null;
        List<Material[]> materials_ = null;
        List<int> defaultLayers_ = null;

        void Awake()
        {
            getDepthMaterial_ = new Material(Shader.Find("Hidden/SD3DDraw/GetDepth"));
            getNormalMaterial_ = new Material(Shader.Find("Hidden/SD3DDraw/GetNormal"));
            maskMaterial_ = new Material(Shader.Find("Hidden/SD3DDraw/CalcMask"));
            reflectMaskMaterial_ = new Material(Shader.Find("Hidden/SD3DDraw/ReflectMask"));
        }

        void Start()
        {
            sdManager_ = FindObjectOfType<SDManager>();

            runModel_ = FindObjectOfType<RunModel>();
        }

        void init()
        {
            GeneratedTexture = new Texture2D(sdManager_.Width, sdManager_.Height);
            depthTexture_ = new Texture2D(sdManager_.Width, sdManager_.Height);
            normalTexture_ = new Texture2D(sdManager_.Width, sdManager_.Height);
            imageTexture_ = new Texture2D(sdManager_.Width, sdManager_.Height);
            sdOutputTexture_ = new Texture2D(sdManager_.Width, sdManager_.Height);
        }

        public void Hide(bool changeMaterials = false)
        {
            if (renderers_ != null || defaultLayers_ != null)
            {
                return;
            }

            defaultLayers_ = new List<int>();
            renderers_ = GetComponentsInChildren<Renderer>();
            if (changeMaterials)
            {
                materials_ = new List<Material[]>();
            }
            for (int loop = 0; loop < renderers_.Length; loop++)
            {
                defaultLayers_.Add(renderers_[loop].gameObject.layer);
                renderers_[loop].gameObject.layer = LayerMask.NameToLayer("SDTarget");
                if (changeMaterials)
                {
                    materials_.Add(renderers_[loop].sharedMaterials);
                    var materials = new Material[renderers_[loop].sharedMaterials.Length];
                    for (int loop2 = 0; loop2 < renderers_[loop].sharedMaterials.Length; loop2++)
                    {
                        if (renderers_[loop].sharedMaterials[loop2].color.a >= 0.001f)
                        {
                            materials[loop2] = new Material(Shader.Find("Standard"));
                        }
                        else
                        {
                            materials[loop2] = renderers_[loop].sharedMaterials[loop2];
                        }
                    }
                    renderers_[loop].sharedMaterials = materials;
                }
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
                if (materials_ != null)
                {
                    renderers_[loop].sharedMaterials = materials_[loop];
                }
            }
            renderers_ = null;
            materials_ = null;
            defaultLayers_ = null;
        }

        public IEnumerator Generate(RenderTexture depthAllTexture)
        {
            init();

            var defaultMask = sdManager_.CaptureCamera.cullingMask;
            sdManager_.CaptureCamera.cullingMask = 1 << LayerMask.NameToLayer("SDTarget");

            Hide(ChangeMaterials);

            //sdManager_.CaptureCamera.Render();
            yield return new WaitForEndOfFrame();

            RenderTexture.active = RenderTexture.GetTemporary(sdManager_.Width, sdManager_.Height);

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

            if (LineartWeight > 0.001f)
            {
                Graphics.Blit(sdManager_.CaptureCamera.targetTexture, RenderTexture.active);
                imageTexture_.ReadPixels(new Rect(0, 0, sdManager_.CaptureCamera.targetTexture.width, sdManager_.CaptureCamera.targetTexture.height), 0, 0);
                imageTexture_.Apply();
                //bytes = imageTexture_.EncodeToPNG();
                //File.WriteAllBytes(@"image.png", bytes);
            }

            RenderTexture.ReleaseTemporary(RenderTexture.active);

            Show();

            sdManager_.CaptureCamera.cullingMask = defaultMask;

            var request = new Txt2ImgRequest();
            if (DisableBackgroundMask)
            {
                request.prompt = sdManager_.DefaultPrompt + ", " + Prompt;
            }
            else
            {
                request.prompt = sdManager_.DefaultPrompt + ", " + ADD_PROMPT + ", " + Prompt;
            }
            request.negative_prompt = sdManager_.DefaultNegativePrompt + ", " + NegativePrompt;
            request.seed = Seed;
            request.width = sdManager_.CaptureSize.x;
            request.height = sdManager_.CaptureSize.y;
            request.enable_hr = sdManager_.HiresFixScale > 1.001f;
            request.hr_scale = sdManager_.HiresFixScale;
            request.hr_upscaler = sdManager_.HiresFixUpscaler;
            request.denoising_strength = sdManager_.HiresFixScale > 1.001f ? sdManager_.DenoisingStrength : 0f;
            request.alwayson_scripts = new Txt2ImgRequestScripts();
            request.alwayson_scripts.controlnet = new Txt2ImgRequestScriptsControlNet();

            List<Txt2ImgRequestScriptsControlNetArgs> args = new List<Txt2ImgRequestScriptsControlNetArgs>();
            if (DepthWeight > 0.001f)
            {
                var arg = new Txt2ImgRequestScriptsControlNetArgs();
                arg.model = "depth";
                arg.image = Convert.ToBase64String(depthTexture_.EncodeToPNG());
                arg.control_mode = (int)DepthControlMode;
                arg.weight = DepthWeight;
                args.Add(arg);
            }
            if (NormalWeight > 0.001f)
            {
                var arg = new Txt2ImgRequestScriptsControlNetArgs();
                arg.model = "normalbae";
                arg.image = Convert.ToBase64String(normalTexture_.EncodeToPNG());
                arg.control_mode = (int)NormalControlMode;
                arg.weight = NormalWeight;
                args.Add(arg);
            }
            if (LineartWeight > 0.001f)
            {
                var arg = new Txt2ImgRequestScriptsControlNetArgs();
                arg.module = "lineart_anime";
                arg.model = "lineart_anime";
                arg.image = Convert.ToBase64String(imageTexture_.EncodeToPNG());
                arg.processor_res = 512;
                arg.control_mode = (int)LineartControlMode;
                arg.weight = LineartWeight;
                args.Add(arg);
            }
            if (ReferenceWeight > 0.001f && ReferenceTexture != null)
            {
                var arg = new Txt2ImgRequestScriptsControlNetArgs();
                arg.module = "reference_only";
                arg.image = Convert.ToBase64String(ReferenceTexture.EncodeToPNG());
                arg.control_mode = (int)ReferenceControlMode;
                arg.weight = ReferenceWeight;
                arg.threshold_a = 0.5f;
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

            RenderTexture maskTexture = null;
            if (!DisableBackgroundMask)
            {
                var originalMaskTexture = runModel_.Execute(sdOutputTexture_);
                maskTexture = RenderTexture.GetTemporary(sdOutputTexture_.width, sdOutputTexture_.height);
                Graphics.Blit(originalMaskTexture, maskTexture);
            }
            var addMaskTexture = RenderTexture.GetTemporary(sdManager_.Width, sdManager_.Height);
            maskMaterial_.SetTexture("_AllTex", depthAllTexture);
            Graphics.Blit(depthTexture_, addMaskTexture, maskMaterial_);

            RenderTexture.active = addMaskTexture;
            GeneratedTexture.ReadPixels(new Rect(0, 0, GeneratedTexture.width, GeneratedTexture.height), 0, 0);
            var pixels = GeneratedTexture.GetPixels32();
            for (int loop = 0; loop < RemovalPixels; loop++)
            {
                var newPixels = pixels.ToArray();
                for (int x = 1; x < GeneratedTexture.width - 1; x++)
                {
                    for (int y = 1; y < GeneratedTexture.height - 1; y++)
                    {
                        if (pixels[x + y * GeneratedTexture.width].r <= 0)
                        {
                            newPixels[(x + 1) + y * GeneratedTexture.width].r = 0;
                            newPixels[(x - 1) + y * GeneratedTexture.width].r = 0;
                            newPixels[x + (y + 1) * GeneratedTexture.width].r = 0;
                            newPixels[x + (y - 1) * GeneratedTexture.width].r = 0;
                        }
                    }
                }
                pixels = newPixels;
            }
            GeneratedTexture.SetPixels32(pixels);
            GeneratedTexture.Apply();
            RenderTexture.ReleaseTemporary(addMaskTexture);

            RenderTexture.active = RenderTexture.GetTemporary(sdManager_.Width, sdManager_.Height);
            reflectMaskMaterial_.SetTexture("_MaskTex", maskTexture);
            reflectMaskMaterial_.SetTexture("_AddMaskTex", GeneratedTexture);
            Graphics.Blit(sdOutputTexture_, RenderTexture.active, reflectMaskMaterial_);
            GeneratedTexture.ReadPixels(new Rect(0, 0, GeneratedTexture.width, GeneratedTexture.height), 0, 0);
            pixels = GeneratedTexture.GetPixels32();
            for (int loop = 0; loop < RemovalPixels; loop++)
            {
                var newPixels = pixels.ToArray();
                for (int x = 1; x < GeneratedTexture.width - 1; x++)
                {
                    for (int y = 1; y < GeneratedTexture.height - 1; y++)
                    {
                        if (pixels[x + y * GeneratedTexture.width].a > ALPHA_THRESHOLD)
                        {
                            newPixels[x + y * GeneratedTexture.width].a = 255;
                            newPixels[(x + 1) + y * GeneratedTexture.width].a = 255;
                            newPixels[(x - 1) + y * GeneratedTexture.width].a = 255;
                            newPixels[x + (y + 1) * GeneratedTexture.width].a = 255;
                            newPixels[x + (y - 1) * GeneratedTexture.width].a = 255;
                        }
                    }
                }
                for (int x = 0; x < GeneratedTexture.width; x++)
                {
                    for (int y = 0; y < GeneratedTexture.height; y++)
                    {
                        if (newPixels[x + y * GeneratedTexture.width].a <= ALPHA_THRESHOLD)
                        {
                            newPixels[x + y * GeneratedTexture.width].a = 0;
                        }
                    }
                }
                pixels = newPixels;
            }
            GeneratedTexture.SetPixels32(pixels);
            GeneratedTexture.Apply();
            RenderTexture.ReleaseTemporary(RenderTexture.active);

            var tempTex = RenderTexture.GetTemporary(GeneratedTexture.width, GeneratedTexture.height);
            reflectMaskMaterial_.SetTexture("_MaskTex", maskTexture);
            reflectMaskMaterial_.SetTexture("_AddMaskTex", null);
            Graphics.Blit(GeneratedTexture, tempTex, reflectMaskMaterial_);
            RenderTexture.active = tempTex;
            GeneratedTexture.ReadPixels(new Rect(0, 0, GeneratedTexture.width, GeneratedTexture.height), 0, 0);
            GeneratedTexture.Apply();
            RenderTexture.active = null;
            RenderTexture.ReleaseTemporary(tempTex);

            if (maskTexture != null)
            {
                RenderTexture.ReleaseTemporary(maskTexture);
            }

#if UNITY_EDITOR
            if (sdManager_.KeepSeedOnPlaying && EditorApplication.isPlaying)
            {
                var info = JsonUtility.FromJson<Txt2ImgResponseInfo>(response.info);
                Seed = info.seed;
            }
#else
            if (sdManager_.KeepSeedOnPlaying)
            {
                var info = JsonUtility.FromJson<Txt2ImgResponseInfo>(response.info);
                Seed = info.seed;
            }
#endif
        }
    }
}
