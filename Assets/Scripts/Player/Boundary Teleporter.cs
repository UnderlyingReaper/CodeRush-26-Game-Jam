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
    [Tooltip("Assign the player's Main Camera here to check look direction")]
    public Transform playerCamera;

    private bool isTeleporting = false;
    private bool _disableRightSide = false;
    private bool _disableLeftSide = false;

    private void Start()
    {
        VoidEncroachment.OnLoop2WallCreated += () => _disableLeftSide = true;
        // Fixed the logic bug from the previous script: Loop 3 disables the right side
        VoidEncroachment.OnLoop3WallsCreated += () => _disableRightSide = true;
    }

    void Update()
    {
        if (isTeleporting) return;

        float distanceFromCenter = Mathf.Abs(transform.position.x - stopCenter.position.x);

        if (distanceFromCenter >= boundaryRadius)
        {
            bool isRightSide = transform.position.x > stopCenter.position.x;

            if (isRightSide)
            {
                // In Loop 3, the right side is completely blocked
                if (_disableRightSide) return;

                // In Loop 2, the left side is blocked. 
                // Wait until the player looks right (+X) before teleporting.
                if (_disableLeftSide)
                {
                    // Vector3.Dot compares two directions. 
                    // A value > 0.5f means the player is looking generally towards the right (within a ~60 degree cone).
                    if (Vector3.Dot(playerCamera.forward, Vector3.right) < 0.5f)
                    {
                        return; // Wait for the player to look right
                    }
                }
            }
            else // Player is on the left side
            {
                // In Loop 2 and 3, the left side is completely blocked
                if (_disableLeftSide) return;
            }

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