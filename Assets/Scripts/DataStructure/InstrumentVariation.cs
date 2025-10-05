using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class InstrumentVariation
{
    public MusicStyle musicStyle;

    public AudioClip audioTrack;
}

[System.Serializable]
public class TrackGroup
{
    public string groupName;

    public List<InstrumentVariation> instrumentVariations = new List<InstrumentVariation>();
}