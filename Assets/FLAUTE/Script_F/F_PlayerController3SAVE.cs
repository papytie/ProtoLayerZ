using Unity.VisualScripting;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Windows;
using static UnityEditor.ShaderGraph.Internal.KeywordDependentCollection;


public class F_PlayerController3SAVE : MonoBehaviour
{
    Controls controls;
    InputAction move;
    InputAction jump;
    InputAction slide;

    public F_GroundCheck myGroundCheck;

    [SerializeField] private float movementForce;
    [SerializeField] private float hangTime = 0.2f;
    private float hangCounter;
    [SerializeField] private float jumpBufferLength = 0.1f;
    private float jumpBufferCount;
    [SerializeField] private float inertiaMaxTime = 0.2f;
    private float inertiaCounter;
    [SerializeField] private float jumpForce;
    [SerializeField] float jumpForceMagnitudeMultiplier = 0.5f;
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
    [SerializeField] float inertiaThreshold;

    public float xInput;
    public float slideInput;
    public float xMomentum = 0;

    public bool canCheckGround = true;

    //public bool isGroundedButFarFromGround;
    public bool isSliding;
    public bool isMoving;
    public bool isJumping;
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
    [SerializeField] float strateFrictionNorm = 1;

    public float MovementSpeed => movementForce;

    /*
    private void OnTriggerEnter2D(Collider2D collision) {

        //si on est dans les airs, que le délai canCheckGroundCounter est passé et qu'on trigger le sol, on peut check le sol
        if(!myGroundCheck.IsGrounded && canCheckGroundCounter <= 0 && ((1 << collision.gameObject.layer) & myGroundCheck.GroundLayer) != 0) {
            canCheckGround = true;
            canCheckGroundCounter = canCheckGroundMaxTime;

            
            Vector2 _closestContactPoint = collision.ClosestPoint(myGroundCheck.transform.position);
            Vector2 _dir = (_closestContactPoint - (Vector2)myGroundCheck.transform.position).normalized;
            RaycastHit2D _hit = Physics2D.Raycast(myGroundCheck.transform.position, _dir, myGroundCheck.CheckedDistance, myGroundCheck.GroundLayer);

            if(_hit && Vector2.Angle(_hit.normal, Vector2.up)>= myGroundCheck.MaxGroundAngle) {
                isInAirAndTouchingNonWalkableSlope = true;
            }         
        }
    }
    */

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


            //canCheckGround = true;
            //canCheckGroundCounter = canCheckGroundMaxTime;

