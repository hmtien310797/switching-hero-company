using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Immortal_Switch.Scripts.Core;
using Immortal_Switch.Scripts.UI;
using Sirenix.OdinInspector;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Serialization;

public class GameCameraController : Singleton<GameCameraController>
{
    [SerializeField] private CinemachineCamera followHeroCamera;
    [SerializeField] private CinemachineCamera followBossCamera;
    [SerializeField] private Camera renderHeroCamera;
    [SerializeField] private CinemachineBasicMultiChannelPerlin followHeroCameraNoise;
    [SerializeField] private float amplitude = 1f;
    [SerializeField] private float frequency = 1f;
    [SerializeField] private float duration = 0.2f;

    [Header("POV per Screen")] [SerializeField]
    private float horizontalPlayerPov;

    [SerializeField] private float verticalPlayerPov;
    [SerializeField] private float horizontalSceneryPov;
    [SerializeField] private float verticalSceneryPov;

    [Header("Hero Camera Zoom Settings")] [SerializeField]
    private CinemachineFollow cineMachineHeroFollow;

    [SerializeField] private CinemachineRotationComposer cineMachineHeroRotation;
    [SerializeField] private float zoomSpeed = 1.5f;
    [SerializeField] private float zoomDelay = 1f;

    [Space] [SerializeField] private float normalFollowOffsetY = 11f;
    [SerializeField] private float normalFollowOffsetZ = -33f;
    [SerializeField] private float normalFollowY = 5f;
    [SerializeField] private float normalFollowZ = -15f;

    [Space] [SerializeField] private float normalTargetOffsetY = -0.03f;
    [SerializeField] private float normalTargetOffsetZ = 6.5f;
    [SerializeField] private float targetOffsetY;
    [SerializeField] private float targetOffsetZ;

    private int activePriority = 20;
    private int inactivePriority = 10;
    private CancellationTokenSource shakeCts;
    private Tween shakeTween;

    private void Start()
    {
        ScreenOrientationTracker.Instance.OnOrientationChanged += SetCameraFieldOfView;
        SetCameraFieldOfView(ScreenOrientationTracker.Instance.CurrentMode);
        followHeroCameraNoise.enabled = true;
        followHeroCameraNoise.AmplitudeGain = 0f;
        followHeroCameraNoise.FrequencyGain = frequency;
    }

    private void SetCameraFieldOfView(ScreenOrientationTracker.ScreenViewMode mode)
    {
        switch (mode)
        {
            case ScreenOrientationTracker.ScreenViewMode.Landscape:
                followHeroCamera.Lens.FieldOfView = horizontalPlayerPov;
                followBossCamera.Lens.FieldOfView = horizontalPlayerPov;
                renderHeroCamera.fieldOfView = horizontalSceneryPov;
                break;
            case ScreenOrientationTracker.ScreenViewMode.Portrait:
                followHeroCamera.Lens.FieldOfView = verticalPlayerPov;
                followBossCamera.Lens.FieldOfView = verticalPlayerPov;
                renderHeroCamera.fieldOfView = verticalSceneryPov;
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public void SetFollowHero(Transform target)
    {
        CameraTarget newCameraTarget = new CameraTarget
        {
            TrackingTarget = target,
            LookAtTarget = target,
        };

        followHeroCamera.Target = newCameraTarget;
    }

    public void FollowLastHeroTarget()
    {
        followHeroCamera.Priority = activePriority;
        followBossCamera.Priority = inactivePriority;
    }

    public void FollowBoss()
    {
        followBossCamera.Priority = activePriority;
        followHeroCamera.Priority = inactivePriority;
    }

    [Button]
    public async UniTask ZoomToHero()
    {
        // Kill tween cũ nếu ZoomToHero bị gọi liên tục
        DOTween.Kill(this);

        Vector3 followOffset = cineMachineHeroFollow.FollowOffset;
        Vector3 targetOffset = cineMachineHeroRotation.TargetOffset;

        Vector3 zoomFollowOffset = new Vector3(
            followOffset.x,
            normalFollowY,
            normalFollowZ
        );

        Vector3 zoomTargetOffset = new Vector3(
            targetOffset.x,
            targetOffsetY,
            targetOffsetZ
        );

        Vector3 normalFollowOffset = new Vector3(
            followOffset.x,
            normalFollowOffsetY,
            normalFollowOffsetZ
        );

        Vector3 normalTargetOffset = new Vector3(
            targetOffset.x,
            normalTargetOffsetY,
            normalTargetOffsetZ
        );
        
        await DOTween.Sequence()
            .SetId(this)
            .Join(DOTween.To(
                () => cineMachineHeroFollow.FollowOffset,
                value => cineMachineHeroFollow.FollowOffset = value,
                zoomFollowOffset,
                zoomSpeed
            ))
            .Join(DOTween.To(
                () => cineMachineHeroRotation.TargetOffset,
                value => cineMachineHeroRotation.TargetOffset = value,
                zoomTargetOffset,
                zoomSpeed
            ))
            .SetEase(Ease.OutQuad)
            .AsyncWaitForCompletion()
            .AsUniTask();

        await UniTask.Delay(TimeSpan.FromSeconds(zoomDelay));
        
        await DOTween.Sequence()
            .SetId(this)
            .Join(DOTween.To(
                () => cineMachineHeroFollow.FollowOffset,
                value => cineMachineHeroFollow.FollowOffset = value,
                normalFollowOffset,
                zoomSpeed
            ))
            .Join(DOTween.To(
                () => cineMachineHeroRotation.TargetOffset,
                value => cineMachineHeroRotation.TargetOffset = value,
                normalTargetOffset,
                zoomSpeed
            ))
            .SetEase(Ease.OutQuad)
            .AsyncWaitForCompletion()
            .AsUniTask();
    }

    [Button]
    public void ShakeCamera()
    {
        if (followHeroCameraNoise == null)
            return;

        shakeTween?.Kill();

        followHeroCameraNoise.AmplitudeGain = amplitude;
        followHeroCameraNoise.FrequencyGain = frequency;

        shakeTween = DOTween.To(
                () => followHeroCameraNoise.AmplitudeGain,
                value => followHeroCameraNoise.AmplitudeGain = value,
                0f,
                duration
            )
            .SetEase(Ease.OutQuad)
            .OnKill(() =>
            {
                if (followHeroCameraNoise != null)
                    followHeroCameraNoise.AmplitudeGain = 0f;
            });
    }


    public override UniTask InitializeAsync()
    {
        return UniTask.CompletedTask;
    }
}