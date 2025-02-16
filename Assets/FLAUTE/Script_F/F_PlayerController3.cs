using Unity.VisualScripting;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Windows;
using static UnityEditor.ShaderGraph.Internal.KeywordDependentCollection;


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
    public float canCheckGroundCounter = 0;
    private bool isInAirAndTouchingNonWalkableSlope = false;


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

        if(!myGroundCheck.IsGrounded && !myGroundCheck.IsGroundOverlaped) {
            canCheckGround = false;
        }

        if(canCheckGround) {
            myGroundCheck.CheckGroundEnabled = true;
        } else {
            myGroundCheck.CheckGroundEnabled = false;
        }

        if(myGroundCheck.IsGrounded) {
            xMomentum = 0;
            isInAirAndTouchingNonWalkableSlope = false;
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
            //canCheckGround = true;
            //canCheckGroundCounter = canCheckGroundMaxTime;
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
            myGroundCheck.CheckGroundEnabled = false;


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


    bool notMovingButTryingToWalk = false;

    private void ApplyMovement() {

 

        //AIR MOVEMENT
        //si pas au sol et pas en contact d'une pente trop abrupte, mais l'angle ne doit pas non plus être égal a 0 (c
        if(!myGroundCheck.IsGrounded && !isInAirAndTouchingNonWalkableSlope) {

            //le clamp min et max de la xVelo est relatif en %age à la velo de départ au moment où le joueur a décollé du sol
            //Set xMomentum 1 fois quand !isGrounded,    

            float _xMomentumAbs = Mathf.Abs(xMomentum);
            float _xVeloAddedWithInput;


            currentmovementSpeed = Mathf.Clamp(currentmovementSpeed + Time.fixedDeltaTime * movementSeepAccel, 0, movementSpeed);
            _xVeloAddedWithInput = Mathf.Clamp(rb.linearVelocity.x + currentmovementSpeed * xInput, -_xMomentumAbs - currentmovementSpeed, _xMomentumAbs + currentmovementSpeed);

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

            if(xInput == 0) {
                newVelocity = Vector2.zero;
                rb.linearVelocity = newVelocity;
                return;
            }

            //FIX STUCK IN HOLE
            RaycastHit2D _chosenHit;

            //si angle entre les deux resultat supérieur a 90 => on est dans un trou
            bool _isInHole = Vector2.Angle(myGroundCheck.GroundHitResult.normal, myGroundCheck.GroundSecondHitResult.normal) >= 90 ? true : false;
            
            
            //Debug.Log("DOT R1 = " + Vector2.Dot(myGroundCheck.GroundHitResult.normal,transform.right));
            //Debug.Log("DOT R2 = " + Vector2.Dot(myGroundCheck.GroundSecondHitResult.normal,transform.right));


            //si dans un trou et que le result1 provient du raycast back, on choisit le result2, sinon le result1
            //si pas dans un trou on prends result1
            Vector2 _directionRaycastOfFirstResult = (myGroundCheck.GroundHitResult.point - (Vector2) myGroundCheck.transform.position).normalized;

            if(_isInHole) {
                Debug.Log("DANS UN TROU ");
                if(Vector2.Angle(_directionRaycastOfFirstResult, -transform.right) == 0) {
                    _chosenHit = myGroundCheck.GroundSecondHitResult;
                } else {
                    _chosenHit = myGroundCheck.GroundHitResult;
                }
            } else {
                _chosenHit = myGroundCheck.GroundHitResult;
            }
            //Vector2 _directionSecondResult = (myGroundCheck.GroundSecondHitResult.point - (Vector2) myGroundCheck.transform.position).normalized;
            //Vector2 _originSecondResult = myGroundCheck.GroundSecondHitResult.point - myGroundCheck.GroundSecondHitResult.distance * _directionSecondResult;

            /*
            */



            //REMETTRE ? NON normalement
            /*
            if(Vector2.Angle(myGroundCheck.GroundHitResult.normal, myGroundCheck.GroundSecondHitResult.normal) > 90) {
                _chosenHit = myGroundCheck.GroundSecondHitResult;
            } else {
                _chosenHit = myGroundCheck.GroundHitResult;
            }
            */



            /*
            _chosenHit =
                Vector2.SignedAngle(-transform.right, myGroundCheck.GroundHitResult.normal) < Vector2.SignedAngle(-transform.right, myGroundCheck.GroundSecondHitResult.normal) ?
                myGroundCheck.GroundHitResult : myGroundCheck.GroundSecondHitResult;
            */
            /*
            if(xInput != 0 && !isMoving) {

                if(notMovingButTryingToWalk) { //STUCK, on choisit le second ground touché
                    _chosenHit = myGroundCheck.GroundSecondHitResult;
                    Debug.Log("STUCK SO SECOND CHOICE");
                    notMovingButTryingToWalk = false;
                } else {
                    notMovingButTryingToWalk = true;
                }
                
            } else if (xInput != 0 && isMoving || xInput == 0 && !isMoving) {
                notMovingButTryingToWalk = false;
            }
            */

            Vector2 _walkDirection = Vector2.Perpendicular(_chosenHit.normal).normalized;


            newVelocity.Set(movementSpeed * _walkDirection.x * -xInput, movementSpeed * _walkDirection.y * -xInput);
            Debug.DrawRay(transform.position, newVelocity, Color.cyan);

            //FIX Décallage avec le sol          
            if(myGroundCheck.IsGroundedButFarFromGround) {
             
                Vector2 _veloToDown = Vector2.down * Mathf.Abs(xInput) * movementSpeed;
                newVelocity = newVelocity + _veloToDown;
                Debug.Log("FIX GROUND DECALAGE");
            }

            /*

            //si dans un trou pas la correction d'apres
            if(_isInHole) {
                rb.linearVelocity = newVelocity;
                return;
            }

            //FIX SHARP ANGLE CHANGE PROB TO STAY ON GROUND
            //=> marche pas trop mais trop galère

            //TODO
            //On sette un predicted point selon notre velocité
            //On tire un raycast vers lui et de sa distance pour savoir si il est dans une collision
            //Si dans une collision on arrête (ne change pas la newVelo)
            //Sinon on tire un raycast depuis predicted point vers transform.right de la distance myGroundCheck.CheckedDistance
            //Si on touche on touche on arrête (ne change pas la newVelo)
            //Sinon on est dans le cas de décrochage predict => on vérifie dot et angle pour modifier newVelo ou non


            predictedPoint = (Vector2) myGroundCheck.transform.position + newVelocity * Time.deltaTime;
            RaycastHit2D _hitToPredictedPoint = Physics2D.Raycast(myGroundCheck.transform.position, newVelocity.normalized, predictedPoint.magnitude, myGroundCheck.GroundLayer);
            //RaycastHit2D _hitToPredictedPoint = Physics2D.Raycast(myGroundCheck.transform.position,transform.right, myGroundCheck.GroundLayer);
   


            if(_hitToPredictedPoint) {
                Debug.Log("predictedPoint IN Collider : NO FIX");
            } else {
                //on touche pas => soit y'a du vide après, soit y'a une autre pente
                RaycastHit2D _secondVerticalhit = Physics2D.Raycast(predictedPoint, Vector2.down, myGroundCheck.CheckedDistance * 10, myGroundCheck.GroundLayer);

                if(!_secondVerticalhit) {
                    Debug.Log("VIDE APRES PENTE : NO FIX");
                    //Debug.Log("DOT = " + Vector2.Dot(_chosenHit.normal, _secondVerticalhit.normal));
                    //Debug.Log("ANGLE = " + Vector2.Angle(_chosenHit.normal, _secondVerticalhit.normal));
                } else if(_secondVerticalhit && _chosenHit.normal != _secondVerticalhit.normal && Vector2.Dot(_chosenHit.normal, _secondVerticalhit.normal) <= 0 && Vector2.Angle(_chosenHit.normal, _secondVerticalhit.normal) <= 90) {//90 ou 180

                        Debug.Log("FIX Decrochage predict"); // MARCHE MAL sur des angles de + 90
                        Debug.Log("DOT = " + Vector2.Dot(_chosenHit.normal, _secondVerticalhit.normal));
                        Debug.Log("ANGLE = " + Vector2.Angle(_chosenHit.normal, _secondVerticalhit.normal));

                        //la vitesse ajoutée pour stick devrait dépendre de l'ampleur du décrochage = _secondVerticalhit.distance ?
                        float _addedVeloToStickGround = movementSpeed * Mathf.Abs(xInput) * _secondVerticalhit.distance * movementSpeed;
                        Debug.Log("_secondVerticalhit distance = " + _secondVerticalhit.distance);
                        newVelocity.Set(newVelocity.x, newVelocity.y - _addedVeloToStickGround);
                    //newVelocity.Set(newVelocity.x, newVelocity.y - movementSpeed * Mathf.Abs(xInput)); 
                } else {
                    Debug.Log("PENTE NON PRATIQUABLE APRES PENTE ou même pente : NO FIX");
                }

            }                  
           */


            //MOVE
            rb.linearVelocity = newVelocity;

            return;
        }
    }

 
    Vector2 predictedPoint = new Vector2();

    private void UpdateAnimator() {
        myAnimator.SetFloat(SRAnimators.Animator_Hero1.Parameters.xVelocity, Mathf.Round(rb.linearVelocity.x));
        myAnimator.SetFloat(SRAnimators.Animator_Hero1.Parameters.yVelocity, Mathf.Round(rb.linearVelocity.y));
        myAnimator.SetBool(SRAnimators.Animator_Hero1.Parameters.isGrounded, myGroundCheck.IsGrounded);
        myAnimator.SetBool(SRAnimators.Animator_Hero1.Parameters.isSliding, isSliding);
        myAnimator.SetBool(SRAnimators.Animator_Hero2.Parameters.isMoving, isMoving);
    }



    private void OnDrawGizmos() {



        //Gizmos.color = Color.magenta;
        //Gizmos.DrawWireSphere(predictedPoint, 0.15f);
    }
       
}
