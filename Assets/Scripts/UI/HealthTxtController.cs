using DG.Tweening;
using TMPro;
using UnityEngine;

public class HealthTxtController : MonoBehaviour
{
    [SerializeField] TMP_Text dameTxt;

    private float offsetY = .3f;
    private float moveTime = .2f;
    private bool isPlayingDameTxtAnim = false;

    private void Start()
    {

    }

    public bool IsFree()
    {
        return !isPlayingDameTxtAnim;
    }

    public void DoShowHealthTxt(float dame, Vector3 pos)
    {
        dameTxt.text = dame.ToString();
        dameTxt.transform.position = pos;

        DoAnimDameTxt();
    }

    private void SetDameTxtState(bool isEnable)
    {
        dameTxt.gameObject.SetActive(isEnable);
    }

    private void DoAnimDameTxt()
    {
        //if(isPlayingDameTxtAnim)
        /*{
            DOTween.Kill("dameMoving");
        }*/

        isPlayingDameTxtAnim = true;
        SetDameTxtState(true);
        var nY = dameTxt.transform.position.y + offsetY;
        dameTxt.transform.DOMoveY(nY, moveTime).SetEase(Ease.InCirc).OnComplete(() =>
        {
            isPlayingDameTxtAnim = false;
            SetDameTxtState(false);
        }).SetId("dameMoving");
    }
}
