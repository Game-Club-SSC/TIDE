using UnityEngine;

public class TideTile : MonoBehaviour
{
    [Header("Tide Properties")]
    [Tooltip("The current amount of Tide on this tile (1-10)")]
    [Range(1, 10)]
    public int currentTideValue = 5;

    [Header("Tile State")]
    [Tooltip("Check this box if this is an 'X' tile that requires combat to unlock.")]
    public bool isSealed = false;
}
