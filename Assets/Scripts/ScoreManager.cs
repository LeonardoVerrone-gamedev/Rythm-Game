using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance;
    public AudioSource hitSFX;
    public AudioSource missSFX;
    public Text scoreText;
    static int comboScore;

    public delegate void ScoreAction();
    public static event ScoreAction OnMiss;
    public static event ScoreAction OnHit;

    public void StartGame()
    {
        Instance = this;
        comboScore = 0;
    }
    public static void Hit()
    {
        comboScore += 500;
        Instance.hitSFX.Play();
        OnHit?.Invoke();
    }
    public static void Miss()
    {
        if((comboScore - 100) >= 0){
            comboScore -= 100;
        }
        else
        {
            comboScore = 0;
        }

        Instance.missSFX.Play();
        OnMiss?.Invoke();
    }
    private void Update()
    {
        scoreText.text = $"Pontuação: {comboScore.ToString()}";
    }
}