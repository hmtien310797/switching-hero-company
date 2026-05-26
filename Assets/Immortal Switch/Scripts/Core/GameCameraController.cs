using Unity.Cinemachine;
using UnityEngine;

public class GameCameraController : MonoBehaviour
{
    [SerializeField] private CinemachineCamera followCamera;

    public void SetFollow(Transform target)
    {
        CameraTarget newCameraTarget = new CameraTarget
        {
            TrackingTarget = target,
            LookAtTarget = target,
        };
        followCamera.Target = newCameraTarget;
    }
}
