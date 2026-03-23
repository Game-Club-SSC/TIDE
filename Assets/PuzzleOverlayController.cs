using UnityEngine;

[DisallowMultipleComponent]
public class PuzzleOverlayController : MonoBehaviour
{
    private TideManager activeManager;
    private GameObject activeRoot;
    private PuzzleBoxInteractable activeBox;
    private bool sessionOpen;

    private static readonly Vector3 OverlaySpawnOffset = new Vector3(0f, 0.02f, 0f);

    public bool IsSessionOpen => sessionOpen;

    private void OnDisable()
    {
        CleanupActiveSession();
    }

    public bool OpenPuzzle(PuzzleBoxInteractable sourceBox)
    {
        if (sourceBox == null || sessionOpen)
        {
            return false;
        }

        GameStateManager gsm = GameStateManager.Instance;
        if (gsm == null || !gsm.CanEnterPuzzle())
        {
            return false;
        }

        string puzzleBoxId = sourceBox.GetPuzzleBoxId();
        if (gsm.IsPuzzleBoxSolved(puzzleBoxId))
        {
            sourceBox.MarkSolved();
            return false;
        }

        sessionOpen = true;
        activeBox = sourceBox;
        gsm.EnterPuzzle();

        bool hasRuntimeLayout = gsm.TryGetPuzzleRuntimeGrid(puzzleBoxId, out int[,] runtimeGrid, out _);
        PuzzleData puzzleData = sourceBox.GetPuzzleData();
        int[,] legacyLayout = sourceBox.GetLegacyPuzzleLayout();
        Vector2Int legacySealed = sourceBox.GetLegacySealedPosition();
        Vector3 boardCenter = sourceBox.GetOverlayBoardCenterWorldPosition() + OverlaySpawnOffset;

        activeRoot = new GameObject($"PuzzleOverlay_{puzzleBoxId}");
        activeManager = activeRoot.AddComponent<TideManager>();
        activeManager.ConfigureOverlaySession(
            puzzleData,
            legacyLayout,
            legacySealed,
            hasRuntimeLayout ? runtimeGrid : null,
            puzzleBoxId,
            sourceBox.GetIslandId(),
            sourceBox.GetEncounterId(),
            sourceBox.GetRestorationValue(),
            boardCenter);

        activeManager.OverlayExitRequested += OnOverlayExitRequested;
        activeManager.OverlayPuzzleSolved += OnOverlayPuzzleSolved;

        EnsurePuzzleHud();
        return true;
    }

    private void OnOverlayExitRequested()
    {
        CloseActiveSession(false);
    }

    private void OnOverlayPuzzleSolved()
    {
        CloseActiveSession(true);
    }

    private void CloseActiveSession(bool solved)
    {
        if (!sessionOpen)
        {
            return;
        }

        GameStateManager gsm = GameStateManager.Instance;
        if (gsm != null && activeManager != null)
        {
            int[,] grid = activeManager.CaptureCurrentGrid();
            gsm.SavePuzzleRuntimeState(activeManager.OverlayPuzzleBoxId, grid, solved || activeManager.IsPuzzleSolved);
            gsm.ExitPuzzle();
        }

        if (solved && activeBox != null)
        {
            activeBox.MarkSolved();
        }

        CleanupActiveSession();
    }

    private void CleanupActiveSession()
    {
        sessionOpen = false;

        if (activeManager != null)
        {
            activeManager.OverlayExitRequested -= OnOverlayExitRequested;
            activeManager.OverlayPuzzleSolved -= OnOverlayPuzzleSolved;
        }

        if (activeRoot != null)
        {
            Destroy(activeRoot);
        }

        activeRoot = null;
        activeManager = null;
        activeBox = null;
    }

    private static void EnsurePuzzleHud()
    {
        if (FindFirstObjectByType<PuzzleHud>() != null)
        {
            return;
        }

        GameObject hudObject = new GameObject("PuzzleHud");
        hudObject.AddComponent<PuzzleHud>();
    }
}
