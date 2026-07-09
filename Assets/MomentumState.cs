using System;
using UnityEngine;

public class MomentumState
{
    private const float MaxValue = 1f;
    private const float MinValue = -1f;

    private float momentum;
    private float strongShiftAmount = 0.15f;
    private float weakShiftRatio = 0.5f;
    private float neutralShiftRatio = 0.33f;
    private float critShiftAmount = 0.1f;
    private float healShiftAmount = 0.05f;
    private float tideBreakShiftAmount = 0.2f;

    public float Value => momentum;
    public bool IsPlayerTideBreakReady => momentum >= MaxValue;
    public bool IsEnemyTideBreakReady => momentum <= MinValue;

    public event Action<float> OnMomentumChanged;

    public void SetShiftAmounts(float strong, float weakRatio, float neutralRatio)
    {
        strongShiftAmount = Mathf.Max(0f, strong);
        weakShiftRatio = Mathf.Clamp01(weakRatio);
        neutralShiftRatio = Mathf.Clamp01(neutralRatio);
    }

    public void SetCritShift(float amount) => critShiftAmount = Mathf.Max(0f, amount);
    public void SetHealShift(float amount) => healShiftAmount = Mathf.Max(0f, amount);
    public void SetTideBreakShift(float amount) => tideBreakShiftAmount = Mathf.Max(0f, amount);

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
                    ShiftTowardPlayer(strongShiftAmount);
                }
                else
                {
                    ShiftTowardEnemy(strongShiftAmount);
                }
                break;

            case MatchupResult.Weak:
                if (attackerIsAlly)
                {
                    ShiftTowardEnemy(strongShiftAmount * weakShiftRatio);
                }
                else
                {
                    ShiftTowardPlayer(strongShiftAmount * weakShiftRatio);
                }
                break;

            case MatchupResult.Neutral:
                if (attackerIsAlly)
                {
                    ShiftTowardPlayer(strongShiftAmount * neutralShiftRatio);
                }
                else
                {
                    ShiftTowardEnemy(strongShiftAmount * neutralShiftRatio);
                }
                break;
        }
    }

    public void ShiftForCrit(CombatUnit attacker)
    {
        if (attacker == null)
        {
            return;
        }

        if (attacker.Type == CombatUnit.UnitType.Ally)
        {
            ShiftTowardPlayer(critShiftAmount);
        }
        else
        {
            ShiftTowardEnemy(critShiftAmount);
        }
    }

    public void ShiftForHeal(CombatUnit healer)
    {
        if (healer == null)
        {
            return;
        }

        if (healer.Type == CombatUnit.UnitType.Ally)
        {
            ShiftTowardPlayer(healShiftAmount);
        }
        else
        {
            ShiftTowardEnemy(healShiftAmount);
        }
    }

    public void ShiftForTideBreak(CombatUnit activator)
    {
        if (activator == null)
        {
            return;
        }

        if (activator.Type == CombatUnit.UnitType.Ally)
        {
            ShiftTowardPlayer(tideBreakShiftAmount);
        }
        else
        {
            ShiftTowardEnemy(tideBreakShiftAmount);
        }
    }

    public void Reset()
    {
        momentum = 0f;
        OnMomentumChanged?.Invoke(momentum);
    }
}
