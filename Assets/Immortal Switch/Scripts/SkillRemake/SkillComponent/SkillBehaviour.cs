using UnityEngine;

namespace Immortal_Switch.Scripts.Skill
{
    public abstract class SkillBehaviour : MonoBehaviour
    {
        protected SkillRuntimeContext Context { get; private set; }

        public virtual void Init(SkillRuntimeContext context)
        {
            Context = context;
        }

        public abstract void Cast();

        public virtual void OnSpineEvent(string eventName) { }
        public virtual void OnAnimationComplete(string animationName) { }
        public virtual void Cancel() { }
    }
}
