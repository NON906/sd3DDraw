using System.Collections;
using System.Collections.Generic;
using Unity.Barracuda;
using UnityEngine;

namespace RemoveBackground
{
    public class RunModel : MonoBehaviour
    {
        NNModel modelAsset_;
        Model runtimeModel_;
        IWorker worker_;

        void changeModelAsset()
        {
            if (modelAsset_ != null)
            {
                runtimeModel_ = ModelLoader.Load(modelAsset_);
                worker_ = WorkerFactory.CreateWorker(WorkerFactory.Type.Auto, runtimeModel_);
            }
        }

        void Awake()
        {
            modelAsset_ = Resources.Load<NNModel>("Models/isnet-anime");
            changeModelAsset();
        }

        public RenderTexture Execute(Texture targetTexture)
        {
            RenderTexture inputTexture = RenderTexture.GetTemporary(targetTexture.width, targetTexture.height);
            Graphics.Blit(targetTexture, inputTexture);

            var channelCount = 3;
            var input = new Tensor(inputTexture, channelCount);

            worker_.Execute(input);

            Tensor output = worker_.PeekOutput("mask");
            var outputTexture = output.ToRenderTexture();

            input.Dispose();
            RenderTexture.ReleaseTemporary(inputTexture);

            return outputTexture;
        }

        void OnDestroy()
        {
            if (worker_ != null)
            {
                worker_.Dispose();
                worker_ = null;
            }
        }
    }
}
