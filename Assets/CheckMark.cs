using UnityEngine;

public class CheckMark : MonoBehaviour
{
    [SerializeField] private GameObject checkMark;

    public void ShowCheckMark(bool value)
    {
        checkMark.SetActive(value);
    }
}
