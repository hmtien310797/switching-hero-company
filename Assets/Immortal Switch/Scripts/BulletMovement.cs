using UnityEngine;

public class BulletMovement : MonoBehaviour
{
    private float speed;
    private bool isInitialized = false;

    public void Setup(float bulletSpeed)
    {
        speed = bulletSpeed;
        isInitialized = true;
        // Tự hủy đạn sau 3 giây để tránh tràn bộ nhớ (Nên dùng Object Pooling nếu làm game thực tế)
        Destroy(gameObject, 3f); 
    }

    void Update()
    {
        if (!isInitialized) return;
        
        // Di chuyển đạn về phía trước theo hướng trục X hoặc Y tùy thuộc vào việc bạn làm game 2D top-down hay side-scrolling
        // Ở đây mặc định hướng Right (Trục X) làm hướng tới của vật thể 2D
        transform.Translate(Vector3.right * speed * Time.deltaTime, Space.Self);
    }
}