using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewSongData", menuName = "RythmGame/Data/SongData")]
public class SongData : ScriptableObject
{
    [Header("Midi and Metadata")]
    public MIDIResource midiResource;

    public string songName;
    public int BPM;

    [Header("Mixagem de Track Variation")]
    public List<TrackGroup> MusicGroups = new List<TrackGroup>();

    //INSERIR DEPOIS STRUCT COM INPUT-NOTA
}
