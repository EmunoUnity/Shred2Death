using UnityEngine.InputSystem;
using UnityEngine;


//README
// This script affects rotation values for two transforms. The parent transform is used for slope orientation, while
// the child transform is used for turning the player, and it's also used to get the player's correct forward direction, which is used for the forward force,
// which is applied to the RB. as affected by the parent transform. This is done to achieve the desired effect of the player model
// rotating perpendicularly to the slope, while the player's forward direction is based on the parent transform's
// forward direction.


public class PlayerBase : MonoBehaviour
{
    // Components
    private Rigidbody rb;
    [SerializeField] private Transform playerModel;
    
    // Movement values
    [Header("Movement Values")]
    [SerializeField] float baseMovementSpeed;

    private float movementSpeed;
    [Range(0, 1)]
    [SerializeField] float deAccelerationSpeed;
    [SerializeField] float turnSharpness;
    [SerializeField] float jumpForce;
    
    //Slope values
    [Header("Slope Values")]
    
    [SerializeField] private float orientToSlopeSpeed;
    [SerializeField] private float slopeDetectionDistance;
    [Tooltip("The distance from the center of the player to the left and right raycast origins. These are used to detect the slope.")]
    [SerializeField] private float slopeRayOffsetFromMid;
    [Tooltip("X is min, Y is max. If the slope is within this range, the player will not be able to exert a forward force. Used for preventing the player from using forward force up slopes that are too steep")]
    [SerializeField] private Vector2 slopeRangeWherePlayerCantMove;

    [SerializeField] private float slopedUpSpeedMultipler, slopedDownSpeedMultipler;
    
    [Header("Ground Detection")]
    [SerializeField] private Transform boxPos;
    [SerializeField] private Vector3 boxSize;
    
    //state machine
    private PlayerStateMachine stateMachine;
    //concrete states
    public PlayerSkatingState skatingState;
    public PlayerAirborneState airborneState;
    public PlayerHalfpipeState halfPipeState;
    
    
    
    float jumpInput;

    private void Awake()
    {
        
        rb = GetComponent<Rigidbody>();
        StateMachineSetup();
            
    }

    private void StateMachineSetup()
    {
        stateMachine = new PlayerStateMachine();
        skatingState = new PlayerSkatingState(this, stateMachine);
        airborneState = new PlayerAirborneState(this, stateMachine);
        halfPipeState = new PlayerHalfpipeState(this, stateMachine);
        stateMachine.Init(skatingState);
    }

    private void OllieJump(InputAction.CallbackContext ctx)
    {
        Debug.Log("jump");
        if (CheckGround()) rb.AddRelativeForce(transform.up * jumpForce, ForceMode.Impulse);
    }
    
    private void OnEnable()
    {
        InputRouting.Instance.input.Player.Jump.performed += ctx => OllieJump(ctx);
    }

    private void OnDisable()
    {
        InputRouting.Instance.input.Player.Jump.performed -= ctx => OllieJump(ctx);
    }

    private void Update()
    {
        stateMachine.currentState.LogicUpdate();
    }
    
    private void CalculateSpeedVector()
    {
        float offset = rb.velocity.y;
        
        if (rb.velocity.y > 0)
        {
            offset = -rb.velocity.y * slopedUpSpeedMultipler;
        }
        else if (rb.velocity.y < 0)
        {
            offset = rb.velocity.y / slopedDownSpeedMultipler;
        }
        // Get the rotation around the x-axis, ranging from -90 to 90
        
        movementSpeed = baseMovementSpeed + offset;
        //Debug.Log(movementSpeed);
    }
    
    private void FixedUpdate()
    {
        stateMachine.currentState.PhysicsUpdate();
    }

    public void HalfPipeAirBehaviour()
    {
        rb.velocity = new Vector3(0, rb.velocity.y, 0);
    }
    
    public void SkateForward()
    {
        CalculateSpeedVector();
        
        float xRotation = TranslateEulersToRange180(transform.rotation.eulerAngles.x);
        float zRotation = TranslateEulersToRange180(transform.rotation.eulerAngles.z);
        
        
        if (Mathf.Abs(xRotation) > slopeRangeWherePlayerCantMove.x &&
            Mathf.Abs(xRotation) < slopeRangeWherePlayerCantMove.y) return;
        if (Mathf.Abs(zRotation) > slopeRangeWherePlayerCantMove.x  &&
            Mathf.Abs(zRotation) < slopeRangeWherePlayerCantMove.y) return;
        
        rb.AddForce(playerModel.forward * (movementSpeed * InputRouting.Instance.GetMoveInput().y), ForceMode.Acceleration); // Only adds force if
                                                                                                  // the player is not
                                                                                                  // on a slope that is
                                                                                                  // too steep.
    }

