using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using System.IO;
using UnityEngine.Networking;
using System;

public class SongManager : MonoBehaviour
{
    public static SongManager Instance;

    public SongData selectedSong;

    public AudioSource audioSource;
    public Lane[] lanes;
    public float songDelayInSeconds;
    public double marginOfError; // in seconds

    public int inputDelayInMilliseconds;


    public string fileLocation;
    public float noteTime;
    public float noteSpawnY;
    public float noteTapY;
    public float noteDespawnY
    {
        get
        {
            return noteTapY - (noteSpawnY - noteTapY);
        }
    }

    public static MidiFile midiFile;

    // Start is called before the first frame update
    void Start()
    {
        Instance = this;
        StartCoroutine(ReadFromFile(selectedSong.midiResource.URI));
    }


    private IEnumerator ReadFromFile(string targetURI)
    {
        Debug.Log($"Tentando carregar MIDI de: {targetURI}");

        using (UnityWebRequest www = UnityWebRequest.Get(targetURI))
        {
            yield return www.SendWebRequest();

            // Unity 2018 - usar as propriedades antigas
            if (www.isNetworkError || www.isHttpError)
            {
                Debug.LogError($"Erro ao carregar arquivo MIDI: {www.error}");
                Debug.LogError($"URL: {targetURI}");
                Debug.LogError($"Código de erro: {www.responseCode}");

                // Debug adicional para verificar se o arquivo existe
#if !UNITY_ANDROID && !UNITY_WEBGL
                string filePath = targetURI.Replace("file://", "");
                if (!File.Exists(filePath))
                {
                    Debug.LogError($"Arquivo não encontrado em: {filePath}");
                }
#endif
            }
            else
            {
                Debug.Log("MIDI carregado com sucesso!");
                byte[] midiBytes = www.downloadHandler.data;
                Debug.Log($"Tamanho do arquivo: {midiBytes.Length} bytes");

                using (var stream = new MemoryStream(midiBytes))
                {
                    try
                    {
                        midiFile = MidiFile.Read(stream);
                        Debug.Log($"MIDI parseado - Tracks: {midiFile.Chunks.Count}");
                        GetDataFromMidi();
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"Erro ao parsear arquivo MIDI: {e.Message}");
                    }
                }
            }
        }
    }

    public void GetDataFromMidi()
    {
        var notes = midiFile.GetNotes();
        var array = new Melanchall.DryWetMidi.Interaction.Note[notes.Count];
        notes.CopyTo(array, 0);

        foreach (var lane in lanes) lane.SetTimeStamps(array);

        Invoke(nameof(StartSong), songDelayInSeconds);
    }

    public void StartSong()
    {
        audioSource.Play();
    }

    public static double GetAudioSourceTime()
    {
        return (double)Instance.audioSource.timeSamples / Instance.audioSource.clip.frequency;
    }
}