using Unity.VisualScripting;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Windows;


public class F_PlayerController3 : MonoBehaviour
{
    Controls controls;
    InputAction move;
    InputAction jump;
    InputAction slide;

    public F_GroundCheck myGroundCheck;

    [SerializeField] private float movementSpeed;
    [SerializeField] private float hangTime = 0.2f;
    private float hangCounter;
    [SerializeField] private float jumpBufferLength = 0.1f;
    private float jumpBufferCount;
    [SerializeField] private float jumpForce;
    [SerializeField] float jumpForceMagnitudeMultiplier = 0.5f;
    [SerializeField] private PhysicsMaterial2D minFriction;
    [SerializeField] private PhysicsMaterial2D maxFriction;
    [SerializeField] private float veloDividerWhenJumping = 2;
    [SerializeField] private float maxAngleBeforeVeloCorrection = 45;
    [SerializeField] private float maxDistVectorForVeloCorrection = 5;
    [SerializeField] float movementSeepAccel = 2;
    [SerializeField] float canCheckGroundMaxTime = 0.2f;
    [SerializeField] private float maxDistGroundBeforeVeloCorrection = 0.5f;
    public float canCheckGroundCounter = 0;


    public float xInput;
    public float slideInput;
    public float xMomentum = 0;

    public bool canCheckGround = true;

    public bool isGroundedButFarFromGround;
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
        CheckInput();

        if(canCheckGround) {
            myGroundCheck.CheckGroundEnabled = true;
        } else {
            myGroundCheck.CheckGroundEnabled = false;
        }

        if(myGroundCheck.IsGrounded) {
            xMomentum = 0;
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

        UpdateIsGroundedButFarFromGround();

        UpdateCanCheckGroundCounter();
        UpdateHangCounter();
        UpdateJumpBufferCounter();

        CheckIsMoving();
    }

    private void FixedUpdate() {

        CheckIsSliding();
        CheckFlip();

        CheckJump();

        SwitchPhysicMaterial();
        ApplyMovement();

        UpdateRigidbodyGravity();
        UpdateAnimator();
    }

    private void CheckInput() {
        xInput = move.ReadValue<float>();
        slideInput = slide.ReadValue<float>();
    }

    private void UpdateIsGroundedButFarFromGround() {
        if(myGroundCheck.IsGrounded
            && Vector2.Distance(new Vector2(myGroundCheck.transform.position.x, myGroundCheck.transform.position.y), myGroundCheck.GroundHitResult.point) <= maxDistGroundBeforeVeloCorrection) {
            isGroundedButFarFromGround = false;
        } else {
            isGroundedButFarFromGround = true;
        }
    }

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

