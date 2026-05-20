using UnityEngine;
using Spine.Unity;

public class SpineCharacterXZMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float followSpeed = 6.2f;
    [SerializeField] private float stopDistance = 0.08f;

    [Header("Spine")]
    [SerializeField] private SkeletonAnimation skeletonAnimation;
    [SerializeField] private string idleAnim = "idle";
    [SerializeField] private string moveAnim = "run";

    [Header("State")]
    [SerializeField] private bool isDead;

    private string currentAnim;

    public bool IsDead => isDead;

    private void Awake()
    {
        if (skeletonAnimation == null)
            skeletonAnimation = GetComponentInChildren<SkeletonAnimation>();
    }

    public void SetDead(bool value)
    {
        isDead = value;

        if (isDead)
        {
            PlayIdle();
        }
    }

    public void MoveByDirection(Vector3 direction)
    {
        if (isDead)
            return;

        direction.y = 0f;

        if (direction.sqrMagnitude > 1f)
            direction.Normalize();

        if (direction.sqrMagnitude <= 0.001f)
        {
            StopMove();
            return;
        }

        transform.position += direction * moveSpeed * Time.deltaTime;

        UpdateFacing(direction);
        PlayMove();
    }

    public void FollowPositionSmooth(Vector3 targetPosition, float smoothTime, ref Vector3 velocity)
    {
        if (isDead)
            return;

        Vector3 currentPosition = transform.position;
        targetPosition.y = currentPosition.y;

        Vector3 direction = targetPosition - currentPosition;
        direction.y = 0f;

        float distance = direction.magnitude;

        if (distance <= stopDistance)
        {
            StopMove();
            velocity = Vector3.zero;
            return;
        }

        Vector3 nextPosition = Vector3.SmoothDamp(
            currentPosition,
            targetPosition,
            ref velocity,
            smoothTime,
            followSpeed
        );

        transform.position = nextPosition;

        Vector3 moveDirection = nextPosition - currentPosition;
        moveDirection.y = 0f;

        if (moveDirection.sqrMagnitude > 0.0001f)
        {
            UpdateFacing(moveDirection.normalized);
            PlayMove();
        }
    }

    public void StopMove()
    {
        if (isDead)
            return;

        PlayIdle();
    }

    private void UpdateFacing(Vector3 direction)
    {
        if (skeletonAnimation == null)
            return;

        if (direction.x > 0.01f)
        {
            skeletonAnimation.Skeleton.ScaleX = 1f;
        }
        else if (direction.x < -0.01f)
        {
            skeletonAnimation.Skeleton.ScaleX = -1f;
        }
    }

    private void PlayIdle()
    {
        PlayAnimation(idleAnim, true);
    }

    private void PlayMove()
    {
        PlayAnimation(moveAnim, true);
    }

    private void PlayAnimation(string animName, bool loop)
    {
        if (skeletonAnimation == null)
            return;

        if (string.IsNullOrEmpty(animName))
            return;

        if (currentAnim == animName)
            return;

        skeletonAnimation.AnimationState.SetAnimation(0, animName, loop);
        currentAnim = animName;
    }
}