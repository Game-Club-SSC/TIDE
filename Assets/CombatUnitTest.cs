using UnityEngine;

/// <summary>
/// Test script to demonstrate CombatUnit functionality.
/// Attach this to a GameObject with a CombatUnit component to test.
/// </summary>
[RequireComponent(typeof(CombatUnit))]
public class CombatUnitTest : MonoBehaviour
{
    private CombatUnit unit;
    
    private void Awake()
    {
        unit = GetComponent<CombatUnit>();
        Debug.Log($"[CombatUnitTest] Test initialized for {unit.UnitName}");
        Debug.Log($"[CombatUnitTest] Starting stats - HP: {unit.HP}/{unit.MaxHP}, MP: {unit.MP}/{unit.MaxMP}");
    }
    
    private void Update()
    {
        // Test taking damage with Space key
        if (Input.GetKeyDown(KeyCode.Space))
        {
            unit.TakeDamage(15);
        }
        
        // Test healing with H key
        if (Input.GetKeyDown(KeyCode.H))
        {
            unit.Heal(10);
        }
        
        // Test spending MP with M key
        if (Input.GetKeyDown(KeyCode.M))
        {
            unit.SpendMp(10);
        }
        
        // Test restoring MP with R key
        if (Input.GetKeyDown(KeyCode.R))
        {
            unit.RestoreMp(5);
        }
        
        // Check death state with C key
        if (Input.GetKeyDown(KeyCode.C))
        {
            unit.CheckDeathState();
        }
        
        // Test instant kill with K key
        if (Input.GetKeyDown(KeyCode.K))
        {
            unit.TakeDamage(9999); // Massive damage
        }
    }
    
    private void OnGUI()
    {
        if (unit != null)
        {
            GUI.Label(new Rect(10, 10, 300, 20), 
                $"Unit: {unit.UnitName}");
            GUI.Label(new Rect(10, 30, 300, 20), 
                $"HP: {unit.HP}/{unit.MaxHP}");
            GUI.Label(new Rect(10, 50, 300, 20), 
                $"MP: {unit.MP}/{unit.MaxMP}");
            GUI.Label(new Rect(10, 70, 300, 20), 
                $"Attack: {unit.Attack} | Defense: {unit.Defense} | Speed: {unit.Speed}");
            GUI.Label(new Rect(10, 90, 300, 20), 
                $"Element: {unit.ElementType} | Alive: {unit.IsAlive}");
            GUI.Label(new Rect(10, 110, 300, 20), 
                $"Controls: Space=Damage, H=Heal, M=Spend MP, R=Restore MP, C=Check Death, K=Instant Kill");
        }
    }
}