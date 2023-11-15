using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SD3DDraw
{
    [CustomEditor(typeof(SDManager))]
    public class SDManagerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            Undo.RecordObject(target, "Parameter Change");

            EditorGUI.BeginChangeCheck();

            SDManager sdManager = (SDManager)target;

            sdManager.ApiUrl = EditorGUILayout.TextField("Api Url", sdManager.ApiUrl);

            EditorGUILayout.LabelField("Default Prompt");
            sdManager.DefaultPrompt = EditorGUILayout.TextArea(sdManager.DefaultPrompt);
            EditorGUILayout.LabelField("Default Negative Prompt");
            sdManager.DefaultNegativePrompt = EditorGUILayout.TextArea(sdManager.DefaultNegativePrompt);

            sdManager.CaptureSize = EditorGUILayout.Vector2IntField("Draw Size", sdManager.CaptureSize);
            sdManager.HiresFixScale = EditorGUILayout.Slider("Hires Fix Scale", sdManager.HiresFixScale, 1f, 4f);
            EditorGUI.BeginDisabledGroup(sdManager.HiresFixScale <= 1.001f);
            sdManager.HiresFixUpscaler = EditorGUILayout.TextField("Hires Fix Upscaler", sdManager.HiresFixUpscaler);
            sdManager.DenoisingStrength = EditorGUILayout.Slider("Denoising Strength", sdManager.DenoisingStrength, 0f, 1f);
            EditorGUI.EndDisabledGroup();
            sdManager.CaptureCamera = (Camera)EditorGUILayout.ObjectField("Capture Camera", sdManager.CaptureCamera, typeof(Camera), true);
            sdManager.GenerateOnStart = EditorGUILayout.Toggle("Generate On Start", sdManager.GenerateOnStart);
            sdManager.SaveDirectory = EditorGUILayout.TextField("Save Directory", sdManager.SaveDirectory);

            if (EditorApplication.isPlaying && sdManager.gameObject.scene.IsValid())
            {
                EditorGUI.BeginDisabledGroup(sdManager.IsGenerating);
                if (GUILayout.Button("Generate"))
                {
                    sdManager.Generate();
                }
                EditorGUI.EndDisabledGroup();
            }

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(sdManager);
            }
        }
    }
}
