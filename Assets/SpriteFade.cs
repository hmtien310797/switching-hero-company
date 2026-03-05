using System.Collections;
using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;

public class SpriteFade : MonoBehaviour
{
    Material mat;

    void Start()
    {
        mat = GetComponent<SpriteRenderer>().material;
    }
    
    public void SetFade(float value)
    {
        mat.SetFloat("_Fade", value);
    }

    [Button]
    public void FadeCo()
    {
        StartCoroutine(Fade());
    }
    
    IEnumerator Fade()
    {
        float t = 0;

        while (t < 1)
        {
            t += Time.deltaTime;
            mat.SetFloat("_Fade", t);
            yield return null;
        }
    }
}