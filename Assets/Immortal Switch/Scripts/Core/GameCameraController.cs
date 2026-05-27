using Unity.Cinemachine;
using UnityEngine;

public class GameCameraController : MonoBehaviour
{
    [SerializeField] private CinemachineCamera followHeroCamera;
    [SerializeField] private CinemachineCamera followBossCamera;
    
    private int activePriority = 20;
    private int inactivePriority = 10;

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
}
