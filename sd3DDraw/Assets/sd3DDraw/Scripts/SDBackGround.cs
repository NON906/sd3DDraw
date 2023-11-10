using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace SD3DDraw
{
    [Serializable]
    public class Txt2ImgRequestScriptsArgs
    {
        public bool enabled = true;
        public string module = "none";
        public string model;
        public float weight = 1f;
        public string image;
        public int resize_mode = 1;
        public bool lowvram = false;
        public float guidance_start = 0f;
        public float guidance_end = 1f;
        public int control_mode = 1;
        public bool pixel_perfect = false;
    }

    [Serializable]
    public class Txt2ImgRequestScriptsControlNet
    {
        public Txt2ImgRequestScriptsArgs[] args;
    }

    [Serializable]
    public class Txt2ImgRequestScripts
    {
        public Txt2ImgRequestScriptsControlNet controlnet = new Txt2ImgRequestScriptsControlNet();
    }

    [Serializable]
    public class Txt2ImgRequest
    {
        public string prompt;
        public string negative_prompt;
        public string[] styles = new string[0];
        public int steps = 20;
        public int cfg_scale = 7;
        public int width;
        public int height;
        public string sampler_index = "Euler a";
        public bool save_images = false;
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

            GeneratedTexture = new Texture2D(sdManager_.CaptureSize.x, sdManager_.CaptureSize.y);
        }

        public IEnumerator Generate()
        {
            var request = new Txt2ImgRequest();
            request.prompt = SDManager.DEFAULT_PROMPT + ", " + Prompt;
            request.negative_prompt = SDManager.DEFAULT_NEGATIVE_PROMPT + ", " + NegativePrompt;
            request.width = GeneratedTexture.width;
            request.height = GeneratedTexture.height;

            var jsonRequest = JsonUtility.ToJson(request);

            using var webRequest = new UnityWebRequest(SDManager.API_URL + "/sdapi/v1/txt2img", "POST")
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
