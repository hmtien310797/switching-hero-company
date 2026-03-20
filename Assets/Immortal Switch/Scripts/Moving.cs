using UnityEngine;

public class Moving : MonoBehaviour
{
    public float speed = 5f;
    public float groundY = 0f;

    void Update()
    {
        float x = Input.GetAxisRaw("Horizontal");
        float z = Input.GetAxisRaw("Vertical");

        Vector3 move = new Vector3(x, 0f, z).normalized * speed * Time.deltaTime;
        transform.position += move;

        // lock to ground height
        Vector3 p = transform.position;
        p.y = groundY;
        transform.position = p;
    }
}
