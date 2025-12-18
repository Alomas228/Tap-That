using UnityEngine;

public class WarmleafStationWorker : ColonistWorker
{
    [Header("Настройки станции теплолиста")]
    [SerializeField] private int warmleafPerCycle = 1;
    [SerializeField] private float interactionTime = 3f;

    [Header("Визуальные эффекты")]
    [SerializeField] private ParticleSystem collectEffect;
    [SerializeField] private AudioClip collectSound;

    protected override float GetInteractionTime()
    {
        return interactionTime;
    }

    protected override void CollectResourcesFromBuilding()
    {
        // Базовое количество теплолиста за цикл
        int baseAmount = warmleafPerCycle;

        // Получаем бонусы от технологий (Автоматизация лесоповала)
        int techBonus = GetTechnologyBonus();

        // Итоговое количество
        int totalAmount = baseAmount + techBonus;

        // Добавляем теплолист в инвентарь колониста
        AddCarriedResource("warmleaf", totalAmount);

        // Визуальные эффекты
        PlayCollectEffects();

        // Логирование для отладки
        Debug.Log($"{name} добыл {totalAmount} теплолиста " +
                 $"(база: {baseAmount}, бонус: {techBonus})");
    }

    private int GetTechnologyBonus()
    {
        int bonus = 0;

        // Получаем бонусы от технологий через TechnologyManager
        if (TechnologyManager.Instance != null)
        {
            bonus = TechnologyManager.Instance.GetWorkerWarmleafBonus();
        }

        return bonus;
    }

    private void PlayCollectEffects()
    {
        // Визуальный эффект
        if (collectEffect != null && !collectEffect.isPlaying)
        {
            collectEffect.Play();
        }

        // Звуковой эффект
        if (collectSound != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayEffect(collectSound, 0.7f);
        }
    }

    protected override bool HasResourcesToDeliver()
    {
        // Сдаем теплолист, если есть
        return carriedWarmleaf > 0;
    }

    #region Публичные методы для UI

    // Получить базовую производительность
    public int GetBaseProduction() => warmleafPerCycle;

    // Получить текущий бонус от технологий
    public int GetCurrentTechBonus()
    {
        return GetTechnologyBonus();
    }

    // Получить общую производительность
    public int GetTotalProduction()
    {
        return warmleafPerCycle + GetTechnologyBonus();
    }

    // Получить информацию о производстве для UI
    public string GetProductionInfo()
    {
        int baseProd = GetBaseProduction();
        int bonus = GetCurrentTechBonus();
        int total = GetTotalProduction();

        if (bonus > 0)
        {
            return $"{total} теплолиста/цикл\n({baseProd} + {bonus} от технологий)";
        }
        else
        {
            return $"{total} теплолиста/цикл";
        }
    }

    #endregion

    #region Отладка и визуализация

    void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying) return;

        // Визуализация количества теплолиста в инвентаре
        if (carriedWarmleaf > 0)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 0.5f, 0.3f);

#if UNITY_EDITOR
            UnityEditor.Handles.Label(
                transform.position + Vector3.up * 1f,
                $" {carriedWarmleaf}"
            );
#endif
        }

        // Визуализация производительности
        if (targetBuilding != null)
        {
            Gizmos.color = Color.yellow;
            Vector3 lineStart = transform.position;
            Vector3 lineEnd = targetBuilding.position;
            Gizmos.DrawLine(lineStart, lineEnd);

#if UNITY_EDITOR
            UnityEditor.Handles.Label(
                (lineStart + lineEnd) / 2,
                $"{GetTotalProduction()}/цикл"
            );
#endif
        }
    }

    #endregion
}