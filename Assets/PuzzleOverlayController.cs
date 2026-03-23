using UnityEngine;

[DisallowMultipleComponent]
public class PuzzleOverlayController : MonoBehaviour
{
    private TideManager activeManager;
    private GameObject activeRoot;
    private PuzzleBoxInteractable activeBox;
    private bool sessionOpen;

    private static readonly Vector3 OverlaySpawnOffset = new Vector3(0f, 0.02f, 0f);

    private bool didSaveCamera;
    private Vector3 savedCamPosition;
    private Quaternion savedCamRotation;
    private bool savedCamOrtho;
    private float savedCamOrthoSize;
    private float savedCamFOV;
    private TopDownFollowCamera savedFollowCamera;

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
        SaveAndZoomCamera(boardCenter);
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
        RestoreCamera();
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

        PuzzleHud existingHud = FindFirstObjectByType<PuzzleHud>();
        if (existingHud != null)
        {
            Destroy(existingHud.gameObject);
        }
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

    private void SaveAndZoomCamera(Vector3 boardCenter)
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        didSaveCamera = true;
        savedCamPosition = cam.transform.position;
        savedCamRotation = cam.transform.rotation;
        savedCamOrtho = cam.orthographic;
        savedCamOrthoSize = cam.orthographicSize;
        savedCamFOV = cam.fieldOfView;

        savedFollowCamera = cam.GetComponent<TopDownFollowCamera>();
        if (savedFollowCamera != null)
        {
            savedFollowCamera.enabled = false;
        }

        cam.transform.position = new Vector3(boardCenter.x, 14f, boardCenter.z);
        cam.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        cam.orthographic = true;
        cam.orthographicSize = 4.5f;
    }

    private void RestoreCamera()
    {
        if (!didSaveCamera) return;
        didSaveCamera = false;

        Camera cam = Camera.main;
        if (cam == null) return;

        cam.transform.position = savedCamPosition;
        cam.transform.rotation = savedCamRotation;
        cam.orthographic = savedCamOrtho;
        cam.orthographicSize = savedCamOrthoSize;
        cam.fieldOfView = savedCamFOV;

        if (savedFollowCamera != null)
        {
            savedFollowCamera.enabled = true;
            savedFollowCamera.SnapToCurrentTarget();
        }
    }
}
