using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.Networking;

namespace SD3DDraw
{
    public class InitTarget : IActiveBuildTargetChanged
    {
        static bool download(string url, string path)
        {
            using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
            {
                UnityWebRequestAsyncOperation request = webRequest.SendWebRequest();
                while (!request.isDone)
                {
                    EditorUtility.DisplayProgressBar("Download Assets", "Downloading: " + url, request.progress);
                    System.Threading.Thread.Sleep(16);
                }
                if (webRequest.error != null)
                {
                    EditorUtility.DisplayDialog("Error", "Download is failed\n" + url + "\n" + webRequest.error, "OK");
                    EditorUtility.ClearProgressBar();
                    return false;
                }
                File.WriteAllBytes(path, webRequest.downloadHandler.data);
            }

            EditorUtility.ClearProgressBar();
            return true;
        }

        static bool settingLayers(string[] layerNames, int[] defaultLayersNo)
        {
            bool isChange = false;

            string[] managerText = File.ReadAllLines("ProjectSettings/TagManager.asset");
            int managerTextLayersStart = 0;
            foreach (string line in managerText)
            {
                if (line == "  layers:")
                {
                    managerTextLayersStart++;
                    break;
                }
                managerTextLayersStart++;
            }

            for (int loop = 0; loop < layerNames.Length; loop++)
            {
                if (LayerMask.NameToLayer(layerNames[loop]) >= 0)
                {
                    continue;
                }

                if (string.IsNullOrEmpty(LayerMask.LayerToName(defaultLayersNo[loop])))
                {
                    managerText[managerTextLayersStart + defaultLayersNo[loop]] = "  - " + layerNames[loop];
                    isChange = true;
                    continue;
                }

                for (int loop2 = managerTextLayersStart + 8; loop2 < managerTextLayersStart + 32; loop2++)
                {
                    if (managerText[loop2] == "  - ")
                    {
                        managerText[loop2] = "  - " + layerNames[loop];
                        isChange = true;
                        break;
                    }
                }
            }

            if (isChange)
            {
                File.WriteAllLines("ProjectSettings/TagManager.asset", managerText);
                AssetDatabase.Refresh();
            }

            return isChange;
        }

        public int callbackOrder => 0;

        public void OnActiveBuildTargetChanged(BuildTarget previousTarget, BuildTarget newTarget)
        {
            if (!File.Exists("Packages/online.mumeigames.RemoveBackground@0.1.0/package.json"))
            {
                Directory.CreateDirectory("Packages/online.mumeigames.RemoveBackground@0.1.0/Resources/Models");

                download("https://github.com/NON906/sd3DDraw/releases/download/ver0.1.0/isnet-anime.onnx", "Packages/online.mumeigames.RemoveBackground@0.1.0/Resources/Models/isnet-anime.onnx");

                if (File.Exists("Assets/sd3DDraw/Jsons/package_local.json"))
                {
                    AssetDatabase.CopyAsset("Assets/sd3DDraw/Jsons/package_local.json", "Packages/online.mumeigames.RemoveBackground@0.1.0/package.json");
                }
                else
                {
                    AssetDatabase.CopyAsset("Packages/online.mumeigames.sd3ddraw/Jsons/package_local.json", "Packages/online.mumeigames.RemoveBackground@0.1.0/package.json");
                }
                AssetDatabase.Refresh();
            }

            settingLayers(new string[] { "SDTarget" }, new int[] { 8 });
        }
    }
}
