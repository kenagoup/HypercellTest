using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Camera))]
public class CameraController : MonoBehaviour
{
    [Header("Zoom")]
    [SerializeField, Range(0.1f, 0.9f)] private float minSizeRatio = 0.4f;
    [SerializeField, Min(0f)] private float zoomSpeed = 0.005f;
    [SerializeField, Min(0f)] private float zoomSmoothTime = 0.1f;

    [Header("Pan")]
    [SerializeField, Min(0f)] private float panSmoothTime = 0.08f;
    [SerializeField, Min(0f)] private float panRadiusAtMaxSize = 4f;
    [SerializeField, Min(0f)] private float panRadiusAtMinSize = 18f;

    private Camera cam;
    private Vector3 startPosition;
    private Vector3 targetPosition;
    private Vector3 positionVelocity;
    private Vector3 groundRight;
    private Vector3 groundForward;
    private float maxSize;
    private float minSize;
    private float targetSize;
    private float sizeVelocity;
    private float forwardCompensation;

    private void Awake()
    {
        cam = GetComponent<Camera>();
        maxSize = cam.orthographicSize;
        minSize = maxSize * minSizeRatio;
        targetSize = maxSize;
        startPosition = transform.position;
        targetPosition = startPosition;

        groundRight = Vector3.ProjectOnPlane(transform.right, Vector3.up).normalized;
        groundForward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;

        float pitch = Mathf.Abs(Vector3.Dot(transform.forward, Vector3.down));
        forwardCompensation = 1f / Mathf.Max(pitch, 0.1f);
    }

    private void Update()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null) return;

        ApplyZoom(mouse);
        ApplyPan(mouse);
        ApplySmoothing();
    }

    private void ApplyZoom(Mouse mouse)
    {
        float scroll = mouse.scroll.ReadValue().y;
        if (Mathf.Approximately(scroll, 0f)) return;

        targetSize = Mathf.Clamp(targetSize - scroll * zoomSpeed, minSize, maxSize);
    }

    private void ApplyPan(Mouse mouse)
    {
        if (mouse.middleButton.isPressed)
        {
            Vector2 delta = mouse.delta.ReadValue();
            float worldPerPixel = cam.orthographicSize * 2f / Screen.height;
            Vector3 movement = groundRight * delta.x + groundForward * (delta.y * forwardCompensation);

            targetPosition -= movement * worldPerPixel;
        }

        targetPosition = ClampToRadius(targetPosition);
    }

    private Vector3 ClampToRadius(Vector3 position)
    {
        float zoomFactor = Mathf.InverseLerp(maxSize, minSize, targetSize);
        float radius = Mathf.Lerp(panRadiusAtMaxSize, panRadiusAtMinSize, zoomFactor);

        Vector3 offset = position - startPosition;
        Vector3 planarOffset = Vector3.ProjectOnPlane(offset, Vector3.up);
        Vector3 clamped = Vector3.ClampMagnitude(planarOffset, radius);

        return startPosition + clamped + Vector3.Project(offset, Vector3.up);
    }

    private void ApplySmoothing()
    {
        cam.orthographicSize = Mathf.SmoothDamp(cam.orthographicSize, targetSize, ref sizeVelocity, zoomSmoothTime);
        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref positionVelocity, panSmoothTime);
    }
}