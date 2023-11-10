using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace SD3DDraw
{
    public class SDManager : MonoBehaviour
    {
        const string API_URL = "http://127.0.0.1:7860";
        const string DEFAULT_PROMPT = "super fine illustration";
        const string DEFAULT_NEGATIVE_PROMPT = "flat color, flat shading, nsfw, retro style, poor quality, bad face, bad fingers, bad anatomy, missing fingers, low res, cropped, signature, watermark, username, artist name, text, logo, logos";

        public string ApiUrl = API_URL;
        public string DefaultPrompt = DEFAULT_PROMPT;
        public string DefaultNegativePrompt = DEFAULT_NEGATIVE_PROMPT;
        public Vector2Int CaptureSize = new Vector2Int(768, 512);
        public Camera CaptureCamera;

        public SDBackGround TargetBackGround
        {
            get;
            set;
        } = null;

        bool isGenerating_ = false;
        Texture2D targetTexture2D_;

        void Start()
        {
            CaptureCamera.depthTextureMode = DepthTextureMode.DepthNormals;
            CaptureCamera.targetTexture = new RenderTexture(CaptureSize.x, CaptureSize.y, 0, RenderTextureFormat.ARGB32);
            targetTexture2D_ = new Texture2D(CaptureSize.x, CaptureSize.y);

            Generate();
        }

        public void Generate()
        {
            StartCoroutine(generateCoroutine());
        }

        IEnumerator generateCoroutine()
        {
            if (isGenerating_)
            {
                yield break;
            }
            isGenerating_ = true;

            float defaultTimeScale = Time.timeScale;
            Time.timeScale = 0f;

            if (TargetBackGround != null)
            {
                yield return TargetBackGround.Generate();
            }

            RenderTexture.active = RenderTexture.GetTemporary(targetTexture2D_.width, targetTexture2D_.height);

            if (TargetBackGround != null && TargetBackGround.GeneratedTexture != null)
            {
                Graphics.Blit(TargetBackGround.GeneratedTexture, RenderTexture.active);
            }

            targetTexture2D_.ReadPixels(new Rect(0, 0, targetTexture2D_.width, targetTexture2D_.height), 0, 0);
            targetTexture2D_.Apply();
            byte[] bytes = targetTexture2D_.EncodeToPNG();
            File.WriteAllBytes(@"result.png", bytes);

            RenderTexture.ReleaseTemporary(RenderTexture.active);

            Time.timeScale = defaultTimeScale;

            isGenerating_ = false;
        }
    }
}
