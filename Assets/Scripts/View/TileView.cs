using System;
using System.Collections;
using PipeMuzzle.Board;
using PipeMuzzle.Data;
using UnityEngine;
using UnityEngine.EventSystems;

namespace PipeMuzzle.View
{
    public class TileView : MonoBehaviour, IPointerClickHandler
    {
        [Header("References")]
        [SerializeField]
        private SpriteRenderer spriteRenderer;

        [SerializeField]
        private SpriteRenderer glowRenderer;

        [Header("Tile Sprites")]
        [SerializeField]
        private Sprite straightSprite;

        [SerializeField]
        private Sprite cornerSprite;

        [SerializeField]
        private Sprite threeWaySprite;

        [SerializeField]
        private Sprite crossSprite;

        [Header("Visual Colors")]
        [SerializeField]
        private Color normalColor = new Color(0.72f, 0.78f, 0.8f, 1f);

        [SerializeField]
        private Color lockedTint = new Color(0.55f, 0.62f, 0.66f, 1f);

        [SerializeField]
        private Color sourceGlowColor = new Color(1f, 0.48f, 0.08f, 0.48f);

        [SerializeField]
        private Color targetGlowColor = new Color(0.12f, 0.9f, 1f, 0.62f);

        [SerializeField]
        private Color poweredGlowColor = new Color(0.08f, 0.92f, 1f, 0.55f);

        [Header("Animation")]
        [SerializeField]
        [Min(0.01f)]
        private float rotationDuration = 0.14f;

        [SerializeField]
        [Range(0.9f, 1f)]
        private float clickScale = 0.97f;

        [SerializeField]
        [Min(0.01f)]
        private float clickDuration = 0.1f;

        [SerializeField]
        [Min(0f)]
        private float poweredTransitionDuration = 0.14f;

        [SerializeField]
        [Min(0.01f)]
        private float completionPulseDuration = 0.36f;

        private TileState tileState;
        private Coroutine rotationCoroutine;
        private Coroutine powerCoroutine;
        private Coroutine completionCoroutine;
        private int queuedQuarterTurns;
        private float visualRotationDegrees;
        private Sprite sourceMarker;
        private Sprite targetMarker;
        private SpriteRenderer roleRenderer;
        private Sprite targetWater;
        private SpriteRenderer endpointWaterRenderer;
        private EndpointConnectorView endpointConnector;
        private bool targetReached;

        public TileState State => tileState;
        public bool HasPendingRotation => rotationCoroutine != null || queuedQuarterTurns > 0;

        public event Action<TileView> Clicked;

        public void ApplyTheme(WorldGameplayTheme theme)
        {
            if (theme == null) return;

            straightSprite = theme.StraightSprite;
            cornerSprite = theme.CornerSprite;
            threeWaySprite = theme.ThreeWaySprite;
            crossSprite = theme.CrossSprite;
            sourceMarker = theme.SourceMarker;
            targetMarker = theme.TargetMarker;
            targetWater = theme.TargetWater;
            normalColor = theme.NormalTint;
            lockedTint = theme.LockedTint;
            sourceGlowColor = theme.SourceGlowColor;
            targetGlowColor = theme.TargetGlowColor;
            poweredGlowColor = theme.PoweredGlowColor;
        }

        public void Initialize(TileState state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            tileState = state;
            EnsureGlowRenderer();
            EnsureRoleRenderer();
            Refresh();
        }

        public void Refresh()
        {
            if (tileState == null)
            {
                return;
            }

            StopVisualCoroutines();
            targetReached = false;
            RefreshSprite();

            visualRotationDegrees =
                GetVisualRotationOffsetDegrees(tileState.Shape) +
                tileState.Rotation * -90f;
            transform.localRotation = Quaternion.Euler(
                0f,
                0f,
                visualRotationDegrees
            );
            transform.localScale = Vector3.one;

            RefreshBaseColor();
            RefreshRoleMarker();
            RefreshEndpointPresentation();
            SetPowered(tileState.IsPowered, false);
        }

