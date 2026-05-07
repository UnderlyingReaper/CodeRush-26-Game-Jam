using UnityEngine;

public class BoundaryTeleporter : MonoBehaviour
{
    [Header("Boundary Settings")]
    [Tooltip("Distance from stop center before teleport")]
    public float boundaryRadius = 20f;
    [Tooltip("Assign the bus stop center in Inspector")]
    public Transform stopCenter;

    [Header("References")]
    public CharacterController characterController;

    private bool isTeleporting = false;

    void Update()
    {
        if (isTeleporting) return;

        float distanceFromCenter = Mathf.Abs(transform.position.x - stopCenter.position.x);

        if (distanceFromCenter >= boundaryRadius)
        {
            Debug.Log("Teleporting");
            TeleportPlayer();
        }
    }

    void TeleportPlayer()
    {
        isTeleporting = true;
        characterController.enabled = false;

        float oppositeX = stopCenter.position.x - (Mathf.Sign(transform.position.x - stopCenter.position.x) * (boundaryRadius - 2f));

        transform.position = new Vector3(
            oppositeX,
            transform.position.y,
            transform.position.z
        );

        characterController.enabled = true;
        isTeleporting = false;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(stopCenter.position, boundaryRadius);
    }
}