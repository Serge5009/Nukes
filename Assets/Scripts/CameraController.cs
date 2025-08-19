using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{

    [Header("Rotation")]
    [SerializeField] private float sensitivity = 0.25f;
    private Vector2 lookInput;
    private bool isDragging = false;

    [Header("Zoom")]
    [SerializeField] private float zoomSensitivity = 0.05f;
    [SerializeField] private float zoomLerpSpeed = 10.0f;
    [SerializeField] private float minZoom = 2.0f;
    [SerializeField] private float maxZoom = 10.0f;
    private float currentZoom;
    private Transform cameraTransform; // The actual camera object, a child of this pivot

    void Awake()
    {
        if (transform.childCount > 0)
        {
            cameraTransform = transform.GetChild(0);
        }
        else
        {
            Debug.LogError("This script requires a Camera as a child object.", this);
        }

        // Set initial zoom based on the camera's starting position
        currentZoom = -cameraTransform.localPosition.z;
    }

    void Update()
    {
        // Apply rotation only when the user is dragging
        if (isDragging)
        {
            // Rotate the pivot object. The camera, as a child, will orbit.
            // Rotate around the world UP axis for horizontal movement (yaw)
            transform.Rotate(Vector3.up, lookInput.x * sensitivity, Space.World);

            // Rotate around our local RIGHT axis for vertical movement (pitch)
            transform.Rotate(Vector3.right, -lookInput.y * sensitivity, Space.Self);
        }
    }

    private void LateUpdate()
    {
        // Smoothly adjust the camera's local position for zooming
        Vector3 targetLocalPosition = new Vector3(0, 0, -currentZoom);
        cameraTransform.localPosition = Vector3.Lerp(cameraTransform.localPosition, targetLocalPosition, Time.deltaTime * zoomLerpSpeed);
    }

    // This method is called by the "Press" Action
    public void OnPress(InputAction.CallbackContext context)
    {
        isDragging = context.ReadValueAsButton();
    }

    // This method is called by the "Look" Action
    public void OnLook(InputAction.CallbackContext context)
    {
        lookInput = context.ReadValue<Vector2>();
    }

    // This method is called by the "Zoom" Action
    public void OnZoom(InputAction.CallbackContext context)
    {
        float scrollValue = context.ReadValue<Vector2>().y;

        // Adjust zoom based on scroll direction
        if (scrollValue > 0)
        {
            currentZoom -= zoomSensitivity;
        }
        else if (scrollValue < 0)
        {
            currentZoom += zoomSensitivity;
        }

        // Clamp the zoom level to prevent zooming too far in or out
        currentZoom = Mathf.Clamp(currentZoom, minZoom, maxZoom);
    }
}
