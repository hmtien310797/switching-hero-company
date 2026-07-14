using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace UI.LoginScene
{
    public class SliderStarFollower : MonoBehaviour
    {
        [System.Serializable]
        public struct StarPathPoint
        {
            public RectTransform Point;
            public Vector3 StarScale;
        }

        [Header("References")] [SerializeField]
        private Image filledImage;

        [SerializeField] private RectTransform star;
        [SerializeField] private StarPathPoint[] pathPoints;
        [SerializeField] private TMP_Text txtProgress;

        [SerializeField] private GameObject goProgressObj;

        [Header("Events")]
        public UnityEvent onLoadComplete;

        [Header("Tween")] [SerializeField] private float duration = 2f;
        [SerializeField] private Ease ease = Ease.Linear;

        [Header("Path")] [SerializeField] private int samplesPerSegment = 20;

        private Tween _progressTween;
        private float _currentProgress;

        private Vector3[] _sampledPositions;
        private Vector3[] _sampledScales;
        private float[] _cumulativeLengths;
        private float _totalLength;

        private void Start()
        {
            if (!IsValid())
            {
                return;
            }

            BuildPathCache();

            _currentProgress = 0f;
            ApplyProgress(0f);
            //PlayTo(1f);
        }

        public void PlayTo(float targetProgress)
        {
            if (!IsValid())
            {
                return;
            }

            BuildPathCache();

            targetProgress = Mathf.Clamp01(targetProgress);

            _progressTween?.Kill();

            var distance = Mathf.Abs(targetProgress - _currentProgress);

            // duration là thời gian chạy từ 0 -> 1.
            // Ví dụ progress tăng 10% thì chỉ tween duration * 10%.
            var tweenDuration = Mathf.Max(
                0.05f,
                duration * distance
            );

            _progressTween = DOTween.To(
                    () => _currentProgress,
                    value =>
                    {
                        _currentProgress = value;
                        ApplyProgress(_currentProgress);
                    },
                    targetProgress,
                    tweenDuration
                )
                .SetEase(ease)
                .SetLink(gameObject)
                .OnComplete(() =>
                {
                    if (Mathf.Approximately(_currentProgress, 1f))
                    {
                        goProgressObj?.SetActive(false);
                        onLoadComplete?.Invoke();
                    }
                });
        }

        public void SetProgressInstant(float progress)
        {
            if (!IsValid())
            {
                return;
            }

            BuildPathCache();

            _progressTween?.Kill();

            _currentProgress = Mathf.Clamp01(progress);
            ApplyProgress(_currentProgress);
        }
        
        public void ResetProgress()
        {
            if (!IsValid())
            {
                return;
            }

            BuildPathCache();

            _progressTween?.Kill();
            _progressTween = null;

            _currentProgress = 0f;

            if (goProgressObj != null)
            {
                goProgressObj.SetActive(true);
            }

            ApplyProgress(0f);
        }

        private void ApplyProgress(float progress)
        {
            progress = Mathf.Clamp01(progress);

            filledImage.fillAmount = progress;

            if (txtProgress != null)
            {
                txtProgress.text = $"{Mathf.RoundToInt(progress * 100f)}%";
            }

            GetPointByDistanceProgress(
                progress,
                out var position,
                out var scale
            );

            star.position = position;
            star.localScale = scale;
        }

        private void GetPointByDistanceProgress(
            float progress,
            out Vector3 position,
            out Vector3 scale
        )
        {
            position = star.position;
            scale = star.localScale;

            if (_sampledPositions == null ||
                _sampledPositions.Length == 0 ||
                _sampledScales == null ||
                _sampledScales.Length == 0 ||
                _cumulativeLengths == null ||
                _totalLength <= 0f)
            {
                return;
            }

            var targetDistance = _totalLength * Mathf.Clamp01(progress);

            for (var i = 1; i < _cumulativeLengths.Length; i++)
            {
                if (_cumulativeLengths[i] < targetDistance)
                {
                    continue;
                }

                var prevLength = _cumulativeLengths[i - 1];
                var nextLength = _cumulativeLengths[i];

                var segmentT = Mathf.InverseLerp(
                    prevLength,
                    nextLength,
                    targetDistance
                );

                position = Vector3.Lerp(
                    _sampledPositions[i - 1],
                    _sampledPositions[i],
                    segmentT
                );

                scale = Vector3.Lerp(
                    _sampledScales[i - 1],
                    _sampledScales[i],
                    segmentT
                );

                return;
            }

            position = _sampledPositions[^1];
            scale = _sampledScales[^1];
        }

        private void BuildPathCache()
        {
            var segmentCount = pathPoints.Length - 1;
            var sampleCount = segmentCount * samplesPerSegment + 1;

            _sampledPositions = new Vector3[sampleCount];
            _sampledScales = new Vector3[sampleCount];
            _cumulativeLengths = new float[sampleCount];

            _totalLength = 0f;

            for (var i = 0; i < sampleCount; i++)
            {
                var t = i / (float)(sampleCount - 1);

                _sampledPositions[i] = GetPositionOnSpline(t);
                _sampledScales[i] = GetScaleOnPath(t);

                if (i == 0)
                {
                    _cumulativeLengths[i] = 0f;
                    continue;
                }

                _totalLength += Vector3.Distance(
                    _sampledPositions[i - 1],
                    _sampledPositions[i]
                );

                _cumulativeLengths[i] = _totalLength;
            }
        }

        private Vector3 GetPositionOnSpline(float t)
        {
            t = Mathf.Clamp01(t);

            var count = pathPoints.Length;

            if (count == 2)
            {
                return Vector3.Lerp(
                    pathPoints[0].Point.position,
                    pathPoints[1].Point.position,
                    t
                );
            }

            var segmentCount = count - 1;
            var scaledT = t * segmentCount;

            var index = Mathf.Min(
                Mathf.FloorToInt(scaledT),
                segmentCount - 1
            );

            var segmentT = scaledT - index;

            var p0 = pathPoints[Mathf.Max(index - 1, 0)].Point.position;
            var p1 = pathPoints[index].Point.position;
            var p2 = pathPoints[index + 1].Point.position;
            var p3 = pathPoints[Mathf.Min(index + 2, count - 1)].Point.position;

            return CatmullRom(p0, p1, p2, p3, segmentT);
        }

        private Vector3 GetScaleOnPath(float t)
        {
            t = Mathf.Clamp01(t);

            var count = pathPoints.Length;

            if (count == 2)
            {
                return Vector3.Lerp(
                    pathPoints[0].StarScale,
                    pathPoints[1].StarScale,
                    t
                );
            }

            var segmentCount = count - 1;
            var scaledT = t * segmentCount;

            var index = Mathf.Min(
                Mathf.FloorToInt(scaledT),
                segmentCount - 1
            );

            var segmentT = scaledT - index;

            return Vector3.Lerp(
                pathPoints[index].StarScale,
                pathPoints[index + 1].StarScale,
                segmentT
            );
        }

        private static Vector3 CatmullRom(
            Vector3 p0,
            Vector3 p1,
            Vector3 p2,
            Vector3 p3,
            float t
        )
        {
            var t2 = t * t;
            var t3 = t2 * t;

            return 0.5f *
                   (
                       2f * p1 +
                       (-p0 + p2) * t +
                       (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
                       (-p0 + 3f * p1 - 3f * p2 + p3) * t3
                   );
        }

        private bool IsValid()
        {
            if (filledImage == null ||
                star == null ||
                pathPoints == null ||
                pathPoints.Length < 2)
            {
                return false;
            }

            foreach (var pathPoint in pathPoints)
            {
                if (pathPoint.Point == null)
                {
                    return false;
                }
            }

            return true;
        }

        private void OnDisable()
        {
            _progressTween?.Kill();
            _progressTween = null;
        }
    }
}