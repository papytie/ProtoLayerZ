﻿using System.Runtime.CompilerServices;
using TMPro;
using Unity.VisualScripting;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Windows;
using static UnityEditor.ShaderGraph.Internal.KeywordDependentCollection;


public class F_PlayerController4 : MonoBehaviour
{
    Controls controls;
    InputAction move;
    InputAction jump;
    InputAction slide;
    InputAction rocket;
    InputAction interact;

    public F_GameManager gm;
    public F_GroundCheck myGroundCheck;
    public ParticleSystem jetpackParticles;
    //public F_SpaceshipController spaceship;

    [SerializeField] private float movementForce =8;
    [SerializeField] private float airDirectControlForce = 8;
    [SerializeField] private float airControlForce = 5;
    [SerializeField] private float slideControlForce = 5;
    [SerializeField] private float jumpForce = 20;
    [SerializeField] private float jetpackForce = 7;
    [SerializeField] private float absoluteVelocityMagnitudeLimit = 150;
    
    [SerializeField] private float hangTime = 0.2f;
    private float hangCounter;
    [SerializeField] private float jumpBufferLength = 0.2f;
    private float jumpBufferCount;

    
    [SerializeField] float jumpForceMagnitudeMultiplier = 0.25f;
    [SerializeField] private PhysicsMaterial2D minFriction;
    [SerializeField] private PhysicsMaterial2D maxFriction;
    [SerializeField] private float veloDividerWhenJumping = 2;
    [SerializeField] private float maxAngleBeforeVeloCorrection = 45;
    [SerializeField] private float maxDistVectorForVeloCorrection = 5;
    [SerializeField] float movementSeepAccel = 2;
    [SerializeField] float canCheckGroundMaxTime = 0.2f;
    public float canCheckGroundCounter = 0;
    [SerializeField] private bool isInAirAndTouchingNonWalkableSlope = false;
    [SerializeField] float velocityMagnitude;
    [SerializeField] AnimationCurve inputInfluenceOnInertia;
    
    /*
    public float xInput;
    public float physicInput;
    public float rocketInput;
    */

    public bool canCheckGround = true;
    public bool canActiveInertia = false;

    public bool isSliding;
    public bool isPhysicMovementActivated;
    public bool isStillControledByPhysic;
    public bool isMoving;
    public bool isJumping;
    public bool isJetpackActivated;
    
    [SerializeField] float outOfPhysicMagnitudeThreshold;
    [SerializeField] float currentThreshold;
   
    public bool canJump;
    public bool tryToJump = false;
    public float currentmovementSpeed = 0;
    public float facingDirection = 1;

    private Vector2 newVelocity;
    private Vector2 newForce;

    private Rigidbody2D rb;
    private CapsuleCollider2D cc;
    private Animator myAnimator;


    //TO DO : CHANGED BY LEVEL
    public float strateGravityScale = 9;
    //strateFriction, 0 means no friction, 1 means full friction
    [SerializeField] float strateFrictionNorm = 1;
    [SerializeField] float minLinearDamping = 0;
    [SerializeField] float maxLinearDamping = 0;
    public float MovementSpeed => movementForce;
    public Vector2 inertiaVelo;


    //ACCESSEURS
    public Rigidbody2D RigidBod => rb;


    private void OnTriggerStay2D(Collider2D collision) {
        //si on est dans les airs, que le délai canCheckGroundCounter est passé et qu'on trigger le sol ET que le closestPoint est Walkable, on peut check le sol
        if(!myGroundCheck.IsGrounded && canCheckGroundCounter <= 0 && ((1 << collision.gameObject.layer) & myGroundCheck.GroundLayer) != 0) {

            Vector2 _closestContactPoint = collision.ClosestPoint(myGroundCheck.transform.position);
            Vector2 _dir = (_closestContactPoint - (Vector2) myGroundCheck.transform.position).normalized;
            RaycastHit2D _hit = Physics2D.Raycast(myGroundCheck.transform.position, _dir, myGroundCheck.CheckedDistance, myGroundCheck.GroundLayer);
            if(_hit && Vector2.Angle(_hit.normal, Vector2.up) >= myGroundCheck.MaxGroundAngle) {
                isInAirAndTouchingNonWalkableSlope = true;
            } else {
                isInAirAndTouchingNonWalkableSlope = false;
                canCheckGround = true;
                canCheckGroundCounter = canCheckGroundMaxTime;
            }
        }
    }
   


    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        myAnimator = GetComponent<Animator>();
        cc = GetComponentInChildren<CapsuleCollider2D>();
        myGroundCheck = GetComponentInChildren<F_GroundCheck>();

