using System;
using Common;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Immortal_Switch.Scripts.Currency;
using UnityEngine;

namespace Battle
{
    public class BattleCoinView : PoolableBehaviour
    {
        [SerializeField] SpriteRenderer spriteRenderer;
        public int CoinNum = 1;
        private PoolHandle _poolHandle;
        private Transform bounceTarget;
        private Tweener dropTween;
        
        public void DoDrop(float dur, Transform pos)
        {
            bounceTarget = pos;

            transform.localScale = Vector3.one;
            Color color = spriteRenderer.color;
            color.a = 1f;
            spriteRenderer.color = color;

            dropTween?.Kill();

            dropTween = transform
                .DOMoveY(0f, dur)
                .SetEase(Ease.InCirc)
                .OnComplete(OnDropComplete);
        }
        
        private void OnDropComplete()
        {
            DoBounce().Forget();
        }

        private async UniTaskVoid DoBounce()
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

            transform.DOJump(nPos, UnityEngine.Random.Range(0.5f, 0f), UnityEngine.Random.Range(2, 5), 0.25f).OnComplete(OnJumpComplete);
        }

        private void OnJumpComplete()
        {
            if (bounceTarget)
                DoFlyToTarget(bounceTarget).Forget();
            else
                DoHideTarget().Forget();
        }

        private async UniTaskVoid DoFlyToTarget(Transform pos)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(.15f), cancellationToken: this.GetCancellationTokenOnDestroy());

            float distance = Vector3.Distance(transform.position, pos.position + Vector3.up * 1.5f);
            float flySpeed = 15f;
            float dynamicDuration = distance / flySpeed;
            dynamicDuration = Mathf.Clamp(dynamicDuration, 0.25f, 0.75f);
            transform.DOMove(pos.position + Vector3.up * 1.5f, dynamicDuration).SetEase(Ease.InCirc).OnComplete(OnFlyToTargetComplete);
        }

        private void OnFlyToTargetComplete()
        {
            CurrencyManager.Instance.AddLocalDemo(CurrencyType.gold, CoinNum);
            DespawnSelf();
        }

        private async UniTaskVoid DoHideTarget()
        {
            await UniTask.Delay(TimeSpan.FromSeconds(2.5f), cancellationToken: this.GetCancellationTokenOnDestroy());
            spriteRenderer?.DOFade(0, 1f).SetEase(Ease.OutCirc).OnComplete(OnFlyToTargetComplete);
        }

        public override void OnDespawnedToPool()
        {
            base.OnDespawnedToPool();
            transform.DOKill();
        }
    }
}
