using UnityEngine;
using UnityEngine.UI;

public class StarHeroDisplay : MonoBehaviour
{
    [SerializeField] private GameObject[] stars;

    public void SetStar(int star)
    {
        switch (star)
        {
            case 0:
                stars[0].SetActive(false);
                stars[1].SetActive(false);
                break;
            case 1:
                stars[0].SetActive(true);
                stars[1].SetActive(false);
                break;
            case 2:
                stars[0].SetActive(true);
                stars[1].SetActive(true);
                break;
        }
    }
}
