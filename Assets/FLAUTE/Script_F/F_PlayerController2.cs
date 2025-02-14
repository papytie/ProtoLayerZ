using Unity.VisualScripting;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;
using UnityEngine.InputSystem;


public class F_PlayerController2 : MonoBehaviour
{
    Controls controls;
    InputAction move;
    InputAction jump;
    InputAction slide;

    F_GroundCheck myGroundCheck;

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
    [SerializeField] private float veloDividerWhenJumping = 2;
    [SerializeField] private float maxAngleBeforeVeloCorrection = 45;
    [SerializeField] private float maxDistVectorForVeloCorrection = 5;
    [SerializeField] private Vector2 intersectionForVeloCorrection = Vector2.zero;
    [SerializeField] float movementSeepAccel = 2;
    [SerializeField] float canCheckGroundMaxTime = 0.2f;
    public float canCheckGroundCounter = 0;


    public float xInput;
    public float slideInput;
    //private float slopeDownAngle;
    //private float slopeSideAngle;
    public float xMomentum = 0;

    public bool canCheckGround = true;
    public bool isGrounded;
    public bool isGroundedButFarFromGround;
    public bool isSliding;
    public bool isMoving;
    public bool contactWithOtherDynamic;
    public bool isJumping;
    public bool canWalkOnSlope;
    public bool canJump;
    public bool tryToJump = false;
    public float currentmovementSpeed = 0;
    public float facingDirection = 1;

    private Vector2 newVelocity;
    private Vector2 newForce;
    private Vector2 capsuleColliderSize;

