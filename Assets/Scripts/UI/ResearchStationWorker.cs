using UnityEngine;

public class ResearchStationWorker : ColonistWorker
{
    [Header("Настройки станции исследования")]
    [SerializeField] private float interactionTime = 5f;
    [SerializeField] private int researchPointsPerCycle = 1;

    // Прогресс исследования (будет подключено к системе технологий)
    private int currentResearchProgress = 0;
    private int requiredResearchPoints = 100; // Пример: 100 очков для исследования

    protected override float GetInteractionTime()
    {
        return interactionTime;
    }

    protected override void CollectResourcesFromBuilding()
    {
        // Вместо сбора ресурсов, добавляем очки исследования
        currentResearchProgress += researchPointsPerCycle;

        Debug.Log($"Добавлено {researchPointsPerCycle} очков исследования. Прогресс: {currentResearchProgress}/{requiredResearchPoints}");

        // Проверяем завершение исследования
        if (currentResearchProgress >= requiredResearchPoints)
        {
            CompleteResearch();
        }
    }

    protected override bool HasResourcesToDeliver()
    {
        // Для исследовательской станции не нужно носить ресурсы в главное здание
        // Вместо этого ресурсы берутся ИЗ главного здания
        return false;
    }

    private void CompleteResearch()
    {
        Debug.Log("Исследование завершено!");
        currentResearchProgress = 0;

        // Здесь будет вызов системы технологий
        // TechnologyManager.Instance.CompleteCurrentResearch();
    }

    // Метод для взятия ресурсов из главного здания (обратный цикл)
    public bool TakeResourcesFromMainBuilding()
    {
        // Проверяем доступность технологии для исследования
        if (!IsResearchAvailable()) return false;

        // Берем ресурсы из главного здания
        // Реализация будет когда добавим MainBuilding
        return true;
    }

    private bool IsResearchAvailable()
    {
        // Проверяем выбрана ли технология для исследования
        // Пока возвращаем true для теста
        return true;
    }

    public int GetResearchProgress() => currentResearchProgress;
    public int GetRequiredResearchPoints() => requiredResearchPoints;
    public float GetResearchPercentage() =>
        requiredResearchPoints > 0 ? (float)currentResearchProgress / requiredResearchPoints : 0f;
}