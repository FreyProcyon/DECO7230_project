using UnityEngine;

public class FreeFlyCamera : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float lookSpeed = 2f;

    private float yaw = 0.0f;
    private float pitch = 0.0f;

    void Update()
    {
        // 鼠标控制方向（右键按住）
        if (Input.GetMouseButton(1))
        {
            yaw += lookSpeed * Input.GetAxis("Mouse X");
            pitch -= lookSpeed * Input.GetAxis("Mouse Y");
            transform.eulerAngles = new Vector3(pitch, yaw, 0.0f);
        }

        // 键盘控制移动
        float horizontal = Input.GetAxis("Horizontal"); // A/D
        float vertical = Input.GetAxis("Vertical");     // W/S
        float upDown = 0;

        if (Input.GetKey(KeyCode.E)) upDown = 1;
        if (Input.GetKey(KeyCode.Q)) upDown = -1;

        Vector3 move = transform.right * horizontal + transform.forward * vertical + transform.up * upDown;
        transform.position += move * moveSpeed * Time.deltaTime;
    }
}
