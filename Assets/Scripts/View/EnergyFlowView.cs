using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PipeMuzzle.View
{
    [DisallowMultipleComponent]
    public class EnergyFlowView : MonoBehaviour
    {
        [Header("Projectile")]
        [SerializeField]
        [Min(0.01f)]
        private float travelSpeed = 9f;

        [SerializeField]
        [Min(0.001f)]
        private float trailWidth = 0.08f;

        [SerializeField]
        [Min(0.01f)]
        private float trailDuration = 0.07f;

        [SerializeField]
        private Color projectileColor =
            new Color(0.12f, 1f, 0.95f, 0.95f);

        [Header("Target Impact")]
        [SerializeField]
        [Range(1f, 1.3f)]
        private float targetPulseScale = 1.12f;

        [SerializeField]
        [Min(0.01f)]
        private float targetPulseDuration = 0.22f;

        private readonly List<Vector3> pathPositions = new();
        private readonly List<float> cumulativeDistances = new();

        private LineRenderer tracerRenderer;
        private Material runtimeMaterial;
        private Coroutine flowCoroutine;
        private TileView targetTile;
        private float totalDistance;

        public bool IsPlaying => flowCoroutine != null;

        private void Awake()
        {
            EnsureTracerRenderer();
        }

        public bool Play(
            IReadOnlyList<Vector3> worldPath,
            TileView target)
        {
            StopAndClear();

            if (worldPath == null || worldPath.Count < 2 || target == null)
            {
                return false;
            }

            if (!EnsureTracerRenderer())
            {
                return false;
            }

            pathPositions.Clear();
            cumulativeDistances.Clear();
            cumulativeDistances.Add(0f);
            totalDistance = 0f;

            for (int i = 0; i < worldPath.Count; i++)
            {
                Vector3 position = worldPath[i];
                position.z = transform.position.z;
                pathPositions.Add(position);

                if (i == 0)
                {
                    continue;
                }

                totalDistance += Vector3.Distance(
                    pathPositions[i - 1],
                    position
                );
                cumulativeDistances.Add(totalDistance);
            }

            if (totalDistance <= Mathf.Epsilon)
            {
                StopAndClear();
                return false;
            }

            targetTile = target;
            tracerRenderer.enabled = true;
            flowCoroutine = StartCoroutine(AnimateFlow());
            return true;
        }

        public void StopAndClear()
        {
            if (flowCoroutine != null)
            {
                StopCoroutine(flowCoroutine);
                flowCoroutine = null;
            }

            targetTile = null;
            pathPositions.Clear();
            cumulativeDistances.Clear();
            totalDistance = 0f;

            if (tracerRenderer != null)
            {
                tracerRenderer.positionCount = 0;
                tracerRenderer.enabled = false;
            }
        }

        private IEnumerator AnimateFlow()
        {
            float headDistance = 0f;
            float trailDistance = travelSpeed * trailDuration;

            while (headDistance < totalDistance)
            {
                headDistance = Mathf.Min(
                    totalDistance,
                    headDistance + travelSpeed * Time.deltaTime
                );

                DrawTracer(
                    Mathf.Max(0f, headDistance - trailDistance),
                    headDistance
                );

                yield return null;
            }

            targetTile.PlayTargetImpact(
                targetPulseScale,
                targetPulseDuration
            );

            float retractElapsed = 0f;
            float initialTailDistance =
                Mathf.Max(0f, totalDistance - trailDistance);

            while (retractElapsed < trailDuration)
            {
                retractElapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(
                    retractElapsed / trailDuration
                );
                float tailDistanceAtFrame = Mathf.Lerp(
                    initialTailDistance,
                    totalDistance,
                    progress
                );

                DrawTracer(tailDistanceAtFrame, totalDistance);
                yield return null;
            }

            tracerRenderer.positionCount = 0;
            tracerRenderer.enabled = false;
            targetTile = null;
            flowCoroutine = null;
        }

        private void DrawTracer(float tailDistance, float headDistance)
        {
            Vector3 tailPosition = GetPositionAtDistance(
                tailDistance,
                out int tailSegment
            );
            Vector3 headPosition = GetPositionAtDistance(
                headDistance,
                out int headSegment
            );

            int intermediateCount = Mathf.Max(
                0,
                headSegment - tailSegment
            );
            tracerRenderer.positionCount = intermediateCount + 2;
            tracerRenderer.SetPosition(0, tailPosition);

            int writeIndex = 1;

            for (int vertexIndex = tailSegment + 1;
                 vertexIndex <= headSegment;
                 vertexIndex++)
            {
                tracerRenderer.SetPosition(
                    writeIndex,
                    pathPositions[vertexIndex]
                );
                writeIndex++;
            }

            tracerRenderer.SetPosition(writeIndex, headPosition);
        }

        private Vector3 GetPositionAtDistance(
            float distance,
            out int segmentIndex)
        {
            distance = Mathf.Clamp(distance, 0f, totalDistance);

            for (int i = 0; i < pathPositions.Count - 1; i++)
            {
                float segmentEnd = cumulativeDistances[i + 1];

                if (distance > segmentEnd && i < pathPositions.Count - 2)
                {
                    continue;
                }

                segmentIndex = i;
                float segmentStart = cumulativeDistances[i];
                float segmentLength = segmentEnd - segmentStart;
                float progress = segmentLength <= Mathf.Epsilon
                    ? 1f
                    : (distance - segmentStart) / segmentLength;

                return Vector3.Lerp(
                    pathPositions[i],
                    pathPositions[i + 1],
                    progress
                );
            }

            segmentIndex = pathPositions.Count - 2;
            return pathPositions[pathPositions.Count - 1];
        }

        private bool EnsureTracerRenderer()
        {
            if (tracerRenderer != null)
            {
                return true;
            }

            tracerRenderer = GetComponent<LineRenderer>();

            if (tracerRenderer == null)
            {
                tracerRenderer = gameObject.AddComponent<LineRenderer>();
            }

            Shader shader = Shader.Find("Sprites/Default");

            if (shader == null)
            {
                Debug.LogWarning(
                    "EnergyFlowView could not find the Sprites/Default shader.",
                    this
                );
                tracerRenderer.enabled = false;
                return false;
            }

            runtimeMaterial = new Material(shader)
            {
                name = "Energy Flow Runtime Material",
                hideFlags = HideFlags.HideAndDontSave
            };

            tracerRenderer.sharedMaterial = runtimeMaterial;
            tracerRenderer.useWorldSpace = true;
            tracerRenderer.loop = false;
            tracerRenderer.alignment = LineAlignment.View;
            tracerRenderer.textureMode = LineTextureMode.Stretch;
            tracerRenderer.numCapVertices = 4;
            tracerRenderer.numCornerVertices = 3;
            tracerRenderer.startWidth = trailWidth;
            tracerRenderer.endWidth = trailWidth * 0.45f;
            tracerRenderer.startColor = new Color(
                projectileColor.r,
                projectileColor.g,
                projectileColor.b,
                projectileColor.a * 0.18f
            );
            tracerRenderer.endColor = projectileColor;
            tracerRenderer.sortingOrder = 3;
            tracerRenderer.positionCount = 0;
            tracerRenderer.enabled = false;
            return true;
        }

        private void OnDisable()
        {
            StopAndClear();
        }

        private void OnDestroy()
        {
            if (runtimeMaterial != null)
            {
                Destroy(runtimeMaterial);
            }
        }
    }
}
