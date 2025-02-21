using System.Runtime.CompilerServices;
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

    public F_GroundCheck myGroundCheck;
    public ParticleSystem jetpackParticles;

    [SerializeField] private float movementForce =8;
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
    

    public float xInput;
    public float physicInput;
    public float rocketInput;

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

    public Rigidbody2D rb;
    private CapsuleCollider2D cc;
    private Animator myAnimator;


    //TO DO : CHANGED BY LEVEL
    public float strateGravityScale = 9;
    //strateFriction, 0 means no friction, 1 means full friction
    [SerializeField] float strateFrictionNorm = 1;
    public float MovementSpeed => movementForce;
    public Vector2 inertiaVelo;

    private void OnTriggerStay2D(Collider2D collision) {
        //si on est dans les airs, que le délai canCheckGroundCounter est passé et qu'on trigger le sol ET que le closestPoint est Walkable, on peut check le sol
        if(!myGroundCheck.IsGrounded && canCheckGroundCounter <= 0 && ((1 << collision.gameObject.layer) & myGroundCheck.GroundLayer) != 0) {

            //Debug.Log("CACA");
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
   

    private void Awake() {
        controls = new Controls();
        move = controls.MainCharacterMap.Walking;
        jump = controls.MainCharacterMap.Jumping;
        slide = controls.MainCharacterMap.Sliding;
        rocket = controls.MainCharacterMap.Rocket;

        move.Enable();
        jump.Enable();
        slide.Enable();
        rocket.Enable();

        jump.performed += TryToJump;
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
        CheckInput();
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
    void TryToJump(InputAction.CallbackContext _context) {

        //Reset jump counter
        jumpBufferCount = jumpBufferLength;
        tryToJump = true;

    }

    //UPDATE
    private void CheckInput() {
        xInput = move.ReadValue<float>();
        physicInput = slide.ReadValue<float>();
        rocketInput = rocket.ReadValue<float>();
    }

    private void CheckFlip() {

        float _signInput = Mathf.Sign(xInput);

        if(!isStillControledByPhysic && xInput != 0 && facingDirection != _signInput) {
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
        if(physicInput != 0 || rocketInput !=0) {
            isPhysicMovementActivated = true;
        } else {
            isPhysicMovementActivated = false;
        }

        ///// isStillControledByPhysic ////
        if(isPhysicMovementActivated) {
            isStillControledByPhysic = true;
        }else if(!isPhysicMovementActivated && rb.linearVelocity.magnitude <= outOfPhysicMagnitudeThreshold) {
            isStillControledByPhysic = false;
        }


        ////// isSliding /////
        if(myGroundCheck.IsGrounded && isStillControledByPhysic) {
            isSliding = true;
        } else {
            isSliding  = false;
        }

        ////// isJetpackActivated /////
        if(rocketInput != 0) {
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
            newVelocity.Set(rb.linearVelocity.x/ veloDividerWhenJumping, rb.linearVelocity.y/ veloDividerWhenJumping);
            rb.linearVelocity = newVelocity;

            //changer la direction du jump selon le cas
            Vector2 _jumpDirection;
            if(rb.linearVelocity.y >= 0) {
                Debug.Log("on va vers le haut");
                _jumpDirection = Vector2.up;
            } else {                
                if(isStillControledByPhysic || isSliding) {
                    Debug.Log("on va vers l'avant");
                    float _angle = Vector2.Angle(myGroundCheck.GroundHitResult.normal, Vector2.up);
                    _jumpDirection = myGroundCheck.RotateVector(myGroundCheck.GroundHitResult.normal, _angle / 2);
                } else {
                    _jumpDirection = Vector2.up;
                }
                
            }

            newForce = _jumpDirection * (jumpForce + rb.linearVelocity.magnitude * jumpForceMagnitudeMultiplier);
            
            rb.AddForce(newForce, ForceMode2D.Impulse);
            newVelocity = rb.linearVelocity;


            //Debug.Log("JUMP newVelocity = " + newVelocity);
        }
    }
  

    private void UpdatePhysicParams() {

        //PHYSIC MAT
        if(myGroundCheck.IsGrounded && !isStillControledByPhysic && xInput == 0 && !isJumping) {
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
            rb.linearDamping = 0;
        } else {
            rb.linearDamping = 1;
        }
    }



    private void ApplyMovement() {


        // WALK MOVEMENT => full contrôle
        if(myGroundCheck.IsGrounded && !isJumping && !isStillControledByPhysic! && !isSliding && !isJetpackActivated) {
            //Debug.Log("SOL MOOV");
            currentmovementSpeed = movementForce;
            Vector2 _walkDirection = -Vector2.Perpendicular(myGroundCheck.GroundHitResult.normal).normalized;

            newVelocity.Set(movementForce * _walkDirection.x * xInput, movementForce * _walkDirection.y * xInput);
            Debug.DrawRay(transform.position, newVelocity, Color.cyan);
            float _dir = Mathf.Sign(xInput) != 0 ? Mathf.Sign(xInput) : 0;

            //FIX Décallage avec le sol           
            if(myGroundCheck.IsGroundedButFarFromGround) {
                Vector2 _veloToDown = Vector2.down * (rb.linearVelocity.magnitude + 1);
                newVelocity = newVelocity + _veloToDown;
                Debug.Log("FIX GROUND DECALAGE");
            }

            //APPLY VELOCITY
            //rb.linearVelocity = newVelocity;
            rb.linearVelocity = newVelocity.normalized * Mathf.Clamp(newVelocity.magnitude, 0, absoluteVelocityMagnitudeLimit);
            return;
        }


        //si on clique une fois sur physicInput on perds le jump movement et on passe dans ONLY PHYSIC MOVEMENT jusqu'a toucher le sol ou jetpack
        //si on clique sur Jump alors qu'on ait dans les airs sans avoir sauté avant, on active isJumping
        //il faudrait qu'après un saut
        //JUMP MOVEMENT
        if(!myGroundCheck.IsGrounded && !isStillControledByPhysic) {
            Debug.Log("JUMP CONTROL MOOV");

            newVelocity.Set(movementForce * xInput, rb.linearVelocity.y);
            rb.linearVelocity = newVelocity;
        }



        // PHYSIC MOVEMENTS

        //ONLY PHYSIC MOVEMENT
        if(isPhysicMovementActivated && !isJetpackActivated) {
            Debug.Log("Only physic move player");
            return;
        }

        // SLIDE MOVEMENT
        if(isSliding) {
            
            Debug.Log("SLIDE MOOV");
            // Calculate the force to apply based on current velocity with logarithmic scaling
            float _forceFactor = Mathf.Abs(rb.linearVelocity.x);
            float _forceToAdd = xInput * slideControlForce / (1f + Mathf.Log(_forceFactor + 1f)); // Logarithmic scaling
            //Debug.Log("SLIDE _addedForce = " + _forceToAdd);

            Vector2 _slideDirection = -Vector2.Perpendicular(myGroundCheck.GroundHitResult.normal).normalized;
            Vector2 _veloToAdd = _slideDirection * _forceToAdd;
            rb.AddForce(_veloToAdd, ForceMode2D.Force);
        }

        /*
        */
        //AIR MOVEMENT 
        if(isStillControledByPhysic && !myGroundCheck.IsGrounded) {

            Debug.Log("AIR MOOV");
  
            // Calculate the force to apply based on current velocity with logarithmic scaling
            float _forceFactor = Mathf.Abs(rb.linearVelocity.x);
            float _forceToAdd = xInput * airControlForce / (1f + Mathf.Log(_forceFactor + 1f)); // Logarithmic scaling
            //Debug.Log("AIR _addedForce = " + _forceToAdd);
            rb.AddForceX(_forceToAdd, ForceMode2D.Force);

        }

        //JETPACK MOVEMENT
        if(isJetpackActivated) {

            Debug.Log("JET MOOV");
            float _forceFactor = Mathf.Abs(rb.linearVelocity.y);
            float _forceToAdd = rocketInput * jetpackForce / (1f + Mathf.Log(_forceFactor + 1f)); // Logarithmic scaling
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
        _jetpackShapeModule.rotation = rocketInput < 0 ? new Vector3(0,0, 101.25f) : new Vector3(0, 0, -101.25f);
    }



    private void OnDrawGizmos() {
        
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
