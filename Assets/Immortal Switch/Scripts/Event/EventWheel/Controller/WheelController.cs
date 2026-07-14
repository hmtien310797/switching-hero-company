using System;
using System.Collections.Generic;
using DG.Tweening;
using Immortal_Switch.Scripts.Event.EventWheel.UI;
using UnityEngine;

namespace Immortal_Switch.Scripts.Event.EventWheel.Controller
{
    public class WheelController : MonoBehaviour
    {
        [SerializeField]
        private List<UIEventWheelSegment> segments;

        [SerializeField]
        [Range(0f, 360f)]
        private float rotateDuration = 1f;

        [SerializeField]
        private int extraTurnsAfterResult = 1;

        // --- Public Fields ---
        public bool IsSpinning { get; private set; }

        // --- Private Fields ---
        private Tweener _tweener;
        private float SegmentAngle => 360f / segments.Count;

        private void OnEnable()
        {
            BuildSegments();
        }

        private void BuildSegments()
        {
            var segmentCount = segments.Count;

            for (int i = 0; i < segmentCount; i++)
            {
                segments[i].transform.localRotation = Quaternion.Euler(0f, 0f, SegmentAngle * i);
                segments[i].BindCommon(i + 1, i % 2 == 0);
            }
        }

        public void StartSpin()
        {
            KillTweens();

            IsSpinning = true;

            // Quay theo chiều kim đồng hồ.
            _tweener = transform
                .DOLocalRotate(
                    new Vector3(0f, 0f, -360f),
                    rotateDuration,
                    RotateMode.LocalAxisAdd)
                .SetEase(Ease.Linear)
                .SetLoops(-1, LoopType.Incremental);
        }

        public void StopAt(int targetIndex, Action onCompleted = null)
        {
            var segmentCount = segments.Count;

            if (targetIndex < 0 ||
                targetIndex >= segmentCount)
            {
                Debug.LogError($"Target index {targetIndex} is outside range 0–{segmentCount - 1}.");
                return;
            }

            _tweener?.Kill();
            _tweener = null;

            float currentZ = NormalizeAngle(transform.localEulerAngles.z);

            // Góc tâm của target trên sprite/wheel.
            float targetSegmentAngle = targetIndex * SegmentAngle;
            float desiredZ = NormalizeAngle(targetSegmentAngle);

            // Khoảng cách cần quay theo chiều kim đồng hồ.
            float clockwiseDistance = Mathf.Repeat(
                currentZ - desiredZ,
                360f
            );

            /*
             * Nếu target đã gần kim vàng dưới 1 segment (36°),
             * không dừng ngay vì sẽ bị giật.
             *
             * Phải cộng 360°, không cộng 36°.
             * Cộng 36° sẽ làm lệch sang segment kế tiếp.
             */
            if (clockwiseDistance < SegmentAngle)
            {
                clockwiseDistance += 360f;
            }

            clockwiseDistance += extraTurnsAfterResult * 360f;

            // Chiều kim đồng hồ là giảm góc Z.
            float finalZ = currentZ - clockwiseDistance;

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

            _tweener = transform
                .DOLocalRotate(
                    new Vector3(0f, 0f, finalZ),
                    stopDuration,
                    RotateMode.FastBeyond360)
                .SetEase(Ease.OutCubic)
                .OnComplete(() =>
                {
                    IsSpinning = false;

                    // Chuẩn hóa rotation để tránh số góc quá lớn sau nhiều lần quay.
                    transform.localRotation = Quaternion.Euler(
                        0f,
                        0f,
                        NormalizeAngle(finalZ)
                    );

                    onCompleted?.Invoke();
                });
        }

        private static float NormalizeAngle(float angle)
        {
            return Mathf.Repeat(angle, 360f);
        }

        private void KillTweens()
        {
            _tweener?.Kill();
            _tweener = null;
        }

        private void OnDestroy()
        {
            KillTweens();
        }
    }
}