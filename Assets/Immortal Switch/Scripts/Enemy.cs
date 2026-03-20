using UnityEngine;

public class Enemy : MonoBehaviour
{
    public Transform player;
    public float speed = 3f;
    public float stoppingDistance = 1.5f;

    void Update()
    {
        if (!player) return;

        Vector3 dir = player.position - transform.position;
        dir.y = 0f;

        float distance = dir.magnitude;

        if (distance > stoppingDistance)
        {
            transform.position += dir.normalized * speed * Time.deltaTime;
        }
        else
        {
            // Đã tới tầm đánh → đứng yên / attack
            // Debug.Log("In attack range");
        }
    }
    
    
}
