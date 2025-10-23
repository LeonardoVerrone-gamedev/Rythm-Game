using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using System.IO;
using UnityEngine.Networking;
using System;

using UnityEngine.UI;

public class SongManager : MonoBehaviour
{
    #region Song and gameplay vars
    public static SongManager Instance;

    [Header("Selected song stuff")]

    public SongData selectedSong;
    public static MidiFile midiFile;

    [Header("Gameplay Stuff")]
    public Lane[] lanes;
    public float songDelayInSeconds;
    public double marginOfError; // in seconds

    public int inputDelayInMilliseconds;

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

    [Header("Band Member stuff")]
    public List<BandMemberInterface> bandMembers = new List<BandMemberInterface>
    {
        new BandMemberInterface(Group.Percussão),
        new BandMemberInterface(Group.Cordas),
        new BandMemberInterface(Group.Melodia)
    };

    [Header("Audio Sources")]

    public List<AudioSourceData> audioSources = new List<AudioSourceData>
    {
        new AudioSourceData(Group.Percussão),
        new AudioSourceData(Group.Cordas),
        new AudioSourceData(Group.Melodia)
    };

    private AudioSource mainAudioSource; //Mesmo da melodia

    #endregion


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

    #region MIDI Management and initialization
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
    #endregion

    #region Gameplay start

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

    #endregion

    #region Get Time, used in gameplay

    public static double GetAudioSourceTime()
    {
        return (double)Instance.mainAudioSource.timeSamples / Instance.mainAudioSource.clip.frequency;
    }

    #endregion


    #region Inspector stuff
    private void OnValidate()
    {
        ResetGroups();
    }

    private void ResetGroups()
    {
        // Para BandMemberInterface - apenas reseta os grupos mantendo as instâncias
        if (bandMembers.Count >= 1)
            ResetBandMemberGroup(bandMembers[0], Group.Percussão);
        if (bandMembers.Count >= 2)
            ResetBandMemberGroup(bandMembers[1], Group.Cordas);
        if (bandMembers.Count >= 3)
            ResetBandMemberGroup(bandMembers[2], Group.Melodia);

        // Para AudioSourceData - apenas reseta os grupos mantendo as instâncias
        if (audioSources.Count >= 1)
            audioSources[0].musicGroup = Group.Percussão;
        if (audioSources.Count >= 2)
            audioSources[1].musicGroup = Group.Cordas;
        if (audioSources.Count >= 3)
            audioSources[2].musicGroup = Group.Melodia;
    }

    private void ResetBandMemberGroup(BandMemberInterface bandMember, Group group)
    {
        // Use reflection ou um método público se disponível
        // Se BandMemberInterface tiver um método para resetar o grupo
        var field = typeof(BandMemberInterface).GetField("_targetGroup",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null)
        {
            field.SetValue(bandMember, group);
        }
    }
    #endregion

    #region GIZMOS

    private void OnDrawGizmos()
    {
        Vector3 pos = transform.position;

        if (midiFile != null)
        {
            var notes = midiFile.GetNotes();

            // Desenha as informações do MIDI
            Gizmos.color = Color.green;
            Gizmos.DrawIcon(pos + Vector3.up * 2f, "d_AudioSource Gizmo", true);

            // Usa Handles.Label que funciona em runtime
            GUI.color = Color.green;
            GUI.Label(new Rect(10, 10, 200, 100),
                    $"MIDI Info:\nTracks: {midiFile.Chunks.Count}\nNotes: {notes.Count}");
        }
        else
        {
            Gizmos.color = Color.red;
            Gizmos.DrawIcon(pos + Vector3.up * 2f, "d_console.erroricon.sml", true);

            GUI.color = Color.red;
            GUI.Label(new Rect(10, 10, 200, 50), "MIDI: Não carregado");
        }
    }

    #endregion
}