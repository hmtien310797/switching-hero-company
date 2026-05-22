using Cysharp.Threading.Tasks;
using Spine.Unity;
using System;
using System.Collections.Generic;
using Battle;
using UnityEngine;

namespace Scripts.Battle
{
    public class PlayerInternalFarSkillController : PlayerSkillController
    {
        [SerializeField] Transform arrowTrans;
        [SerializeField] Transform oArrowTrans;
        [SerializeField] string boneArraw = "BOW_ARROW";
        [SerializeField] string boneAiming = "AIMING";

        private Spine.Bone handBone;
        private Spine.Bone aimBone;

        private void Awake()
        {
            if (arrowTrans != null)
                arrowTrans.gameObject.SetActive(false);
        }

        private void Start()
        {
            handBone = BaseAnimController.GetBaseSka().Skeleton.FindBone(boneArraw);
            aimBone = BaseAnimController.GetBaseSka().Skeleton.FindBone(boneAiming);
        }

        public override void InitSkill(List<int> bEscs, Transform soTrans)
        {
            base.InitSkill(bEscs, soTrans);
            RegisterHitFrontAttactEvent();

            RegisterHitBackAttactEvent();
        }

        protected void RegisterHitFrontAttactEvent()
        {
            BaseAnimController.RegisterAnimEvent(StandAnimName.Attack1, eventAttack, DoAttackByEvent);
            BaseAnimController.RegisterAnimEvent(StandAnimName.Attack2, eventAttack, DoAttackByEvent);
            BaseAnimController.RegisterAnimEvent(StandAnimName.Attack3, eventAttack, DoAttackByEvent);
            BaseAnimController.RegisterAnimEvent(StandAnimName.Attack3, eventFinalAttack, DoAttackByEvent);
        }

        private void RegisterHitBackAttactEvent()
        {
            BaseAnimController.RegisterAnimEvent(StandAnimName.Attack1Back, eventAttack, DoAttackByEvent);
            BaseAnimController.RegisterAnimEvent(StandAnimName.Attack2Back, eventAttack, DoAttackByEvent);
            BaseAnimController.RegisterAnimEvent(StandAnimName.Attack3Back, eventAttack, DoAttackByEvent);
            BaseAnimController.RegisterAnimEvent(StandAnimName.Attack3Back, eventFinalAttack, DoAttackByEvent);
        }

        public override void DoAttack(Action endAct)
        {
            DoAnimAttack(endAct, out var animName);
            CoDoAttack(endAct, animName).Forget();
        }

        public async UniTaskVoid CoDoAttack(Action endAct, string animName)
        {
            var dur = BaseAnimController.GetDurByAnimName(animName);
            var targetPos = PlayerHeroController.GetMonsterPos() + Vector3.up * 1.5f;
            if (aimBone == null) aimBone = BaseAnimController.GetBaseSka().skeleton.FindBone(boneAiming);

            if (aimBone != null)
            {
                var isAfterTarger = transform.position.z > targetPos.z;
                var camPos = Camera.main.transform.position;
                camPos.z *= (isAfterTarger ? -1 : 1);
                camPos.y -= (isAfterTarger ? 9 : 0);
                var dir = (targetPos - camPos).normalized;
                Ray ray = new Ray(camPos, dir);
                Plane planeZ = new Plane(Vector3.forward, transform.position);
                var point = transform.position;
                if (planeZ.Raycast(ray, out var distance))
                {
                    point = ray.GetPoint(distance);
                    point = transform.InverseTransformPoint(point);
                }

                aimBone.X = point.x;
                aimBone.Y = point.y;
            }

            await UniTask.Delay(TimeSpan.FromSeconds(dur), cancellationToken: DisableCts.Token);
            endAct?.Invoke();
        }

        private void DoAttackByEvent(bool isFinal = false)
        {
            // if (oArrowTrans == null) return;
            // var (arrow, b) = PoolController.Instance.Get(arrowTrans, oArrowTrans.position);
            // var targetPos = PlayerHeroController.GetMonsterPos() + Vector3.up * 1.5f;
            // if (arrow != null)
            // {
            //     if (handBone == null) handBone = BaseAnimController.GetBaseSka().skeleton.FindBone(boneArraw);
            //     arrow.position = handBone != null ? handBone.GetWorldPosition(BaseAnimController.GetBaseSka().transform) : oArrowTrans.position;
            //     Vector3 direction = (targetPos - arrow.position).normalized;
            //     arrow.rotation = Quaternion.FromToRotation(Vector3.right, direction);
            // }
            //
            // DoFlyAsync(arrow, targetPos).Forget();
        }

        private async UniTaskVoid DoFlyAsync(Transform arrow, Vector3 target)
        {
            // if (arrow == null) return;
            // arrow.position = Vector3.MoveTowards(arrow.position, target, 30 * Time.deltaTime);
            // while ((arrow.position - target).sqrMagnitude > 0.1f)
            // {
            //     arrow.position = Vector3.MoveTowards(arrow.position, target, 30 * Time.deltaTime);
            //     arrow.Rotate(Vector3.right, 30, Space.Self);
            //     await UniTask.Yield(PlayerLoopTiming.Update, DisableCts.Token);
            // }
            //
            // DoAttackFx();
            // PlayerHeroController?.AttackBySpecific();
            // PoolController.Instance.ReturnToPool(arrow.gameObject);
            // await UniTask.Delay(TimeSpan.FromSeconds(.2f), cancellationToken: DisableCts.Token);
            // SkaFx.gameObject.SetActive(false);
        }

        public override void DoSwitch(Action endAct)
        {
            BaseAnimController?.PlayAmin(StandAnimName.Switch, 1, false);
            CoDoSwitch(endAct, StandAnimName.Switch).Forget();
        }

        private async UniTaskVoid CoDoSwitch(Action endAct, string animName)
        {
            var dur = BaseAnimController.GetDurByAnimName(animName);
            await UniTask.Delay(TimeSpan.FromSeconds(dur), cancellationToken: DisableCts.Token);
            DoActivePassive(endAct);
        }

        private void DoActivePassive(Action endAct)
        {
            var isShow = UnityEngine.Random.Range(0, 3) == 0;
            if (isShow)
            {
                BaseAnimController.AddPassiveAnim(5f);
                endAct?.Invoke();
            }
            else
                endAct?.Invoke();
        }
    }
}
