using System;
using System.Collections;
using UnityEngine;

public class PlayerMovementMethods
{
    private Rigidbody rb;
    private PlayerData playerData;
    private PlayerBase player;
    private Transform inputTurningTransform;
    private float movementSpeed;
    public float turnSharpness { get; private set; }
    
    private bool burnCooldownActive;
    float turnSmoothVelocity;
    private float timeElapsed;
    public bool currentlyBoosting;
    private bool autoMove;
    
    public PlayerMovementMethods(PlayerBase player, Rigidbody rb, PlayerData playerData, Transform inputTurningTransform)
    {
        this.rb = rb;
        this.playerData = playerData;
        this.player = player;
        this.inputTurningTransform = inputTurningTransform;
        
    }
    
    /// <summary>
    /// Will exert a force forward if the player's slope isn't too steep. Meant to be used in FixedUpdate.
    /// </summary>
    
    public void SkateForward()
    {
        CalculateCurrentSpeed();
        
        Vector2 maxSlopeRange = new Vector2(playerData.slopeRangeWherePlayerCantMove.x + 90, playerData.slopeRangeWherePlayerCantMove.y + 90);
        
        // calculates the angle between the player's forward direction and the world's down direction
        float angleWithDownward = player.GetOrientationWithDownward();

        //Debug.Log(angleWithDownward);

        bool isFacingUpward = angleWithDownward.IsInRangeOf(maxSlopeRange.x, maxSlopeRange.y);
        
        if (isFacingUpward) return;
        
        Quaternion localTargetRotation = Quaternion.Inverse(player.transform.rotation) * player.GetOrientationHandler().targetRotation;
        // we inverse the player's rotation to get the rotation difference between the player's current rotation and the target rotation
        
        
        // Use localTargetRotation to get the local rotation and use forward to set the direction
        Vector3 forwardAfterRotation = localTargetRotation * inputTurningTransform.forward;
        Debug.DrawRay(player.transform.position, forwardAfterRotation, Color.magenta);
        
        
        // Apply force in the direction of forwardAfterRotation
        rb.AddForce(player.transform.forward * movementSpeed, ForceMode.Acceleration);
        
        //stop local horizontal forces by setting the local x and z velocity to 0. need to convert world velocity to local
        //velocity to do this
        rb.SetLocalAxisVelocity(Vector3.right, 0);
        
        
    }

    public void OllieJump(float overrideForce = 0)
    {
        if (player != null) rb.AddRelativeForce(player.transform.up * playerData.baseJumpForce, ForceMode.Impulse);
    }
    
    /// <summary>
    /// Handles turning the player model with left and right input. Rotating the player works best for the movement we
    /// are trying to achieve, as movement is based on the player's forward direction. Meant to be used in FixedUpdate.
    /// </summary>
    public void TurnPlayer() // Rotates the input turning transform
    {
        /*if (overrideTurnSharpness)
        {
            player.inputTurningTransform.Rotate(0,
                newTurnSharpness * InputRouting.Instance.GetMoveInput().x, 
                0, Space.Self);
        }
        else
        {
            player.transform.Rotate(0,
                turnSharpness * InputRouting.Instance.GetMoveInput().x * Time.fixedDeltaTime, 
                0, Space.Self);
        }*/ //keeping this comment here as a reference to what turning used to look like. Drift state needs this, so im
        //keeping this here as an example of how it was when the drift state was being implemented.
        
        CalculateTurnSharpness();
        float turnSmoothTime = .5f;
        
        
        Vector3 targetDirection =
            new Vector3(InputRouting.Instance.GetMoveInput().x, 0, InputRouting.Instance.GetMoveInput().y);
        float targetAngle = Mathf.Atan2(targetDirection.x, targetDirection.z) * Mathf.Rad2Deg + player.GetPlayerCamera().transform.eulerAngles.y;
        float angle = Mathf.SmoothDampAngle(player.transform.eulerAngles.y, targetAngle, ref turnSmoothVelocity, turnSharpness);
        
        if (targetDirection == Vector3.zero) return;
        player.transform.rotation = Quaternion.Euler(player.transform.eulerAngles.x, angle, player.transform.eulerAngles.z);
        
    }

