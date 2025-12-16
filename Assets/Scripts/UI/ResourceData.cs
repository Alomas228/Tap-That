using System;
using UnityEngine;

[System.Serializable]
public class ResourceData
{
    public string resourceId;
    public string displayName;
    public Sprite icon;
    public int currentAmount = 0;
    public int maxCapacity = 999999;
}

// Классы для каждого типа ресурса
[System.Serializable]
public class WarmleafData : ResourceData
{
    public float baseRegenerationRate = 0.5f; // Время восстановления (в секундах)
    public int clickReward = 1; // Количество за клик
    public bool isInfinite = false;
}

[System.Serializable]
public class ThunderiteData : ResourceData
{
    public float baseRegenerationRate = 0.3f; // Время восстановления (в секундах)
    public int clickReward = 1; // Количество за клик
    public bool isInfinite = true; // Бесконечный источник
}

[System.Serializable]
public class MiralliteData : ResourceData
{
    public int durability = 100; // Прочность источника
    public int maxDurability = 100;
    public float durabilityRegenRate = 0.5f; // Восстановление прочности в секунду
    public int clickReward = 1; // Количество за клик
    public bool isDepleted = false;
}