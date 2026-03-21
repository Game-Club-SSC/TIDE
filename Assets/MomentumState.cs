using System;
using UnityEngine;

public class MomentumState
{
    private const float DefaultShiftAmount = 0.15f;
    private const float MaxValue = 1f;
    private const float MinValue = -1f;

    private float momentum;

    public float Value => momentum;
    public bool IsPlayerTideBreakReady => momentum >= MaxValue;
    public bool IsEnemyTideBreakReady => momentum <= MinValue;

    public event Action<float> OnMomentumChanged;

    public void ShiftTowardPlayer(float amount)
    {
        momentum = Mathf.Clamp(momentum + amount, MinValue, MaxValue);
        OnMomentumChanged?.Invoke(momentum);
    }

    public void ShiftTowardEnemy(float amount)
    {
        momentum = Mathf.Clamp(momentum - amount, MinValue, MaxValue);
        OnMomentumChanged?.Invoke(momentum);
    }

    public void ShiftForAction(CombatUnit attacker, MatchupResult matchup)
    {
        bool attackerIsAlly = attacker.Type == CombatUnit.UnitType.Ally;

        switch (matchup)
        {
            case MatchupResult.Strong:
                if (attackerIsAlly)
                {
                    ShiftTowardPlayer(DefaultShiftAmount);
                }
                else
                {
                    ShiftTowardEnemy(DefaultShiftAmount);
                }
                break;

            case MatchupResult.Weak:
                if (attackerIsAlly)
                {
                    ShiftTowardEnemy(DefaultShiftAmount * 0.5f);
                }
                else
                {
                    ShiftTowardPlayer(DefaultShiftAmount * 0.5f);
                }
                break;

            case MatchupResult.Neutral:
                if (attackerIsAlly)
                {
                    ShiftTowardPlayer(DefaultShiftAmount * 0.33f);
                }
                else
                {
                    ShiftTowardEnemy(DefaultShiftAmount * 0.33f);
                }
                break;
        }
    }

    public void Reset()
    {
        momentum = 0f;
        OnMomentumChanged?.Invoke(momentum);
    }
}
