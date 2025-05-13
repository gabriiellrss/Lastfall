using UnityEngine;

public class BarScript : MonoBehaviour
{

    private Transform myCanera;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        myCanera = Camera.main.transform;
    }

    // Update is called once per frame
    void Update()
    {
        transform.LookAt(transform.position + myCanera.forward);
    }
}
