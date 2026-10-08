using System;
using System.Collections.Generic;
using PipeMuzzle.Data;
using PipeMuzzle.Feedback;
using UnityEngine;

namespace PipeMuzzle.View
{
    [DisallowMultipleComponent]
    public class EnergyFlowView : MonoBehaviour
    {
        [Header("Channel Flow")]
        [SerializeField, Min(.01f)] private float travelSpeed = 9f;
        // Separate from the legacy thin projectile trail serialized in the scene.
        [SerializeField, Min(.001f)] private float channelWidth = .19f;
        [SerializeField] private Color projectileColor = new(.18f, .8f, .94f, 1f);
        [Header("Target Arrival")]
        [SerializeField, Range(1f, 1.3f)] private float targetPulseScale = 1.06f;
        [SerializeField, Min(.01f)] private float targetPulseDuration = .22f;
        [SerializeField, Min(0f)] private float arrivalHold = .55f;

        private readonly List<Vector3> pathPositions = new();
        private readonly List<float> cumulativeDistances = new();
        private LineRenderer fillRenderer;
        private LineRenderer headRenderer;
        private Material runtimeMaterial;
        private TileView targetTile;
        private Action completed;
        private Action targetReachedCallback;
        private Action<int> tileReachedCallback;
        private int nextTileIndex;
        private float totalDistance;
        private float elapsed;
        private float travelDuration;
        private bool playing;
        private bool arrived;

        public bool IsPlaying => playing;
        public float Duration => travelDuration + arrivalHold;

        public void Configure(WorldGameplayTheme theme)
        {
            if (theme != null) projectileColor = theme.FlowColor;
            if (fillRenderer != null) ApplyColor();
        }

        public bool Play(IReadOnlyList<Vector3> worldPath, TileView target, Action onCompleted = null,
            string worldId = null, Action onTargetReached = null, Action<int> onTileReached = null)
        {
            StopAndClear();
            if (!isActiveAndEnabled || worldPath == null || worldPath.Count < 2 || target == null)
                return false;
            if (!EnsureRenderers()) return false;
            cumulativeDistances.Add(0f);
            for (int i = 0; i < worldPath.Count; i++)
            {
                Vector3 position = worldPath[i];
                position.z = transform.position.z;
                pathPositions.Add(position);
                if (i == 0) continue;
                totalDistance += Vector3.Distance(pathPositions[i - 1], position);
                cumulativeDistances.Add(totalDistance);
            }
            if (totalDistance <= Mathf.Epsilon) { StopAndClear(); return false; }
            targetTile = target;
            completed = onCompleted;
            targetReachedCallback = onTargetReached;
            tileReachedCallback = onTileReached;
            travelDuration = Mathf.Clamp(totalDistance / travelSpeed, .5f, 1.05f);
            playing = true;
            fillRenderer.enabled = true;
            headRenderer.enabled = true;
            ApplyColor();
            DrawSection(fillRenderer, 0f, 0f);
            GameFeedback.StartFlow(this, worldId, travelDuration);
            VisitPassedTiles(0f);
            return true;
        }

        private void Update() => Advance(Time.deltaTime);

        // Shared animation clock also lets EditMode tests verify ordering without wall-clock waits.
        public void Advance(float deltaTime)
        {
            if (!playing || deltaTime <= 0f || float.IsNaN(deltaTime)) return;
            elapsed += deltaTime;
            float distance = totalDistance * Mathf.Clamp01(elapsed / travelDuration);
            DrawSection(fillRenderer, 0f, distance);
            DrawSection(headRenderer, Mathf.Max(0f, distance - .18f), distance);
            VisitPassedTiles(distance);
            if (!playing) return;
            if (!arrived && elapsed >= travelDuration)
            {
                arrived = true;
                headRenderer.enabled = false;
                if (targetTile != null && Application.isPlaying)
                    targetTile.PlayTargetImpact(targetPulseScale, targetPulseDuration);
                GameFeedback.PlayTargetReached(this);
                Action arrivalCallback = targetReachedCallback;
                targetReachedCallback = null;
                // Start the hold at visible arrival, even when a slow frame overshoots both thresholds.
                elapsed = travelDuration;
                arrivalCallback?.Invoke();
                return;
            }
            if (elapsed < Duration) return;
            playing = false;
            targetTile = null;
            Action callback = completed;
            completed = null;
            GameFeedback.StopFlow(this);
            // The filled channel stays visible until restart, next level or screen navigation.
            callback?.Invoke();
        }

