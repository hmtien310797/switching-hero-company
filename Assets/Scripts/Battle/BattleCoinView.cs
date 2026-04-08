using System;
using Common;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Immortal_Switch.Scripts.Currency;
using UnityEngine;

namespace Battle
{
    public class BattleCoinView : MonoBehaviour
    {
        [SerializeField] SpriteRenderer spriteRenderer;
        public int CoinNum = 1;

        public async UniTaskVoid DoDrop(float dur, Transform pos)
        {
            transform.localScale = Vector3.one;
            spriteRenderer?.DOFade(1, 0);
            transform.DOMoveY(0f, dur).SetEase(Ease.InCirc).OnComplete(() =>
            {
                DoBounce(pos).Forget();
            });
        }

        private async UniTaskVoid DoBounce(Transform pos)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(.1f), cancellationToken: this.GetCancellationTokenOnDestroy());
            var rand = UnityEngine.Random.Range(0, 4);
            var nPos = (rand) switch
            {
                0 => Vector3.left,
                1 => Vector3.forward,
                2 => Vector3.right,
                _ => Vector3.back,
            };

            nPos = transform.position + nPos * UnityEngine.Random.Range(0.5f, .95f);

            transform.DOJump(nPos, UnityEngine.Random.Range(0.5f, 0f), UnityEngine.Random.Range(2, 5), 0.25f).OnComplete(() =>
            {
                if (pos)
                    DoFlyToTarget(pos).Forget();
                else
                    DoHideTarget(pos.position).Forget();
            });
        }

        private async UniTaskVoid DoFlyToTarget(Transform pos)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(.15f), cancellationToken: this.GetCancellationTokenOnDestroy());

            float distance = Vector3.Distance(transform.position, pos.position + Vector3.up * 1.5f);
            float flySpeed = 15f;
            float dynamicDuration = distance / flySpeed;
            dynamicDuration = Mathf.Clamp(dynamicDuration, 0.25f, 0.75f);
            transform.DOMove(pos.position + Vector3.up * 1.5f, dynamicDuration).SetEase(Ease.InCirc).OnComplete(() =>
            {
                PoolController.Instance.ReturnToPool(gameObject);
                CurrencyManager.Instance.Add(CurrencyType.Gold, CoinNum);
                PoolController.Instance.ReturnToPool(gameObject);
            });
        }

        private async UniTaskVoid DoHideTarget(Vector3 pos)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(2.5f), cancellationToken: this.GetCancellationTokenOnDestroy());
            spriteRenderer?.DOFade(0, 1f).SetEase(Ease.OutCirc).OnComplete(() => 
            { 
                PoolController.Instance.ReturnToPool(gameObject);
                CurrencyManager.Instance.Add(CurrencyType.Gold, CoinNum);
                PoolController.Instance.ReturnToPool(gameObject);
            });
        }

        public Vector3 GetTargetPosByViewport(Vector3 pos, float initialDistance, Vector3 initialScale)
        {
            var battleCamera = Camera.main;
            float distance = Mathf.Abs(battleCamera.transform.position.z - pos.z);
            distance = Mathf.Max(distance, 0.1f);
            float frustumHeight = 2.0f * distance * Mathf.Tan(battleCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);
            float frustumWidth = frustumHeight * battleCamera.aspect;

            Vector3 camPos = battleCamera.transform.position;
            Vector3 camForward = battleCamera.transform.forward;
            Vector3 camUp = battleCamera.transform.up;
            Vector3 camRight = battleCamera.transform.right;

            Vector3 bottomLeft = camPos + camForward * distance
                               - camRight * (frustumWidth * 0.5f)
                               - camUp * (frustumHeight * 0.5f);

            Vector3 currentTargetPos = bottomLeft
                                     + camRight * (pos.x * frustumWidth)
                                     + camUp * (pos.y * frustumHeight);

            float scaleFactor = distance / initialDistance;
            transform.localScale = initialScale * scaleFactor;
            return currentTargetPos;
        }
    }
}
