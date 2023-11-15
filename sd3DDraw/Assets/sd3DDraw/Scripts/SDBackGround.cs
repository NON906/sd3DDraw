using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace SD3DDraw
{
    [Serializable]
    public class Txt2ImgRequestScriptsControlNetArgs
    {
        public bool enabled = true;
        public string module = "none";
        public string model = "none";
        public float weight = 1f;
        public string image;
        public int resize_mode = 1;
        public bool lowvram = false;
        public float guidance_start = 0f;
        public float guidance_end = 1f;
        public int control_mode = 0;
        public bool pixel_perfect = false;
        public int processor_res = -1;
        public float threshold_a = -1f;
        public float threshold_b = -1f;
    }

    [Serializable]
    public class Txt2ImgRequestScriptsControlNet
    {
        public Txt2ImgRequestScriptsControlNetArgs[] args;
    }

    [Serializable]
    public class Txt2ImgRequestScripts
    {
        public Txt2ImgRequestScriptsControlNet controlnet = null;
    }

    [Serializable]
    public class Txt2ImgRequest
    {
        public string prompt;
        public string negative_prompt;
        public string[] styles = new string[0];
        public int seed = -1;
        public int steps = 20;
        public int cfg_scale = 7;
        public int width;
        public int height;
        public string sampler_index = "Euler a";
        public bool save_images = false;
        public bool enable_hr = false;
        public float hr_scale = 2f;
        public string hr_upscaler = "R-ESRGAN 4x+ Anime6B";
        public int hr_second_pass_steps = 0;
        public float denoising_strength = 0f;
        public Txt2ImgRequestScripts alwayson_scripts = null;
    }

    [Serializable]
    class Txt2ImgResponse
    {
        public string[] images;
        public string info;
    }

    public class SDBackGround : MonoBehaviour
    {
        [TextArea(1, 10)]
        public string Prompt = "";
        [TextArea(1, 10)]
        public string NegativePrompt = "";
        public int Seed = -1;

        public Texture2D GeneratedTexture
        {
            get;
            private set;
        } = null;

        SDManager sdManager_;

        void Awake()
        {
            sdManager_ = FindObjectOfType<SDManager>();
            sdManager_.TargetBackGround = this;
        }

        void init()
        {
            GeneratedTexture = new Texture2D(sdManager_.Width, sdManager_.Height);
        }

        public IEnumerator Generate()
        {
            init();

            var request = new Txt2ImgRequest();
            request.prompt = sdManager_.DefaultPrompt + ", " + Prompt;
            request.negative_prompt = sdManager_.DefaultNegativePrompt + ", " + NegativePrompt;
            request.seed = Seed;
            request.width = sdManager_.CaptureSize.x;
            request.height = sdManager_.CaptureSize.y;
            request.enable_hr = sdManager_.HiresFixScale > 1.001f;
            request.hr_scale = sdManager_.HiresFixScale;
            request.hr_upscaler = sdManager_.HiresFixUpscaler;
            request.denoising_strength = sdManager_.HiresFixScale > 1.001f ? sdManager_.DenoisingStrength : 0f;

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
            GeneratedTexture.LoadImage(baseImage);
        }
    }
}
