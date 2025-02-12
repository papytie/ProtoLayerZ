using UnityEngine;
using UnityEngine.InputSystem;


public class F_PlayerController2 : MonoBehaviour
{
    Controls controls;
    InputAction move;
    InputAction jump;
    InputAction slide;


    [SerializeField] private float movementSpeed;
    [SerializeField] private float hangTime = 0.2f;
    private float hangCounter;
    [SerializeField] private float jumpBufferLength = 0.1f;
    private float jumpBufferCount;
    [SerializeField] private float groundCheckRadius;
    [SerializeField] private float secondGroundCheckRadius;
    [SerializeField] private float jumpForce;
    [SerializeField] float jumpForceMagnitudeMultiplier = 0.5f;
    [SerializeField] private float slopeCheckHorizontalDistance;
    [SerializeField] private float slopeCheckVerticalDistance;
    [SerializeField] private float maxSlopeAngle;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private LayerMask whatIsGround;
    [SerializeField] private PhysicsMaterial2D minFriction;
    [SerializeField] private PhysicsMaterial2D maxFriction;
    [SerializeField] private Transform camTarget;
    [SerializeField] private float aheadAmount = 5;
    [SerializeField] private float aheadSpeed = 10;
    [SerializeField] private float veloDividerWhenJumping = 2;
    private Vector3 camTargetDestinationPos = new Vector3();

    private float xInput;
    public float slideInput;
    private float slopeDownAngle;
    private float slopeSideAngle;
    public float xMomentum = 0;

    public bool isGrounded;
    public bool isGroundedButFarFromGround;
    public bool isSliding;
    public bool contactWithOtherDynamic;
    private bool isJumping;
    private bool canWalkOnSlope;
    public bool canJump;
    public bool tryToJump = false;

    private Vector2 newVelocity;
    private Vector2 newForce;
    private Vector2 capsuleColliderSize;

    private Vector2 slopeNormalPerp;

    private Rigidbody2D rb;
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

