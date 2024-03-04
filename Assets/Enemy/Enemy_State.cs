using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// States are the behaviors of how an enemy moves around and shoots at the player.
/// 
/// There are a few basic states that derive off of this basic state,
/// which classes outside of the enemy object may reference.
/// </summary>
public class Enemy_State : MonoBehaviour
{
    //public static GameObject playerObject
    protected Enemy e;

    //Is the enemy currently playing an animation?
    protected bool isAnimationPlaying = false;

    [Header("Animation")]
    [SerializeField] protected string animationEnter = "";

    //States will control movement directly.
    //Rigidbody will be set in the start function

    #region STATE MACHINE

    private void Awake ()
    {
        e = transform.parent.GetComponent<Enemy>();
    }

    public virtual void Enter ()
    {
    }

    public virtual void Exit ()
    {
        StopAllCoroutines();
    }

    public virtual void machineUpdate ()
    {

    }

    public virtual void machinePhysics ()
    {

    }


    #endregion

    #region EVENT RECEPTORS
    public virtual void onPlayerSensorActivated ()
    {

    }

    public virtual void onPlayerSensorDeactivated ()
    {

    }

    public virtual void OnBullet ()
    {

    }

    public virtual void OnAnimationFinished ()
    {
        isAnimationPlaying = false;
    }

    #endregion

    #region COROUTINES

    #endregion
}
