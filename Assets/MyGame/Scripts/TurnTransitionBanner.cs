using UnityEngine;
using System.Collections;

public class TurnTransitionBanner : MonoBehaviour
{
    public CanvasGroup canvasGroup;

    public void StartTransition()
    {
        StartCoroutine(FadeSequence());
    }

    private IEnumerator FadeSequence()
    {
        // Fade In
        yield return StartCoroutine(Fade(0f, 1f, 0.5f));

        // Hold
        yield return new WaitForSeconds(1.0f);

        // Fade Out
        yield return StartCoroutine(Fade(1f, 0f, 0.5f));

        // Fully disable at end for safety
        gameObject.SetActive(false);
    }

    private IEnumerator Fade(float startAlpha, float endAlpha, float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            canvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, t);
            yield return null;
        }

        canvasGroup.alpha = endAlpha;
    }
}