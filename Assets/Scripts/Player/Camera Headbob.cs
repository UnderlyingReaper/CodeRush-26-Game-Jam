using UnityEngine;

public class CameraHeadbob : MonoBehaviour
{
    [Header("Bob Settings")]
    public float bobFrequency = 2.0f;      // How fast the bob cycles
    public float bobAmplitudeY = 0.06f;    // Up/down strength
    public float bobAmplitudeX = 0.03f;    // Side-to-side strength (set 0 to disable)

    [Header("Smoothing")]
    public float bobSmoothSpeed = 10f;     // How fast it returns to rest

    [Header("References")]
    public CharacterController cc;         // Drag your player's CC here

    private float _timer = 0f;
    private Vector3 _restPosition;
    private Vector3 _targetPosition;

    private void Start()
    {
        _restPosition = transform.localPosition;
    }

    private void Update()
    {
        bool isMoving = cc != null
            && cc.isGrounded
            && cc.velocity.magnitude > 0.1f;

        if (isMoving)
        {
            _timer += Time.deltaTime * bobFrequency;

            float bobY = Mathf.Sin(_timer) * bobAmplitudeY;
            float bobX = Mathf.Cos(_timer * 0.5f) * bobAmplitudeX;

            _targetPosition = _restPosition + new Vector3(bobX, bobY, 0f);
        }
        else
        {
            // Smoothly return to rest when not moving
            _timer = 0f;
            _targetPosition = _restPosition;
        }

        transform.localPosition = Vector3.Lerp(
            transform.localPosition,
            _targetPosition,
            Time.deltaTime * bobSmoothSpeed
        );
    }
}