            //Debug.Log("SlideVelo Reorientation BECAUSE touching Ground");
            //slideVelo = rb.linearVelocity.normalized * slideVelo.magnitude;      
        }
    }
    /*
    */

    private void Awake() {
        controls = new Controls();
        move = controls.MainCharacterMap.Walking;
        jump = controls.MainCharacterMap.Jumping;
        slide = controls.MainCharacterMap.Sliding;

        move.Enable();
        jump.Enable();
        slide.Enable();

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
        //il faudra une autre variable que movementForce qui puisse être affectée par la Strate => movementForce (truc qui ne change pas) et movementSpeed(force/gravité/friction)
        //dans l'idée :
        //float _movementSpeed = movementForce * strateGravityScale * strateFrictionNorm;
        inertiaThreshold = movementForce * 2;



        CheckInput();

        if(!myGroundCheck.IsGrounded && !myGroundCheck.IsGroundOverlaped || isInAirAndTouchingNonWalkableSlope) {
            canCheckGround = false;
        }

        if(canCheckGround) {
            myGroundCheck.CheckGroundEnabled = true;
        } else {
            myGroundCheck.CheckGroundEnabled = false;
        }

        if(myGroundCheck.IsGrounded) {
            xMomentum = 0;
            //isInAirAndTouchingNonWalkableSlope = false;
        } else {
            if(xMomentum == 0) {
                xMomentum = rb.linearVelocity.x;
                currentmovementSpeed = 0;
            }
        }

        //OTHER STUFF
        if(rb.linearVelocity.y <= 0.0f || myGroundCheck.IsGrounded) {
            isJumping = false;
        }

        if(hangCounter > 0 && !isJumping) {
            canJump = true;
        }

        //UpdateCanCheckGroundCounter();
        //UpdateHangCounter();
        //UpdateJumpBufferCounter();
        UpdateCounters();
        //CheckIsMoving();
        CheckState(); // isMoving, isSliding, isInInertia
    }

    private void FixedUpdate() {

        //CheckJump();
        //CheckIsSliding();
        CheckFlip();
        CheckJump();


        UpdatePhysicParams();
        //SwitchPhysicMaterial();
        //UpdateRigidbodyGravity();
        ApplyMovement();

        UpdateAnimator();
    }

    private void CheckInput() {
        xInput = move.ReadValue<float>();
        slideInput = slide.ReadValue<float>();
    }


    /*
    private void UpdateHangCounter() {
        if(myGroundCheck.IsGrounded) {
            hangCounter = hangTime;
        } else {
            hangCounter -= Time.deltaTime;
        }
    }

    private void UpdateJumpBufferCounter() {
         
        if(jumpBufferCount < 0) return;

        jumpBufferCount -= Time.deltaTime;
    }

    private void UpdateCanCheckGroundCounter() {

        if(canCheckGround) return;

        canCheckGroundCounter -= Time.deltaTime;
        //canCheckGround true se fait dans OnTriggerStay
    }
    */

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

        //INERTIA COUNTER
        if(rb.linearVelocity.magnitude > inertiaThreshold) {
            inertiaCounter += Time.deltaTime;
        } else {
            inertiaCounter = 0;
        }
    }

 
    private void CheckState() {

        //isMoving
        isMoving = rb.linearVelocity.magnitude >= 0.01f ? true : false;

        //isSliding
        if(slideInput != 0 && myGroundCheck.IsGrounded) {
            isSliding = true;
        } else {
            if(isSliding) {
                //Debug.Log("save slide velo");
                //slideVelo = rb.linearVelocity;
            }
            isSliding = false;
        }

        //isInInertia
        //si on dépasse le seuil pendant un délai, on est considéré comme en inertie
       
        if(rb.linearVelocity.magnitude > inertiaThreshold && inertiaCounter >= inertiaMaxTime) {
            isInInertia = true;
        } else {
            isInInertia = false;
        }

    }

    /*
    private void UpdateRigidbodyGravity() {

        if(myGroundCheck.IsGrounded && !isSliding) {
            rb.gravityScale = 0;
        } else {
            rb.gravityScale = strateGravityScale;
        }
    }
    
    */

    /*
    private void CheckIsSliding() {
        if( slideInput !=0 && myGroundCheck.IsGrounded) { 
            isSliding = true;
        } else {

            if(isSliding) {
                //Debug.Log("save slide velo");
                //slideVelo = rb.linearVelocity;
            }

            isSliding = false;
            
        }
    }
    */

    private void CheckFlip() {

        float _signInput = Mathf.Sign(xInput);

        if(!isSliding && xInput != 0 && facingDirection != _signInput) {
            facingDirection = _signInput;
            transform.Rotate(0.0f, 180.0f, 0.0f);
            return;
        }

        float _signVeloX = Mathf.Sign(rb.linearVelocity.x);

        if(isSliding && _signVeloX != 0 && facingDirection != _signVeloX) {
            facingDirection = _signVeloX;
            transform.Rotate(0.0f, 180.0f, 0.0f);
            return;
        }

    }
    private void CheckJump() {

        if(jumpBufferCount <= 0) {
            tryToJump = false;
        }

        if(tryToJump && canJump) {
            canJump = false;
            isJumping = true;
            canCheckGround = false;
            myGroundCheck.CheckGroundEnabled = false;


            newVelocity.Set(rb.linearVelocity.x/ veloDividerWhenJumping, rb.linearVelocity.y/ veloDividerWhenJumping);
            rb.linearVelocity = newVelocity;
            newForce.Set(0.0f, jumpForce + rb.linearVelocity.magnitude * jumpForceMagnitudeMultiplier);
            

            isSliding = false;
            //Debug.Log("save slide velo just before jump");
            //slideVelo = rb.linearVelocity;
            Debug.Log("force added = " + newForce);
            rb.AddForce(newForce, ForceMode2D.Impulse);
            newVelocity = rb.linearVelocity;
            tryToJump = false;
        }
    }

    /*
    private void CheckIsMoving() {
        isMoving = rb.linearVelocity.magnitude >= 0.01f ? true : false;
    }
    */
    void TryToJump(InputAction.CallbackContext _context) {

        //Reset jump counter
        jumpBufferCount = jumpBufferLength;
        tryToJump = true;
        
    }

    [SerializeField] bool isInInertia = false;
    private void UpdatePhysicParams() {

        //PHYSIC MAT
        if(myGroundCheck.IsGrounded && !isSliding && xInput == 0 && !isInInertia) {
            rb.sharedMaterial = maxFriction;
        } else {
            rb.sharedMaterial = minFriction;
        }

        //GRAVITY
        if(myGroundCheck.IsGrounded && !isSliding && !isInInertia) {
            rb.gravityScale = 0;
        } else {
            rb.gravityScale = strateGravityScale;
        }
    }
    /*
    private void SwitchPhysicMaterial() {
        if(myGroundCheck.IsGrounded&& !isSliding && xInput == 0) {
            rb.sharedMaterial = maxFriction;
        } else {
            rb.sharedMaterial = minFriction;
        }
    }
    */

    /*
    [SerializeField] Vector2 slideVelo = Vector2.zero;
    [SerializeField] float decelerationAfterSliding = 10;
    [SerializeField] float slideVeloDivider = 2;
    [SerializeField] float slideVeloOffset = 0.1f;
    [SerializeField] float correctionSpeed;
    [SerializeField] float signedAngleSlideAndVelo;
    */

    //strateFriction, 0 means no friction, 1 means full friction

    private void ApplyMovement() {

        /*
        //reduction du la slide velo
        //ne pas réduire quand on est dans les air
        if(myGroundCheck.IsGrounded) {
            if(slideVelo.magnitude - slideVeloOffset > 0 && !isSliding) {
                slideVelo /= slideVeloDivider; //sorte de friction de sol
            } else {
                slideVelo = Vector2.zero;
            }
        }
        signedAngleSlideAndVelo = Vector2.SignedAngle(slideVelo.normalized, rb.linearVelocity.normalized);
        Debug.Log("signedAngleSlideAndVelo = " + signedAngleSlideAndVelo);
        */
 

                // SLIDE MOVEMENT
        if(isSliding) {
            currentmovementSpeed = movementForce;
            Debug.Log("sliding");
            return;
        }

        //DERAPAGE MOVEMENT
        if(!isSliding && rb.linearVelocity.magnitude > movementForce*2) {
            Debug.Log("BRAKE");
            float _frictionNormalized = -Mathf.Clamp01(strateFrictionNorm) + 1;
            Debug.Log("_frictionNormalized = " + _frictionNormalized);
            newVelocity = rb.linearVelocity * _frictionNormalized;
            Debug.Log("new velo magnitude braking = " + newVelocity.magnitude);
            rb.linearVelocity = newVelocity;
            return;
        }
          
        /*     
        */

        //AIR MOVEMENT
        //si pas au sol et pas en contact d'une pente trop abrupte, mais l'angle ne doit pas non plus être égal a 0 (c
        else if(!myGroundCheck.IsGrounded && !isInAirAndTouchingNonWalkableSlope) {
            Debug.Log("AIR MOOV");
            //le clamp min et max de la xVelo est relatif en %age à la velo de départ au moment où le joueur a décollé du sol
            //Set xMomentum 1 fois quand !isGrounded,    

            float _xMomentumAbs = Mathf.Abs(xMomentum);
            float _xVeloAddedWithInput;


            currentmovementSpeed = Mathf.Clamp(currentmovementSpeed + Time.fixedDeltaTime * movementSeepAccel, 0, movementForce);
            _xVeloAddedWithInput = Mathf.Clamp(rb.linearVelocity.x + currentmovementSpeed * xInput, -_xMomentumAbs - currentmovementSpeed, _xMomentumAbs + currentmovementSpeed);

            newVelocity.Set(_xVeloAddedWithInput, rb.linearVelocity.y);
            //rb.linearVelocity = newVelocity;

        }

        /*
        */

        // BASE MOVEMENT : if is on slope / ground
        else if(myGroundCheck.IsGrounded && !isJumping) {
            Debug.Log("SOL MOOV");
            currentmovementSpeed = movementForce;
      
            Vector2 _walkDirection = -Vector2.Perpendicular(myGroundCheck.GroundHitResult.normal).normalized;

            
            newVelocity.Set(movementForce * _walkDirection.x * xInput, movementForce * _walkDirection.y * xInput);
            Debug.DrawRay(transform.position, newVelocity, Color.cyan);

            float _dir = Mathf.Sign(xInput) != 0 ? Mathf.Sign(xInput) : 0;



            //FIX Décallage avec le sol           
            if(myGroundCheck.IsGroundedButFarFromGround) {

                //float _xInputMinForced = Mathf.Abs(xInput) >= 01f ? Mathf.Abs(xInput) : 0.1f;

                //Vector2 _veloToDown = Vector2.down * correctionSpeed; //* _xInputMinForced;
                Vector2 _veloToDown = Vector2.down * (rb.linearVelocity.magnitude+1) ; //* _xInputMinForced;
                newVelocity = newVelocity + _veloToDown;
                Debug.Log("FIX GROUND DECALAGE");
            }                 
        }



        //APPLY VELOCITY
        Debug.Log("APPLY VELO");
        rb.linearVelocity = newVelocity;
    }

 
    //Vector2 predictedPoint = new Vector2();

    private void UpdateAnimator() {
        myAnimator.SetFloat(SRAnimators.Animator_Hero1.Parameters.xVelocity, Mathf.Round(rb.linearVelocity.x));
        myAnimator.SetFloat(SRAnimators.Animator_Hero1.Parameters.yVelocity, Mathf.Round(rb.linearVelocity.y));
        myAnimator.SetBool(SRAnimators.Animator_Hero1.Parameters.isGrounded, myGroundCheck.IsGrounded);
        myAnimator.SetBool(SRAnimators.Animator_Hero1.Parameters.isSliding, isSliding);
        myAnimator.SetBool(SRAnimators.Animator_Hero2.Parameters.isMoving, isMoving);
    }



    private void OnDrawGizmos() {

        Gizmos.color = Color.magenta;
        Vector3 _newPos = transform.position + transform.up * 1;
        //Gizmos.DrawRay(_newPos, slideVelo);
        
        
        Gizmos.color = Color.green;
        Vector3 _newPos2 = transform.position + transform.up * 0.5f;
        Gizmos.DrawRay(_newPos2, rb.linearVelocity);


    }
       
}
