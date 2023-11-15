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

            sdManager.CaptureSize = EditorGUILayout.Vector2IntField("Capture Size", sdManager.CaptureSize);
            sdManager.CaptureCamera = (Camera)EditorGUILayout.ObjectField("Capture Camera", sdManager.CaptureCamera, typeof(Camera), true);
            sdManager.GenerateOnStart = EditorGUILayout.Toggle("Generate On Start", sdManager.GenerateOnStart);
            sdManager.SaveDirectory = EditorGUILayout.TextField("Save Directory", sdManager.SaveDirectory);

            if (EditorApplication.isPlaying)
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
