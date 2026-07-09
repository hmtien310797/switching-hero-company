using Coffee.UIExtensions;
using UnityEngine;

namespace SwitchingHero.UI
{
    /// <summary>
    /// Tự động lặp lại hiệu ứng ShinyEffectForUGUI (vệt sáng quét chéo qua icon) theo chu kỳ —
    /// component gốc chỉ có Play() 1 lần, không tự lặp.
    /// </summary>
    [RequireComponent(typeof(ShinyEffectForUGUI))]
    public class ShinyLoopEffect : MonoBehaviour
    {
        [SerializeField] private float playDuration = 1f;
        [SerializeField] private float interval = 2.5f;

        private ShinyEffectForUGUI _shiny;
        private float _timer;

        private void Awake()
        {
            _shiny = GetComponent<ShinyEffectForUGUI>();
        }

        private void OnEnable()
        {
            _timer = interval;
        }

        private void Update()
        {
            _timer -= Time.deltaTime;
            if (_timer > 0f) return;

            _shiny.Play(playDuration);
            _timer = interval;
        }
    }
}
