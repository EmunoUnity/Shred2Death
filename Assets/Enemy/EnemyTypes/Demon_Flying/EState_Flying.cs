using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// Flying states are for enemies that navigate straight to the player, without a navmesh.
/// This type of state references a spatial sensor on the enemy as well as functions for moving around using it.
/// 
/// Scripts that utilize this functionality are abbreviated ESF_
/// </summary>
public class EState_Flying : Enemy_State
{
    protected Enemy_Flying eFly;

    protected Vector3 movementAvoidance = Vector3.zero;
    protected Vector3 movementDirection = Vector3.zero;


    private void Awake ()
    {
        e = transform.parent.GetComponent<Enemy> ();
        eFly = transform.parent.GetComponent<Enemy_Flying>();
    }

    /// <summary>
    /// Used by MoveToPoint to determine if the destination has been reached.
    /// </summary>
    /// <returns>True if the enemy is close enough to a stopping point based on Flying Enemy stopping distance</returns>
    bool isAtObject (GameObject p)
    {
        return Vector3.Distance (transform.position, p.transform.position) <= eFly.movementStoppingDistance;
    }

    bool isAtPoint(Vector3 p, bool stoppingDistance)
    {
        if (stoppingDistance) return Vector3.Distance (transform.position, p) <= eFly.movementStoppingDistance;
        else return Vector3.Distance (transform.position, p) <= 0.1f;
    }


    /// <summary>
    /// Move towards the target point
    /// </summary>
    /// <param name="p"></param>
    /// <returns></returns>
    protected IEnumerator MoveToObject (GameObject p)
    {
        RaycastHit hit;
        //movementAvoidance = transform.forward;
        //movementDirection = transform.forward;
        while (!isAtObject (p))
        {
            eFly.stateMachine.travelPoint = p.transform.position;

            //transform.parent.LookAt(e.stateMachine.travelPoint);
            //transform.parent.LookAt(new Vector3(p.transform.position.x, transform.position.y, p.transform.position.z));
            //e.transform.LookAt (e.transform.TransformPoint (e.rb.velocity));
            //e.transform.LookAt (e.transform.position + e.rb.velocity);
            //e.transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(e.rb.velocity), eFly.movementTurnSpeed * Time.deltaTime) ;
            e.transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(p.transform.position - e.transform.position), eFly.movementTurnSpeed * Time.deltaTime) ;
            
            movementAvoidance = Vector3.MoveTowards (
                    movementAvoidance,
                    eFly.s_Spatial.pingResult.normalized,
                    eFly.movementSpeedShift * Time.deltaTime
                    );

            movementDirection = Vector3.MoveTowards 
                (
                movementDirection,
                (p.transform.position - transform.position).normalized,
                eFly.movementSpeedShift * Time.deltaTime
                );

            eFly.rb.velocity = (movementDirection.normalized + movementAvoidance) * eFly.movementSpeed;

            //Check to see if the path to the destination is blocked.
            //If it is, move in that direction but with obstacle avoidance.
            if ( Physics.SphereCast (
                    e.sensorsObject.transform.position,
                    eFly.s_Spatial.sensorWidth,
                    p.transform.position - transform.position,
                    out hit,
                    eFly.s_Spatial.sensorLength,
                    eFly.s_Spatial.maskRaycast
                    )
                )
            {
                eFly.s_Spatial.updateSpatialSensor ();
            }
            else
            {
                eFly.s_Spatial.pingResult = Vector3.zero;
            }

            Debug.DrawLine (
            transform.position,
            transform.position + (eFly.stateMachine.travelPoint - transform.position).normalized * eFly.s_Spatial.sensorLength,
            Physics.Raycast (transform.position, eFly.stateMachine.travelPoint - transform.position, eFly.s_Spatial.sensorLength, eFly.s_Spatial.maskRaycast) ? Color.red : Color.white
            );

            Debug.DrawLine (transform.position, transform.position + movementAvoidance, Color.green);



            yield return new WaitForFixedUpdate();
        }

