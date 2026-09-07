using System;
using UnityEngine;
using UnityEngine.UI;

public class GEnergyManager : MonoBehaviour
{
    public static GEnergyManager Instance { get; private set; }

    public int MaxEnergy => 28;
    public int CurrentEnergy { get; private set; }
    public int ConsumePerUse => 1;

    public event Action<int, int> OnEnergyChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.Log("Duplicate GEnergyManager detected, destroying: " + gameObject.name);
            Destroy(gameObject);
            return;
        }

        Instance = this;
        CurrentEnergy = MaxEnergy;

        Debug.Log("GEnergyManager in scene: " + gameObject.name);

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
}
