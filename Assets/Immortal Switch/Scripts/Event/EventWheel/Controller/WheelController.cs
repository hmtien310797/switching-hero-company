using System;
using System.Collections.Generic;
using DG.Tweening;
using Game.Configs.Generated;
using Immortal_Switch.Scripts.Event.EventWheel.UI;
using Immortal_Switch.Scripts.Shared;
using UnityEngine;

namespace Immortal_Switch.Scripts.Event.EventWheel.Controller
{
    public class WheelController : MonoBehaviour
    {
        [SerializeField]
        private List<UIEventWheelSegment> segments;

        [SerializeField]
        private RectTransform rotate;

        [SerializeField]
        private RectTransform vfx;

        [SerializeField]
        [Range(0f, 360f)]
        private float rotateDuration = 1f;

        // --- Public Fields ---
        public bool IsSpinning { get; private set; }

        // --- Private Fields ---
        private Tweener _spinTweener;
        private Tweener _vfxTweener;
        private Vector3 _orgWheelLocalPos;

        private float SegmentAngle => 360f / segments.Count;

        private void Awake()
        {
            _orgWheelLocalPos = vfx.localPosition;
        }

        private void OnEnable()
        {
            KillTweens();
        }

        public void Bind(List<DynamicHeroesGlobalSpecificationsEventWheelRewardsPoolRow> rows)
        {
            var segmentCount = segments.Count;

            for (int i = 0; i < segmentCount; i++)
            {
                var segment = segments[i];
                segment.transform.localRotation = Quaternion.Euler(0f, 0f, SegmentAngle * i);

                if (rows.Count > i)
                {
                    var reward = rows[i];
                    var itemDisplay = DatabaseManager.Instance.GetDisplayData(reward.itemId);

                    if (itemDisplay != null)
                    {
                        segment.Bind(itemDisplay.ItemIcon,
                            itemDisplay.TierInfo.border,
                            itemDisplay.TierInfo.background,
                            itemDisplay.TierInfo.tierIcon
                        );

                        segment.BindCommon(reward.amount, i % 2 == 0);
                    }
                }
            }
        }

        public void StartSpin()
        {
            KillTweens();

            IsSpinning = true;

            // Quay theo chiều kim đồng hồ.
            _spinTweener = rotate
                .DOLocalRotate(
                    Vector3.back * 360f,
                    rotateDuration,
                    RotateMode.LocalAxisAdd
                )
                .SetEase(Ease.InCubic)
                .SetLoops(-1, LoopType.Incremental);
        }

        public void StopAt(int targetIndex, bool force = false, Action onCompleted = null)
        {
            var segmentCount = segments.Count;

            if (targetIndex < 0 ||
                targetIndex >= segmentCount)
            {
                Debug.LogError($"Target index {targetIndex} is outside range 0–{segmentCount - 1}.");
                return;
            }

            _spinTweener?.Kill();
            _spinTweener = null;

            float currentZ = NormalizeAngle(rotate.localEulerAngles.z);

            // Segment được xếp ngược kim đồng hồ (+Z), nên để đưa target
            // về kim chỉ thì góc đích của wheel phải mang dấu âm.
            float targetSegmentAngle = targetIndex * SegmentAngle;

            if (!force)
            {
                float desiredZ = NormalizeAngle(-targetSegmentAngle);

                // Khoảng cách cần quay theo chiều kim đồng hồ.
                float clockwiseDistance = Mathf.Repeat(
                    currentZ - desiredZ,
                    360f
                );

                // Không dừng ngay nếu target vừa nằm sát phía trước kim.
                if (clockwiseDistance < SegmentAngle)
                {
                    clockwiseDistance += 360f;
                }

                // LocalAxisAdd nhận delta. Clockwise luôn là Z âm.
                float finalZ = -Mathf.Abs(clockwiseDistance);

                float currentSpeed = 360f / rotateDuration;

                /*
                 * Ease.OutCubic có vận tốc đầu xấp xỉ:
                 * 3 * distance / duration.
                 *
                 * Chọn duration như dưới giúp vận tốc đầu lúc giảm tốc
                 * gần với vận tốc đang quay đều, tránh khựng.
                 */
                float stopDuration = 3f * clockwiseDistance / currentSpeed;
                stopDuration = Mathf.Clamp(stopDuration, 1.5f, 6f);

                _spinTweener = rotate
                    .DOLocalRotate(
                        new Vector3(0f, 0f, finalZ),
                        stopDuration,
                        RotateMode.LocalAxisAdd
                    )
                    .SetEase(Ease.OutCubic)
                    .OnComplete(() =>
                    {
                        IsSpinning = false;

                        ShowVfx(targetIndex);
                        onCompleted?.Invoke();
                    });
            }
            else
            {
                IsSpinning = false;

                rotate.localRotation = Quaternion.Euler(
                    0f,
                    0f,
                    -targetSegmentAngle
                );

                ShowVfx(targetIndex);
                onCompleted?.Invoke();
            }
        }

        private static float NormalizeAngle(float angle)
        {
            return Mathf.Repeat(angle, 360f);
        }

        private void ShowVfx(int targetIndex)
        {
            if (segments.Count > targetIndex)
            {
                _vfxTweener?.Kill();
                vfx.gameObject.SetActive(true);
                vfx.SetParent(segments[targetIndex].Segment);
                vfx.SetAsFirstSibling();

                vfx.localPosition = _orgWheelLocalPos;

                _vfxTweener = vfx
                    .DOLocalRotate(
                        Vector3.forward * 360f,
                        rotateDuration,
                        RotateMode.FastBeyond360
                    )
                    .SetEase(Ease.Linear)
                    .SetLoops(-1, LoopType.Incremental);
            }
            else
            {
                Debug.LogError($"[WheelController] WinIdx: {targetIndex} > {segments.Count}");
            }
        }

        private void KillTweens()
        {
            vfx.gameObject.SetActive(false);

            _vfxTweener?.Kill();
            _vfxTweener = null;

            _spinTweener?.Kill();
            _spinTweener = null;
        }

        private void OnDestroy()
        {
            KillTweens();
        }
    }
}