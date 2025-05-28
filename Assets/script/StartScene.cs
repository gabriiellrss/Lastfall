using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Video;
using System.Collections;

public class StartScene : MonoBehaviour
{
    public Image fadeImage;
    public float fadeDuration = 1f;
    public float waitTime = 2f;
    public Player player;

    void Start()
    {
        player.StopPlayer(true);
        fadeImage.gameObject.SetActive(true);
        fadeImage.color = new Color(0, 0, 0, 1);
        StartCoroutine(FadeSequence());
    }

    IEnumerator FadeSequence()
    {
        yield return new WaitForSeconds(waitTime);

        float t = 0;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            float alpha = 1 - (t / fadeDuration);
            fadeImage.color = new Color(0, 0, 0, alpha);
            yield return null;
        }

        fadeImage.color = new Color(0, 0, 0, 0);
        fadeImage.gameObject.SetActive(false);

        yield return new WaitForSeconds(1f);

        player.StopPlayer(false);
    }
}
