using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;

public class GridManager : MonoBehaviour
{
    public static GridManager Instance { get; private set; }

    [Header("Ссылки")]
    [SerializeField] private Grid grid;
    [SerializeField] private Tilemap groundTilemap;
    [SerializeField] private Tilemap collisionTilemap;

    [Header("Настройки сетки")]
    [SerializeField] private Vector2Int gridSize = new(100, 100);
    [SerializeField] private bool showGridGizmos = true;

    [Header("Визуализация сетки")]
    [SerializeField] private GameObject gridCellPrefab;
    [SerializeField] private Color validColor = new(0, 1, 0, 0.3f);
    [SerializeField] private Color invalidColor = new(1, 0, 0, 0.3f);

    [Header("Ресурсы на карте")]
    [SerializeField] private bool autoFindResourcesOnStart = true;

    // Состояние
    private bool[,] occupiedCells;
    private GameObject[,] gridVisualization;

    // Текущее состояние строительства
    private bool isBuildingMode = false;
    private BuildingData currentBuildingData;
    private GameObject buildingPreview;
    private Vector3Int lastHoveredCell = Vector3Int.one * -1000;

    // Input System
    private Mouse currentMouse;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        if (grid == null) grid = GetComponent<Grid>();
        if (groundTilemap == null) groundTilemap = GetComponentInChildren<Tilemap>();

        currentMouse = Mouse.current;