    /// <summary>
    /// Translates eulerAngles from 0 - +360, to -180 - +180. Makes eulerAngles easier to work with, logically.
    /// Rotations should never be applied with this method, as it will cause weirdness. This is simply for getting
    /// eulerAngle values in a range that makes sense.
    /// </summary>
    private float TranslateEulersToRange180(float eulerAngle)
    {
        return eulerAngle > 180 ? eulerAngle - 360 : eulerAngle;
    }
    
    /// <summary>
    /// Handles turning the player model with left and right input. Rotating the player works best for the movement we
    /// are trying to achieve, as movement is based on the player's forward direction. Meant to be used in FixedUpdate.
    /// </summary>
    public void TurnPlayer() // Rotates the PLAYER MODEL TRANSFORM. We must work with 2 transforms to achieve the desired effect.
    {
       playerModel.transform.Rotate(0, turnSharpness * InputRouting.Instance.GetMoveInput().x * Time.fixedDeltaTime, 0, Space.Self);
    }

    RaycastHit leftSlopeHit, rightSlopeHit;
    public void OrientToSlope()
    {
        // Define points on either side of the skateboard
        Vector3 leftRayOrigin = transform.position - transform.forward * slopeRayOffsetFromMid;
        Vector3 rightRayOrigin = transform.position + transform.forward * slopeRayOffsetFromMid;

        // Perform raycasts from the defined points
        bool leftHit = Physics.Raycast(leftRayOrigin, -transform.up, out leftSlopeHit, slopeDetectionDistance, 1 << LayerMask.NameToLayer("Ground"));
        bool rightHit = Physics.Raycast(rightRayOrigin, -transform.up, out rightSlopeHit, slopeDetectionDistance, 1 << LayerMask.NameToLayer("Ground"));
        
        if (leftHit && rightHit)
        {
            Vector3 averageNormal = (leftSlopeHit.normal + rightSlopeHit.normal).normalized;

            // stores perpendicular angle into targetRotation
            Quaternion targetRotation = Quaternion.FromToRotation(transform.up, averageNormal) * transform.rotation;
            
            // Lerp to the desired rotation
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.fixedDeltaTime * orientToSlopeSpeed);
        }
    }

    /// <summary>
    /// Slowly re-orients the player mid-air to be upright. Meant to be used in FixedUpdate.
    /// </summary>
    public void ReOrient()
    {
        Quaternion targetRotation = Quaternion.FromToRotation(transform.up, Vector3.up) * transform.rotation;
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.fixedDeltaTime * 1f);
    }
    
    /// <summary>
    /// De-accelerates the player by a fixed value. As long as the de-acceleration value is less than the acceleration
    /// value, the desired effect will work properly. Meant to be used in FixedUpdate.
    /// </summary>
    public void DeAccelerate() // Add Force feels too floaty, used on every frame to counteract the force.
    {
        rb.velocity = Vector3.Lerp(rb.velocity, new Vector3(0, rb.velocity.y, 0), deAccelerationSpeed);
    }

    public bool CheckGround()
    {
        return Physics.CheckBox(boxPos.position, boxSize, transform.rotation, 1 << LayerMask.NameToLayer("Ground"));
    }
    
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position - transform.forward * slopeRayOffsetFromMid, leftSlopeHit.point);
        Gizmos.DrawLine(transform.position + transform.forward * slopeRayOffsetFromMid, rightSlopeHit.point);
        Gizmos.DrawWireCube(boxPos.position, boxSize/2);

    }

    private void OnTriggerEnter(Collider other)
    {
        stateMachine.currentState.StateTriggerEnter(other);
        
    }

    private void OnTriggerStay(Collider other)
    {
        stateMachine.currentState.StateTriggerStay(other);
    }

    private void OnTriggerExit(Collider other)
    {
        stateMachine.currentState.StateTriggerExit(other);
    }
}
