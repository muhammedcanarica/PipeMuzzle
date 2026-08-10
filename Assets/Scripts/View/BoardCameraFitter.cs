using UnityEngine;

namespace PipeMuzzle.View
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    public class BoardCameraFitter : MonoBehaviour
    {
        [Header("References")]
        [SerializeField]
        private Camera targetCamera;

        [SerializeField]
        private BoardView boardView;

        [Header("Fit Settings")]
        [SerializeField]
        [Min(0f)]
        private float padding = 0.5f;

        [SerializeField]
        [Min(0.01f)]
        private float minOrthographicSize = 1.5f;

        private int lastPixelWidth = -1;
        private int lastPixelHeight = -1;
        private float lastAspect = -1f;
        private bool hasFitted;

        private void Awake()
        {
            if (targetCamera == null)
            {
                targetCamera = GetComponent<Camera>();
            }
        }

        private void LateUpdate()
        {
            if (!hasFitted || targetCamera == null)
            {
                return;
            }

            if (targetCamera.pixelWidth == lastPixelWidth &&
                targetCamera.pixelHeight == lastPixelHeight &&
                Mathf.Approximately(
                    targetCamera.aspect,
                    lastAspect
                ))
            {
                return;
            }

            Fit();
        }

        public bool Fit()
        {
            if (targetCamera == null)
            {
                targetCamera = GetComponent<Camera>();
            }

            if (targetCamera == null)
            {
                Debug.LogError(
                    $"{nameof(BoardCameraFitter)} requires a Camera reference.",
                    this
                );

                return false;
            }

            if (!targetCamera.orthographic)
            {
                Debug.LogError(
                    $"{nameof(BoardCameraFitter)} requires an orthographic Camera.",
                    this
                );

                return false;
            }

            if (boardView == null)
            {
                Debug.LogError(
                    $"{nameof(BoardCameraFitter)} requires a BoardView reference.",
                    this
                );

                return false;
            }

            if (!boardView.TryGetWorldBounds(out Bounds boardBounds))
            {
                Debug.LogWarning(
                    $"{nameof(BoardCameraFitter)} could not find any visible board renderers.",
                    this
                );

                return false;
            }

            float aspect = targetCamera.aspect;

            if (aspect <= 0f ||
                float.IsNaN(aspect) ||
                float.IsInfinity(aspect))
            {
                Debug.LogError(
                    $"{nameof(BoardCameraFitter)} received an invalid camera aspect ratio.",
                    this
                );

                return false;
            }

            float requiredVerticalSize =
                boardBounds.extents.y + padding;

            float requiredHorizontalSize =
                (boardBounds.extents.x + padding) / aspect;

            targetCamera.orthographicSize = Mathf.Max(
                minOrthographicSize,
                Mathf.Max(
                    requiredVerticalSize,
                    requiredHorizontalSize
                )
            );

            Vector3 cameraPosition = targetCamera.transform.position;

            targetCamera.transform.position = new Vector3(
                boardBounds.center.x,
                boardBounds.center.y,
                cameraPosition.z
            );

            lastPixelWidth = targetCamera.pixelWidth;
            lastPixelHeight = targetCamera.pixelHeight;
            lastAspect = aspect;
            hasFitted = true;

            return true;
        }

        private void OnValidate()
        {
            padding = Mathf.Max(0f, padding);
            minOrthographicSize =
                Mathf.Max(0.01f, minOrthographicSize);
        }
    }
}
