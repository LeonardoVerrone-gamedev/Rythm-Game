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

    [Header("Band Member stuff")]
    public List<BandMemberInterface> bandMembers = new List<BandMemberInterface>
    {
        new BandMemberInterface(Group.Percussão),
        new BandMemberInterface(Group.Cordas),
        new BandMemberInterface(Group.Melodia)
    };

    public List<AudioSourceData> audioSources = new List<AudioSourceData>
    {
        new AudioSourceData(Group.Percussão),
        new AudioSourceData(Group.Cordas),
        new AudioSourceData(Group.Melodia)
    };

    private AudioSource mainAudioSource; //Mesmo da melodia

    void Start()
    {
        AudioSourceData melodiaSource = audioSources.Find(asd => asd.musicGroup == Group.Melodia);

        if (melodiaSource != null && melodiaSource.audioSource != null)
        {
            mainAudioSource = melodiaSource.audioSource;
            Debug.Log("mainAudioSource configurado para o AudioSource da Melodia");
        }
        else
        {
            Debug.LogError("Não foi possível encontrar o AudioSource da Melodia");
        }

        Instance = this;
        InitializeSong();
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

    public void SetWaves()
    {
        foreach (AudioSourceData audioSourceData in audioSources)
        {
            // Encontra o bandMember correspondente ao grupo
            BandMemberInterface bandMember = bandMembers.Find(bm => bm.targetGroup == audioSourceData.musicGroup);
            
            if (bandMember != null && bandMember.bandMember != null)
            {
                // Encontra o TrackGroup correspondente no SongData
                TrackGroup trackGroup = selectedSong.MusicGroups.Find(tg => tg.groupName == audioSourceData.musicGroup);
                
                if (trackGroup != null)
                {
                    // Encontra a variação do instrumento com o estilo do bandMember
                    InstrumentVariation variation = trackGroup.instrumentVariations.Find(iv => 
                        iv.musicStyle == bandMember.bandMember.style);
                    
                    if (variation != null && variation.audioTrack != null)
                    {
                        // Atribui o AudioClip ao AudioSource
                        audioSourceData.audioSource.clip = variation.audioTrack;
                        Debug.Log($"AudioClip atribuído para {audioSourceData.musicGroup} - Estilo: {bandMember.bandMember.style}");
                    }
                    else
                    {
                        Debug.LogWarning($"Variação de áudio não encontrada para {audioSourceData.musicGroup} com estilo {bandMember.bandMember.style}");
                    }
                }
                else
                {
                    Debug.LogWarning($"TrackGroup não encontrado para {audioSourceData.musicGroup} no SongData");
                }
            }
            else
            {
                Debug.LogWarning($"BandMember não encontrado ou não atribuído para {audioSourceData.musicGroup}");
            }
        }
    }

    public void InitializeSong()
    {
        if (selectedSong == null)
        {
            Debug.LogError("Nenhuma SongData selecionada!");
            return;
        }

        // Configura as waves baseadas nos band members
        SetWaves();
        
        // Inicia a corrotina para carregar o MIDI
        StartCoroutine(ReadFromFile(selectedSong.midiResource.URI));
    }

    public void StartSong()
    {
        // Para todos os áudios primeiro para garantir sincronização
        StopSong();

        // Pequeno delay para garantir que todos parem completamente
        StartCoroutine(PlayDelayed());
    }

    private IEnumerator PlayDelayed()
    {
        // Espera um frame para garantir que todos os AudioSources pararam
        yield return null;

        double startTime = AudioSettings.dspTime + 0.1; // 100ms de delay para sincronização

        // Agenda todos os AudioSources dos grupos para tocar no mesmo tempo
        foreach (AudioSourceData audioSourceData in audioSources)
        {
            if (audioSourceData.audioSource != null && audioSourceData.audioSource.clip != null)
            {
                audioSourceData.audioSource.PlayScheduled(startTime);
                Debug.Log($"Agendado áudio para {audioSourceData.musicGroup} em {startTime}");
            }
        }

        Debug.Log($"Todos os AudioSources agendados para: {startTime}");
    }

    public void StopSong()
    {

        // Para todos os AudioSources dos grupos
        foreach (AudioSourceData audioSourceData in audioSources)
        {
            if (audioSourceData.audioSource != null)
            {
                audioSourceData.audioSource.Stop();
            }
        }

        Debug.Log("Todos os AudioSources parados");
    }

    public static double GetAudioSourceTime()
    {
        return (double)Instance.mainAudioSource.timeSamples / Instance.mainAudioSource.clip.frequency;
    }
}