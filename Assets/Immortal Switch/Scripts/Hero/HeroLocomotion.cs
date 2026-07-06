using UnityEngine;

namespace Immortal_Switch.Scripts.Hero
{
    public class HeroLocomotion : MonoBehaviour
    {
        private Vector3 lastVelocity;

        public Vector3 LastVelocity => lastVelocity;

        public bool IsMoving => lastVelocity.sqrMagnitude > 0.0001f;

        public void MoveByDirection(Vector3 direction, float moveSpeed)
        {
            direction.y = 0f;

            if (direction.sqrMagnitude > 1f)
                direction.Normalize();

            if (direction.sqrMagnitude <= 0.001f)
            {
                lastVelocity = Vector3.zero;
                return;
            }

            Vector3 currentPosition = transform.position;
            Vector3 nextPosition = currentPosition + direction * moveSpeed * Time.deltaTime;

            transform.position = nextPosition;

            Vector3 delta = nextPosition - currentPosition;
            delta.y = 0f;

            lastVelocity = delta / Mathf.Max(Time.deltaTime, 0.0001f);
        }
        
        public void MoveTowardsPosition(
            Vector3 targetPosition,
            float moveSpeed,
            float stopDistance = 0f)
        {
            Vector3 currentPosition = transform.position;

            targetPosition.y = currentPosition.y;

            Vector3 direction = targetPosition - currentPosition;
            direction.y = 0f;

            float distance = direction.magnitude;

            if (distance <= stopDistance || distance <= 0.001f)
            {
                lastVelocity = Vector3.zero;
                return;
            }

            Vector3 moveDirection = direction / distance;
            float moveDistance = moveSpeed * Time.deltaTime;

            if (moveDistance > distance - stopDistance)
                moveDistance = Mathf.Max(0f, distance - stopDistance);

            Vector3 nextPosition =
                currentPosition + moveDirection * moveDistance;

            transform.position = nextPosition;

            Vector3 delta = nextPosition - currentPosition;
            delta.y = 0f;

            lastVelocity =
                delta / Mathf.Max(Time.deltaTime, 0.0001f);
        }

        public void FollowPositionSmooth(
            Vector3 targetPosition,
            float followSpeed,
            float stopDistance,
            float smoothTime,
            ref Vector3 velocity
        )
        {
            Vector3 currentPosition = transform.position;

            targetPosition.y = currentPosition.y;

            Vector3 direction = targetPosition - currentPosition;
            direction.y = 0f;

            float distance = direction.magnitude;

            if (distance <= stopDistance)
            {
                velocity = Vector3.zero;
                lastVelocity = Vector3.zero;
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

            Vector3 delta = nextPosition - currentPosition;
            delta.y = 0f;

            lastVelocity = delta / Mathf.Max(Time.deltaTime, 0.0001f);
        }

        public void Stop()
        {
            lastVelocity = Vector3.zero;
        }
    }
}