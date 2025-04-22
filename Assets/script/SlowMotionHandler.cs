using UnityEngine;
using System.Collections;

public class SlowMotionHandler : MonoBehaviour
{
    private Coroutine timeScaleCoroutine;

    /// <summary>
    /// Inicia câmera lenta e retorna ao normal automaticamente após um tempo.
    /// </summary>
    /// <param name="amount">Quão lenta será a cena (0.2 = 20%)</param>
    /// <param name="transitionSpeed">Velocidade para suavizar a transição</param>
    /// <param name="duration">Quanto tempo durará a câmera lenta</param>
    public void TriggerSlowMotionTimed(float amount, float transitionSpeed, float duration)
    {
        if (timeScaleCoroutine != null)
            StopCoroutine(timeScaleCoroutine);

        timeScaleCoroutine = StartCoroutine(SlowMotionRoutine(amount, transitionSpeed, duration));
    }

    /// <summary>
    /// Inicia câmera lenta com transição suave, sem reset automático.
    /// </summary>
    public void TriggerSlowMotion(float amount, float transitionSpeed)
    {
        if (timeScaleCoroutine != null)
            StopCoroutine(timeScaleCoroutine);

        timeScaleCoroutine = StartCoroutine(SmoothTimeScale(amount, transitionSpeed));
    }

    /// <summary>
    /// Reseta o timeScale para 1 suavemente.
    /// </summary>
    public void ResetTimeScale(float transitionSpeed = 3f)
    {
        if (timeScaleCoroutine != null)
            StopCoroutine(timeScaleCoroutine);

        timeScaleCoroutine = StartCoroutine(SmoothTimeScale(1f, transitionSpeed));
    }

    private IEnumerator SlowMotionRoutine(float targetScale, float speed, float duration)
    {
        yield return StartCoroutine(SmoothTimeScale(targetScale, speed));
        yield return new WaitForSecondsRealtime(duration);
        yield return StartCoroutine(SmoothTimeScale(1f, speed));
    }

    private IEnumerator SmoothTimeScale(float target, float speed)
    {
        while (!Mathf.Approximately(Time.timeScale, target))
        {
            Time.timeScale = Mathf.Lerp(Time.timeScale, target, Time.unscaledDeltaTime * speed);
            Time.fixedDeltaTime = 0.02f * Time.timeScale;
            yield return null;
        }

        Time.timeScale = target;
        Time.fixedDeltaTime = 0.02f * Time.timeScale;
    }
}
