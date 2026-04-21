using UnityEngine;

public interface IPlayerInteractionAssistTarget
{
    Vector3 GetInteractionAssistPosition();
    float GetInteractionAssistRadius();
    bool IsInteractionAssistActive();
}
