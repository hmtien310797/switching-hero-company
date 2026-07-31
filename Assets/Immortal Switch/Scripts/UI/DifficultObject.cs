using UnityEngine;
using UnityEngine.UI;

public class DifficultObject : MonoBehaviour
{
    public GameObject highLightImage;
    public RectTransform rectTransform;

    public void Show(bool value)
    {
        highLightImage.SetActive(value);
    }
}
