using System.Collections;
using System.Collections.Generic;
using Unity.Barracuda;
using UnityEngine;

namespace RemoveBackground
{
    //[ExecuteInEditMode]
    public class RunModel : MonoBehaviour
    {
        public NNModel modelAsset;
        public NNModel ModelAsset
        {
            get
            {
                return modelAsset;
            }
            set
            {
                if (modelAsset != value)
                {
                    modelAsset = value;
                    changeModelAsset();
                }
            }
        }
        Model runtimeModel_;
        IWorker worker_;

        void Start()
        {
            changeModelAsset();
        }

        void changeModelAsset()
        {
            if (modelAsset != null)
            {
                runtimeModel_ = ModelLoader.Load(modelAsset);
                worker_ = WorkerFactory.CreateWorker(WorkerFactory.Type.Auto, runtimeModel_);
            }
        }

        public RenderTexture Execute(Texture targetTexture)
        {
            RenderTexture inputTexture = RenderTexture.GetTemporary(targetTexture.width, targetTexture.height);
            Graphics.Blit(targetTexture, inputTexture);

            var channelCount = 3;
            var input = new Tensor(inputTexture, channelCount);

            if (ModelAsset == null)
            {
                ModelAsset = Resources.Load<NNModel>("Models/isnet-anime");
            }
            else
            {
                changeModelAsset();
            }
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
