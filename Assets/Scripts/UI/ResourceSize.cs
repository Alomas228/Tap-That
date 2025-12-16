using UnityEngine;

public class ResourceSize : MonoBehaviour
{
    [Header("Размер на сетке")]
    [SerializeField] private Vector2Int _size = Vector2Int.one;
    [SerializeField] private bool showGizmo = true;
    [SerializeField] private Color gizmoColor = new Color(0.2f, 0.8f, 0.2f, 0.3f);

    public Vector2Int size => _size;

    void OnDrawGizmosSelected()
    {
        if (!showGizmo || !Application.isPlaying) return;

        GridManager gridManager = GridManager.Instance;
        if (gridManager == null) return;

        Vector2Int gridPos = gridManager.WorldToGridPosition(transform.position);

        // Отображаем занимаемые ячейки
        for (int x = 0; x < _size.x; x++)
        {
            for (int y = 0; y < _size.y; y++)
            {
                Vector2Int cellPos = new Vector2Int(gridPos.x + x, gridPos.y + y);
                Vector3 worldPos = gridManager.GridToWorldPosition(cellPos);

                Gizmos.color = gizmoColor;
                Gizmos.DrawCube(worldPos, new Vector3(0.8f, 0.8f, 0.1f));
            }
        }
    }
}