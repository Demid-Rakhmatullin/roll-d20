using System;
using UnityEngine;

public class DiceMoveAnimationBehaviour : StateMachineBehaviour
{
    public event EventHandler OnMoveEnd;

    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        OnMoveEnd?.Invoke(this, EventArgs.Empty);
    }    
}
