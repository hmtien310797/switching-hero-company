using DG.Tweening;
using UnityEngine;

namespace Scripts.Battle
{
    public class PlayerCamController : MonoBehaviour
    {
        [SerializeField] float moveSpeed = 2f;

        private Transform playerTrans;

        private Vector3 camPos;
        private Vector3 playerPos;
        private bool isShaked = false;

        public void InitCam(Transform player)
        {
            playerTrans = player;
        }

        private void Update()
        {
            if(Input.GetKeyDown(KeyCode.Space))
            {
                ShakeCamera();

                return;
            }

            if (playerTrans == null || isShaked)
            {
                return;
            }

            Domove();
        }

        private void Domove()
        {
            camPos = transform.position;
            playerPos = playerTrans.position + new Vector3(0,7,-28);
            if (Vector3.Distance(camPos, playerPos) < 0.1f)
            {
                return;
            }

            transform.position = Vector3.Lerp(camPos,playerPos, Time.deltaTime * moveSpeed);
        }

        public void ShakeCamera()
        {
            if (isShaked) return;

            isShaked = true;
            transform.DOShakePosition(0.5f, new Vector3(0.2f, 0.2f, 0f), 20, 90f).OnComplete(() =>
            {
                isShaked = false;
            });
        }
    }
}
