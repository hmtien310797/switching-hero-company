using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Spine.Unity;
using UnityEngine;

namespace Battle
{
    public class BaseAnimController : MonoBehaviour
    {
        [SerializeField] SkeletonAnimation ska;

        private Dictionary<string, float> animDict = new Dictionary<string, float>();

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        protected virtual void Start()
        {
            if (ska && !ska.valid)
            {
                ska.Initialize(false);
            }

            InitAnimDict();
        }

        public SkeletonAnimation GetBaseSka()
        {
            return ska; 
        }

        public void RegisterAnimEvent(string animName, string eventName, Action<bool> eventAct)
        {
            ska.AnimationState.Event += (entry, e) =>
            {
                if(animName == entry.Animation.Name && e.Data.Name == eventName)
                {
                    eventAct?.Invoke(eventName == PlayerSkillController.eventFinalAttack);
                }
            };
        }

        private void InitAnimDict()
        {
            if (ska && !ska.valid)
            {
                ska.Initialize(false);
            }

            if (animDict.Count > 0) animDict.Clear();

            var animations = ska.Skeleton.Data.Animations;
            foreach (var anim in animations)
            {
                animDict.Add(anim.Name, anim.Duration);
            }
        }

        public void PlayAmin(string name, float speed = 1, bool isLooped = true)
        {
            if (ska == null) return;

            ska.timeScale = speed;      
            ska.AnimationState.SetAnimation(0, name, isLooped);
        }

        public void AddPassiveAnim(float delayTime)
        {
            var passiveTrack = ska.AnimationState.SetAnimation(1, StandAnimName.PassiveSwitch, true);
            passiveTrack.Alpha = 1;
            StopPassiveAnimAsync(delayTime).Forget();
        }

        public async UniTaskVoid StopPassiveAnimAsync(float dur)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(dur));
            ska.AnimationState.SetEmptyAnimation(1, 0.2f);
        }

        public float GetDurByAnimName(string name)
        {
            if (animDict.ContainsKey(name))
            {
                if (animDict[name] == 0)
                {
                    animDict[name] = ska.Skeleton.Data.FindAnimation(name).Duration;
                }

                return animDict[name];
            }

            animDict[name] = ska.Skeleton.Data.FindAnimation(name).Duration;
            return animDict[name];
        }

    }
}