    private Vector2 slopeNormalPerp;
    private RaycastHit2D slopeHitVerticalResult;
    private RaycastHit2D slopeHitHorizontalFrontResult;
    private RaycastHit2D slopeHitHorizontalBackResult;

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
        capsuleColliderSize = cc.size;
    }

    private void Update() {
        CheckInput();
        UpdateCanCheckGroundCounter();
        UpdateHangCounter();
        UpdateJumpBufferCounter();

        CheckIsMoving();
    }

    private void FixedUpdate() {


        if(canCheckGround) {
            CheckGround();
        } 

        CheckIsSliding();
        CheckFlip();

        CheckJump();

        SlopeCheck();
        SwitchPhysicMaterial();
        ApplyMovement();

        UpdateRigidbodyGravity();
        UpdateAnimator();
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


    private void UpdateCanCheckGroundCounter() {

        if(canCheckGround) return;

        canCheckGroundCounter -= Time.deltaTime;

        if(canCheckGroundCounter <= 0) {
            canCheckGround = true;
            canCheckGroundCounter = canCheckGroundMaxTime;
        }
    }

    public float firstSlopeHitAngle;
    public float previousSlopeHitAngle;
    public float selectedSlopeHitAngle;
    RaycastHit2D hitResultCircleCast;
    RaycastHit2D hitResultCircleCastPrevious;
    RaycastHit2D hitResultRay;
    private void CheckGround() {


        bool _isGroundOverlaped = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, whatIsGround);
        hitResultCircleCastPrevious = hitResultCircleCast;
        hitResultCircleCast = Physics2D.CircleCast(groundCheck.position, groundCheckRadius, Vector2.zero, 0, whatIsGround);
        
        //trouver le point le plus proche entre le précédent et le nouveau
        if(hitResultCircleCast && hitResultCircleCastPrevious && hitResultCircleCast.point != hitResultCircleCastPrevious.point) {
            float _distNewCast = Vector2.Distance(hitResultCircleCast.point, groundCheck.position);
            float _distPreviousCast = Vector2.Distance(hitResultCircleCastPrevious.point, groundCheck.position);
            float _minDist = Mathf.Min(_distNewCast, _distPreviousCast);
            //choisir quel result prendre en compte
            hitResultCircleCast = _minDist == _distNewCast ? hitResultCircleCast : hitResultCircleCastPrevious;
        }


        if(hitResultCircleCast) {
            hitResultRay = Physics2D.Raycast(groundCheck.position, -hitResultCircleCast.normal, groundCheckRadius, whatIsGround);
            if(hitResultRay) {               
                firstSlopeHitAngle = Vector2.Angle(hitResultRay.normal, Vector2.up);
                selectedSlopeHitAngle = firstSlopeHitAngle <= maxSlopeAngle ? firstSlopeHitAngle : previousSlopeHitAngle;
                previousSlopeHitAngle = selectedSlopeHitAngle;
            } else {
                //firstSlopeHitAngle = 0;
                selectedSlopeHitAngle = previousSlopeHitAngle;
            }
        } else {
            //firstSlopeHitAngle = 0;
            selectedSlopeHitAngle = previousSlopeHitAngle;
        }

        //Debug.Log("first slope = " + firstSlopeHitAngle);
        //Debug.Log("isGroundOverlaped = " + _isGroundOverlaped);
        Debug.DrawRay(hitResultRay.point, hitResultRay.normal * 5);

        if(_isGroundOverlaped && hitResultCircleCast && selectedSlopeHitAngle <= maxSlopeAngle) {
            isGrounded = true;
            xMomentum = 0;
            //TEST DECALLAGE FROM GROUND
            bool _secondHit = Physics2D.OverlapCircle(groundCheck.position, secondGroundCheckRadius, whatIsGround);
            if(_secondHit) {
                isGroundedButFarFromGround = false;
            } else {
                isGroundedButFarFromGround = true;
            }
        } else {
            if(isGrounded) {
                xMomentum = rb.linearVelocity.x;
                currentmovementSpeed = 0;
            }
            isGrounded = false;

        }

        /*
        if(_isGroundOverlaped) {

            isGrounded = true;
            xMomentum = 0;

            //TEST DECALLAGE FROM GROUND
            bool _secondHit = Physics2D.OverlapCircle(groundCheck.position, secondGroundCheckRadius, whatIsGround);
            if(_secondHit) {
                isGroundedButFarFromGround = false;
            } else {
                isGroundedButFarFromGround = true;
            }


        } else {
            if(isGrounded) {
                xMomentum = rb.linearVelocity.x;
                currentmovementSpeed = 0;
            }
            isGrounded = false;
            
        }
        */

        //OTHER STUFF
        if(rb.linearVelocity.y <= 0.0f || isGrounded) {
            isJumping = false;
        }

        if(hangCounter > 0 && !isJumping && slopeDownAngle <= maxSlopeAngle) {
            canJump = true;
        }

    }

    private void UpdateRigidbodyGravity() {
        /*
        if(isGrounded && !isSliding && !isGroundedButFarFromGround && canWalkOnSlope) {
            rb.gravityScale = 0;
        } else {
            rb.gravityScale = strateGravityScale;
        }
        */
        if(isGrounded && !isSliding) {
            if(canWalkOnFrontSlope || canWalkOnDownSlope) {
                rb.gravityScale = 0;
            } else {
                rb.gravityScale = strateGravityScale;
            }
           
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
            isGrounded = false;
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


    private void SlopeCheck() {
        Vector2 checkPos = transform.position - (Vector3) (new Vector2(0.0f, capsuleColliderSize.y / 2));
        //Vector2 checkPos = transform.position - (Vector3) (new Vector2(0.0f, capsuleColliderSize.y / 2 + 0.1f));

        SlopeCheckHorizontal(checkPos);
        SlopeCheckVertical(checkPos);
 
    }

    public float slopeFrontAngle;
    public float slopeDownAngle;
    public float slopeBackAngle;
    public bool canWalkOnFrontSlope;
    public bool canWalkOnBackSlope;
    public bool canWalkOnDownSlope;
    private void SlopeCheckHorizontal(Vector2 checkPos) {
        slopeHitHorizontalFrontResult = Physics2D.Raycast(checkPos, transform.right, slopeCheckHorizontalDistance, whatIsGround);
        slopeHitHorizontalBackResult = Physics2D.Raycast(checkPos, -transform.right, groundCheckRadius, whatIsGround);

        if(slopeHitHorizontalFrontResult) {
            slopeFrontAngle = Vector2.Angle(slopeHitHorizontalFrontResult.normal, Vector2.up);

        } else {
            //slopeFrontAngle = 0.0f;
            slopeFrontAngle =maxSlopeAngle+1;
        }

        if(slopeHitHorizontalBackResult) {
            slopeBackAngle = Vector2.Angle(slopeHitHorizontalBackResult.normal, Vector2.up);
        } else {
            //slopeBackAngle = 0.0f;
            slopeBackAngle = maxSlopeAngle + 1;
        }
            


        /*
        if(slopeHitHorizontalFrontResult) {
            slopeSideAngle = Vector2.Angle(slopeHitHorizontalFrontResult.normal, Vector2.up);
            if(slopeSideAngle > maxSlopeAngle) {
                transform.Rotate(0.0f, 180.0f, 0.0f);
                facingDirection = Mathf.Sign(transform.right.x);
            }

        } else if(slopeHitHorizontalBackResult) {
            //slopeSideAngle = Vector2.Angle(slopeHitHorizontalBackResult.normal, Vector2.up);
            slopeSideAngle = 0.0f;

        } else {
            slopeSideAngle = 0.0f;
        }
        */
    }

    private void SlopeCheckVertical(Vector2 checkPos) {
        slopeHitVerticalResult = Physics2D.Raycast(checkPos, Vector2.down, slopeCheckVerticalDistance, whatIsGround);
        //Debug.Log("slopeHitVerticalResult = " + (bool)slopeHitVerticalResult);
        if(slopeHitVerticalResult) {

            //slopeNormalPerp = Vector2.Perpendicular(slopeHitVerticalResult.normal).normalized;
            slopeDownAngle = Vector2.Angle(slopeHitVerticalResult.normal, Vector2.up);


            //lastSlopeAngle = slopeDownAngle;

            Debug.DrawRay(slopeHitVerticalResult.point, slopeHitVerticalResult.normal, Color.green);
        } else {
            //slopeDownAngle = 0;
            slopeDownAngle = maxSlopeAngle + 1;
        }

        //INCOHERENCE / REDONDANCE AVEC CHECKGROUND, le check ground devrait setter la slopeNormalPerp qui correspond au result designé comme ground praticable
        //SET DISTANCES
        float _distToSlopeFront = slopeHitHorizontalFrontResult ? // si y'a une slope down
            Vector2.Distance(slopeHitHorizontalFrontResult.point, new Vector2(groundCheck.position.x, groundCheck.position.y))
            : 1000;

        float _distToSlopeDown = slopeHitVerticalResult ?
            Vector2.Distance(slopeHitVerticalResult.point, new Vector2(groundCheck.position.x, groundCheck.position.y))
            : 1000;

        float _distToSlopeBack = slopeHitHorizontalBackResult ?
            Vector2.Distance(slopeHitHorizontalBackResult.point, new Vector2(groundCheck.position.x, groundCheck.position.y))
            : 1000;


        //SET SLOPE NORMAL
        //Check aussi le back pour éviter le petit saut en se raprochant du sol depuis une pente

        float _selectedDistance = Mathf.Min(_distToSlopeFront, _distToSlopeDown, _distToSlopeBack);
        slopeNormalPerp = _selectedDistance == _distToSlopeFront ? Vector2.Perpendicular(slopeHitHorizontalFrontResult.normal).normalized
                         : _selectedDistance == _distToSlopeDown ? Vector2.Perpendicular(slopeHitVerticalResult.normal).normalized
                         : _selectedDistance == _distToSlopeBack ? Vector2.Perpendicular(slopeHitHorizontalBackResult.normal).normalized
                         : Vector2.zero;


        /*
        if(!slopeHitHorizontalFrontResult && !slopeHitVerticalResult && !slopeHitHorizontalBackResult) {
            slopeNormalPerp = Vector2.up;
            return;
        }

        if(!slopeHitHorizontalFrontResult && slopeHitVerticalResult) {
            slopeNormalPerp = Vector2.Perpendicular(slopeHitVerticalResult.normal).normalized;
            return;
        }

        if(slopeHitHorizontalFrontResult && !slopeHitVerticalResult) {
            slopeNormalPerp = Vector2.Perpendicular(slopeHitHorizontalFrontResult.normal).normalized;
            return;
        }

        //FIX STUCK IN HOLE => prendre la parallèle a la slope devant sois plutôt que dessous
        if(xInput != 0 && !isMoving && slopeHitHorizontalFrontResult && slopeHitVerticalResult) {
            slopeNormalPerp = Vector2.Perpendicular(slopeHitHorizontalFrontResult.normal).normalized;
            return;
        }

        //SI Que le back touche
        if(!slopeHitHorizontalFrontResult && !slopeHitVerticalResult && slopeHitHorizontalBackResult) {
            slopeNormalPerp = Vector2.Perpendicular(slopeHitHorizontalBackResult.normal).normalized; ;
            return;
        }

        //si front et down dispo, on prends la slope la plus proche
        if(Vector2.Distance(slopeHitHorizontalFrontResult.point, new Vector2(groundCheck.position.x,groundCheck.position.y)) 
            < Vector2.Distance(slopeHitVerticalResult.point, new Vector2(groundCheck.position.x, groundCheck.position.y))){
            slopeNormalPerp = Vector2.Perpendicular(slopeHitHorizontalFrontResult.normal).normalized;
        } else {
            slopeNormalPerp = Vector2.Perpendicular(slopeHitVerticalResult.normal).normalized;
        }
    */
    }

 
    private void SwitchPhysicMaterial() {
        //Debug.Log("front angle " + slopeFrontAngle + " ; " + "back angle " + slopeBackAngle + " ; " + "Down angle " + slopeDownAngle);
        /*
        */

        if(slopeFrontAngle <= maxSlopeAngle) {
            canWalkOnFrontSlope = true;
        } else {
            canWalkOnFrontSlope = false;
        }

        if(slopeDownAngle <= maxSlopeAngle) {
            canWalkOnDownSlope = true;
        }else {
            canWalkOnDownSlope = false;
        }

     
        if(xInput == 0 && !isSliding) {
            if(canWalkOnFrontSlope || canWalkOnDownSlope) {
                rb.sharedMaterial = maxFriction;
            } else {
                rb.sharedMaterial = minFriction;
            }
        } else {
            rb.sharedMaterial = minFriction;
        }



        /*
        if(slopeDownAngle > maxSlopeAngle || slopeFrontAngle > maxSlopeAngle || slopeBackAngle >maxSlopeAngle) {
            canWalkOnSlope = false;
        } else {
            canWalkOnSlope = true;
        }


        if(canWalkOnSlope && xInput == 0.0f && !isSliding) {
            rb.sharedMaterial = maxFriction;
        } else {
            rb.sharedMaterial = minFriction;
        }
        */

        /*
        if(slopeDownAngle > maxSlopeAngle || slopeSideAngle > maxSlopeAngle) {
            canWalkOnSlope = false;
        } else {
            canWalkOnSlope = true;
        }

        if(canWalkOnSlope && xInput == 0.0f && !isSliding) {
            rb.sharedMaterial = maxFriction;
            return;
        }

        if(!canWalkOnSlope && slopeDownAngle <= maxSlopeAngle && slopeSideAngle > maxSlopeAngle && xInput == 0.0f) {
            rb.sharedMaterial = maxFriction;
            return;
        }

        rb.sharedMaterial = minFriction;
        */
    }




    private void ApplyMovement() {

        //SI FONCE DANS UN GROUND A 90degré
        
        /*
        if(slopeFrontAngle > maxSlopeAngle) {
            newVelocity.Set(0, rb.linearVelocity.y);
            rb.linearVelocity = newVelocity;
            return;
        }
        */

        //AIR MOVEMENT
        if(!isGrounded) { 

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
        if(isGrounded /*&& canWalkOnSlope */&& !isJumping) { 
            if(/*canWalkOnFrontSlope || canWalkOnDownSlope*/ 1==1) {
                currentmovementSpeed = movementSpeed;

                newVelocity.Set(movementSpeed * slopeNormalPerp.x * -xInput, movementSpeed * slopeNormalPerp.y * -xInput);
                Debug.DrawRay(transform.position, newVelocity, Color.cyan);

                //FIX Décallage avec le sol          
                if(isGroundedButFarFromGround) {
                    //float _addedVeloToGround = movementSpeed * Mathf.Abs(xInput) / 10;
                    //float _addedVeloToGround = movementSpeed;
                    //newVelocity.Set(newVelocity.x, newVelocity.y - movementSpeed * Mathf.Abs(xInput) / 10);
                    Vector2 _addedVeloToGround = -hitResultRay.normal * movementSpeed;
              
                    newVelocity.Set(newVelocity.x+ _addedVeloToGround.x, newVelocity.y +_addedVeloToGround.y);
                    Debug.DrawRay(transform.position, newVelocity, Color.black);
                }

                //FIX SHARP ANGLE PROB TO STAY ON GROUND
                Vector2 _checkPos = transform.position - (Vector3) (new Vector2(0.0f, capsuleColliderSize.y / 2));
                Vector2 _predictedPoint = new Vector2(_checkPos.x, _checkPos.y) + rb.linearVelocity * Time.deltaTime;
                RaycastHit2D _secondVerticalhit = Physics2D.Raycast(_predictedPoint, Vector2.down, slopeCheckVerticalDistance * 10, whatIsGround);
                if(_secondVerticalhit && Vector2.Angle(slopeHitVerticalResult.normal, _secondVerticalhit.normal) >= maxAngleBeforeVeloCorrection) {
                    //Debug.Log("FIX");
                   
                    newVelocity.Set(newVelocity.x, newVelocity.y - movementSpeed * Mathf.Abs(xInput));

                }

                rb.linearVelocity = newVelocity;

                return;
            }
  
        }
    }

    private void UpdateAnimator() {
        myAnimator.SetFloat(SRAnimators.Animator_Hero1.Parameters.xVelocity, Mathf.Round(rb.linearVelocity.x));
        myAnimator.SetFloat(SRAnimators.Animator_Hero1.Parameters.yVelocity, Mathf.Round(rb.linearVelocity.y));
        myAnimator.SetBool(SRAnimators.Animator_Hero1.Parameters.isGrounded, isGrounded);
        myAnimator.SetBool(SRAnimators.Animator_Hero1.Parameters.isSliding, isSliding);
        myAnimator.SetBool(SRAnimators.Animator_Hero2.Parameters.isMoving, isMoving);
    }



    private void OnDrawGizmos() {

        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(groundCheck.position, secondGroundCheckRadius);

        //Gizmos.color = Color.magenta;
        //Gizmos.DrawWireSphere(((Vector3) intersectionForVeloCorrection), 0.1f);

        if(hitResultCircleCast) {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(hitResultCircleCast.point, 0.05f);
        }

        if(hitResultCircleCastPrevious){
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(hitResultCircleCastPrevious.point, 0.04f);
        }
    }
       
}
