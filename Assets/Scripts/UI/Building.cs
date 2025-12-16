using UnityEngine;

public class Building : MonoBehaviour
{
    [Header("Информация о здании")]
    [SerializeField] private BuildingData buildingData;
    [SerializeField] private int buildCost;
    [SerializeField] private int buildingNumber;

    [Header("Состояние")]
    [SerializeField] protected bool isBuilt = false;    // Изменяем на protected
    [SerializeField] protected bool isActive = true;    // Изменяем на protected

    public virtual void Initialize(BuildingData data, int cost)
    {
        buildingData = data;
        buildCost = cost;

        // Получаем номер здания
        if (BuildingManager.Instance != null)
        {
            buildingNumber = BuildingManager.Instance.GetBuildingCount(data);
        }

        Debug.Log($"Здание инициализировано: {data.buildingName} #{buildingNumber}, Стоимость: {cost}");

        // Автоматически считаем построенным
        CompleteBuilding();
    }

    protected virtual void CompleteBuilding()
    {
        isBuilt = true;
        isActive = true;
        Debug.Log($"{buildingData.buildingName} #{buildingNumber} построено");
    }

    public string GetBuildingInfo()
    {
        if (buildingData == null) return "Неизвестное здание";

        return $"{buildingData.buildingName} #{buildingNumber}\n" +
               $"Стоимость: {buildCost} {GetResourceName(buildingData.costType)}";
    }

    private string GetResourceName(BuildingData.CostType costType)
    {
        return costType switch
        {
            BuildingData.CostType.Warmleaf => "Теплолиста",
            BuildingData.CostType.Thunderite => "Грозалита",
            BuildingData.CostType.Mirallite => "Мираллита",
            _ => ""
        };
    }

    // Вызывается каждый кадр когда здание активно
    void Update()
    {
        if (isActive && isBuilt)
        {
            // Здесь логика производства/работы здания
        }
    }

    #region Публичные геттеры
    public BuildingData GetBuildingData() => buildingData;
    public int GetBuildCost() => buildCost;
    public int GetBuildingNumber() => buildingNumber;
    public bool IsBuilt() => isBuilt;
    public bool IsActive() => isActive;
    #endregion
}