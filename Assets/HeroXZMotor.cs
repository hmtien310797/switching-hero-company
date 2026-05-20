using Spine.Unity;
using UnityEngine;

public class HeroXZMotor : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float followSpeed = 6f;
    [SerializeField] private float stopDistance = 0.1f;

    [Header("Spine")]
    [SerializeField] private SkeletonAnimation skeletonAnimation;
    [SerializeField] private string idleAnim = "idle";
    [SerializeField] private string moveAnim = "run";

    [Header("State")]
    [SerializeField] private bool isDead;

    private Vector3 lastMoveDirection;
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
        direction = direction.normalized;

        if (direction.sqrMagnitude <= 0.001f)
        {
            PlayIdle();
            return;
        }

        transform.position += direction * moveSpeed * Time.deltaTime;

        lastMoveDirection = direction;

        UpdateFacing(direction);
        PlayMove();
    }

    public void FollowPosition(Vector3 targetPosition)
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
            return;
        }

        Vector3 nextPosition = Vector3.MoveTowards(
            currentPosition,
            targetPosition,
            followSpeed * Time.deltaTime
        );

        transform.position = nextPosition;

        Vector3 moveDirection = nextPosition - currentPosition;
        moveDirection.y = 0f;

        if (moveDirection.sqrMagnitude > 0.001f)
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