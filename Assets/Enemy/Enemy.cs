using System;
using System.Collections;
using System.Collections.Generic;
using Interfaces;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;

[SelectionBase]
[RequireComponent(typeof(Enemy_StateMachine))]
public class Enemy : MonoBehaviour, IDamageable, ITrickOffable
{
    #region SCRIPT VARIABLES
    #region Game Objects
    public static SetPlayerReference playerReference;

    public Rigidbody rb;
   

    public Enemy_StateMachine stateMachine;

    public GameObject sensorsObject;

    public GameObject bodyObject;
    public Animator animator;
    #endregion

    #region Enemy Stats
    public int maxHealth { get; private set; }
    public int health;
    bool isDead = false;
    #endregion

    [Header ("Components")]
    WaveManager waveManager; //Set by waveManager when the enemy object is instantiated
    
    #endregion

    #region SETUP
    void Awake ()
    {
        EnemyGetComponentReferences ();
    }

    /// <summary>
    /// Gets the basic components of the enemy class to reduce boilerplate code. Future iterations of enemies should extend this function as well to provide for the next inherited enemy type.
    /// </summary>
    protected virtual void EnemyGetComponentReferences ()
    {
        stateMachine = GetComponent<Enemy_StateMachine> ();
        rb = GetComponent<Rigidbody> ();

        //muzzleObject = transform.Find ("Body/MuzzlePoint").gameObject;

        bodyObject = transform.Find ("Body").gameObject;
        sensorsObject = transform.Find ("Sensors").gameObject;

        animator = bodyObject.GetComponent<Animator> ();

        GetRagdollComponents ();

        maxHealth = health;
        //Debug.Log ($"{health} / {maxHealth}");
    }

    //After it's spawned, the static variable for agentSettings should exist.
    private void Start ()
    {
        SetRagdollEnabled (false);
    }

    public void SetManager (WaveManager w)
    {
        waveManager = w;
    }

    #endregion

    #region SCRIPT FUNCTIONS

    public void TakeDamage (float damage)
    {        
        health -= Mathf.FloorToInt(damage);

        if (health <= 0 && !isDead) 
        {
            isDead = true;
            stateMachine.aiUpdateEnabled = false;

            rb.detectCollisions = false;
            GetComponent<Collider> ().enabled = false;

            DissolvingController d = bodyObject.GetComponent<DissolvingController>();
            d.StartCoroutine (d.Dissolve ());

            if (stateMachine.stateCurrent != stateMachine.statesObject.GetComponent<ES_Ragdoll>())
                stateMachine.transitionState (stateMachine.statesObject.GetComponent<ES_Ragdoll> ());
        }
    }

    public void DeathFinished ()
    {
        //Debug.Log ("Enemy Dead");
        try
        {
            waveManager.removeEnemy ();
        }
        catch
        {
            Debug.LogWarning ($"{name} ({GetInstanceID()}) could not be removed from a waveManager");
        }
        finally
        {
            Destroy (gameObject);
        }
    }

    public void TrickOffEvent (Vector3 playerVel)
    {
        stateMachine.transitionState (stateMachine.statesObject.GetComponent<ES_Bonk> ());
    }

    #endregion

    #region RAGDOLL PHYSICS
    

    Collider enemyCollider;
    Rigidbody enemyRigidbody;

    /// <summary>
    /// TODO: Store the position and rotation of each of the bones
    /// </summary>
    public Vector3[] ragdollResetPosition { get; private set; }
    public Quaternion[] ragdollResetRotation { get; private set; }
    public Collider[] ragdollColliders { get; private set; }
    public Rigidbody[] ragdollBodies { get; private set; }

    void GetRagdollComponents ()
    {
        enemyCollider = GetComponent<Collider> ();
        enemyRigidbody = GetComponent<Rigidbody> ();

        ragdollBodies = bodyObject.GetComponentsInChildren<Rigidbody> ();
        ragdollColliders = bodyObject.GetComponentsInChildren<Collider> ();

        ragdollResetPosition = new Vector3[ragdollBodies.Length];
        ragdollResetRotation = new Quaternion[ragdollBodies.Length];

        for (int i = 0; i < ragdollBodies.Length; i++)
        {
            //Debug.Log (ragdollBodies[i].name);
            ragdollResetPosition[i] = ragdollBodies[i].transform.localPosition;
            ragdollResetRotation[i] = ragdollBodies[i].transform.localRotation;
        }


        //Debug.Log ($"{ enemyCollider} { enemyRigidbody} {ragdollBodies} {ragdollColliders}");

    }

    public virtual void SetRagdollEnabled (bool en)
    {
        animator.enabled = !en;

        enemyCollider.enabled = false;

        foreach (Rigidbody rigidbody in ragdollBodies)
        {
            //Debug.Log (rigidbody);
            rigidbody.isKinematic = !en;
        }
        foreach (Collider collider in ragdollColliders)
        {
            //Debug.Log (collider);
            collider.enabled = en;
        }
        enemyCollider.enabled = !en;

        //When reenabling the ragdoll return each of the limbs to their original position and rotation
        for (int i = 0; i < ragdollBodies.Length; i++)
        {
            ragdollBodies[i].transform.localPosition = ragdollResetPosition[i];
            ragdollBodies[i].transform.localRotation = ragdollResetRotation[i];
        }

    }
    #endregion

    #region UNITY FUNCTIONS
    void Update ()
    {
        if (stateMachine.enabled)
        {
            stateMachine.machineUpdate();
        }
    }

    private void FixedUpdate ()
    {
        if (stateMachine.enabled)
        {
            stateMachine.machinePhysics();
        }

    }
    #endregion

}
