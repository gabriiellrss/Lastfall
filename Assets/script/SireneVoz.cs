using UnityEngine;

public class AlarmSound : MonoBehaviour
{
    public AudioClip alarmClip;      
    public float interval = 30f;     

    public AudioSource audioSource;
    private float timer;

    void Start()
    {
        audioSource.clip = alarmClip;
        timer = interval;
    }

    void Update()
    {
        timer -= Time.deltaTime;

        if (timer <= 0f)
        {
            PlayAlarm();
            timer = interval; 
        }
    }

    void PlayAlarm()
    {
        audioSource.Play();
    }
}
