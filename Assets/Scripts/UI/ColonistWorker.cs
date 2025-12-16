using UnityEngine;
using System.Collections;

public class ColonistWorker : MonoBehaviour
{
    [Header("Базовые настройки")]
    [SerializeField] protected float moveSpeed = 2f;
    [SerializeField] protected float interactionRange = 1f;

    // Изменяем protected на public или добавляем свойства
    [Header("Визуальные ссылки")]
    [SerializeField] public GameObject visualObject;        // Изменяем на public
    [SerializeField] public SpriteRenderer spriteRenderer;  // Изменяем на public

    [Header("Состояние")]
    protected ColonistState currentState = ColonistState.Idle;
    protected Transform targetBuilding;
    protected Transform mainBuilding;
    protected Vector3 targetPosition;

    // Инвентарь
    protected int carriedWarmleaf = 0;
    protected int carriedThunderite = 0;
    protected int carriedMirallite = 0;

    // Ссылки
    protected BuildingManager buildingManager;
    protected ResourceManager resourceManager;

    public enum ColonistState
    {
        Idle,
        MovingToBuilding,
        Interacting,
        MovingToMainBuilding,
        DeliveringResources
    }

    void Start()
    {
        buildingManager = BuildingManager.Instance;
        resourceManager = ResourceManager.Instance;

        FindMainBuilding();
        StartCoroutine(WorkRoutine());
    }

    protected virtual void Update()
    {
        UpdateMovement();
        UpdateVisuals();
    }

    #region БАЗОВАЯ ЛОГИКА

    protected virtual IEnumerator WorkRoutine()
    {
        // Ждем пока назначат здание
        while (targetBuilding == null)
        {
            Debug.Log($"{name} ждет назначения здания...");
            yield return new WaitForSeconds(1f);
        }

        Debug.Log($"{name} начал работу на здании: {targetBuilding.name}");

        while (true)
        {
            // Идем к зданию
            currentState = ColonistState.MovingToBuilding;
            SetTargetPosition(targetBuilding.position);

            yield return StartCoroutine(MoveToTarget());

            // Взаимодействуем с зданием
            yield return StartCoroutine(InteractWithBuilding());

            // Если есть ресурсы - несем в главное здание
            if (HasResourcesToDeliver())
            {
                yield return StartCoroutine(DeliverResourcesToMainBuilding());
            }

            // Краткая пауза между циклами
            yield return new WaitForSeconds(0.5f);
        }
    }

    protected virtual IEnumerator MoveToTarget()
    {
        Debug.Log($"{name} движется к цели: {targetPosition}");

        while (Vector3.Distance(transform.position, targetPosition) > interactionRange)
        {
            yield return null;
        }

        Debug.Log($"{name} достиг цели");

        if (currentState == ColonistState.MovingToBuilding)
        {
            currentState = ColonistState.Interacting;
        }
        else if (currentState == ColonistState.MovingToMainBuilding)
        {
            currentState = ColonistState.DeliveringResources;
        }
    }

    protected virtual IEnumerator InteractWithBuilding()
    {
        Debug.Log($"{name} взаимодействует с зданием");

        if (visualObject != null)
        {
            visualObject.SetActive(false);
        }

        float interactionTime = GetInteractionTime();
        Debug.Log($"{name} будет взаимодействовать {interactionTime} секунд");
        yield return new WaitForSeconds(interactionTime);

        CollectResourcesFromBuilding();

        if (visualObject != null)
        {
            visualObject.SetActive(true);
        }

        currentState = ColonistState.Idle;
    }

    protected virtual IEnumerator DeliverResourcesToMainBuilding()
    {
        if (mainBuilding == null)
        {
            Debug.Log($"{name}: главное здание не найдено");
            yield break;
        }

        currentState = ColonistState.MovingToMainBuilding;
        SetTargetPosition(mainBuilding.position);

        Debug.Log($"{name} несет ресурсы в главное здание");
        yield return StartCoroutine(MoveToTarget());

        DeliverResources();
        currentState = ColonistState.Idle;
    }