        rb.gravityScale = strateGravityScale;
        capsuleColliderSize = cc.size;
    }

    private void Update() {
        CheckInput();
        UpdateHangCounter();
        UpdateJumpBufferCounter();
    }

    private void FixedUpdate() {
        CheckGround();
        CheckIsSliding();

        CheckJump();
        SlopeCheck();
        SwitchPhysicMaterial();
        ApplyMovement();

        UpdateRigidbodyGravity();
        UpdateAnimator();

        MoveCamTarget();
    }

    private void CheckInput() {
        xInput = move.ReadValue<float>();
        slideInput = slide.ReadValue<float>();
    }

    private void UpdateHangCounter() {
        if(isGrounded) {
            hangCounter = hangTime;
        } else {
            hangCounter -= Time.deltaTime;
        }
    }

    private void UpdateJumpBufferCounter() {
         
        if(jumpBufferCount < 0) return;

        jumpBufferCount -= Time.deltaTime;
    }

    private void CheckGround() {

        bool _hit = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, whatIsGround);
        if(_hit) {
            isGrounded = true;
            xMomentum = 0;
            
            //TEST DECALLAGE FROM GROUND
            bool _secondHit = Physics2D.OverlapCircle(groundCheck.position,secondGroundCheckRadius, whatIsGround);
            if(_secondHit) {
                isGroundedButFarFromGround = false;
            } else {
                isGroundedButFarFromGround = true;
            }


        } else {
            if(isGrounded) {
                xMomentum = rb.linearVelocity.x;
            }
            isGrounded = false;
        }

        if(rb.linearVelocity.y <= 0.0f) {
            isJumping = false;
        }

        if(hangCounter > 0 && !isJumping && slopeDownAngle <= maxSlopeAngle) {
            canJump = true;
        }

    }

    private void UpdateRigidbodyGravity() {
        if(isGrounded && !isSliding && !isGroundedButFarFromGround) {
            rb.gravityScale = 0;
        } else {
            rb.gravityScale = strateGravityScale;
        }
    }
    

    private void CheckIsSliding() {
        if( slideInput !=0 && isGrounded) { 
            isSliding = true;
        } else {
            isSliding = false; 
        }
    }


    private void CheckJump() {

        if(jumpBufferCount <= 0) {
            tryToJump = false;
        }

        if(tryToJump && canJump) {
            canJump = false;
            isJumping = true;
            //newForce.Set(0.0f, jumpForce + rb.linearVelocity.magnitude * jumpForceMagnitudeMultiplier);
            
            newVelocity.Set(rb.linearVelocity.x/ veloDividerWhenJumping, rb.linearVelocity.y/ veloDividerWhenJumping);
            rb.linearVelocity = newVelocity;
            newForce.Set(0.0f, jumpForce + rb.linearVelocity.magnitude * jumpForceMagnitudeMultiplier);
            
            rb.AddForce(newForce, ForceMode2D.Impulse);
            tryToJump = false;
        }
    }

    void TryToJump(InputAction.CallbackContext _context) {

        //Reset jump counter
        jumpBufferCount = jumpBufferLength;
        tryToJump = true;
        
    }
 

    private void SlopeCheck() {
        Vector2 checkPos = transform.position - (Vector3) (new Vector2(0.0f, capsuleColliderSize.y / 2));

        SlopeCheckHorizontal(checkPos);
        SlopeCheckVertical(checkPos);
    }

    private void SlopeCheckHorizontal(Vector2 checkPos) {
        RaycastHit2D slopeHitFront = Physics2D.Raycast(checkPos, transform.right, slopeCheckHorizontalDistance, whatIsGround);
        RaycastHit2D slopeHitBack = Physics2D.Raycast(checkPos, -transform.right, slopeCheckHorizontalDistance, whatIsGround);

        if(slopeHitFront) {
            slopeSideAngle = Vector2.Angle(slopeHitFront.normal, Vector2.up);
        } else if(slopeHitBack) {
            slopeSideAngle = Vector2.Angle(slopeHitBack.normal, Vector2.up);
        } else {
            slopeSideAngle = 0.0f;
        }

    }

    private void SlopeCheckVertical(Vector2 checkPos) {
        RaycastHit2D hit = Physics2D.Raycast(checkPos, Vector2.down, slopeCheckVerticalDistance, whatIsGround);

        if(hit) {

            slopeNormalPerp = Vector2.Perpendicular(hit.normal).normalized;

            slopeDownAngle = Vector2.Angle(hit.normal, Vector2.up);


            //lastSlopeAngle = slopeDownAngle;

            Debug.DrawRay(hit.point, slopeNormalPerp, Color.blue);
            Debug.DrawRay(hit.point, hit.normal, Color.green);
        }
    }

    private void SwitchPhysicMaterial() {
        if(slopeDownAngle > maxSlopeAngle || slopeSideAngle > maxSlopeAngle) {
            canWalkOnSlope = false;
        } else {
            canWalkOnSlope = true;
        }

        if(canWalkOnSlope && xInput == 0.0f && !isSliding) {
            rb.sharedMaterial = maxFriction;
        } else {
            rb.sharedMaterial = minFriction;
        }
    }


    private void ApplyMovement() {

        if(!isGrounded) { //If in Air

            //le clamp min et max de la xVelo est relatif en %age à la velo de départ au moment où le joueur a décollé du sol
            //Set xMomentum 1 fois quand !isGrounded,    

            float _xMomentumAbs = Mathf.Abs(xMomentum);
            float _xVeloAddedWithInput = Mathf.Clamp(rb.linearVelocity.x + movementSpeed * xInput,-_xMomentumAbs - movementSpeed, _xMomentumAbs + movementSpeed);

            newVelocity.Set(_xVeloAddedWithInput, rb.linearVelocity.y);
            rb.linearVelocity = newVelocity;
            return;
        }

        if(isSliding) { // If Sliding

            return;
        }

        if(isGrounded && canWalkOnSlope && !isJumping) //If on slope or ground
          {
            newVelocity.Set(movementSpeed * slopeNormalPerp.x * -xInput, movementSpeed * slopeNormalPerp.y * -xInput);
            Debug.DrawRay(transform.position, newVelocity, Color.cyan);

            if(isGroundedButFarFromGround) {
                newVelocity.Set(newVelocity.x, newVelocity.y - movementSpeed * Mathf.Abs(xInput)/10);
                Debug.DrawRay(transform.position, newVelocity, Color.black);
            }

            rb.linearVelocity = newVelocity;
            return;
        }
    }



    private void UpdateAnimator() {
        //myAnimator.SetFloat(SRAnimators.a)
        myAnimator.SetFloat(SRAnimators.Animator_Hero1.Parameters.xVelocity, Mathf.Round(rb.linearVelocity.x));
        myAnimator.SetFloat(SRAnimators.Animator_Hero1.Parameters.yVelocity, Mathf.Round(rb.linearVelocity.y));
        myAnimator.SetBool(SRAnimators.Animator_Hero1.Parameters.isGrounded, isGrounded);
        myAnimator.SetBool(SRAnimators.Animator_Hero1.Parameters.isSliding, isSliding);
    }

    private void MoveCamTarget() {

        if(Mathf.Abs(xInput)>= 0.2f || isSliding || !isGrounded) {
            Vector3 _veloNormalized = rb.linearVelocity.normalized;
            camTargetDestinationPos = new Vector3(aheadAmount * _veloNormalized.x, aheadAmount/2 * _veloNormalized.y,0);
        }

        camTarget.localPosition = Vector3.Lerp(camTarget.localPosition, camTargetDestinationPos, aheadSpeed * Time.deltaTime);
    }

    private void OnDrawGizmos() {

        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(groundCheck.position, secondGroundCheckRadius);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(camTarget.position, groundCheckRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position + camTargetDestinationPos, groundCheckRadius);
    }
}