        public void PlayRotationFeedback()
        {
            if (tileState == null || tileState.Shape == TileShape.Empty)
            {
                return;
            }

            queuedQuarterTurns++;

            if (rotationCoroutine == null)
            {
                rotationCoroutine = StartCoroutine(AnimateQueuedRotations());
            }
        }

        public void SetPowered(bool powered, bool animated)
        {
            if (tileState == null || glowRenderer == null)
            {
                return;
            }

            if (completionCoroutine != null)
            {
                StopCoroutine(completionCoroutine);
                completionCoroutine = null;
                glowRenderer.transform.localScale = Vector3.one;
            }

            if (powerCoroutine != null)
            {
                StopCoroutine(powerCoroutine);
                powerCoroutine = null;
            }

            Color targetColor = GetGlowColor(powered);

            if (!animated || poweredTransitionDuration <= 0f)
            {
                glowRenderer.color = targetColor;
                return;
            }

            powerCoroutine = StartCoroutine(
                AnimateGlowColor(glowRenderer.color, targetColor)
            );
        }

        public void PlayCompletionPulse()
        {
            if (tileState == null ||
                !tileState.IsPowered ||
                tileState.Shape == TileShape.Empty ||
                glowRenderer == null)
            {
                return;
            }

            StartGlowPulse(1.06f, completionPulseDuration, 0.38f);
        }

        public void PlayTargetImpact(float pulseScale, float pulseDuration)
        {
            if (tileState == null ||
                tileState.Role != TileRole.Target ||
                glowRenderer == null)
            {
                return;
            }

            targetReached = true;
            if (endpointWaterRenderer != null) endpointWaterRenderer.enabled = endpointWaterRenderer.sprite != null;
            StartGlowPulse(pulseScale, pulseDuration, 0.18f);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData == null ||
                eventData.button != PointerEventData.InputButton.Left)
            {
                return;
            }

            Clicked?.Invoke(this);
        }

        private void EnsureGlowRenderer()
        {
            if (glowRenderer != null || spriteRenderer == null)
            {
                return;
            }

            GameObject glowObject = new GameObject("GlowVisual");
            glowObject.transform.SetParent(transform, false);

            glowRenderer = glowObject.AddComponent<SpriteRenderer>();
            glowRenderer.sharedMaterial = spriteRenderer.sharedMaterial;
            glowRenderer.sortingLayerID = spriteRenderer.sortingLayerID;
            glowRenderer.sortingOrder = spriteRenderer.sortingOrder + 1;
        }

        private void EnsureRoleRenderer()
        {
            if (roleRenderer != null || spriteRenderer == null) return;
            GameObject roleObject = new("RoleVisual");
            roleObject.transform.SetParent(transform, false);
            roleRenderer = roleObject.AddComponent<SpriteRenderer>();
            roleRenderer.sharedMaterial = spriteRenderer.sharedMaterial;
            roleRenderer.sortingLayerID = spriteRenderer.sortingLayerID;
            roleRenderer.sortingOrder = spriteRenderer.sortingOrder + 4;
        }

        private void RefreshRoleMarker()
        {
            if (roleRenderer == null) return;
            roleRenderer.sprite = tileState.Role == TileRole.Source ? sourceMarker
                : tileState.Role == TileRole.Target ? targetMarker : null;
            roleRenderer.enabled = roleRenderer.sprite != null;
            roleRenderer.transform.localRotation = Quaternion.Inverse(transform.localRotation);
            if (glowRenderer != null)
                glowRenderer.transform.localRotation = roleRenderer.enabled
                    ? roleRenderer.transform.localRotation : Quaternion.identity;
        }

