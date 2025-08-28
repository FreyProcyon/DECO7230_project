using UnityEngine;

public class CollisionHandler : MonoBehaviour
{
    public bool destroyOnCollision = true;
    public string collisionMessage = "Object collected!";

    void OnTriggerEnter(Collider other)
    {
        // Log the collision event
        Debug.Log("Collided with: " + other.name);
        
        // Display a message in the console
        Debug.Log(collisionMessage);
        
        // Destroy or deactivate the other object
        if (destroyOnCollision)
        {
            other.gameObject.SetActive(false);
        }
    }
}