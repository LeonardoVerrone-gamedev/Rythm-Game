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

    void Start()
    {
        Instance = this;
        comboScore = 0;
    }
    public static void Hit()
    {
        comboScore += 1;
        Instance.hitSFX.Play();
        OnHit?.Invoke();
    }
    public static void Miss()
    {
        comboScore = 0;
        Instance.missSFX.Play();
        OnMiss?.Invoke();
    }
    private void Update()
    {
        scoreText.text = comboScore.ToString();
    }
}