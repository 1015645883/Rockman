using System;
using UnityEngine;

public class EEnergyManager : MonoBehaviour
{
    [Header("能量配置")]
    public int MaxEnergy = 28;
    public int ConsumePerUse = 1;

    public int CurrentEnergy { get; private set; }

    // 通知 UI 更新
    public event Action<int, int> OnEnergyChanged;

    private void Awake()
    {
        CurrentEnergy = MaxEnergy;
        OnEnergyChanged?.Invoke(CurrentEnergy, MaxEnergy);
    }

    public bool TryUseEnergy(int amount)
    {
        if (CurrentEnergy >= amount)
        {
            CurrentEnergy -= amount;
            OnEnergyChanged?.Invoke(CurrentEnergy, MaxEnergy);
            return true;
        }
        return false;
    }

    public void RestoreEnergy(int amount)
    {
        CurrentEnergy = Mathf.Min(CurrentEnergy + amount, MaxEnergy);
        OnEnergyChanged?.Invoke(CurrentEnergy, MaxEnergy);
    }

    public void ResetEnergy()
    {
        CurrentEnergy = MaxEnergy;
        OnEnergyChanged?.Invoke(CurrentEnergy, MaxEnergy);
    }

    public void SetEnergyDirectly(int value)
    {
        CurrentEnergy = Mathf.Clamp(value, 0, MaxEnergy);
        OnEnergyChanged?.Invoke(CurrentEnergy, MaxEnergy);
    }
}
