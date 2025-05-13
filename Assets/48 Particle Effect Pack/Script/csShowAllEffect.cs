using UnityEngine;
using UnityEngine.UI; // Usar UI moderna

public class csShowAllEffect : MonoBehaviour
{
    public string[] EffectNames;
    public string[] Effect2Names;
    public Transform[] Effect;
    public Text Text1; // Substituiu GUIText
    int i = 0;

    void Start()
    {
        ShowEffect();
    }

    void Update()
    {
        if (EffectNames.Length > 0)
        {
            Text1.text = (i + 1) + ": " + EffectNames[i];
        }

        if (Input.GetKeyDown(KeyCode.Z))
        {
            i = (i - 1 + EffectNames.Length) % EffectNames.Length;
            ShowEffect();
        }

        if (Input.GetKeyDown(KeyCode.X))
        {
            i = (i + 1) % EffectNames.Length;
            ShowEffect();
        }

        if (Input.GetKeyDown(KeyCode.C))
        {
            ShowEffect();
        }
    }

    void ShowEffect()
    {
        if (i < 0 || i >= Effect.Length)
            return;

        Vector3 pos = IsInEffect2Names(EffectNames[i]) ? new Vector3(0, 0.01f, 0) : new Vector3(0, 5, 0);
        Instantiate(Effect[i], pos, Quaternion.identity);
    }

    bool IsInEffect2Names(string name)
    {
        foreach (var effect2Name in Effect2Names)
        {
            if (name == effect2Name)
                return true;
        }
        return false;
    }
}
