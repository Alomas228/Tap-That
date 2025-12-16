using UnityEngine;

public class ResearchStationWorker : ColonistWorker
{
    [Header("Настройки станции исследования")]
    [SerializeField] private float interactionTime = 5f;
    [SerializeField] private int warmleafCostPerCycle = 1;
    [SerializeField] private float researchPointsPerCycle = 1f;

    [Header("Текущее исследование")]
    private string currentResearchId = "";
    private float currentResearchProgress = 0f;
    private float requiredResearchPoints = 100f;
    private bool hasResourcesForResearch = false;
    private bool isTakingResources = false;

    protected override float GetInteractionTime()
    {
        return interactionTime;
    }

    protected override void CollectResourcesFromBuilding()
    {
        // Проверяем, можем ли начать исследование
        if (!CanStartResearch())
        {
            Debug.Log($"{name}: не может начать исследование");
            return;
        }

        // Добавляем прогресс исследования
        currentResearchProgress += researchPointsPerCycle;

        Debug.Log($"{name} добавил {researchPointsPerCycle} очков исследования. " +
                 $"Програесс: {currentResearchProgress:F1}/{requiredResearchPoints}");

        // Тратим теплолист из инвентаря
        carriedWarmleaf = Mathf.Max(0, carriedWarmleaf - warmleafCostPerCycle);

        // Проверяем завершение исследования
        if (currentResearchProgress >= requiredResearchPoints)
        {
            CompleteResearch();
        }
    }

    private bool CanStartResearch()
    {
        // Проверяем активное исследование
        if (string.IsNullOrEmpty(currentResearchId))
        {
            // Пытаемся получить новое исследование
            // currentResearchId = ResearchManager.Instance.GetCurrentResearchId();
            // requiredResearchPoints = ResearchManager.Instance.GetRequiredPoints(currentResearchId);

            if (string.IsNullOrEmpty(currentResearchId))
            {
                Debug.Log($"{name}: нет активного исследования");
                return false;
            }
        }

        // Проверяем, достаточно ли ресурсов в инвентаре
        if (carriedWarmleaf < warmleafCostPerCycle)
        {
            // Пытаемся взять ресурсы из главного здания
            if (!TakeResourcesFromMainBuilding())
            {
                return false;
            }
        }

        return true;
    }

    private bool TakeResourcesFromMainBuilding()
    {
        if (isTakingResources) return false;

        if (mainBuilding == null)
        {
            Debug.Log($"{name}: главное здание не найдено");
            return false;
        }

        MainBuilding mainBuildingScript = mainBuilding.GetComponent<MainBuilding>();
        if (mainBuildingScript == null)
        {
            Debug.Log($"{name}: скрипт MainBuilding не найден");
            return false;
        }

        // Пытаемся взять теплолист из главного здания
        if (mainBuildingScript.TryTakeResource("warmleaf", warmleafCostPerCycle))
        {
            carriedWarmleaf += warmleafCostPerCycle;
            hasResourcesForResearch = true;
            Debug.Log($"{name} взял {warmleafCostPerCycle} теплолиста из главного здания");
            return true;
        }

        Debug.Log($"{name}: недостаточно теплолиста в главном здании");
        hasResourcesForResearch = false;
        return false;
    }

    protected override bool HasResourcesToDeliver()
    {
        // Исследовательская станция НЕ сдает ресурсы, а берет их
        return false;
    }

    private void CompleteResearch()
    {
        Debug.Log($"Исследование '{currentResearchId}' завершено!");

        // Сообщаем менеджеру исследований
        // ResearchManager.Instance.CompleteResearch(currentResearchId);

        // Сбрасываем прогресс
        currentResearchProgress = 0f;
        currentResearchId = "";
        hasResourcesForResearch = false;
        carriedWarmleaf = 0;
    }

    // Публичные методы для UI
    public float GetResearchProgressPercentage() =>
        requiredResearchPoints > 0 ? currentResearchProgress / requiredResearchPoints : 0f;

    public string GetCurrentResearchName() => currentResearchId;
    public bool HasActiveResearch() => !string.IsNullOrEmpty(currentResearchId);
    public bool HasResearchResources() => hasResourcesForResearch;
}