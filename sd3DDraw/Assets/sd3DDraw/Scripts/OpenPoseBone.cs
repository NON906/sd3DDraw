using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SD3DDraw
{
    public class OpenPoseBone : MonoBehaviour
    {
        public Transform TargetA;
        public Transform TargetB;

        void LateUpdate()
        {
            transform.position = (TargetA.position + TargetB.position) * 0.5f;
            transform.localScale = new Vector3(transform.localScale.x, transform.localScale.y, Vector3.Distance(TargetA.position, TargetB.position));
            transform.LookAt(TargetA.position);
        }
    }
}
