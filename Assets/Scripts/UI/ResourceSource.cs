using UnityEngine;
using UnityEngine.EventSystems;

public class ResourceSource : MonoBehaviour, IPointerClickHandler
{
    public enum ResourceType { Warmleaf, Thunderite, Mirallite }

    [Header("Настройки источника")]
    [SerializeField] private ResourceType resourceType = ResourceType.Warmleaf;
    [SerializeField] private int clickReward = 1;
    [SerializeField] private float clickCooldown = 0.5f;

    [Header("Размер и смещение на сетке")]
    [SerializeField] private Vector2Int gridSize = Vector2Int.one;

    [Header("Смещение от pivot (в клетках)")]
    [SerializeField] private int offsetLeft = 0;
    [SerializeField] private int offsetRight = 0;
    [SerializeField] private int offsetDown = 0;
    [SerializeField] private int offsetUp = 0;

    [Header("Звуки")]
    [SerializeField] private AudioClip collectSound;
    [SerializeField] private AudioClip exhaustedSound;
    [SerializeField] private float volumeMultiplier = 1f;

    [Header("Визуальные эффекты")]
    [SerializeField] private ParticleSystem collectEffect;

    // ЛОКАЛЬНЫЕ данные
    private int currentDurability = 100;
    private float lastClickTime = 0f;
    private float regenTimer = 0f;

    // Рассчитанные границы
    private Vector2Int gridPosition;
    private Vector2Int occupiedStart;
    private Vector2Int occupiedSize;

    void Start()
    {
        if (GetComponent<Collider>() == null)
            gameObject.AddComponent<BoxCollider>();

        CalculateOccupiedArea();
        RegisterInGrid();
    }

    void Update()
    {
        if (resourceType == ResourceType.Mirallite)
        {
            regenTimer += Time.deltaTime;

            if (regenTimer >= 2f)
            {
                regenTimer = 0f;

                if (currentDurability < 100)
                {
                    currentDurability = Mathf.Min(currentDurability + 1, 100);
                }
            }
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // Для мираллита проверяем прочность
        if (resourceType == ResourceType.Mirallite && currentDurability <= 0)
        {
            PlayExhaustedSound();
            Debug.Log($"{gameObject.name} - нет прочности!");
            return;
        }

        // Проверяем кулдаун
        if (Time.time - lastClickTime < clickCooldown)
        {
            Debug.Log("Кулдаун!");
            return;
        }

        lastClickTime = Time.time;

        string resourceId = GetResourceId();
        string displayName = ResourceManager.Instance.GetDisplayName(resourceId);

        // Для мираллита уменьшаем прочность
        if (resourceType == ResourceType.Mirallite)
        {
            currentDurability--;
            Debug.Log($"{displayName}: +{clickReward}, Прочность: {currentDurability}/100");
        }
        else
        {
            Debug.Log($"{displayName}: +{clickReward}");
        }

        // Добавляем ресурс игроку
        switch (resourceType)
        {
            case ResourceType.Warmleaf:
                ResourceManager.Instance.CollectWarmleaf(clickReward);
                PlayCollectSound();
                break;
            case ResourceType.Thunderite:
                ResourceManager.Instance.CollectThunderite(clickReward);
                PlayCollectSound();
                break;
            case ResourceType.Mirallite:
                ResourceManager.Instance.AddResource(resourceId, clickReward);
                PlayCollectSound();
                break;
        }

        // Визуальные эффекты
        if (collectEffect != null)
            collectEffect.Play();
    }

    private void PlayCollectSound()
    {
        if (collectSound != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayEffect(collectSound, volumeMultiplier);
        }
    }

    private void PlayExhaustedSound()
    {
        if (exhaustedSound != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayEffect(exhaustedSound, volumeMultiplier);
        }
    }

    private string GetResourceId()
    {
        return resourceType.ToString().ToLower();
    }

    private void CalculateOccupiedArea()
    {
        if (GridManager.Instance != null)
        {
            gridPosition = GridManager.Instance.WorldToGridPosition(transform.position);

            occupiedStart = new Vector2Int(
                gridPosition.x - offsetLeft,
                gridPosition.y - offsetDown
            );

            occupiedSize = new Vector2Int(
                offsetLeft + offsetRight + 1,
                offsetDown + offsetUp + 1
            );
        }
    }

    private void RegisterInGrid()
    {
        if (GridManager.Instance != null)
        {
            GridManager.Instance.RegisterResource(this.gameObject);
        }
    }

    public float GetDurabilityPercentage()
    {
        if (resourceType != ResourceType.Mirallite) return 1f;
        return (float)currentDurability / 100f;
    }

    public bool CanBeMined()
    {
        if (resourceType != ResourceType.Mirallite) return true;
        return currentDurability > 0;
    }

    public int GetCurrentDurability() => currentDurability;

    public Vector2Int GetOccupiedStart() => occupiedStart;
    public Vector2Int GetOccupiedSize() => occupiedSize;
    public Vector2Int GetGridSize() => occupiedSize;
}