        onPointReached ();
    }

    protected IEnumerator MoveThroughPath (GameObject [] points)
    {

        foreach (GameObject g in points)
        {
            yield return MoveToObject (g);
        }

        onPathComplete ();
        Debug.Log ("Path complete", gameObject);
    }

    protected enum ESF_MoveAnimationType {LINEAR, SPHERICAL, SMOOTHDAMP, MOVETOWARDS}
    /// <summary>
    /// Moves the enemy by modifying its velocity by interpolating an internal vector3.
    /// Followthrough is not implemented yet.
    /// </summary>
    /// <param name="targetPos"></param>
    /// <param name="t"></param>
    /// <param name="moveType"></param>
    /// <returns></returns>
    /// 
    protected IEnumerator MoveAnimation (Vector3 targetPos, ESF_MovementOptions options)
    {
        //Setup
        Vector3 startPos = transform.position;
        Vector3 currentPos = transform.position;
        Vector3 previousPos = transform.position;

        float timer = 0;

        //While you are in transit to the point, conduct movement calculation based on which type of movement you selected.
        while (Vector3.Distance(currentPos, targetPos) > 0.1f)
        {
            //Move according to the selected movement type.
            switch (options.type)
            {
                case ESF_MoveAnimationType.LINEAR:
                    currentPos = Vector3.Lerp (startPos, targetPos, options.curve.Evaluate((timer) / options.t));
                    break;

                case ESF_MoveAnimationType.SPHERICAL:
                    currentPos = Vector3.Slerp (startPos, targetPos, options.curve.Evaluate((timer) / options.t));
                    break;

                case ESF_MoveAnimationType.SMOOTHDAMP:
                    Vector3 currentVel = e.rb.velocity;
                    currentPos = Vector3.SmoothDamp (currentPos, targetPos, ref currentVel, options.t);

                    break;

                case ESF_MoveAnimationType.MOVETOWARDS: //Uses t as a speed veloicty vector instead of a time to there by.
                    currentPos = Vector3.MoveTowards (currentPos, targetPos, options.t * Time.fixedDeltaTime);
                    break;

                default: //Wait this is just linear interpolation lmao
                    Debug.LogWarning ($"{this.name} ({e.GetInstanceID()}) chose an incorrect method of interpolation");
                    currentPos = Vector3.MoveTowards (currentPos, targetPos, Time.fixedDeltaTime * (Vector3.Distance (startPos, targetPos) / options.t));
                    break;
            }

            e.rb.velocity = (currentPos - previousPos) / Time.fixedDeltaTime;
            previousPos = currentPos;            
            yield return new WaitForFixedUpdate ();
            timer += Time.fixedDeltaTime;
        }

        e.rb.velocity = Vector3.zero;

    }

    protected IEnumerator TrackPlayer ()
    {
        while (true)
        {
            //e.transform.LookAt (new Vector3 (Enemy.playerReference.transform.position.x, e.transform.position.y, Enemy.playerReference.transform.position.z));
            e.transform.LookAt (Enemy.playerReference.transform.position, Vector3.up);

            yield return new WaitForFixedUpdate ();
        }
        
    }


    protected virtual void onPointReached ()
    {
        if (stateDebugLogging) Debug.Log ($"<color=#ffff00>{e.name}</color> (<color=#ffff00>{e.gameObject.GetInstanceID()}</color>) onPointReached", this);
    }

    protected virtual void onPathComplete ()
    {
        if (stateDebugLogging) Debug.Log ($"<color=#ffff00>{e.name}</color> (<color=#ffff00>{e.gameObject.GetInstanceID ()}</color>)", this);
    }


    [System.Serializable]
    protected class ESF_MovementOptions
    {
        public ESF_MoveAnimationType type;
        public AnimationCurve curve = new AnimationCurve(new Keyframe(0, 0), new Keyframe(1,1));
        [Min(0.1f)] public float t = 1;

        [Tooltip("Vector given to the target you want to move to should you need it in code.")]
        public Vector3 moveTarget = Vector3.zero;

    }
}
