using UnityEngine;

public class PlayerMovement : MonoBehaviour
{

    public float moveSpeed = 5f;

    void Update()
    {
        // Get input values (-1 to 1)
        float horizontalInput = Input.GetAxis("Horizontal");
        float verticalInput = Input.GetAxis("Vertical");
        
        // Create movement vector (no Y movement for ground-based movement)
        Vector3 movement = new Vector3(horizontalInput, 0f, verticalInput);
        
        // Apply movement using Transform
        transform.Translate(movement * moveSpeed * Time.deltaTime, Space.World);
    }
}