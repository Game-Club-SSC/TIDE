using UnityEngine;

public class TideManager : MonoBehaviour
{
    private int currentHeldTide = 0;
    public LayerMask tileLayer;

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (Camera.main == null) return;
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, tileLayer))
            {
                TideTile tile = hit.collider.GetComponent<TideTile>();
                if (tile != null)
                {
                    InteractWithTile(tile);
                }
            }
        }
    }

    private void InteractWithTile(TideTile tile)
    {
        if (tile.isSealed)
        {
            Debug.Log("Tile is sealed");
            return;
        }

        if (currentHeldTide == 0)
        {
            // State A: Player is NOT holding Tide
            if (tile.currentTideValue > 5)
            {
                currentHeldTide = tile.currentTideValue - 5;
                tile.currentTideValue = 5;
            }
            else if (tile.currentTideValue < 5)
            {
                currentHeldTide = tile.currentTideValue - 1;
                tile.currentTideValue = 1;
            }
            // If tile == 5: do nothing
        }
        else
        {
            // State B: Player IS holding Tide
            int spaceInTile = 10 - tile.currentTideValue;
            if (currentHeldTide <= spaceInTile)
            {
                tile.currentTideValue += currentHeldTide;
                currentHeldTide = 0;
            }
            else
            {
                currentHeldTide -= spaceInTile;
                tile.currentTideValue = 10;
            }
        }
    }
}
