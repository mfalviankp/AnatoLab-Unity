using UnityEngine;
using UnityEngine.EventSystems;

namespace AnatoLab
{
    [RequireComponent(typeof(Camera))]
    public sealed class OrbitCamera : MonoBehaviour
    {
        public Vector3 homeTarget = new Vector3(0, 1, 0);
        public float homeDistance = 3.9f;
        public float minDistance = 0.45f;
        public float maxDistance = 6.5f;
        public float rotationSpeed = 4f;
        public float zoomSpeed = 1.8f;
        Vector3 wantedTarget, currentTarget, velocity;
        float yaw, pitch = 4f, wantedDistance, currentDistance;
        bool dragging;
        public bool IsDragging => dragging;
        void Start() { Initialize(homeTarget, homeDistance); }
        public void Initialize(Vector3 center, float distance)
        {
            homeTarget = center; homeDistance = distance;
            wantedTarget = currentTarget = center;
            wantedDistance = currentDistance = distance;
            yaw = 0; pitch = 4;
            Apply(true);
        }
        public void ResetView() { wantedTarget = homeTarget; wantedDistance = homeDistance; yaw = 0; pitch = 4; }
        public void SetView(float degrees) { wantedTarget = homeTarget; wantedDistance = homeDistance; yaw = degrees; pitch = 4; }
        public void Zoom(float step) { wantedDistance = Mathf.Clamp(wantedDistance * Mathf.Exp(-step * 0.18f), minDistance, maxDistance); }
        public void FocusOn(OrganData organ)
        {
            var b = organ.Bounds;
            wantedTarget = b.center;
            wantedDistance = Mathf.Clamp(b.size.magnitude * 3.0f, 1.5f, homeDistance);
            var direction = organ.viewDirection.normalized;
            yaw = Mathf.Atan2(-direction.x, -direction.z) * Mathf.Rad2Deg;
            pitch = 7;
        }
        void LateUpdate()
        {
            bool overUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
            if (Input.GetMouseButtonDown(1) && !overUI) dragging = true;
            if (Input.GetMouseButtonUp(1) || !Application.isFocused) dragging = false;
            if (dragging)
            {
                yaw += Input.GetAxisRaw("Mouse X") * rotationSpeed;
                pitch = Mathf.Clamp(pitch - Input.GetAxisRaw("Mouse Y") * rotationSpeed, -65, 75);
            }
            if (!overUI) wantedDistance = Mathf.Clamp(wantedDistance * Mathf.Exp(-Input.mouseScrollDelta.y * 0.12f * zoomSpeed), minDistance, maxDistance);
            Apply(false);
        }
        void Apply(bool instant)
        {
            float t = instant ? 1f : 1f - Mathf.Exp(-Time.unscaledDeltaTime * 10f);
            currentTarget = instant ? wantedTarget : Vector3.SmoothDamp(currentTarget, wantedTarget, ref velocity, 0.12f, Mathf.Infinity, Time.unscaledDeltaTime);
            currentDistance = Mathf.Lerp(currentDistance, wantedDistance, t);
            var rotation = Quaternion.Euler(pitch, yaw, 0);
            transform.rotation = Quaternion.Slerp(transform.rotation, rotation, t);
            transform.position = currentTarget + transform.rotation * Vector3.back * currentDistance;
        }
    }
}

// AnatoLab package revision: 1795099051142233200
