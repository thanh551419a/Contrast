using UnityEngine;

namespace Contrast.Camera
{
    /// <summary>
    /// Camera đi theo Player (tâm camera = vị trí player), bị giới hạn trong một
    /// hình chữ nhật boundary đọc từ JSON. Chạm biên thì camera dừng, player vẫn đi tiếp.
    /// </summary>
    [RequireComponent(typeof(UnityEngine.Camera))]
    public class CameraFollow : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private bool hasBoundary = false;
        [SerializeField] private Vector2 boundaryCenter = Vector2.zero;
        [SerializeField] private Vector2 boundarySize = new Vector2(40f, 20f);

        private UnityEngine.Camera cam;

        public Transform Target
        {
            get => target;
            set => target = value;
        }

        public bool HasBoundary => hasBoundary;
        public Vector2 BoundaryCenter => boundaryCenter;
        public Vector2 BoundarySize => boundarySize;

        private void Awake()
        {
            cam = GetComponent<UnityEngine.Camera>();
        }

        public void SetBoundary(Vector2 center, Vector2 size)
        {
            boundaryCenter = center;
            boundarySize = size;
            hasBoundary = true;
        }

        public void ClearBoundary()
        {
            hasBoundary = false;
        }

        public void ProcessCameraUpdate()
        {
            if (target == null) return;
            if (cam == null) cam = GetComponent<UnityEngine.Camera>();

            Vector3 targetPos = target.position;
            float desiredX = targetPos.x;
            float desiredY = targetPos.y;

            if (hasBoundary && cam != null)
            {
                float vertExtent = cam.orthographic
                    ? cam.orthographicSize
                    : Mathf.Abs(transform.position.z - targetPos.z) * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
                float horzExtent = vertExtent * cam.aspect;

                float minX = boundaryCenter.x - boundarySize.x * 0.5f;
                float maxX = boundaryCenter.x + boundarySize.x * 0.5f;
                float minY = boundaryCenter.y - boundarySize.y * 0.5f;
                float maxY = boundaryCenter.y + boundarySize.y * 0.5f;

                // Boundary nhỏ hơn màn hình thì giữ camera ở giữa boundary
                desiredX = boundarySize.x >= horzExtent * 2f
                    ? Mathf.Clamp(desiredX, minX + horzExtent, maxX - horzExtent)
                    : boundaryCenter.x;

                desiredY = boundarySize.y >= vertExtent * 2f
                    ? Mathf.Clamp(desiredY, minY + vertExtent, maxY - vertExtent)
                    : boundaryCenter.y;
            }

            transform.position = new Vector3(desiredX, desiredY, transform.position.z);
        }

        private void OnDrawGizmosSelected()
        {
            if (!hasBoundary) return;
            Gizmos.color = UnityEngine.Color.cyan;
            Gizmos.DrawWireCube(boundaryCenter, boundarySize);
        }
    }
}
