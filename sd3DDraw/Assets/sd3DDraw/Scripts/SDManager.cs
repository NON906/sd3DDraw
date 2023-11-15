using RemoveBackground;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace SD3DDraw
{
    public class SDManager : MonoBehaviour
    {
        const string API_URL = "http://127.0.0.1:7860";
        const string DEFAULT_PROMPT = "super fine illustration";
        const string DEFAULT_NEGATIVE_PROMPT = "flat color, flat shading, nsfw, retro style, poor quality, bad face, bad fingers, bad anatomy, missing fingers, low res, cropped, signature, watermark, username, artist name, text, logo, logos";

        class DrawTargetWithDistance
        {
            public float Distance;
            public SDDrawTarget Target;
        }

        public string ApiUrl = API_URL;
        [TextArea(1, 10)]
        public string DefaultPrompt = DEFAULT_PROMPT;
        [TextArea(1, 10)]
        public string DefaultNegativePrompt = DEFAULT_NEGATIVE_PROMPT;
        public Vector2Int CaptureSize = new Vector2Int(512, 512);
        public Camera CaptureCamera;
        public bool GenerateOnStart = false;
        public string SaveDirectory = "";

        public SDBackGround TargetBackGround
        {
            get;
            set;
        } = null;

        bool isGenerating_ = false;
        public bool IsGenerating
        {
            get
            {
                return isGenerating_;
            }
        }

        Texture2D targetTexture2D_;
        List<DrawTargetWithDistance> drawTargets_ = new List<DrawTargetWithDistance>();
        RenderTexture depthAllTexture_;
        RenderTexture otherTexture_;
        Material getDepthMaterial_;
        Material overlayMaterial_;
        int saveId_ = 0;

        void Reset()
        {
            if (FindObjectOfType<RunModel>() == null)
            {
                gameObject.AddComponent<RunModel>();
            }
        }

        void Start()
        {
            CaptureCamera.depthTextureMode = DepthTextureMode.DepthNormals;
            CaptureCamera.targetTexture = new RenderTexture(CaptureSize.x, CaptureSize.y, 0, RenderTextureFormat.ARGB32);
            targetTexture2D_ = new Texture2D(CaptureSize.x, CaptureSize.y);
            depthAllTexture_ = new RenderTexture(CaptureSize.x, CaptureSize.y, 0);
            otherTexture_ = new RenderTexture(CaptureSize.x, CaptureSize.y, 0, RenderTextureFormat.ARGB32);
            getDepthMaterial_ = new Material(Shader.Find("Hidden/SD3DDraw/GetDepth"));
            overlayMaterial_ = new Material(Shader.Find("Hidden/SD3DDraw/Overlay"));

            if (GenerateOnStart)
            {
                Generate();
            }
        }

        public void AddDrawTarget(SDDrawTarget target)
        {
            var drawTarget = new DrawTargetWithDistance();
            drawTarget.Distance = Vector3.Distance(target.transform.position, CaptureCamera.transform.position);
            drawTarget.Target = target;
            drawTargets_.Add(drawTarget);
            drawTargets_ = drawTargets_.OrderByDescending(item => item.Distance).ToList();
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

            if (TargetBackGround != null && CaptureCamera.clearFlags == CameraClearFlags.Skybox)
            {
                CaptureCamera.clearFlags = CameraClearFlags.SolidColor;
                CaptureCamera.backgroundColor = Color.clear;
            }

            yield return new WaitForEndOfFrame();

            Graphics.Blit(CaptureCamera.targetTexture, depthAllTexture_, getDepthMaterial_);
            foreach (var drawTarget in drawTargets_)
            {
                drawTarget.Target.Hide();
            }
            var defaultMask = CaptureCamera.cullingMask;
            CaptureCamera.cullingMask = ~(1 << LayerMask.NameToLayer("SDTarget"));
            //CaptureCamera.Render();
            yield return new WaitForEndOfFrame();
            Graphics.Blit(CaptureCamera.targetTexture, otherTexture_);
            CaptureCamera.cullingMask = defaultMask;
            foreach (var drawTarget in drawTargets_)
            {
                drawTarget.Target.Show();
            }

            if (TargetBackGround != null)
            {
                yield return TargetBackGround.Generate();
            }
            foreach (var drawTarget in drawTargets_)
            {
                yield return drawTarget.Target.Generate(depthAllTexture_);
            }

            var activeTexture = RenderTexture.GetTemporary(targetTexture2D_.width, targetTexture2D_.height, 0, RenderTextureFormat.ARGB32);
            var tempTex = RenderTexture.GetTemporary(targetTexture2D_.width, targetTexture2D_.height, 0, RenderTextureFormat.ARGB32);

            if (TargetBackGround != null && TargetBackGround.GeneratedTexture != null)
            {
                Graphics.Blit(TargetBackGround.GeneratedTexture, tempTex);
                overlayMaterial_.SetTexture("_BaseTex", tempTex);
                Graphics.Blit(otherTexture_, activeTexture, overlayMaterial_);
            }
            else
            {
                Graphics.Blit(otherTexture_, activeTexture);
            }
            foreach (var drawTarget in drawTargets_)
            {
                Graphics.Blit(activeTexture, tempTex);
                overlayMaterial_.SetTexture("_BaseTex", tempTex);
                Graphics.Blit(drawTarget.Target.GeneratedTexture, activeTexture, overlayMaterial_);
            }

            RenderTexture.ReleaseTemporary(tempTex);

            RenderTexture.active = activeTexture;
            targetTexture2D_.ReadPixels(new Rect(0, 0, targetTexture2D_.width, targetTexture2D_.height), 0, 0);
            targetTexture2D_.Apply();
            byte[] bytes = targetTexture2D_.EncodeToPNG();
            while (File.Exists(Path.Combine(SaveDirectory, "result_" + saveId_.ToString("000000000") + ".png")))
            {
                saveId_++;
            }
            File.WriteAllBytes(Path.Combine(SaveDirectory, "result_" + saveId_.ToString("000000000") + ".png"), bytes);

            RenderTexture.ReleaseTemporary(activeTexture);

            Time.timeScale = defaultTimeScale;

            isGenerating_ = false;
        }
    }
}
