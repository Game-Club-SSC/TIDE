using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>Consistent focus and navigation for runtime-created menu buttons.</summary>
public static class MenuNavigationUtility
{
    public static void ConfigureVertical(params Button[] buttons)
    {
        if (buttons == null) return;
        List<Button> enabledButtons = new List<Button>();
        for (int i = 0; i < buttons.Length; i++)
        {
            Button button = buttons[i];
            if (button != null && button.interactable && button.gameObject.activeInHierarchy)
            {
                enabledButtons.Add(button);
            }
        }

        for (int i = 0; i < enabledButtons.Count; i++)
        {
            Navigation navigation = enabledButtons[i].navigation;
            navigation.mode = Navigation.Mode.Explicit;
            navigation.selectOnUp = enabledButtons[(i + enabledButtons.Count - 1) % enabledButtons.Count];
            navigation.selectOnDown = enabledButtons[(i + 1) % enabledButtons.Count];
            navigation.selectOnLeft = null;
            navigation.selectOnRight = null;
            enabledButtons[i].navigation = navigation;
        }
    }

    public static void Select(Button button)
    {
        if (button == null || !button.interactable || !button.gameObject.activeInHierarchy) return;
        EventSystem eventSystem = EventSystem.current;
        if (eventSystem == null) eventSystem = Object.FindFirstObjectByType<EventSystem>();
        if (eventSystem == null) return;
        eventSystem.SetSelectedGameObject(null);
        eventSystem.SetSelectedGameObject(button.gameObject);
        button.Select();
    }
}
