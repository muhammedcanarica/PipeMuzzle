using PipeMuzzle.Gameplay;
using PipeMuzzle.View;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
#endif

namespace PipeMuzzle.UI
{
    public enum RuilayCursorState { Default, Hover, Rotate, Pressed }

    /// <summary>Uses existing pointer events; never casts an additional UI or physics ray.</summary>
    [DisallowMultipleComponent]
    public sealed class CursorManager : MonoBehaviour
    {
        [SerializeField] private Texture2D defaultCursor;
        [SerializeField] private Vector2 defaultHotspot;
        [SerializeField] private Texture2D hoverCursor;
        [SerializeField] private Vector2 hoverHotspot;
        [SerializeField] private Texture2D rotateCursor;
        [SerializeField] private Vector2 rotateHotspot;
        [SerializeField] private Texture2D pressedCursor;
        [SerializeField] private Vector2 pressedHotspot;

        private const float PressDuration = .09f;
        private static CursorManager instance;
        private Button hoveredButton;
        private TileView hoveredTile;
        private PointerEventData mousePointer;
        private GameController controller;
        private float pressedUntil;
        private bool focused = true;
        private bool presentationApplied;
        private Texture2D appliedTexture;
        private Vector2 appliedHotspot;
        private bool refreshPointer;
        private int resetFrame;

        public RuilayCursorState CurrentState { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => instance = null;

        public static CursorManager EnsureInstance()
        {
            if (instance != null || !Application.isPlaying) return instance;
            var prefab = Resources.Load<GameObject>("UI/CursorManager");
            if (prefab != null) Instantiate(prefab);
            else new GameObject("CursorManager").AddComponent<CursorManager>();
            return instance;
        }

        private void Awake()
        {
            if (!Application.isPlaying) return;
            if (instance != null && instance != this) { Destroy(gameObject); return; }
            instance = this;
            DontDestroyOnLoad(gameObject);
            controller = FindFirstObjectByType<GameController>();
            RefreshState(Time.unscaledTime);
            RefreshPointerTarget();
        }

        private void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (instance != this) return;
            controller = FindFirstObjectByType<GameController>();
            mousePointer = null;
            ResetState();
        }

        public static bool IsMouseEvent(PointerEventData data)
        {
            if (data == null) return false;
#if ENABLE_INPUT_SYSTEM
            if (data is ExtendedPointerEventData extended)
                return extended.pointerType == UIPointerType.MouseOrPen && extended.device is Mouse;
#endif
            return data.pointerId == -1 || data.pointerId == -2 || data.pointerId == -3;
        }

        public static void EnterButton(Button button, PointerEventData data)
        {
            if (instance == null || !instance.isActiveAndEnabled || !IsMouseEvent(data)) return;
            instance.hoveredButton = button;
            instance.hoveredTile = null;
            instance.mousePointer = data;
        }

        public static void EnterTile(TileView tile, PointerEventData data)
        {
            if (instance == null || !instance.isActiveAndEnabled || !IsMouseEvent(data)) return;
            instance.hoveredButton = null;
            instance.hoveredTile = tile;
            instance.mousePointer = data;
            if (instance.controller == null)
                instance.controller = FindFirstObjectByType<GameController>();
        }

        public static void ExitTarget(Object target, PointerEventData data)
        {
            if (IsMouseEvent(data)) ReleaseTarget(target);
        }

        public static void ReleaseTarget(Object target)
        {
            if (instance == null) return;
            if (instance.hoveredButton == target) instance.hoveredButton = null;
            if (instance.hoveredTile == target) instance.hoveredTile = null;
        }

        public static void Press(PointerEventData data)
        {
            if (instance != null && instance.isActiveAndEnabled && IsMouseEvent(data) &&
                data.button == PointerEventData.InputButton.Left)
                instance.BeginPress(Time.unscaledTime);
        }