    #endregion

    #region ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ

    protected virtual void UpdateMovement()
    {
        if (currentState == ColonistState.MovingToBuilding ||
            currentState == ColonistState.MovingToMainBuilding)
        {
            Vector3 direction = (targetPosition - transform.position).normalized;
            transform.position += direction * moveSpeed * Time.deltaTime;

            if (spriteRenderer != null && direction.x != 0)
            {
                spriteRenderer.flipX = direction.x < 0;
            }
        }
    }

    protected virtual void UpdateVisuals()
    {
        // Можно добавить визуальные эффекты
    }

    protected virtual void SetTargetPosition(Vector3 position)
    {
        targetPosition = position;
    }

    protected virtual void FindMainBuilding()
    {
        GameObject mainBuildingObj = GameObject.FindGameObjectWithTag("MainBuilding");
        if (mainBuildingObj != null)
        {
            mainBuilding = mainBuildingObj.transform;
            Debug.Log($"{name} нашел главное здание: {mainBuilding.name}");
        }
        else
        {
            Debug.LogWarning($"{name}: главное здание не найдено (тег MainBuilding)");
        }
    }

    protected virtual float GetInteractionTime()
    {
        return 3f;
    }

    protected virtual void CollectResourcesFromBuilding()
    {
        // Будет переопределено в наследниках
    }

    protected virtual bool HasResourcesToDeliver()
    {
        return carriedWarmleaf > 0 || carriedThunderite > 0 || carriedMirallite > 0;
    }

    protected virtual void DeliverResources()
    {
        if (resourceManager == null || !HasResourcesToDeliver()) return;

        if (carriedWarmleaf > 0)
        {
            resourceManager.AddResource("warmleaf", carriedWarmleaf);
            carriedWarmleaf = 0;
        }

        if (carriedThunderite > 0)
        {
            resourceManager.AddResource("thunderite", carriedThunderite);
            carriedThunderite = 0;
        }

        if (carriedMirallite > 0)
        {
            resourceManager.AddResource("mirallite", carriedMirallite);
            carriedMirallite = 0;
        }

        Debug.Log($"{name} сдал ресурсы");
    }

    #endregion

    #region ПУБЛИЧНЫЕ МЕТОДЫ

    public void AssignToBuilding(Transform building)
    {
        if (building == null)
        {
            Debug.LogWarning($"Попытка назначить null здание для {name}");
            return;
        }

        targetBuilding = building;
        Debug.Log($"{name} назначен на здание: {building.name}");
    }

    public void UnassignFromBuilding()
    {
        targetBuilding = null;
        currentState = ColonistState.Idle;
    }

    public bool IsWorking() => currentState != ColonistState.Idle;
    public ColonistState GetCurrentState() => currentState;

    public void AddCarriedResource(string resourceType, int amount)
    {
        switch (resourceType.ToLower())
        {
            case "warmleaf":
                carriedWarmleaf += amount;
                break;
            case "thunderite":
                carriedThunderite += amount;
                break;
            case "mirallite":
                carriedMirallite += amount;
                break;
        }

        Debug.Log($"{name} получил {amount} {resourceType}, всего: {GetCarriedResourceCount(resourceType)}");
    }

    public int GetCarriedResourceCount(string resourceType)
    {
        return resourceType.ToLower() switch
        {
            "warmleaf" => carriedWarmleaf,
            "thunderite" => carriedThunderite,
            "mirallite" => carriedMirallite,
            _ => 0
        };
    }

    #endregion

    void OnDrawGizmosSelected()
    {
        if (currentState == ColonistState.MovingToBuilding ||
            currentState == ColonistState.MovingToMainBuilding)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, targetPosition);
            Gizmos.DrawWireSphere(targetPosition, 0.2f);
        }

        Gizmos.color = new Color(0, 1, 0, 0.3f);
        Gizmos.DrawWireSphere(transform.position, interactionRange);
    }
}