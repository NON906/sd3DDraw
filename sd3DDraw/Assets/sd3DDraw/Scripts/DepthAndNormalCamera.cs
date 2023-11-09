using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace SD3DDraw
{
    public class DepthAndNormalCamera : MonoBehaviour
    {
        public Vector2Int CaptureSize = new Vector2Int(768, 512);
        public Camera CaptureCamera;

        Texture2D targetTexture2D_;
        Material getDepthMaterial_;
        Material getNormalMaterial_;

        void Start()
        {
            CaptureCamera.depthTextureMode = DepthTextureMode.DepthNormals;
            CaptureCamera.targetTexture = new RenderTexture(CaptureSize.x, CaptureSize.y, 0, RenderTextureFormat.ARGB32);
            targetTexture2D_ = new Texture2D(CaptureSize.x, CaptureSize.y);
            getDepthMaterial_ = new Material(Shader.Find("Hidden/SD3DDraw/GetDepth"));
            getNormalMaterial_ = new Material(Shader.Find("Hidden/SD3DDraw/GetNormal"));

            StartCoroutine(captureCoroutine());
        }

        IEnumerator captureCoroutine()
        {
            yield return new WaitForSeconds(1f);

            yield return new WaitForEndOfFrame();

            RenderTexture.active = RenderTexture.GetTemporary(targetTexture2D_.width, targetTexture2D_.height);

            // Depth
            Graphics.Blit(CaptureCamera.targetTexture, RenderTexture.active, getDepthMaterial_);

            targetTexture2D_.ReadPixels(new Rect(0, 0, CaptureCamera.targetTexture.width, CaptureCamera.targetTexture.height), 0, 0);
            targetTexture2D_.Apply();

            byte[] bytes = targetTexture2D_.EncodeToPNG();
            File.WriteAllBytes(@"depth.png", bytes);

            // Normal
            Graphics.Blit(CaptureCamera.targetTexture, RenderTexture.active, getNormalMaterial_);

            targetTexture2D_.ReadPixels(new Rect(0, 0, CaptureCamera.targetTexture.width, CaptureCamera.targetTexture.height), 0, 0);
            targetTexture2D_.Apply();

            bytes = targetTexture2D_.EncodeToPNG();
            File.WriteAllBytes(@"normal.png", bytes);

            // Image
            Graphics.Blit(CaptureCamera.targetTexture, RenderTexture.active);

            targetTexture2D_.ReadPixels(new Rect(0, 0, CaptureCamera.targetTexture.width, CaptureCamera.targetTexture.height), 0, 0);
            targetTexture2D_.Apply();

            bytes = targetTexture2D_.EncodeToPNG();
            File.WriteAllBytes(@"image.png", bytes);

            RenderTexture.ReleaseTemporary(RenderTexture.active);
        }
    }
}
