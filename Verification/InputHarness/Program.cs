using System;
using System.Reflection;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

internal static class Program
{
    private static int checks;
    private static int Main()
    {
        try
        {
            var manager = new GamepadInputManager();
            Tick(manager);
            Check(!manager.IsGamepadConnected && !manager.AttackPressed && manager.MoveH == 0, "Disconnected channels cleared");
#if ENABLE_INPUT_SYSTEM
            var pad = new Gamepad();
            Gamepad.current = pad;
            pad.buttonSouth.isPressed = true;
            pad.buttonSouth.wasPressedThisFrame = true;
            pad.leftStick.Value = new Vector2(1,0);
#else
            Input.Names = new[] { "", "Controller" };
            Input.Held.Add(0); Input.Pressed.Add(0);
            Input.Axes["Gamepad Horizontal"] = 1;
            Time.unscaledTime = 1;
#endif
            Tick(manager);
            Check(manager.IsGamepadConnected && manager.IsGamepadActive, "Connected gamepad activates from real device input");
            Check(manager.AttackPressed && manager.InteractPressed && manager.ConfirmPressed, "South button action aliases");
            Check(manager.MoveH == 1 && !manager.SprintHeld, "Full stick independent of sprint");
            Check(manager.SelectedTargetIndex == 1, "Stick target rising edge");
            Tick(manager);
            Check(manager.SelectedTargetIndex == 1, "Held stick cannot repeatedly cycle target");
#if ENABLE_INPUT_SYSTEM
            pad.buttonSouth.isPressed = pad.buttonSouth.wasPressedThisFrame = false;
            pad.leftStick.Value = new Vector2(0.1f,0);
            pad.dpad.Value = new Vector2(0,1);
            pad.leftStickButton.isPressed = true;
#else
            Input.Held.Clear(); Input.Pressed.Clear(); Input.Held.Add(6);
            Input.Axes["Gamepad Horizontal"] = 0.1f; Input.Axes["DPad Vertical"] = 1;
#endif
            Tick(manager);
            Check(!manager.AttackPressed && manager.MoveH == 0 && manager.SprintHeld, "Released press, dead zone, independent L3 sprint");
            Check(manager.NavUpPressed, "D-pad UI navigation");
            Tick(manager);
            Check(!manager.NavUpPressed, "Held D-pad cannot repeat navigation edges");
#if ENABLE_INPUT_SYSTEM
            pad.dpad.Value = new Vector2(0,-1);
#else
            Input.Axes["DPad Vertical"] = -1;
#endif
            Tick(manager);
            Check(manager.NavDownPressed, "Direction reversal navigation");
#if ENABLE_INPUT_SYSTEM
            Gamepad.current = null;
#else
            int reads = Input.ConnectionReads;
            for (int i=0;i<100;i++) Tick(manager);
            Check(Input.ConnectionReads == reads, "Connection detection avoids per-frame allocations");
            Input.Names = Array.Empty<string>(); Time.unscaledTime = 2;
#endif
            Tick(manager);
            Check(!manager.IsGamepadActive && !manager.IsGamepadConnected && !manager.SprintHeld && !manager.NavDownPressed && manager.SelectedTargetIndex == 0, "Disconnect clears stale state");
            manager.ResetTargetIndex();
            Check(manager.SelectedTargetIndex == 0, "Explicit target reset");
            TestMenuNavigation();
            Console.WriteLine($"Input harness: {checks} checks passed ({BuildName()}); Unity input hardware unverified.");
            return 0;
        }
        catch (Exception error) { Console.Error.WriteLine(error); return 1; }
    }
    private static void Tick(GamepadInputManager manager) => typeof(GamepadInputManager).GetMethod("Update",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(manager,null);
    private static void Check(bool ok,string name) { if(!ok) throw new InvalidOperationException(name); checks++; }
    private static string BuildName()
    {
#if ENABLE_INPUT_SYSTEM
        return "Input System";
#else
        return "legacy";
#endif
    }
    private static void TestMenuNavigation()
    {
        var first=new UnityEngine.UI.Button();
        var disabled=new UnityEngine.UI.Button{interactable=false};
        var hidden=new UnityEngine.UI.Button();hidden.gameObject.activeInHierarchy=false;
        var last=new UnityEngine.UI.Button();
        MenuNavigationUtility.ConfigureVertical(first,disabled,hidden,last,null);
        Check(first.navigation.selectOnDown==last && last.navigation.selectOnUp==first,"Menu navigation skips disabled, hidden, null actions");
        Check(first.navigation.selectOnUp==last && last.navigation.selectOnDown==first,"Enabled menu focus wraps in both directions");
        UnityEngine.EventSystems.EventSystem.current=new UnityEngine.EventSystems.EventSystem();
        MenuNavigationUtility.Select(first);
        Check(UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject==first.gameObject,"Menu initial focus selects enabled action");
        MenuNavigationUtility.Select(disabled);MenuNavigationUtility.Select(hidden);
        Check(UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject==first.gameObject,"Disabled/hidden actions cannot steal focus");
        MenuNavigationUtility.ConfigureVertical(first);
        Check(first.navigation.selectOnDown==first && first.navigation.selectOnUp==first,"Single enabled action stays reachable");
        MenuNavigationUtility.ConfigureVertical((UnityEngine.UI.Button[])null);
        UnityEngine.EventSystems.EventSystem.current=null;
        MenuNavigationUtility.Select(first);
    }
}
