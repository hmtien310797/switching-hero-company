using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Scripts.Battle
{
    public class HeroPassiveController : MonoBehaviour
    {
        private bool isInPassiveAction = false;

        public virtual void ActivePassive(out bool isActived)
        {
            if (isInPassiveAction)
            {
                isActived = true;
                return;
            }

            isActived = false;
            isInPassiveAction = true;
        }

        public virtual void EndPassive()
        {
            isInPassiveAction = false;
        }
    }

    public class HeroKnightPassiveController : HeroPassiveController, IHeroPassiveController
    {


        public void ActivePassive(float passiveTime)
        {
            base.ActivePassive(out bool isActived);
            if (!isActived)
            {
                DoPassiveActionAsync(passiveTime).Forget();
            }
        }

        public async UniTaskVoid DoPassiveActionAsync(float passiveTime)
        {
            DoStartPassiveAction();
            await UniTask.Delay(System.TimeSpan.FromSeconds(passiveTime));
            EndPassive();
            DoEndPassiveAction();
        }

        public void DoStartPassiveAction()
        {

        }

        public void DoEndPassiveAction()
        {

        }

    }

    public class HeroAssassinPassiveController : HeroPassiveController, IHeroPassiveController
    {
        public void ActivePassive(float passiveTime)
        {
            base.ActivePassive(out bool isActived);

            if (!isActived)
            {
                DoPassiveActionAsync(passiveTime).Forget();
            }
        }

        public void DoEndPassiveAction()
        {
        }

        public async UniTaskVoid DoPassiveActionAsync(float passiveTime)
        {
        }

        public void DoStartPassiveAction()
        {
        }
    }

    public class HeroMagePassiveController : HeroPassiveController, IHeroPassiveController
    {
        public void ActivePassive(float passiveTime)
        {
            base.ActivePassive(out bool isActived);

            if (!isActived)
            {
                DoPassiveActionAsync(passiveTime).Forget();
            }
        }

        public void DoEndPassiveAction()
        {
        }

        public async UniTaskVoid DoPassiveActionAsync(float passiveTime)
        {
        }

        public void DoStartPassiveAction()
        {
        }
    }

    public class HeroArcherPassiveController : HeroPassiveController, IHeroPassiveController
    {
        public void ActivePassive(float passiveTime)
        {
            base.ActivePassive(out bool isActived);

            if (!isActived)
            {
                DoPassiveActionAsync(passiveTime).Forget();
            }
        }

        public void DoEndPassiveAction()
        {
        }

        public async UniTaskVoid DoPassiveActionAsync(float passiveTime)
        {
        }

        public void DoStartPassiveAction()
        {
        }
    }

    public interface IHeroPassiveController
    {
        public void ActivePassive(float passiveTime);
        public UniTaskVoid DoPassiveActionAsync(float passiveTime);
        public void DoStartPassiveAction();
        public void DoEndPassiveAction();
    }
}