        public static void ReleasePress(PointerEventData data)
        {
            if (instance == null || !IsMouseEvent(data) || data.button != PointerEventData.InputButton.Left) return;
            // Keep a short tap visible, even when down/up occur in the same frame.
            instance.RefreshState(Time.unscaledTime);
        }

        private void BeginPress(float now)
        {
            if (focused) pressedUntil = now + PressDuration;
        }

        public static void ResetState()
        {
            if (instance == null) return;
            instance.hoveredButton = null;
            instance.hoveredTile = null;
            instance.pressedUntil = 0;
            instance.RefreshState(Time.unscaledTime);
            RefreshPointerTarget();
        }

        public static void RefreshPointerTarget()
        {
            if (instance == null) return;
            instance.refreshPointer = true;
            instance.resetFrame = Time.frameCount;
        }

        private void LateUpdate()
        {
            if (instance != this || !focused) return;
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                BeginPress(Time.unscaledTime);
#elif ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetMouseButtonDown(0)) BeginPress(Time.unscaledTime);
#endif
            if (refreshPointer && Time.frameCount > resetFrame) RecoverPointerTarget();
            RefreshState(Time.unscaledTime);
        }

        private void RecoverPointerTarget()
        {
            refreshPointer = false;
            // The module reuses mouse/pen event data. Validate its current device before
            // reusing its latest raycast, rather than assuming a mouse ID still owns it.
            if (!IsMouseEvent(mousePointer)) return;
            GameObject hit = mousePointer.pointerCurrentRaycast.gameObject;
            if (hit == null || !hit.activeInHierarchy) return;
            hoveredButton = hit.GetComponentInParent<Button>();
            hoveredTile = hoveredButton == null ? hit.GetComponentInParent<TileView>() : null;
        }

        private void RefreshState(float now)
        {
            RuilayCursorState next = RuilayCursorState.Default;
            if (focused && (mousePointer == null || IsMouseEvent(mousePointer)))
            {
                if (now < pressedUntil) next = RuilayCursorState.Pressed;
                else if (hoveredButton != null && hoveredButton.isActiveAndEnabled && hoveredButton.IsInteractable())
                    next = RuilayCursorState.Hover;
                else if (hoveredTile != null && hoveredTile.isActiveAndEnabled && controller != null &&
                    controller.CanInteractWithTile(hoveredTile)) next = RuilayCursorState.Rotate;
            }
            CurrentState = next;
            Texture2D texture = defaultCursor;
            Vector2 hotspot = defaultHotspot;
            switch (next)
            {
                case RuilayCursorState.Hover: texture = hoverCursor; hotspot = hoverHotspot; break;
                case RuilayCursorState.Rotate: texture = rotateCursor; hotspot = rotateHotspot; break;
                case RuilayCursorState.Pressed: texture = pressedCursor; hotspot = pressedHotspot; break;
            }
            if (texture == null) { texture = defaultCursor; hotspot = defaultHotspot; }
            if (defaultCursor == null || !focused || !isActiveAndEnabled) texture = null;
            if (texture == null) hotspot = Vector2.zero;
            else hotspot = new Vector2(Mathf.Clamp(hotspot.x, 0, texture.width - 1),
                Mathf.Clamp(hotspot.y, 0, texture.height - 1));
            Apply(texture, hotspot);
        }

        private void Apply(Texture2D texture, Vector2 hotspot)
        {
            if (presentationApplied && texture == appliedTexture && hotspot == appliedHotspot) return;
            appliedTexture = texture;
            appliedHotspot = hotspot;
            presentationApplied = true;
            if (Application.isPlaying) Cursor.SetCursor(texture, hotspot, CursorMode.Auto);
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            focused = hasFocus;
            presentationApplied = false;
            ResetState();
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            if (instance != this) return;
            ResetState();
            Apply(null, Vector2.zero);
        }

        private void OnDestroy()
        {
            if (instance != this) return;
            Apply(null, Vector2.zero);
            instance = null;
        }
    }
}