    public void DoBurnForce(Vector3 contactPoint, float dmg, bool keepHozForces = false)
    {
        if (burnCooldownActive) return;
        Debug.Log("burn dmg");
        
        player.GetComponentInChildren<IDamageable>()?.TakeDamage(dmg);
        
        player.StartCoroutine(BurnForceTimer());
        
        // Calculate the direction from the player to the point of collision
        Vector3 collisionDirection = contactPoint - player.transform.position;
        //transform.rotation = Quaternion.LookRotation(new Vector3(transform.rotation.x, collisionDirection.y, transform.rotation.z));

        // Normalize the direction
        collisionDirection = collisionDirection.normalized;
        if (keepHozForces)
        {
            rb.velocity = Vector3.zero;
            rb.AddForce(new Vector3(collisionDirection.x, playerData.extraBurnVerticalForce , collisionDirection.z) * playerData.burnForce, ForceMode.Impulse);
            return;
        }

        // Apply a force in the opposite direction of the collision
        rb.velocity = Vector3.zero;
        rb.AddForce(new Vector3(-collisionDirection.x, playerData.extraBurnVerticalForce , -collisionDirection.z) * playerData.burnForce, ForceMode.Impulse);
    }
    
    private IEnumerator BurnForceTimer()
    {
        burnCooldownActive = true;
        yield return new WaitForSeconds(playerData.burnBounceCooldown);
        burnCooldownActive = false;
    }

    public void ToggleAutoMove(bool state)
    {
        autoMove = state;
    }
    
    private void CalculateCurrentSpeed() 
    {
        timeElapsed = Mathf.Clamp01(timeElapsed);
        
        if (InputRouting.Instance.GetMoveInput().magnitude > 0)
        {
            timeElapsed += Time.deltaTime;
        } else timeElapsed = 0;
        
        float offset = rb.velocity.y;
        Func<float, float> calculateExtraForce = (slopeMultiplier) =>
            -(player.GetOrientationWithDownward() - 90) * slopeMultiplier; // this is a negative so if we are going
                                                                           // down, we add force, if we are going up,
                                                                           // we decrease force
        if (rb.velocity.y > 0)
        {
            offset = calculateExtraForce(playerData.slopedUpSpeedMult);
        }
        else if (rb.velocity.y < 0)
        {
            offset = calculateExtraForce(playerData.slopedDownSpeedMult);
        }
        // Get the rotation around the x-axis, ranging from -90 to 90
        
        //movementSpeed = baseSpeed + offset;

        if (currentlyBoosting)
        {
            return;
        }
        
        movementSpeed = Mathf.Lerp(playerData.minSpeed, playerData.baseMovementSpeed, 
            autoMove ? 1 : playerData.accelerationCurve.Evaluate(InputRouting.Instance.GetMoveInput().magnitude)) + offset;
    }

    public void CalculateTurnSharpness()
    {
        float t = rb.velocity.magnitude / playerData.speedMagnitudeThresholdForMaxTurnSharpness;
        turnSharpness = Mathf.Lerp(playerData.minMaxTurnSharpness.x, playerData.minMaxTurnSharpness.y, 
            playerData.turnSharpnessCurve.Evaluate(t));
        
    }
    
    /// <summary>
    /// De-accelerates the player by a fixed value. As long as the de-acceleration value is less than the acceleration
    /// value, the desired effect will work properly. Meant to be used in FixedUpdate.
    /// </summary>
    public void DeAccelerate() // Add Force feels too floaty, used on every frame to counteract the force.
    {
        rb.velocity = Vector3.Lerp(rb.velocity, new Vector3(0, rb.velocity.y, 0), playerData.deAccelerationSpeed);
    }
    
    public void SetMovementSpeed(float speed)
    {
        movementSpeed = speed;
    }
    


    
}
