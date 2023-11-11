using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SD3DDraw
{
    public class MoveToMainCamera : MonoBehaviour
    {
        SDManager sdManager_;

        void Start()
        {
            sdManager_ = FindObjectOfType<SDManager>();
        }

        void Update()
        {
            sdManager_.CaptureCamera.transform.parent = Camera.main.transform;
            sdManager_.CaptureCamera.transform.localPosition = Vector3.zero;
            sdManager_.CaptureCamera.transform.localRotation = Quaternion.identity;
        }
    }
}