        private void RefreshEndpointPresentation()
        {
            bool endpoint = tileState.Role != TileRole.Normal && roleRenderer != null && roleRenderer.enabled;
            if (endpoint && endpointConnector == null)
            {
                GameObject ports = new("EndpointConnector");
                ports.transform.SetParent(transform, false);
                endpointConnector = ports.AddComponent<EndpointConnectorView>();
            }
            endpointConnector?.Configure(spriteRenderer, endpoint);
            if (endpoint) spriteRenderer.enabled = false;
            if (roleRenderer != null)
                roleRenderer.sortingOrder = spriteRenderer.sortingOrder - 1;

            if (tileState.Role == TileRole.Target && targetWater != null && endpointWaterRenderer == null)
            {
                GameObject water = new("EndpointWater");
                water.transform.SetParent(transform, false);
                endpointWaterRenderer = water.AddComponent<SpriteRenderer>();
                endpointWaterRenderer.sharedMaterial = spriteRenderer.sharedMaterial;
                endpointWaterRenderer.sortingLayerID = spriteRenderer.sortingLayerID;
                endpointWaterRenderer.sortingOrder = spriteRenderer.sortingOrder + 3;
            }
            if (endpointWaterRenderer != null)
            {
                endpointWaterRenderer.sprite = tileState.Role == TileRole.Target ? targetWater : null;
                endpointWaterRenderer.enabled = targetReached && endpointWaterRenderer.sprite != null;
                endpointWaterRenderer.transform.localRotation = Quaternion.Inverse(transform.localRotation);
            }
        }

        private void LateUpdate()
        {
            if (roleRenderer != null && roleRenderer.enabled)
                roleRenderer.transform.localRotation = Quaternion.Inverse(transform.localRotation);
            if (endpointWaterRenderer != null)
                endpointWaterRenderer.transform.localRotation = Quaternion.Inverse(transform.localRotation);
        }

        private void RefreshSprite()
        {
            Sprite sprite = tileState.Shape switch
            {
                TileShape.Straight => straightSprite,
                TileShape.Corner => cornerSprite,
                TileShape.ThreeWay => threeWaySprite,
                TileShape.Cross => crossSprite,
                _ => null
            };

            spriteRenderer.sprite = sprite;
            spriteRenderer.enabled = sprite != null;

            if (glowRenderer != null)
            {
                glowRenderer.sprite = tileState.Role == TileRole.Source && sourceMarker != null ? sourceMarker
                    : tileState.Role == TileRole.Target && targetMarker != null ? targetMarker : sprite;
                glowRenderer.enabled = sprite != null;
            }
        }

        private static float GetVisualRotationOffsetDegrees(
            TileShape shape)
        {
            return shape switch
            {
                // Theme artwork preserves these existing base directions:
                // Straight: East + West; Corner: South + West;
                // ThreeWay: North + East + West.
                TileShape.Straight => -90f,
                TileShape.Corner => 180f,
                TileShape.ThreeWay => -90f,
                _ => 0f
            };
        }

        private void RefreshBaseColor()
        {
            spriteRenderer.color = tileState.IsLocked
                ? MultiplyColors(normalColor, lockedTint)
                : normalColor;
        }

        private Color GetGlowColor(bool powered)
        {
            if (tileState.Shape == TileShape.Empty)
            {
                return Color.clear;
            }

            Color color;

            switch (tileState.Role)
            {
                case TileRole.Source:
                    color = sourceGlowColor;
                    color.a *= .12f;
                    break;

                case TileRole.Target:
                    color = targetGlowColor;
                    color.a *= targetReached ? .18f : 0f;
                    break;

                default:
                    color = powered ? poweredGlowColor : Color.clear;
                    // Connection preview highlights the body softly; the channel stays empty.
                    color.a *= .18f;
                    break;
            }

            if (tileState.IsLocked)
            {
                color.a *= 0.82f;
            }

            return color;
        }

