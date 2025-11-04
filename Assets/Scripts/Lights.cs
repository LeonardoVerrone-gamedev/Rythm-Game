using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Lights : MonoBehaviour
{
    public SpriteRenderer[] lightRenderers;
    public float initialOpacity;
    
    private Coroutine resetCoroutine;

    void Start()
    {
        ScoreManager.OnMiss += HandleMiss;
        ScoreManager.OnHit += HandleHit;
        
        // Guarda a opacidade inicial das luzes
        if (lightRenderers != null && lightRenderers.Length > 0)
        {
            initialOpacity = lightRenderers[0].color.a;
        }
    }

    public void HandleMiss()
    {
        // Muda todas as luzes para opacidade 0
        foreach(SpriteRenderer renderer in lightRenderers)
        {
            if (renderer != null)
            {
                Color newColor = renderer.color;
                newColor.a = 0f;
                renderer.color = newColor;
            }
        }
        
        // Inicia a corrotina para resetar após 30 quadros
        if (resetCoroutine != null)
        {
            StopCoroutine(resetCoroutine);
        }
        resetCoroutine = StartCoroutine(ResetLightsAfterFrames());
    }
    
    public void HandleHit()
    {
        // Opcional: comportamento quando acerta
        // Pode manter as luzes normais ou fazer outro efeito
    }

    private IEnumerator ResetLightsAfterFrames()
    {
        // Espera 30 quadros
        for (int i = 0; i < 30; i++)
        {
            yield return new WaitForEndOfFrame();
        }
        
        // Retorna todas as luzes para a opacidade inicial
        foreach(SpriteRenderer renderer in lightRenderers)
        {
            if (renderer != null)
            {
                Color newColor = renderer.color;
                newColor.a = initialOpacity;
                renderer.color = newColor;
            }
        }
    }

    void OnDestroy()
    {
        // Importante: sempre remover os eventos quando o objeto for destruído
        ScoreManager.OnMiss -= HandleMiss;
        ScoreManager.OnHit -= HandleHit;
        
        if (resetCoroutine != null)
        {
            StopCoroutine(resetCoroutine);
        }
    }
}