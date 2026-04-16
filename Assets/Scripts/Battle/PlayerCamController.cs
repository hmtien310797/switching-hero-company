using System.Collections;
using DG.Tweening;
using UnityEngine;

namespace Battle
{
    public enum ShakeType
    {
        Shake,
        Punch
    }

    public class PlayerCamController : MonoBehaviour
    {
        [SerializeField] Transform camHolder;
        [SerializeField] float moveSpeed = 1.5f;
        [SerializeField] Vector3 offset = new Vector3(0, 7, 25);

        private Camera cam;
        private Transform[] playerTrans = new Transform[2];

        private Vector3 camPos;
        private Vector3 playerPos;
        private bool isShaked = false;
        
        private float _lastAspectRatio;
        private float landscapeFov = 30f;
        private float portraitFov = 60f;
        private float zPortraitCam = 20;
        private float zLandscapeCam = 28;
        private float zCam = 26;
        

        public void InitCam(Transform player, bool isMain)
        {
            if(isMain)
                playerTrans[0] = player;
            else 
                playerTrans[1] = player;
        }

        private void Awake()
        {
            cam = GetComponent<Camera>();
        }

        private void Update()
        {
            float currentAspect = (float)Screen.width / Screen.height;

            if (Mathf.Abs(currentAspect - _lastAspectRatio) > 0.01f)
            {
                _lastAspectRatio = currentAspect;
                UpdateFov(currentAspect);
            }

            if (Input.GetKeyDown(KeyCode.Space))
            {
                ShakeCamera(.5f);

                return;
            }
        }

        private void LateUpdate()
        {
            if (playerTrans[0] == null || playerTrans[1] == null)
            {
                return;
            }

            Domove();
        }

        private void Domove()
        {
            camPos = camHolder.position;
            var pos = GetTargetPos(playerTrans[0].position, playerTrans[1].position);
            playerPos = pos;
            if ((camPos - playerPos).sqrMagnitude < 0.1f)
            {
                camPos = playerPos;
                return;
            }

            camHolder.position = Vector3.Lerp(camPos, playerPos, Time.deltaTime * moveSpeed);
        }

        private Vector3 GetTargetPos(Vector3 a, Vector3 b)
        {
            float minZ = Mathf.Min(a.z, b.z);
            float zC = minZ - offset.z;

            float hA = a.z - zC;
            float hB = b.z - zC;

            float xC = (b.x * hA + a.x * hB) / (hA + hB);

            float yC = offset.y;

            return new Vector3(xC, yC, zC);
        }

        public void ShakeCamera(float dur, int viration = 50, ShakeType shakeType = ShakeType.Shake)
        {
            if (isShaked) return;

            StartCoroutine(DoShakeCamAsync(dur,viration,shakeType));
        }

        private IEnumerator DoShakeCamAsync(float dur, int viration = 50, ShakeType shakeType = ShakeType.Shake)
        {
            isShaked = true;
            switch (shakeType)
            {
                case ShakeType.Shake:
                    transform.DOShakePosition(dur, new Vector3(0.2f, 0.2f, 0f), viration, 0f, false, true).SetEase(Ease.Linear).OnComplete(() =>
                    {
                        isShaked = false;
                    }).SetRelative(true);
                    break;

                case ShakeType.Punch:
                    transform.DOPunchPosition(new Vector3(0.2f, 0.2f, 0f), dur, viration, .75f, false).SetEase(Ease.Linear).OnComplete(() =>
                    {
                        isShaked = false;
                    }).SetRelative(true);
                    break;
            }
            yield return null;
        }

        private void UpdateFov(float aspect)
        {
            if (aspect >= 1f)
            {
                cam.fieldOfView = landscapeFov;
                zCam = zLandscapeCam;
            }
            else
            {
                cam.fieldOfView = portraitFov;
                zCam = zPortraitCam;
            }

            offset.z = zCam;
        }
    }
}
