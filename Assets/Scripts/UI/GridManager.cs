using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

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

    [Header("Главное здание")]
    [SerializeField] private bool markMainBuildingOnStart = true;

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

        // Помечаем главное здание на сцене
        if (markMainBuildingOnStart)
        {
            MarkAllBuildingsOnScene();
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

    // НОВЫЙ МЕТОД: Пометить все здания на сцене
    private void MarkAllBuildingsOnScene()
    {
        // Находим все здания на сцене
        Building[] allBuildings = FindObjectsByType<Building>(FindObjectsSortMode.None);
        Debug.Log($"Найдено зданий на сцене: {allBuildings.Length}");

        foreach (var building in allBuildings)
        {
            if (building.IsBuilt())
            {
                RegisterBuildingOnGrid(building);
            }
        }
    }

    // НОВЫЙ МЕТОД: Регистрация здания в сетке
    public void RegisterBuildingOnGrid(Building building)
    {
        if (building == null) return;

        Vector2Int gridPos = WorldToGridPosition(building.transform.position);
        BuildingData buildingData = building.GetBuildingData();

        if (buildingData != null)
        {
            Debug.Log($"Регистрируем здание в сетке: {buildingData.buildingName} в {gridPos}, Размер: {buildingData.gridSize}");

            // Помечаем все ячейки под зданием
            for (int x = 0; x < buildingData.gridSize.x; x++)
            {
                for (int y = 0; y < buildingData.gridSize.y; y++)
                {
                    Vector2Int cellPos = new(gridPos.x + x, gridPos.y + y);

                    if (IsWithinGrid(cellPos))
                    {
                        occupiedCells[cellPos.x, cellPos.y] = true;

                        // Для отладки
                        if (gridVisualization != null && gridVisualization[cellPos.x, cellPos.y] != null)
                        {
                            SpriteRenderer sr = gridVisualization[cellPos.x, cellPos.y].GetComponent<SpriteRenderer>();
                            if (sr != null)
                                sr.color = new Color(1, 0, 0, 0.5f);
                        }
                    }
                }
            }

            Debug.Log($"Здание '{buildingData.buildingName}' зарегистрировано в сетке: {gridPos}, Размер: {buildingData.gridSize}");
        }
        else
        {
            Debug.LogWarning($"Здание {building.name} не имеет BuildingData");
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

        Building buildingComponent = building.GetComponent<Building>();
        if (buildingComponent != null)
        {
            RegisterBuildingOnGrid(buildingComponent);
        }
        else
        {
            // Для совместимости со старым кодом
            MarkCellsAsOccupied(gridPosition, currentBuildingData.gridSize, true);
        }

        Debug.Log($"Здание '{building.name}' зарегистрировано в {gridPosition}");
    }

    // ИСПРАВЛЕННЫЙ МЕТОД: Теперь правильно проверяет строительство поверх зданий
    public bool CanPlaceBuildingAtGrid(Vector2Int gridPosition, Vector2Int buildingSize)
    {
        if (!IsWithinGrid(gridPosition))
        {
            Debug.Log($"Позиция вне сетки: {gridPosition}");
            return false;
        }

        // Список для сбора информации о препятствиях
        List<string> obstructionInfo = new List<string>();

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

                // Проверяем занята ли ячейка в сетке
                if (occupiedCells[checkPos.x, checkPos.y])
                {
                    Vector3 worldPos = GridToWorldPosition(checkPos);

                    // Проверяем коллизии с объектами для более точной информации
                    Collider2D[] colliders = Physics2D.OverlapCircleAll(worldPos, 0.1f);

                    bool foundSpecificObject = false;

                    foreach (var collider in colliders)
                    {
                        // Проверяем главное здание
                        if (collider.GetComponent<MainBuilding>() != null)
                        {
                            obstructionInfo.Add("главное здание");
                            foundSpecificObject = true;
                            break;
                        }

                        // Проверяем обычные здания
                        Building building = collider.GetComponent<Building>();
                        if (building != null && building.IsBuilt())
                        {
                            string buildingName = building.GetBuildingData()?.buildingName ?? "неизвестное здание";
                            obstructionInfo.Add($"здание '{buildingName}'");
                            foundSpecificObject = true;
                            break;
                        }

                        // Проверяем ресурсы
                        ResourceSource resource = collider.GetComponent<ResourceSource>();
                        if (resource != null)
                        {
                            obstructionInfo.Add($"ресурс '{resource.gameObject.name}'");
                            foundSpecificObject = true;
                            break;
                        }
                    }

                    if (foundSpecificObject)
                    {
                        // Показываем информацию о всех препятствиях
                        if (obstructionInfo.Count > 0)
                        {
                            string obstructions = string.Join(", ", obstructionInfo);
                            Debug.Log($"Нельзя строить здесь: {checkPos}. Препятствия: {obstructions}");
                        }
                        return false;
                    }

                    // Если ячейка просто занята в сетке (например, тайл коллизии)
                    Debug.Log($"Ячейка занята: {checkPos}");
                    return false;
                }
            }
        }

        return true;
    }

    // Дополнительный метод для проверки зоны строительства
    public List<GameObject> GetObstructionsInArea(Vector2Int gridPosition, Vector2Int buildingSize)
    {
        List<GameObject> obstructions = new List<GameObject>();

        if (!IsWithinGrid(gridPosition)) return obstructions;

        for (int x = 0; x < buildingSize.x; x++)
        {
            for (int y = 0; y < buildingSize.y; y++)
            {
                Vector2Int checkPos = new(gridPosition.x + x, gridPosition.y + y);

                if (IsWithinGrid(checkPos) && occupiedCells[checkPos.x, checkPos.y])
                {
                    Vector3 worldPos = GridToWorldPosition(checkPos);
                    Collider2D[] colliders = Physics2D.OverlapCircleAll(worldPos, 0.2f);

                    foreach (var collider in colliders)
                    {
                        GameObject obj = collider.gameObject;

                        if (obj.GetComponent<MainBuilding>() != null ||
                            obj.GetComponent<Building>() != null ||
                            obj.GetComponent<ResourceSource>() != null)
                        {
                            if (!obstructions.Contains(obj))
                            {
                                obstructions.Add(obj);
                            }
                        }
                    }
                }
            }
        }

        return obstructions;
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

        // Дополнительная проверка для лучшей обратной связи
        if (canPlace)
        {
            // Проверяем есть ли препятствия в зоне строительства
            var obstructions = GetObstructionsInArea(gridPos, currentBuildingData.gridSize);
            if (obstructions.Count > 0)
            {
                canPlace = false;
                // Можно показать более детальное сообщение о препятствиях
                foreach (var obj in obstructions)
                {
                    string objType = GetObjectType(obj);
                    Debug.Log($"На пути строительства: {objType} '{obj.name}'");
                }
            }
        }

        SetPreviewColor(canPlace ? currentBuildingData.previewColor : invalidColor);

        // Обновляем визуализацию сетки только если ячейка изменилась
        Vector2Int lastHovered2D = new Vector2Int(lastHoveredCell.x, lastHoveredCell.y);
        if (gridPos != lastHovered2D)
        {
            UpdateGridVisualization(gridPos, canPlace);
            lastHoveredCell = new Vector3Int(gridPos.x, gridPos.y, 0);
        }
    }

    private string GetObjectType(GameObject obj)
    {
        if (obj.GetComponent<MainBuilding>() != null) return "Главное здание";
        if (obj.GetComponent<Building>() != null) return "Здание";
        if (obj.GetComponent<ResourceSource>() != null) return "Ресурс";
        if (obj.GetComponent<ColonistWorker>() != null) return "Колонист";
        return "Объект";
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