        private IEnumerator AnimateQueuedRotations()
        {
            while (queuedQuarterTurns > 0)
            {
                queuedQuarterTurns--;

                float startAngle = visualRotationDegrees;
                float targetAngle = startAngle - 90f;
                float elapsed = 0f;

                while (elapsed < rotationDuration)
                {
                    elapsed += Time.deltaTime;
                    float progress = Mathf.Clamp01(elapsed / rotationDuration);
                    float easedProgress = 1f - Mathf.Pow(1f - progress, 3f);

                    visualRotationDegrees = Mathf.LerpUnclamped(
                        startAngle,
                        targetAngle,
                        easedProgress
                    );
                    transform.localRotation = Quaternion.Euler(
                        0f,
                        0f,
                        visualRotationDegrees
                    );

                    float clickProgress = Mathf.Clamp01(elapsed / clickDuration);
                    float punch = 1f - Mathf.Abs((clickProgress * 2f) - 1f);
                    float scale = Mathf.Lerp(1f, clickScale, punch);
                    transform.localScale = Vector3.one * scale;

                    yield return null;
                }

                visualRotationDegrees = targetAngle;
                transform.localRotation = Quaternion.Euler(
                    0f,
                    0f,
                    visualRotationDegrees
                );
                transform.localScale = Vector3.one;
            }

            rotationCoroutine = null;
        }

        private IEnumerator AnimateGlowColor(Color startColor, Color targetColor)
        {
            float elapsed = 0f;

            while (elapsed < poweredTransitionDuration)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(
                    elapsed / poweredTransitionDuration
                );
                glowRenderer.color = Color.Lerp(
                    startColor,
                    targetColor,
                    progress
                );
                yield return null;
            }

            glowRenderer.color = targetColor;
            powerCoroutine = null;
        }

        private void StartGlowPulse(
            float pulseScale,
            float pulseDuration,
            float brightness)
        {
            if (powerCoroutine != null)
            {
                StopCoroutine(powerCoroutine);
                powerCoroutine = null;
            }

            if (completionCoroutine != null)
            {
                StopCoroutine(completionCoroutine);
            }

            completionCoroutine = StartCoroutine(
                AnimateGlowPulse(pulseScale, pulseDuration, brightness)
            );
        }

        private IEnumerator AnimateGlowPulse(
            float pulseScale,
            float pulseDuration,
            float brightness)
        {
            Color baseColor = GetGlowColor(true);
            Color peakColor = Color.Lerp(baseColor, Color.white, brightness);
            peakColor.a = Mathf.Min(1f, baseColor.a * 1.35f);

            float elapsed = 0f;

            while (elapsed < pulseDuration)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(
                    elapsed / pulseDuration
                );
                float pulse = Mathf.Sin(progress * Mathf.PI);

                glowRenderer.color = Color.Lerp(baseColor, peakColor, pulse);
                glowRenderer.transform.localScale =
                    Vector3.one * Mathf.Lerp(1f, pulseScale, pulse);

                yield return null;
            }

            glowRenderer.color = baseColor;
            glowRenderer.transform.localScale = Vector3.one;
            completionCoroutine = null;
        }

        private void StopVisualCoroutines()
        {
            if (rotationCoroutine != null)
            {
                StopCoroutine(rotationCoroutine);
                rotationCoroutine = null;
            }

            if (powerCoroutine != null)
            {
                StopCoroutine(powerCoroutine);
                powerCoroutine = null;
            }

            if (completionCoroutine != null)
            {
                StopCoroutine(completionCoroutine);
                completionCoroutine = null;
            }

            queuedQuarterTurns = 0;

            if (glowRenderer != null)
            {
                glowRenderer.transform.localScale = Vector3.one;
            }
        }

        private static Color MultiplyColors(Color first, Color second)
        {
            return new Color(
                first.r * second.r,
                first.g * second.g,
                first.b * second.b,
                first.a * second.a
            );
        }

        private void OnDisable() => StopVisualCoroutines();
    }
}
