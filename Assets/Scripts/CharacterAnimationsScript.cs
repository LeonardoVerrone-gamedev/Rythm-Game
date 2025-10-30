using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterAnimationsScript : MonoBehaviour
{
    [SerializeField] Animator[] characterAnimators;

    public enum AnimationParameters { Idle, OnWrong, OnRight }

    public void PlayAnimation(AnimationParameters parameter)
    {
        switch (parameter)
        {
            case AnimationParameters.Idle:
                SetAnimator("Idle");
                break;
            case AnimationParameters.OnWrong:
                SetAnimator("OnWrong");
                break;
            case AnimationParameters.OnRight:
                SetAnimator("OnRight");
                break;
        }
    }
    
    private void SetAnimator(string trigger)
    {
        foreach(Animator anim in characterAnimators)
        {
            anim.SetTrigger(trigger);
        }
    }
}