        rb.gravityScale = strateGravityScale;       
    }

  
    private void Update() {
        
        velocityMagnitude = rb.linearVelocity.magnitude;

        CheckJump();
        //CheckInput();
        CheckFlip();

        if(!myGroundCheck.IsGrounded && !myGroundCheck.IsGroundOverlaped || isInAirAndTouchingNonWalkableSlope || isJetpackActivated) {
            canCheckGround = false;
        }

        if(canCheckGround) {
            myGroundCheck.CheckGroundEnabled = true;
        } else {
            myGroundCheck.CheckGroundEnabled = false;
        }

        //OTHER STUFF
        //Debug.Log("Y VELO = " + rb.linearVelocity.y);
        //Debug.Log("canCheckGround = " + canCheckGround);
        if(rb.linearVelocity.y <= 0.0f || canCheckGround || /*myGroundCheck.IsGrounded ||*/ isJetpackActivated) {
            isJumping = false;
        }

        if(hangCounter > 0 && !isJumping && myGroundCheck.IsGrounded) {
            canJump = true;
        }else if(!myGroundCheck.IsGrounded) {
            canJump = false;
        }


        UpdateCounters();
        CheckState();

   
    }

    private void FixedUpdate() {

        //CheckJump();
        UpdatePhysicParams();
        
        ApplyMovement();

        UpdateVisual();
    }



    //TRIGGER BY INPUT
    public void TryToJump(InputAction.CallbackContext _context) {

        //Reset jump counter
        jumpBufferCount = jumpBufferLength;
        tryToJump = true;
    }

    public void TryToInteract(InputAction.CallbackContext _context) {

        //si autre interaction possible, faire la plus proche en priorisant les autres interactions par rapport a embarquer

        if(gm.PlayerSpaceship.CanEnterSpaceship) {
            gm.OnShipEnter.Invoke();
        }
    }


    //UPDATE

    private void CheckFlip() {

        float _signInput = Mathf.Sign(gm.Inputs.CharInputX);

        if(!isStillControledByPhysic && gm.Inputs.CharInputX != 0 && facingDirection != _signInput) {
            facingDirection = _signInput;
            transform.Rotate(0.0f, 180.0f, 0.0f);
            return;
        }

        float _signVeloX = Mathf.Sign(rb.linearVelocity.x);

        if(isStillControledByPhysic && _signVeloX != 0 && facingDirection != _signVeloX) {
            facingDirection = _signVeloX;
            transform.Rotate(0.0f, 180.0f, 0.0f);
            return;
        }

    }


    private void UpdateCounters() {

        //CAN CHECK GROUND COUNTER
        if(!canCheckGround) {
            canCheckGroundCounter -= Time.deltaTime;
        }

        //HANG JUMP COUNTER
        if(myGroundCheck.IsGrounded) {
            hangCounter = hangTime;
        } else {
            hangCounter -= Time.deltaTime;
        }

        //JUMP BUFFER COUNTER
        if(jumpBufferCount > 0) {
            jumpBufferCount -= Time.deltaTime;
        }

      
    }


    private void CheckState() {

        ////// //isMoving /////
        isMoving = rb.linearVelocity.magnitude >= 0.01f ? true : false;



        ////// //isControledByPhysic ////
        if(gm.Inputs.CharPhysicInput != 0 && myGroundCheck.IsGrounded ) { //|| (rocketInput !=0)
            isPhysicMovementActivated = true;
        } else {
            isPhysicMovementActivated = false;
        }

        ///// isStillControledByPhysic ////
        if(isPhysicMovementActivated) {
            isStillControledByPhysic = true;
        }else if((!isPhysicMovementActivated && rb.linearVelocity.magnitude <= outOfPhysicMagnitudeThreshold) || isJetpackActivated) { //mettre un compteur de jetpack a dépasser pour désactiver la physic
            isStillControledByPhysic = false;
        }


        ////// isSliding /////
        if(myGroundCheck.IsGrounded && isStillControledByPhysic) {
            isSliding = true;
        } else {
            isSliding  = false;
        }

        ////// isJetpackActivated /////
        if(gm.Inputs.CharRocketInput != 0) {
            isJetpackActivated = true;
        } else {
            isJetpackActivated = false;
        }
  
    }



  //FIXED UPDATE
    private void CheckJump() {

        if(jumpBufferCount <= 0) {
            tryToJump = false;
        }

        //hangCounter > 0 && !isJumping && myGroundCheck.IsGrounded=> canJump
        if(tryToJump && canJump) {
            canJump = false;
            isJumping = true;
            canCheckGround = false;
            myGroundCheck.CheckGroundEnabled = false;
            tryToJump = false;

            //simplifiable
            //newVelocity.Set(rb.linearVelocity.x/ veloDividerWhenJumping, rb.linearVelocity.y/ veloDividerWhenJumping);
            //rb.linearVelocity = newVelocity;

            //changer la direction du jump selon le cas
            Vector2 _jumpDirection;
            if(rb.linearVelocity.y >= 0) {
                //Debug.Log("on va vers le haut");
                _jumpDirection = Vector2.up;
            } else {                
                if(isStillControledByPhysic || isSliding) {
                    //Debug.Log("on va vers l'avant");
                    float _angle = Vector2.Angle(myGroundCheck.GroundHitResult.normal, Vector2.up);
                    _jumpDirection = myGroundCheck.RotateVector(myGroundCheck.GroundHitResult.normal, _angle / 2);
                } else {
                    _jumpDirection = Vector2.up;
                }
                
            }

            newForce = _jumpDirection * (jumpForce + rb.linearVelocity.magnitude * jumpForceMagnitudeMultiplier);
            
            rb.AddForce(newForce, ForceMode2D.Impulse);
            //newVelocity = rb.linearVelocity;


            //Debug.Log("JUMP newVelocity = " + newVelocity);
        }
    }
  

    private void UpdatePhysicParams() {

        //PHYSIC MAT
        if(myGroundCheck.IsGrounded && !isStillControledByPhysic && gm.Inputs.CharInputX == 0 && !isJumping) {
            rb.sharedMaterial = maxFriction;
        } else {
            rb.sharedMaterial = minFriction;
        }

        //GRAVITY + linear damping
        if(myGroundCheck.IsGrounded && !isStillControledByPhysic && !isJumping) {
            rb.gravityScale = 0;
        } else {
            rb.gravityScale = strateGravityScale;
        }

        //LINEAR DAMPING
        if(isPhysicMovementActivated && myGroundCheck.IsGrounded) {
            rb.linearDamping = minLinearDamping;
        } else {
            rb.linearDamping = maxLinearDamping;
        }
    }

    public bool alreadySavedThisFrame = false;
    public float velocityXPreviousFrame;
    public bool canUpdateDeltaV;
    public float deltaV = 0;
    private void ApplyMovement() {


        // WALK MOVEMENT => full contrôle
        if(myGroundCheck.IsGrounded && !isJumping && !isStillControledByPhysic! && !isSliding && !isJetpackActivated) {
            //Debug.Log("SOL MOOV");
            currentmovementSpeed = movementForce;
            Vector2 _walkDirection = -Vector2.Perpendicular(myGroundCheck.GroundHitResult.normal).normalized;

            newVelocity.Set(movementForce * _walkDirection.x * gm.Inputs.CharInputX, movementForce * _walkDirection.y * gm.Inputs.CharInputX);
            Debug.DrawRay(transform.position, newVelocity, Color.cyan);
            float _dir = Mathf.Sign(gm.Inputs.CharInputX) != 0 ? Mathf.Sign(gm.Inputs.CharInputX) : 0;

            //FIX Décallage avec le sol           
            if(myGroundCheck.IsGroundedButFarFromGround) {
                Vector2 _veloToDown = Vector2.down * (rb.linearVelocity.magnitude + 1);
                newVelocity = newVelocity + _veloToDown;
                //Debug.Log("FIX GROUND DECALAGE");
            }

            //APPLY VELOCITY
            //rb.linearVelocity = newVelocity;
            rb.linearVelocity = newVelocity.normalized * Mathf.Clamp(newVelocity.magnitude, 0, absoluteVelocityMagnitudeLimit);
            return;
        }



        //MIXED PHYSIC AND DIRECT CONTROL

        //AERIAL X MOVEMENT
        if(!myGroundCheck.IsGrounded) {
            

            if(Mathf.Abs(rb.linearVelocity.x) <= airDirectControlForce * Mathf.Abs(gm.Inputs.CharInputX) && !isJetpackActivated) { // !isJetPackActivated enlevable selon préférence
                //Debug.Log("direct control");


                float _forceToAddWithInput = airDirectControlForce * gm.Inputs.CharInputX;

                //fix grimper sur des pentes non praticable juste avec ce mouvement
                RaycastHit2D _castFront = Physics2D.Raycast(myGroundCheck.transform.position, transform.right, 0.6f, myGroundCheck.GroundLayer);
                Debug.DrawRay(myGroundCheck.transform.position, transform.right * 0.6f, Color.gray);
                if(_castFront && Vector2.Angle(_castFront.normal, Vector2.up) > myGroundCheck.MaxGroundAngle) {
                    //fix, on est en train d'essayer de grimper une mauvaise pente => pas de mouvement
                    //Debug.Log("FIX BAD ANGLE");
                } else {
                    //mouvement normal
                    newVelocity.Set(_forceToAddWithInput, rb.linearVelocity.y);
                    rb.linearVelocity = newVelocity;
                }
                
            } else {

                //Debug.Log("physic Control");
                float _forceFactor = Mathf.Abs(rb.linearVelocity.x);
                float _forceToAdd = gm.Inputs.CharInputX * airControlForce / (1f + Mathf.Log(_forceFactor + 1f));                                                                                      //Debug.Log("AIR _addedForce = " + _forceToAdd);
                rb.AddForceX(_forceToAdd, ForceMode2D.Force);
            }

        }



        // PHYSIC MOVEMENTS

        //ONLY PHYSIC MOVEMENT
        if(isPhysicMovementActivated && !isJetpackActivated) {
            //Debug.Log("Only physic move player");
            return;
        }

        // SLIDE MOVEMENT
        if(isSliding) {
            
            //Debug.Log("SLIDE MOOV");
            // Calculate the force to apply based on current velocity with logarithmic scaling
            float _forceFactor = Mathf.Abs(rb.linearVelocity.x);
            float _forceToAdd = gm.Inputs.CharInputX * slideControlForce / (1f + Mathf.Log(_forceFactor + 1f)); // Logarithmic scaling
            //Debug.Log("SLIDE _addedForce = " + _forceToAdd);

            Vector2 _slideDirection = -Vector2.Perpendicular(myGroundCheck.GroundHitResult.normal).normalized;
            Vector2 _veloToAdd = _slideDirection * _forceToAdd;
            rb.AddForce(_veloToAdd, ForceMode2D.Force);
        }

    
        //JETPACK MOVEMENT
        if(isJetpackActivated) {

            //Debug.Log("JET MOOV");
            float _forceFactor = Mathf.Abs(rb.linearVelocity.y);
            float _forceToAdd = jetpackForce / (1f + Mathf.Log(_forceFactor + 1f)); // ajouter rocketInput* pour pouvoir faire le jetpack vers le bas
            //Debug.Log("JET _addedForce = " + _forceToAdd);
            rb.AddForceY(_forceToAdd, ForceMode2D.Force);
        }


    }

 
    private void UpdateVisual() {
        myAnimator.SetFloat(SRAnimators.Animator_Hero1.Parameters.xVelocity, Mathf.Round(rb.linearVelocity.x));
        myAnimator.SetFloat(SRAnimators.Animator_Hero1.Parameters.yVelocity, Mathf.Round(rb.linearVelocity.y));
        myAnimator.SetBool(SRAnimators.Animator_Hero1.Parameters.isGrounded, myGroundCheck.IsGrounded);
        myAnimator.SetBool(SRAnimators.Animator_Hero1.Parameters.isSliding, isPhysicMovementActivated);
        myAnimator.SetBool(SRAnimators.Animator_Hero2.Parameters.isMoving, isMoving);

        ParticleSystem.EmissionModule _jetpackEmissionModule = jetpackParticles.emission;
        ParticleSystem.ShapeModule _jetpackShapeModule = jetpackParticles.shape;
        _jetpackEmissionModule.enabled = isJetpackActivated;
        _jetpackShapeModule.rotation = gm.Inputs.CharRocketInput < 0 ? new Vector3(0,0, 101.25f) : new Vector3(0, 0, -101.25f);
    }



    private void OnDrawGizmos() {
        
        if(!Application.isPlaying)return;

        Gizmos.color = Color.green;
        Vector3 _newPos2 = transform.position + transform.up * 0.5f;
        Gizmos.DrawRay(_newPos2, rb.linearVelocity);



        Gizmos.color = Color.magenta;
        if(rb.linearVelocity.y >= 0) {
            Gizmos.DrawRay(transform.position, Vector2.up);
        } else {
            if(isStillControledByPhysic || isSliding) {
                float _angle = Vector2.Angle(myGroundCheck.GroundHitResult.normal, Vector2.up);
                Gizmos.DrawRay(transform.position, myGroundCheck.RotateVector(myGroundCheck.GroundHitResult.normal, _angle / 2));
            } else {
                Gizmos.DrawRay(transform.position, Vector2.up);
            }

        }

    }
       
}
