using UnityEngine;

namespace Immortal_Switch.Scripts.Skill
{
    public static class SkillAreaResolver
    {
        private const float DirectionEpsilon = 0.0001f;

        public static Vector3 NormalizeDirection(
            Vector3 direction,
            Vector3 fallbackDirection)
        {
            direction.y = 0f;

            if (direction.sqrMagnitude <= DirectionEpsilon)
            {
                direction = fallbackDirection;
                direction.y = 0f;
            }

            if (direction.sqrMagnitude <= DirectionEpsilon)
                direction = Vector3.right;

            return direction.normalized;
        }

        public static Vector3 GetRightDirection(
            Vector3 forwardDirection)
        {
            return new Vector3(
                -forwardDirection.z,
                0f,
                forwardDirection.x);
        }

        public static Vector3 ResolveCenter(
            SkillAreaData areaData,
            Vector3 areaOrigin,
            Vector3 forwardDirection)
        {
            if (areaData == null)
                return areaOrigin;

            if (areaData.Shape != SkillAreaShape.Box)
                return areaOrigin;

            float halfLength =
                areaData.BoxLength * 0.5f;

            switch (areaData.Anchor)
            {
                case SkillAreaAnchor.Forward:
                    return areaOrigin
                           + forwardDirection * halfLength;

                case SkillAreaAnchor.Backward:
                    return areaOrigin
                           - forwardDirection * halfLength;

                case SkillAreaAnchor.Center:
                default:
                    return areaOrigin;
            }
        }

        public static bool IsInsideBox(
            Vector3 targetPosition,
            Vector3 boxCenter,
            Vector3 forwardDirection,
            float boxLength,
            float boxWidth)
        {
            Vector3 rightDirection =
                GetRightDirection(forwardDirection);

            Vector3 offset =
                targetPosition - boxCenter;

            offset.y = 0f;

            float forwardDistance =
                Vector3.Dot(offset, forwardDirection);

            float sideDistance =
                Vector3.Dot(offset, rightDirection);

            float halfLength =
                Mathf.Max(0f, boxLength) * 0.5f;

            float halfWidth =
                Mathf.Max(0f, boxWidth) * 0.5f;

            return Mathf.Abs(forwardDistance) <= halfLength &&
                   Mathf.Abs(sideDistance) <= halfWidth;
        }

        public static Quaternion GetAreaRotation(
            Vector3 forwardDirection)
        {
            return Quaternion.LookRotation(
                forwardDirection,
                Vector3.up);
        }

        public static Vector3 GetOverlapBoxHalfExtents(
            SkillAreaData areaData,
            float height)
        {
            if (areaData == null)
                return Vector3.zero;

            return new Vector3(
                areaData.BoxWidth * 0.5f,
                Mathf.Max(0.01f, height) * 0.5f,
                areaData.BoxLength * 0.5f);
        }
    }
}