        if(canCheckGroundCounter <= 0) {
            canCheckGround = true;
            canCheckGroundCounter = canCheckGroundMaxTime;
        }
    }



    private void UpdateRigidbodyGravity() {

        if(myGroundCheck.IsGrounded && !isSliding) {
            rb.gravityScale = 0;
        } else {
            rb.gravityScale = strateGravityScale;
        }
    }
    

    private void CheckIsSliding() {
        if( slideInput !=0 && myGroundCheck.IsGrounded) { 
            isSliding = true;
        } else {
            isSliding = false; 
        }
    }

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
            
            newVelocity.Set(rb.linearVelocity.x/ veloDividerWhenJumping, rb.linearVelocity.y/ veloDividerWhenJumping);
            rb.linearVelocity = newVelocity;
            newForce.Set(0.0f, jumpForce + rb.linearVelocity.magnitude * jumpForceMagnitudeMultiplier);
            
            rb.AddForce(newForce, ForceMode2D.Impulse);
            tryToJump = false;
        }
    }

    private void CheckIsMoving() {
        isMoving = rb.linearVelocity.magnitude >= 0.01f ? true : false;
    }

    void TryToJump(InputAction.CallbackContext _context) {

        //Reset jump counter
        jumpBufferCount = jumpBufferLength;
        tryToJump = true;
        
    }

    private void SwitchPhysicMaterial() {
        if(myGroundCheck.IsGrounded && xInput == 0 && !isSliding) {
            rb.sharedMaterial = maxFriction;
        } else {
            rb.sharedMaterial = minFriction;
        }
    }




    private void ApplyMovement() {

 

        //AIR MOVEMENT
        if(!myGroundCheck.IsGrounded) {

            //le clamp min et max de la xVelo est relatif en %age à la velo de départ au moment où le joueur a décollé du sol
            //Set xMomentum 1 fois quand !isGrounded,    

            float _xMomentumAbs = Mathf.Abs(xMomentum);
            float _xVeloAddedWithInput;


            currentmovementSpeed = Mathf.Clamp(currentmovementSpeed + Time.fixedDeltaTime * movementSeepAccel, 0, movementSpeed);

            _xVeloAddedWithInput = Mathf.Clamp(rb.linearVelocity.x + currentmovementSpeed * xInput, -_xMomentumAbs - currentmovementSpeed, _xMomentumAbs + currentmovementSpeed);

            //_xVeloAddedWithInput = Mathf.Clamp(rb.linearVelocity.x + movementSpeed * xInput, -_xMomentumAbs - movementSpeed, _xMomentumAbs + movementSpeed);
            /*
            if(isJumping) {
                _xVeloAddedWithInput = Mathf.Clamp(rb.linearVelocity.x + movementSpeed * xInput, -_xMomentumAbs - movementSpeed, _xMomentumAbs + movementSpeed);
            } else {
                _xVeloAddedWithInput = Mathf.Clamp(rb.linearVelocity.x + movementSpeed * xInput, -_xMomentumAbs - movementSpeed/2, _xMomentumAbs + movementSpeed/2);
            }
            */

            newVelocity.Set(_xVeloAddedWithInput, rb.linearVelocity.y);
            rb.linearVelocity = newVelocity;
            return;
        }


        // SLIDE MOVEMENT
        if(isSliding) {
            currentmovementSpeed = movementSpeed;
            return;
        }


        // BASE MOVEMENT : if is on slope / ground
        if(myGroundCheck.IsGrounded && !isJumping) {
            currentmovementSpeed = movementSpeed;

            newVelocity.Set(movementSpeed * myGroundCheck.WalkDirection.x * -xInput, movementSpeed * myGroundCheck.WalkDirection.y * -xInput);
            Debug.DrawRay(transform.position, newVelocity, Color.cyan);

            //FIX Décallage avec le sol          
            if(isGroundedButFarFromGround) {
                Vector2 _addedVeloToGround = -myGroundCheck.GroundHitResult.normal * movementSpeed;
                newVelocity.Set(newVelocity.x + _addedVeloToGround.x, newVelocity.y + _addedVeloToGround.y);
                Debug.DrawRay(transform.position, newVelocity, Color.black);
            }

            //FIX SHARP ANGLE PROB TO STAY ON GROUND

            //A FAIRE DANS GROUNDCHECK ? ça devrait faire partie du calcul qui set WalkDirection ?
            //OU BIEN Setter newVelocity directement dans GroundCheck ?.

            Vector2 _checkPos = myGroundCheck.transform.position;
            Vector2 _predictedPoint = new Vector2(_checkPos.x, _checkPos.y) + rb.linearVelocity * Time.deltaTime;
            RaycastHit2D _secondVerticalhit = Physics2D.Raycast(_predictedPoint, Vector2.down, myGroundCheck.CheckedDistance * 10, myGroundCheck.GroundLayer);

            if(_secondVerticalhit && Vector2.Angle(myGroundCheck.GroundHitResult.normal, _secondVerticalhit.normal) >= maxAngleBeforeVeloCorrection) {
                //Debug.Log("FIX");
                //newVelocity.Set(newVelocity.x, newVelocity.y - movementSpeed * Mathf.Abs(xInput));
            }

            rb.linearVelocity = newVelocity;
            return;
        }
    }

 
    private void UpdateAnimator() {
        myAnimator.SetFloat(SRAnimators.Animator_Hero1.Parameters.xVelocity, Mathf.Round(rb.linearVelocity.x));
        myAnimator.SetFloat(SRAnimators.Animator_Hero1.Parameters.yVelocity, Mathf.Round(rb.linearVelocity.y));
        myAnimator.SetBool(SRAnimators.Animator_Hero1.Parameters.isGrounded, myGroundCheck.IsGrounded);
        myAnimator.SetBool(SRAnimators.Animator_Hero1.Parameters.isSliding, isSliding);
        myAnimator.SetBool(SRAnimators.Animator_Hero2.Parameters.isMoving, isMoving);
    }



    private void OnDrawGizmos() {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(myGroundCheck.transform.position, maxDistGroundBeforeVeloCorrection);
    }
       
}
