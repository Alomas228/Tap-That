using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    [Header("Настройки перемещения")]
    [SerializeField] private float moveSpeed = 10f;
    [SerializeField] private float smoothTime = 0.1f;

    [Header("Настройки зума")]
    [SerializeField] private float zoomSpeed = 15f;
    [SerializeField] private float minZoom = 5f;
    [SerializeField] private float maxZoom = 20f;
    [SerializeField] private float zoomSmoothTime = 0.2f;

    [Header("Границы камеры")]
    [SerializeField] private Vector2 minBounds = new(-25, -15);
    [SerializeField] private Vector2 maxBounds = new(25, 15);

    [Header("Настройки ввода")]
    [SerializeField] private float mouseDragSensitivity = 1f;

    // Состояние
    private Vector3 targetPosition;
    private float targetZoom;
    private Vector3 velocity = Vector3.zero;
    private float zoomVelocity = 0f;

    // Для перемещения мышью
    private Vector3 dragOrigin;
    private bool isDragging = false;

    // Ссылки
    private Camera cam;
    private Mouse currentMouse;

    void Start()
    {
        cam = GetComponent<Camera>();
        if (cam == null)
        {
            Debug.LogError("CameraController требует компонент Camera!");
            enabled = false;
            return;
        }

        // Инициализация Input System
#if ENABLE_INPUT_SYSTEM
        currentMouse = Mouse.current;
#endif

        // Инициализация целевых значений
        targetPosition = transform.position;
        targetZoom = cam.orthographic ? cam.orthographicSize : cam.fieldOfView;

        Debug.Log("CameraController инициализирован");
    }

    void Update()
    {
        HandleMouseInput();
        HandleKeyboardInput();
        ApplyMovementAndZoom();
    }

    void LateUpdate()
    {
        // Гарантируем что камера всегда в границах
        ForceCameraInBounds();
    }

    private void HandleMouseInput()
    {
        // ПРИБЛИЖЕНИЕ/ОТДАЛЕНИЕ - Колесико мыши
        float scroll = 0f;

#if ENABLE_INPUT_SYSTEM
        if (currentMouse != null)
        {
            scroll = currentMouse.scroll.ReadValue().y * 0.01f;
        }
#else
        scroll = Input.GetAxis("Mouse ScrollWheel");
#endif

        if (Mathf.Abs(scroll) > 0.001f)
        {
            if (cam.orthographic)
            {
                targetZoom -= scroll * zoomSpeed;
                targetZoom = Mathf.Clamp(targetZoom, minZoom, maxZoom);

                // Немедленно корректируем позицию при изменении зума
                ClampTargetPosition();
            }
            else
            {
                targetZoom -= scroll * zoomSpeed;
                targetZoom = Mathf.Clamp(targetZoom, minZoom, maxZoom);
            }
        }

        // ПЕРЕМЕЩЕНИЕ - Правая кнопка мыши
        bool rightMousePressed = false;

#if ENABLE_INPUT_SYSTEM
        if (currentMouse != null)
        {
            rightMousePressed = currentMouse.rightButton.isPressed;
        }
#else
        rightMousePressed = Input.GetMouseButton(1);
#endif

        if (rightMousePressed)
        {
            if (!isDragging)
            {
                // Начало перетаскивания
                isDragging = true;
                dragOrigin = GetMouseWorldPosition();
            }
            else
            {
                // Продолжение перетаскивания
                Vector3 currentMousePos = GetMouseWorldPosition();
                Vector3 difference = dragOrigin - currentMousePos;

                // Обновляем целевую позицию камеры
                targetPosition = transform.position + difference * mouseDragSensitivity;
                ClampTargetPosition();
            }
        }
        else
        {
            // Кнопка отпущена
            isDragging = false;
        }
    }

    private void HandleKeyboardInput()
    {
        // Дополнительное управление с клавиатуры (опционально)
        float horizontal = 0f;
        float vertical = 0f;

#if ENABLE_INPUT_SYSTEM
        var keyboard = Keyboard.current;
        if (keyboard != null)
        {
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) horizontal -= 1f;
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) horizontal += 1f;
            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) vertical -= 1f;
            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) vertical += 1f;
        }
#else
        horizontal = Input.GetAxis("Horizontal");
        vertical = Input.GetAxis("Vertical");
