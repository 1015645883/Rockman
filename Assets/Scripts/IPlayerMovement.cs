using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IPlayerMovement
{
    bool isHurt { get; }
    bool IsFacingRight();
    void SetShootingState(bool state);
}
