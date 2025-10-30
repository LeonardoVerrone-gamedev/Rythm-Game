using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BandMember : MonoBehaviour
{
    public MusicStyle style;
    public Group group;
    private CharacterAnimationsScript animations;

    public GameObject bandMemberPrefab;

    void Start()
    {
        animations = GetComponent<CharacterAnimationsScript>();
        ScoreManager.OnMiss += HandleMiss;
        ScoreManager.OnHit += HandleHit;
    }

    private void HandleMiss()
    {
        animations.PlayAnimation(CharacterAnimationsScript.AnimationParameters.OnWrong);
    }

    private void HandleHit()
    {
        
    }

    void OnDestroy()
    {
        ScoreManager.OnMiss -= HandleMiss;
        ScoreManager.OnHit -= HandleHit;
    }
}