        private void VisitPassedTiles(float distance)
        {
            while (playing && nextTileIndex < cumulativeDistances.Count && cumulativeDistances[nextTileIndex] <= distance)
            {
                int index = nextTileIndex++;
                tileReachedCallback?.Invoke(index);
            }
        }

        public void StopAndClear()
        {
            GameFeedback.StopFlow(this);
            playing = false;
            arrived = false;
            completed = null;
            targetReachedCallback = null;
            tileReachedCallback = null;
            nextTileIndex = 0;
            targetTile = null;
            elapsed = totalDistance = travelDuration = 0f;
            pathPositions.Clear();
            cumulativeDistances.Clear();
            ClearRenderer(fillRenderer);
            ClearRenderer(headRenderer);
        }

        private static void ClearRenderer(LineRenderer renderer)
        {
            if (renderer == null) return;
            renderer.enabled = false;
            renderer.positionCount = 0;
        }

        private void DrawSection(LineRenderer renderer, float tail, float head)
        {
            Vector3 start = PositionAt(tail, out int startSegment);
            Vector3 end = PositionAt(head, out int endSegment);
            renderer.positionCount = endSegment - startSegment + 2;
            renderer.SetPosition(0, start);
            int index = 1;
            for (int vertex = startSegment + 1; vertex <= endSegment; vertex++)
                renderer.SetPosition(index++, pathPositions[vertex]);
            renderer.SetPosition(index, end);
        }

        private Vector3 PositionAt(float distance, out int segment)
        {
            distance = Mathf.Clamp(distance, 0f, totalDistance);
            for (int i = 0; i < pathPositions.Count - 1; i++)
            {
                float end = cumulativeDistances[i + 1];
                if (distance > end && i < pathPositions.Count - 2) continue;
                segment = i;
                float start = cumulativeDistances[i];
                return Vector3.Lerp(pathPositions[i], pathPositions[i + 1],
                    end > start ? (distance - start) / (end - start) : 1f);
            }
            segment = pathPositions.Count - 2;
            return pathPositions[pathPositions.Count - 1];
        }

        private bool EnsureRenderers()
        {
            if (runtimeMaterial == null)
            {
                Shader shader = Shader.Find("Sprites/Default");
                if (shader == null) return false;
                runtimeMaterial = new Material(shader)
                {
                    name = "Pipe Channel Flow", hideFlags = HideFlags.HideAndDontSave
                };
            }
            if (fillRenderer == null)
            {
                fillRenderer = GetComponent<LineRenderer>();
                if (fillRenderer == null) fillRenderer = gameObject.AddComponent<LineRenderer>();
                ConfigureRenderer(fillRenderer, channelWidth, 2);
            }
            if (headRenderer == null)
            {
                GameObject head = new("FlowHead");
                head.transform.SetParent(transform, false);
                headRenderer = head.AddComponent<LineRenderer>();
                ConfigureRenderer(headRenderer, channelWidth * .7f, 3);
            }
            return true;
        }

        private void ConfigureRenderer(LineRenderer renderer, float width, int order)
        {
            renderer.sharedMaterial = runtimeMaterial;
            renderer.useWorldSpace = true;
            renderer.loop = false;
            renderer.alignment = LineAlignment.View;
            renderer.numCapVertices = 8;
            renderer.numCornerVertices = 8;
            renderer.startWidth = renderer.endWidth = width;
            renderer.sortingOrder = order;
            renderer.positionCount = 0;
            renderer.enabled = false;
        }

        private void ApplyColor()
        {
            Color water = projectileColor;
            water.a = 1f;
            fillRenderer.startColor = fillRenderer.endColor = water;
            Color head = Color.Lerp(water, Color.white, .65f);
            headRenderer.startColor = water;
            headRenderer.endColor = head;
        }

        private void OnDisable() => StopAndClear();
        private void OnDestroy()
        {
            StopAndClear();
            if (runtimeMaterial == null) return;
            if (Application.isPlaying) Destroy(runtimeMaterial);
            else DestroyImmediate(runtimeMaterial);
        }
    }
}
