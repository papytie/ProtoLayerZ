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
    [SerializeField] private float jumpForce;
    [SerializeField] float jumpForceMagnitudeMultiplier = 0.5f;
    [SerializeField] private float slopeCheckDistance;
    [SerializeField] private float maxSlopeAngle;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private LayerMask whatIsGround;
    [SerializeField] private PhysicsMaterial2D minFriction;
    [SerializeField] private PhysicsMaterial2D maxFriction;
    [SerializeField] private Transform camTarget;
    [SerializeField] private float aheadAmount = 5;
    [SerializeField] private float aheadSpeed = 10;
    private Vector3 camTargetDestinationPos = new Vector3();

    private float xInput;
    public float slideInput;
    private float slopeDownAngle;
    private float slopeSideAngle;
    //private float lastSlopeAngle;

    public bool isGrounded;
    public bool isSliding;
    public bool contactWithOtherDynamic;
    private bool isJumping;
    private bool canWalkOnSlope;
    public bool canJump;

    private Vector2 newVelocity;
    private Vector2 newForce;
    private Vector2 capsuleColliderSize;

    private Vector2 slopeNormalPerp;

    private Rigidbody2D rb;
    private CapsuleCollider2D cc;
    private Animator myAnimator;

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
        capsuleColliderSize = cc.size;
    }

    private void Update() {
        CheckInput();
        UpdateHangCounter();
        UpdateJumpBufferCounter();
        //UpdateAnimator();
    }

    private void FixedUpdate() {
        CheckGround();
        CheckIsSliding();
        CheckJump();
        SlopeCheck();
        SwitchPhysicMaterial();
        ApplyMovement();

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
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, whatIsGround);

        if(rb.linearVelocity.y <= 0.0f) {
            isJumping = false;
        }

        if(hangCounter > 0 && !isJumping && slopeDownAngle <= maxSlopeAngle) {
            canJump = true;
        }

        /*
        if(isGrounded && !isJumping && slopeDownAngle <= maxSlopeAngle) {
            canJump = true;
        }
        */
    }

    private void CheckIsSliding() {
        if( slideInput !=0 && isGrounded) { 
            isSliding = true;
        } else {
            isSliding = false; 
        }
    }

    public bool tryToJump = false;

    private void CheckJump() {

        if(jumpBufferCount <= 0) {
            tryToJump = false;
        }

        if(tryToJump && canJump) {
            canJump = false;
            isJumping = true;
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
 
    private void OnCollisionStay2D(Collision2D _collision) {
        if(_collision != null) {
            
            
            
            if(_collision.rigidbody != null && _collision.rigidbody.bodyType == RigidbodyType2D.Dynamic) {
                contactWithOtherDynamic = true;
                //Debug.Log(_collision.gameObject.name);

            } else {
                contactWithOtherDynamic = false;
            }
            
        }
        
    }

    private void SlopeCheck() {
        Vector2 checkPos = transform.position - (Vector3) (new Vector2(0.0f, capsuleColliderSize.y / 2));

        SlopeCheckHorizontal(checkPos);
        SlopeCheckVertical(checkPos);
    }

    private void SlopeCheckHorizontal(Vector2 checkPos) {
        RaycastHit2D slopeHitFront = Physics2D.Raycast(checkPos, transform.right, slopeCheckDistance, whatIsGround);
        RaycastHit2D slopeHitBack = Physics2D.Raycast(checkPos, -transform.right, slopeCheckDistance, whatIsGround);

        if(slopeHitFront) {
            slopeSideAngle = Vector2.Angle(slopeHitFront.normal, Vector2.up);
        } else if(slopeHitBack) {
            slopeSideAngle = Vector2.Angle(slopeHitBack.normal, Vector2.up);
        } else {
            slopeSideAngle = 0.0f;
        }

    }

    private void SlopeCheckVertical(Vector2 checkPos) {
        RaycastHit2D hit = Physics2D.Raycast(checkPos, Vector2.down, slopeCheckDistance, whatIsGround);

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

        if(contactWithOtherDynamic) //TEST
        {
            Debug.Log("dynamic contact");
            //newVelocity.Set(movementSpeed * xInput, 0.0f);
            //rb.linearVelocity = newVelocity;
        }

        if(isSliding || !isGrounded) {

            return;
        }

        if(isGrounded && canWalkOnSlope && !isJumping) //If on slope or ground
          {
            newVelocity.Set(movementSpeed * slopeNormalPerp.x * -xInput, movementSpeed * slopeNormalPerp.y * -xInput);
            rb.linearVelocity = newVelocity;
            return;
        }
        
        /*
        if(!isGrounded) //If in air
          {
            newVelocity.Set(movementSpeed * xInput, rb.linearVelocity.y);
            rb.linearVelocity = newVelocity;
        }
        */
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
            camTargetDestinationPos = aheadAmount * rb.linearVelocity.normalized;
        }

        camTarget.localPosition = Vector3.Lerp(camTarget.localPosition, camTargetDestinationPos, aheadSpeed * Time.deltaTime);
    }

    private void OnDrawGizmos() {
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(camTarget.position, groundCheckRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position + camTargetDestinationPos, groundCheckRadius);
    }
}
