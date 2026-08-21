using Common;
using Coffee.UIExtensions;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Immortal_Switch.Scripts.Fx
{
    /// <summary>
    /// A pooled UIParticle effect spawned by <see cref="ClickFxManager"/> at a screen position.
    /// Renders on the dedicated top-most FX sub-canvas, never blocks raycast
    /// (UIParticle.raycastTarget is forced off), and auto-returns to the pool after its lifetime.
    /// </summary>
    public sealed class ClickFxObject : PoolableBehaviour
    {
        [Header("Components")]
        [SerializeField] private UIParticle uiParticle;

        [Header("Life Time")]
        [Tooltip("How long the object stays alive before returning to the pool (after the particle plays).")]
        [SerializeField] private float lifeTime = 0.8f;

        public float LifeTime
        {
            get => lifeTime;
            set => lifeTime = value;
        }

        /// <summary>Assign the particle template (used when the manager builds a default prefab at runtime).</summary>
        public void AssignParticle(UIParticle particle)
        {
            uiParticle = particle;

            if (uiParticle != null)
                uiParticle.raycastTarget = false;
        }

        /// <summary>Place the effect at a canvas-local offset (in the FX canvas space).</summary>
        public void SetPosition(Vector2 anchoredPosition)
        {
            var rt = (RectTransform)transform;

            // Center anchors are expected so anchoredPosition maps 1:1 to the full-screen FX root.
            rt.anchoredPosition = anchoredPosition;
        }

        /// <summary>
        /// Place the effect at a world-space point (e.g. the click point on the FX canvas plane).
        /// This is robust against nested-canvas coordinate quirks — the object renders exactly there.
        /// </summary>
        public void SetWorldPosition(Vector3 worldPosition)
        {
            transform.position = worldPosition;
        }

        public override void OnSpawnedFromPool()
        {
            if (uiParticle != null)
            {
                uiParticle.raycastTarget = false;
                uiParticle.Play();
            }
            else
            {
                // Fallback: even without a wired UIParticle, try to play a child ParticleSystem.
                var ps = GetComponentInChildren<ParticleSystem>();
                if (ps != null)
                    ps.Play(true);
            }

            if (lifeTime > 0f)
                DespawnSelf(lifeTime);
        }

        [Button]
        public void Play()
        {
            // var ps = GetComponentInChildren<ParticleSystem>();
            // if (ps != null)
            //     ps.Play(true);
            // return;
            uiParticle.raycastTarget = false;
            uiParticle.Play();
        }

        public override void OnDespawnedToPool()
        {
            if (uiParticle != null)
            {
                uiParticle.Stop();
                uiParticle.Clear();
            }
        }
    }
}