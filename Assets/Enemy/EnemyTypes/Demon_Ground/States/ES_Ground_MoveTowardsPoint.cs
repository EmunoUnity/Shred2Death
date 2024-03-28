using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Grounded enemies enter this state when they spawn. 
/// This state guides them towards a destination passed to them by a WaveManager.
/// </summary>
public class ES_Ground_MoveTowardsPoint : ES_DemonGround
{
    public override void Enter ()
    {
        base.Enter ();

        //eGround.agent.SetDestination (eGround.stateMachine.travelPoint);
        //eGround.agent.CalculatePath (eGround.stateMachine.travelPoint, eGround.agentPath);
        StartCoroutine (MoveToPoint (eg.stateMachine.travelPoint, animationEnter));

    }


    /// <summary>
    /// When the enemy gets close enough to their destination, begin chasing the player.
    /// </summary>
    public override void machinePhysics ()
    {
        Vector3 distanceToDestination = eg.agent.destination - transform.position;

        if (distanceToDestination.magnitude <= eg.agent.stoppingDistance)
        {
            eg.stateMachine.transitionState(GetComponent<ES_Ground_Chase>());
        }
    }

    public override void onPlayerSensorActivated ()
    {
        eg.stateMachine.transitionState (GetComponent<ES_Ground_Chase> ());
        GetComponent<ES_Ground_Chase> ().onPlayerSensorActivated ();
    }

    protected override void OnDestinationReached ()
    {
        eg.stateMachine.transitionState(GetComponent<ES_Ground_Idle> ());
    }

    protected override void OnDestinationFailed ()
    {
        Debug.LogWarning ($"{name} could not navigate path to entry point");
    }
}
