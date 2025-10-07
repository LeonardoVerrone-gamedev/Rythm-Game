using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class AudioSourceData
{

    [SerializeField] public Group musicGroup;
    public AudioSource audioSource;

    public AudioSourceData(Group musicGroup)
    {
        this.musicGroup = musicGroup;
    }
}