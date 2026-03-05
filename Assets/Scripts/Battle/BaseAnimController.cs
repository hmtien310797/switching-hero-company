using Spine.Unity;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Scripts.Battle
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

        public void RegisterAnimEvent(string animName, string eventName, Action eventAct)
        {
            ska.AnimationState.Event += (entry, e) =>
            {
                if(animName == entry.Animation.Name && e.Data.Name == eventName)
                {
                    Debug.Log($"Anim event {eventName} triggered.");
                    eventAct?.Invoke();
                }
            };
        }

        private void InitAnimDict()
        {
            if(animDict.Count > 0) animDict.Clear();

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

        public float GetDurByAnimName(string name)
        {
            if(animDict.ContainsKey(name)) return animDict[name];

            return 0f;
        }

    }
}