        InitializeGrid();
    }

    void Start()
    {
        Debug.Log($"GridManager инициализирован. Размер: {gridSize.x}x{gridSize.y}");

        // Автоматически находим и регистрируем ресурсы на сцене
        if (autoFindResourcesOnStart)
        {
            RegisterAllResourcesOnScene();
        }
    }

    void Update()
    {
        if (isBuildingMode && buildingPreview != null)
        {
            UpdateBuildingPreview();
        }
    }

    private void InitializeGrid()
    {
        occupiedCells = new bool[gridSize.x, gridSize.y];

        if (gridCellPrefab != null)
        {
            CreateGridVisualization();
        }

        MarkCollisionCellsAsOccupied();
    }

    private void MarkCollisionCellsAsOccupied()
    {
        if (collisionTilemap == null) return;

        BoundsInt bounds = collisionTilemap.cellBounds;
        int occupiedCount = 0;

        foreach (var position in bounds.allPositionsWithin)
        {
            if (collisionTilemap.HasTile(position))
            {
                Vector2Int gridPos = WorldToGridPosition(collisionTilemap.GetCellCenterWorld(position));

                if (IsWithinGrid(gridPos))
                {
                    occupiedCells[gridPos.x, gridPos.y] = true;
                    occupiedCount++;
                }
            }
        }

        Debug.Log($"Заблокировано ячеек с коллизиями: {occupiedCount}");

        // Также помечаем ячейки под главным зданием если оно уже на сцене
        MarkMainBuildingCells();
    }

    private void CreateGridVisualization()
    {
        gridVisualization = new GameObject[gridSize.x, gridSize.y];

        for (int x = 0; x < gridSize.x; x++)
        {
            for (int y = 0; y < gridSize.y; y++)
            {
                Vector3 worldPos = GridToWorldPosition(new Vector2Int(x, y));
                GameObject cell = Instantiate(gridCellPrefab, worldPos, Quaternion.identity, transform);
                cell.SetActive(false);
                gridVisualization[x, y] = cell;
            }
        }
    }

    private void MarkMainBuildingCells()
    {
        // Исправляем устаревший метод FindObjectsOfType
        MainBuilding[] mainBuildings = FindObjectsByType<MainBuilding>(FindObjectsSortMode.None);

        foreach (var mainBuilding in mainBuildings)
        {
            if (mainBuilding.IsBuilt())
            {
                Vector2Int gridPos = WorldToGridPosition(mainBuilding.transform.position);
                BuildingData buildingData = mainBuilding.GetBuildingData();

                if (buildingData != null)
                {
                    // Помечаем все ячейки под главным зданием
                    for (int x = 0; x < buildingData.gridSize.x; x++)
                    {
                        for (int y = 0; y < buildingData.gridSize.y; y++)
                        {
                            Vector2Int cellPos = new(gridPos.x + x, gridPos.y + y);

                            if (IsWithinGrid(cellPos))
                            {
                                occupiedCells[cellPos.x, cellPos.y] = true;
                            }
                        }
                    }

                    Debug.Log($"Главное здание зарегистрировано в сетке: {gridPos}, Размер: {buildingData.gridSize}");
                }
            }
        }
    }

    #region Регистрация ресурсов

    // Автоматически найти все ресурсы на сцене
    private void RegisterAllResourcesOnScene()
    {
        ResourceSource[] allResources = FindObjectsByType<ResourceSource>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        Debug.Log($"Найдено ресурсов на сцене: {allResources.Length}");

        foreach (ResourceSource resource in allResources)
        {
            RegisterResource(resource.gameObject);
        }
    }

    // Основной метод регистрации ресурса
    public void RegisterResource(GameObject resourceObject)
    {
        if (resourceObject == null)
        {
            Debug.LogWarning("Попытка зарегистрировать null ресурс!");
            return;
        }

        // Получаем начальную позицию и размер занятой области
        GetResourceArea(resourceObject, out Vector2Int startCell, out Vector2Int size);

        if (IsWithinGrid(startCell))
        {
            // Помечаем ячейки как занятые
            MarkResourceCells(startCell, size, true);
            Debug.Log($"Ресурс '{resourceObject.name}' зарегистрирован. Начало: {startCell}, Размер: {size.x}x{size.y}");
        }
        else
        {
            Debug.LogWarning($"Ресурс '{resourceObject.name}' вне сетки: {startCell}");
        }
    }

    // Получить область занятых ячеек ресурса
    private void GetResourceArea(GameObject resourceObject, out Vector2Int startCell, out Vector2Int size)
    {
        ResourceSource resourceSource = resourceObject.GetComponent<ResourceSource>();
        if (resourceSource != null)
        {
            // Используем рефлексию для доступа к приватным полям
            var offsetLeftField = resourceSource.GetType().GetField("offsetLeft",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var offsetRightField = resourceSource.GetType().GetField("offsetRight",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var offsetDownField = resourceSource.GetType().GetField("offsetDown",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var offsetUpField = resourceSource.GetType().GetField("offsetUp",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            // Получаем позицию pivot в сетке
            Vector2Int pivotCell = WorldToGridPosition(resourceObject.transform.position);

            if (offsetLeftField != null && offsetRightField != null &&
                offsetDownField != null && offsetUpField != null)
            {
                // Есть параметры смещения - используем их
                int left = (int)offsetLeftField.GetValue(resourceSource);
                int right = (int)offsetRightField.GetValue(resourceSource);
                int down = (int)offsetDownField.GetValue(resourceSource);
                int up = (int)offsetUpField.GetValue(resourceSource);

                // Рассчитываем начальную позицию (левый нижний угол)
                startCell = new Vector2Int(
                    pivotCell.x - left,
                    pivotCell.y - down
                );

                // Рассчитываем размер области
                size = new Vector2Int(
                    left + right + 1,  // +1 для pivot ячейки
                    down + up + 1
                );

                return;
            }

            // Если нет параметров смещения, используем старый метод
            System.Reflection.MethodInfo method = resourceSource.GetType().GetMethod("GetGridSize");
            if (method != null)
            {
                Vector2Int gridSize = (Vector2Int)method.Invoke(resourceSource, null);
                startCell = pivotCell;
                size = gridSize;
                return;
            }
        }

        // По умолчанию - 1x1 под pivot
        startCell = WorldToGridPosition(resourceObject.transform.position);
        size = Vector2Int.one;
    }

    // Пометить ячейки занятые ресурсом
    private void MarkResourceCells(Vector2Int startCell, Vector2Int size, bool occupied)
    {
        for (int x = 0; x < size.x; x++)
        {
            for (int y = 0; y < size.y; y++)
            {
                Vector2Int cellPos = new Vector2Int(startCell.x + x, startCell.y + y);

                if (IsWithinGrid(cellPos))
                {
                    occupiedCells[cellPos.x, cellPos.y] = occupied;
                }
            }
        }
    }

    #endregion

    #region Публичные методы для строительства

    public void EnterBuildingMode(BuildingData buildingData)
    {
        if (buildingData == null || buildingData.prefab == null)
        {
            Debug.LogError("BuildingData или префаб не назначен!");
            return;
        }

        isBuildingMode = true;
        currentBuildingData = buildingData;

        // Создаем превью
        if (buildingPreview == null)
        {
            buildingPreview = Instantiate(buildingData.prefab);
            buildingPreview.name = "BuildingPreview";

            // Отключаем ненужные компоненты
            foreach (var collider in buildingPreview.GetComponents<Collider2D>())
                collider.enabled = false;

            foreach (var monoBehaviour in buildingPreview.GetComponents<MonoBehaviour>())
                if (monoBehaviour != this)
                    monoBehaviour.enabled = false;

            // Устанавливаем цвет превью
            SetPreviewColor(buildingData.previewColor);
        }

        ShowGrid(true);
        Debug.Log($"Режим строительства: {buildingData.buildingName} ({buildingData.gridSize.x}x{buildingData.gridSize.y})");
    }

    public void ExitBuildingMode()
    {
        isBuildingMode = false;
        currentBuildingData = null;

        if (buildingPreview != null)
        {
            Destroy(buildingPreview);
            buildingPreview = null;
        }

        ShowGrid(false);
        Debug.Log("Режим строительства выключен");
    }

    // ИЗМЕНЕНИЕ 1: Этот метод только проверяет, можно ли построить
    public bool CanPlaceBuilding(Vector3 worldPosition)
    {
        if (currentBuildingData == null) return false;

        Vector2Int gridPos = WorldToGridPosition(worldPosition);
        return CanPlaceBuildingAtGrid(gridPos, currentBuildingData.gridSize);
    }

    // ИЗМЕНЕНИЕ 2: Убрали создание здания отсюда
    public bool TryPlaceBuilding(Vector3 worldPosition)
    {
        if (!isBuildingMode || currentBuildingData == null) return false;

        Vector2Int gridPos = WorldToGridPosition(worldPosition);

        if (!CanPlaceBuildingAtGrid(gridPos, currentBuildingData.gridSize))
        {
            Debug.Log($"Нельзя построить здесь: {gridPos}");
            return false;
        }

        // Только проверяем, можно ли построить
        // Здание будет создано в BuildingManager
        return true;
    }

    // ИЗМЕНЕНИЕ 3: Новый метод для регистрации построенного здания
    public void RegisterBuilding(GameObject building, Vector2Int gridPosition)
    {
        if (building == null || currentBuildingData == null) return;

        // Помечаем ячейки как занятые
        MarkCellsAsOccupied(gridPosition, currentBuildingData.gridSize, true);

        Debug.Log($"Здание '{building.name}' зарегистрировано в {gridPosition}");
    }

    public bool CanPlaceBuildingAtGrid(Vector2Int gridPosition, Vector2Int buildingSize)
    {
        if (!IsWithinGrid(gridPosition))
        {
            Debug.Log($"Позиция вне сетки: {gridPosition}");
            return false;
        }

        // Проверяем все ячейки, которые займет здание
        for (int x = 0; x < buildingSize.x; x++)
        {
            for (int y = 0; y < buildingSize.y; y++)
            {
                Vector2Int checkPos = new(gridPosition.x + x, gridPosition.y + y);

                if (!IsWithinGrid(checkPos))
                {
                    Debug.Log($"Ячейка вне сетки: {checkPos}");
                    return false;
                }

                if (occupiedCells[checkPos.x, checkPos.y])
                {
                    // ПРОВЕРЯЕМ ЧТО НА ЭТОЙ ЯЧЕЙКЕ
                    Vector3 worldPos = GridToWorldPosition(checkPos);

                    // Ищем объекты на этой позиции
                    Collider2D[] colliders = Physics2D.OverlapCircleAll(worldPos, 0.1f);

                    foreach (var collider in colliders)
                    {
                        // Если это главное здание - запрещаем строительство
                        if (collider.GetComponent<MainBuilding>() != null)
                        {
                            Debug.Log($"Нельзя строить на главном здании: {checkPos}");
                            return false;
                        }
                    }

                    Debug.Log($"Ячейка занята: {checkPos}");
                    return false;
                }
            }
        }

        return true;
    }

    #endregion

    #region Утилиты преобразования координат

    public Vector2Int WorldToGridPosition(Vector3 worldPosition)
    {
        Vector3Int cellPosition = grid.WorldToCell(worldPosition);
        return new Vector2Int(cellPosition.x, cellPosition.y);
    }

    public Vector3 GridToWorldPosition(Vector2Int gridPosition)
    {
        Vector3Int cellPosition = new Vector3Int(gridPosition.x, gridPosition.y, 0);
        return grid.GetCellCenterWorld(cellPosition);
    }

    public Vector3 GetBuildingWorldPosition(Vector2Int gridPosition)
    {
        Vector3 basePosition = GridToWorldPosition(gridPosition);

        // Центрируем для больших зданий
        if (currentBuildingData != null &&
            (currentBuildingData.gridSize.x > 1 || currentBuildingData.gridSize.y > 1))
        {
            Vector3 offset = new Vector3(
                (currentBuildingData.gridSize.x - 1) * grid.cellSize.x * 0.5f,
                (currentBuildingData.gridSize.y - 1) * grid.cellSize.y * 0.5f,
                0
            );
            return basePosition + offset;
        }

        return basePosition;
    }

    public bool IsWithinGrid(Vector2Int gridPosition)
    {
        return gridPosition.x >= 0 && gridPosition.x < gridSize.x &&
               gridPosition.y >= 0 && gridPosition.y < gridSize.y;
    }

    #endregion

    #region Визуализация и превью

    private void UpdateBuildingPreview()
    {
        if (!isBuildingMode || buildingPreview == null || currentBuildingData == null) return;

        Vector3 mouseWorldPos = GetMouseWorldPosition();
        Vector2Int gridPos = WorldToGridPosition(mouseWorldPos);

        // Всегда позиционируем превью
        Vector3 snapPosition = GetBuildingWorldPosition(gridPos);
        buildingPreview.transform.position = snapPosition;

        // Меняем цвет в зависимости от доступности
        bool canPlace = CanPlaceBuildingAtGrid(gridPos, currentBuildingData.gridSize);
        SetPreviewColor(canPlace ? currentBuildingData.previewColor : invalidColor);

        // Обновляем визуализацию сетки только если ячейка изменилась
        Vector2Int lastHovered2D = new Vector2Int(lastHoveredCell.x, lastHoveredCell.y);
        if (gridPos != lastHovered2D)
        {
            UpdateGridVisualization(gridPos, canPlace);
            lastHoveredCell = new Vector3Int(gridPos.x, gridPos.y, 0);
        }
    }

    private Vector3 GetMouseWorldPosition()
    {
        if (currentMouse == null) return Vector3.zero;

        Vector2 mousePos = currentMouse.position.ReadValue();
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(
            new Vector3(mousePos.x, mousePos.y, Mathf.Abs(Camera.main.transform.position.z))
        );

        return new Vector3(worldPos.x, worldPos.y, 0);
    }

    private void SetPreviewColor(Color color)
    {
        if (buildingPreview == null) return;

        SpriteRenderer[] renderers = buildingPreview.GetComponentsInChildren<SpriteRenderer>();
        foreach (var renderer in renderers)
        {
            renderer.color = color;
        }
    }

    private void ShowGrid(bool show)
    {
        if (gridVisualization == null) return;

        for (int x = 0; x < gridSize.x; x++)
        {
            for (int y = 0; y < gridSize.y; y++)
            {
                if (gridVisualization[x, y] != null)
                {
                    gridVisualization[x, y].SetActive(show);
                }
            }
        }
    }

    private void UpdateGridVisualization(Vector2Int centerCell, bool canPlace)
    {
        if (gridVisualization == null) return;

        // Сбрасываем все ячейки
        for (int x = 0; x < gridSize.x; x++)
        {
            for (int y = 0; y < gridSize.y; y++)
            {
                if (gridVisualization[x, y] != null)
                {
                    SpriteRenderer sr = gridVisualization[x, y].GetComponent<SpriteRenderer>();
                    if (sr != null)
                        sr.color = occupiedCells[x, y] ? invalidColor : new Color(0, 0, 0, 0.1f);
                }
            }
        }

        // Подсвечиваем ячейки под зданием
        if (currentBuildingData != null)
        {
            for (int x = 0; x < currentBuildingData.gridSize.x; x++)
            {
                for (int y = 0; y < currentBuildingData.gridSize.y; y++)
                {
                    Vector2Int cellPos = new Vector2Int(centerCell.x + x, centerCell.y + y);

                    if (IsWithinGrid(cellPos) && gridVisualization[cellPos.x, cellPos.y] != null)
                    {
                        SpriteRenderer sr = gridVisualization[cellPos.x, cellPos.y].GetComponent<SpriteRenderer>();
                        if (sr != null)
                            sr.color = canPlace ? validColor : invalidColor;
                    }
                }
            }
        }
    }

    #endregion

    #region Вспомогательные методы

    private void MarkCellsAsOccupied(Vector2Int startCell, Vector2Int size, bool occupied)
    {
        for (int x = 0; x < size.x; x++)
        {
            for (int y = 0; y < size.y; y++)
            {
                Vector2Int cellPos = new Vector2Int(startCell.x + x, startCell.y + y);

                if (IsWithinGrid(cellPos))
                {
                    occupiedCells[cellPos.x, cellPos.y] = occupied;
                    Debug.Log($"Ячейка {cellPos} помечена как {(occupied ? "занятая" : "свободная")}");
                }
            }
        }
    }

    #endregion

    #region Публичные геттеры

    public bool IsBuildingMode => isBuildingMode;
    public Vector2Int GridSize => gridSize;
    public BuildingData CurrentBuildingData => currentBuildingData;

    #endregion
}