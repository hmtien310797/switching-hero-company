using System.Collections.Generic;
using UnityEngine;

public class HeroLeaderTrail
{
    private readonly List<Vector3> points = new();

    private readonly float minRecordDistance;
    private readonly int maxPoints;

    public HeroLeaderTrail(float minRecordDistance = 0.05f, int maxPoints = 256)
    {
        this.minRecordDistance = minRecordDistance;
        this.maxPoints = maxPoints;
    }

    public void Reset(Vector3 leaderPosition, Vector3 backDirection, float followDistance)
    {
        points.Clear();

        leaderPosition.y = 0f;
        backDirection.y = 0f;

        if (backDirection.sqrMagnitude <= 0.001f)
            backDirection = Vector3.back;

        backDirection.Normalize();

        // Quan trọng:
        // Seed sẵn 1 điểm phía sau leader.
        // Nếu không có điểm này, lúc mới switch hoặc mới đổi hướng follower dễ chạy vào leader.
        Vector3 behindPoint = leaderPosition + backDirection * followDistance;

        points.Add(behindPoint);
        points.Add(leaderPosition);
    }

    public void Record(Vector3 position)
    {
        position.y = 0f;

        if (points.Count == 0)
        {
            points.Add(position);
            return;
        }

        Vector3 last = points[^1];
        last.y = 0f;

        float sqrDistance = (position - last).sqrMagnitude;

        if (sqrDistance < minRecordDistance * minRecordDistance)
            return;

        points.Add(position);

        if (points.Count > maxPoints)
            points.RemoveAt(0);
    }

    public Vector3 GetPointBehind(float distanceBehind, Vector3 fallback)
    {
        fallback.y = 0f;

        if (points.Count == 0)
            return fallback;

        if (points.Count == 1)
            return points[0];

        float accumulated = 0f;

        for (int i = points.Count - 1; i > 0; i--)
        {
            Vector3 current = points[i];
            Vector3 previous = points[i - 1];

            current.y = 0f;
            previous.y = 0f;

            float segmentDistance = Vector3.Distance(current, previous);

            if (segmentDistance <= 0.0001f)
                continue;

            if (accumulated + segmentDistance >= distanceBehind)
            {
                float remain = distanceBehind - accumulated;
                float t = remain / segmentDistance;

                return Vector3.Lerp(current, previous, t);
            }

            accumulated += segmentDistance;
        }

        return points[0];
    }
}