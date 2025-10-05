using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;

[Serializable]
public class MIDIResource : ISerializationCallbackReceiver
{
    [SerializeField] private UnityEngine.Object midiAsset;
    [SerializeField] string resourcePath = "";

    public string path;
    string uri;
    public string URI => uri;

    public void OnBeforeSerialize()
    {
#if UNITY_EDITOR
        if (midiAsset != null)
        {
            string assetPath = UnityEditor.AssetDatabase.GetAssetPath(midiAsset);

            // Verifica se o arquivo está na pasta StreamingAssets
            if (assetPath.Contains("StreamingAssets"))
            {
                // Pega apenas o nome do arquivo
                resourcePath = Path.GetFileName(assetPath);
                path = Path.Combine(Application.streamingAssetsPath, resourcePath);

                // Configura URI baseado na plataforma
#if UNITY_ANDROID && !UNITY_EDITOR
                        uri = path;
#elif UNITY_IOS && !UNITY_EDITOR
                        uri = path;
#else
                uri = "file://" + path;
#endif
            }
            else
            {
                Debug.LogWarning("Arquivo MIDI deve estar na pasta StreamingAssets");
                resourcePath = "error";
            }
        }
        else
        {
            resourcePath = "error";
        }
#endif
    }

    public void OnAfterDeserialize()
    {
        // Configura URI quando desserializar
        if (!string.IsNullOrEmpty(resourcePath) && resourcePath != "error")
        {
            path = Path.Combine(Application.streamingAssetsPath, resourcePath);

#if UNITY_ANDROID && !UNITY_EDITOR
                uri = path;
#elif UNITY_IOS && !UNITY_EDITOR
                uri = path;
#else
            uri = "file://" + path;
#endif
        }
    }
}