#endif

        bool hasHorizontalInput = Mathf.Abs(horizontal) > 0.01f;
        bool hasVerticalInput = Mathf.Abs(vertical) > 0.01f;

        if (hasHorizontalInput || hasVerticalInput)
        {
            Vector3 moveDirection = new(horizontal, vertical, 0);
            targetPosition += moveDirection * moveSpeed * Time.deltaTime;
            ClampTargetPosition();
        }
    }

    private void ApplyMovementAndZoom()
    {
        // Плавное перемещение к целевой позиции
        transform.position = Vector3.SmoothDamp(
            transform.position,
            targetPosition,
            ref velocity,
            smoothTime
        );

        // Плавный зум
        if (cam.orthographic)
        {
            float oldSize = cam.orthographicSize;
            cam.orthographicSize = Mathf.SmoothDamp(
                cam.orthographicSize,
                targetZoom,
                ref zoomVelocity,
                zoomSmoothTime
            );

            // Если зум изменился - корректируем границы
            if (Mathf.Abs(cam.orthographicSize - oldSize) > 0.001f)
            {
                ClampTargetPosition();
            }
        }
        else
        {
            float currentZoom = transform.position.z;
            float newZ = Mathf.SmoothDamp(
                currentZoom,
                -targetZoom,
                ref zoomVelocity,
                zoomSmoothTime
            );
            transform.position = new Vector3(
                transform.position.x,
                transform.position.y,
                newZ
            );
        }
    }

    private void ForceCameraInBounds()
    {
        if (cam == null) return;

        // Вычисляем границы для ТЕКУЩЕГО зума
        float cameraHeight = cam.orthographicSize * 2;
        float cameraWidth = cameraHeight * cam.aspect;

        // Границы для центра камеры
        float minX = minBounds.x + cameraWidth / 2;
        float maxX = maxBounds.x - cameraWidth / 2;
        float minY = minBounds.y + cameraHeight / 2;
        float maxY = maxBounds.y - cameraHeight / 2;

        // Если камера больше карты - центрируем
        if (minX > maxX)
        {
            minX = maxX = (minBounds.x + maxBounds.x) / 2;
        }

        if (minY > maxY)
        {
            minY = maxY = (minBounds.y + maxBounds.y) / 2;
        }

        // Принудительно фиксируем позицию камеры
        Vector3 currentPos = transform.position;
        currentPos.x = Mathf.Clamp(currentPos.x, minX, maxX);
        currentPos.y = Mathf.Clamp(currentPos.y, minY, maxY);

        // Обновляем и целевую позицию тоже
        targetPosition = currentPos;
        transform.position = currentPos;
    }

    private void ClampTargetPosition()
    {
        if (cam == null) return;

        // Вычисляем границы для ЦЕЛЕВОГО зума
        float targetCameraHeight = targetZoom * 2;
        float targetCameraWidth = targetCameraHeight * cam.aspect;

        // Границы для центра камеры
        float minX = minBounds.x + targetCameraWidth / 2;
        float maxX = maxBounds.x - targetCameraWidth / 2;
        float minY = minBounds.y + targetCameraHeight / 2;
        float maxY = maxBounds.y - targetCameraHeight / 2;

        // Если камера больше карты - центрируем
        if (minX > maxX)
        {
            minX = maxX = (minBounds.x + maxBounds.x) / 2;
        }

        if (minY > maxY)
        {
            minY = maxY = (minBounds.y + maxBounds.y) / 2;
        }

        targetPosition.x = Mathf.Clamp(targetPosition.x, minX, maxX);
        targetPosition.y = Mathf.Clamp(targetPosition.y, minY, maxY);
    }

    private Vector3 GetMouseWorldPosition()
    {
        Vector3 mousePos = Vector3.zero;

#if ENABLE_INPUT_SYSTEM
        if (currentMouse != null)
        {
            mousePos = currentMouse.position.ReadValue();
        }
#else
        mousePos = Input.mousePosition;
#endif

        mousePos.z = cam.orthographic ?
            cam.transform.position.z :
            Mathf.Abs(cam.transform.position.z);

        return cam.ScreenToWorldPoint(mousePos);
    }

    #region Публичные методы

    public void SetBounds(Vector2 newMinBounds, Vector2 newMaxBounds)
    {
        minBounds = newMinBounds;
        maxBounds = newMaxBounds;
        ClampTargetPosition();
        ForceCameraInBounds();
    }

    public void FocusOnPosition(Vector3 position, float zoomLevel = -1)
    {
        targetPosition = position;
        if (zoomLevel > 0)
        {
            targetZoom = Mathf.Clamp(zoomLevel, minZoom, maxZoom);
        }
        ClampTargetPosition();
    }

    public void ResetCamera()
    {
        targetPosition = Vector3.zero;
        targetZoom = (minZoom + maxZoom) / 2f;
        ClampTargetPosition();
    }

    public bool IsDragging() => isDragging;

    #endregion

    #region Визуализация границ

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;

        Vector3 center = new(
            (minBounds.x + maxBounds.x) / 2,
            (minBounds.y + maxBounds.y) / 2,
            0
        );

        Vector3 size = new(
            maxBounds.x - minBounds.x,
            maxBounds.y - minBounds.y,
            0.1f
        );

        Gizmos.DrawWireCube(center, size);
    }

    #endregion
}