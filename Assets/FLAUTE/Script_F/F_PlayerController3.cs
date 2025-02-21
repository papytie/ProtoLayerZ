using TMPro;
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
    InputAction rocket;

    public F_GroundCheck myGroundCheck;
    public ParticleSystem jetpackParticles;

    [SerializeField] private float movementForce =8;
    [SerializeField] private float airControlForce = 5;
    [SerializeField] private float jumpForce = 20;
    [SerializeField] private float jetpackForce = 7;
    [SerializeField] private float maxJetpackForce = 6;
    [SerializeField] private float absoluteVelocityMagnitudeLimit = 150;
    
    [SerializeField] private float hangTime = 0.2f;
    private float hangCounter;
    [SerializeField] private float jumpBufferLength = 0.2f;
    private float jumpBufferCount;
    [SerializeField] private float inertiaMaxTime = 0.2f;
    private float inertiaCounter;
    //[SerializeField] private float physicInputMaxTime = 0.2f;
    //private float physicInputCurrentTime = 0.2f;
    
    [SerializeField] float jumpForceMagnitudeMultiplier = 0.25f;
    [SerializeField] private PhysicsMaterial2D minFriction;
    [SerializeField] private PhysicsMaterial2D maxFriction;
    [SerializeField] private float veloDividerWhenJumping = 1;
    [SerializeField] private float maxAngleBeforeVeloCorrection = 45;
    [SerializeField] private float maxDistVectorForVeloCorrection = 20;
    [SerializeField] float movementSeepAccel = 8;
    [SerializeField] float canCheckGroundMaxTime = 0.2f;
    public float canCheckGroundCounter = 0;
    [SerializeField] private bool isInAirAndTouchingNonWalkableSlope = false;
    [SerializeField] float velocityMagnitude;
    [SerializeField] AnimationCurve inputInfluenceOnInertia;
    

    public float xInput;
    public float physicInput;
    public float rocketInput;
    public Vector2 savedVelocity;
    public float xMomentum = 0;
    public float yMomentum = 0;

    public bool canCheckGround = true;
    public bool canActiveInertia = false;

    public bool isSliding;
    public bool isControledByPhysic;
    public bool isMoving;
    public bool isJumping;
    public bool isJetpackActivated;
    public bool isInInertia;
    [SerializeField] bool inertiaMovementActivated = false;
    
    [SerializeField] float inertiaThreshold;
    [SerializeField] float currentInertiaThreshold = 8;
   
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
    [SerializeField] float strateFrictionNorm = 0.02f;
    public float MovementSpeed => movementForce;

   
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
        //il faudra une autre variable que movementForce qui puisse être affectée par la Strate => movementForce (truc qui ne change pas) et movementSpeed(force/gravité/friction)
        //dans l'idée :
        //float _movementSpeed = movementForce * strateGravityScale * strateFrictionNorm;
        inertiaThreshold = movementForce; //* 2;

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

        //momentum was here
  

     

        //OTHER STUFF
        if(rb.linearVelocity.y <= 0.0f && canCheckGround || myGroundCheck.IsGrounded ||isJetpackActivated) {
            isJumping = false;
        }

        if(hangCounter > 0 && !isJumping) {
            canJump = true;
        }


        UpdateCounters();
        CheckState(); // isMoving, isSliding, isInInertia
    }

    private void FixedUpdate() {

        CheckJump();
        UpdateMomentum();
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
        //slideInput = slide.ReadValue<float>();
        physicInput = slide.ReadValue<float>();
        rocketInput = rocket.ReadValue<float>();
    }
    private void CheckFlip() {

        float _signInput = Mathf.Sign(xInput);

        if(!isControledByPhysic && xInput != 0 && facingDirection != _signInput) {
            facingDirection = _signInput;
            transform.Rotate(0.0f, 180.0f, 0.0f);
            return;
        }

        float _signVeloX = Mathf.Sign(rb.linearVelocity.x);

        if(isControledByPhysic && _signVeloX != 0 && facingDirection != _signVeloX) {
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

        //INERTIA COUNTER
        if(rb.linearVelocity.magnitude > inertiaThreshold) {
            inertiaCounter += Time.deltaTime;
        } else {
            inertiaCounter = 0;
        }
    }


    private void CheckState() {

        ////// //isMoving /////
        isMoving = rb.linearVelocity.magnitude >= 0.01f ? true : false;

        //l'inertia ne peut s'actier qu'a la suite d'un physic movement
        canActiveInertia = isControledByPhysic;
        

        ////// isInInertia /////
        //inertiaMovementActivated => condition supplémentaire qui ne sers qu'à setter isInInertia
        if(inertiaMovementActivated && rb.linearVelocity.magnitude <= currentInertiaThreshold/*inertiaThreshold   /*<= 0.01f */) {
            inertiaMovementActivated = false; //sort du mode inertie
        }
        //isInInertia => délai d'activation mais sinon devient TRUE si on prends trop de vitesse avec la physique AU SOL (mais on peut y rester dans les airs car la désactivation dépends que de la velo (et du jump)
        if(rb.linearVelocity.magnitude > inertiaThreshold && inertiaCounter >= inertiaMaxTime && physicInput == 0 && myGroundCheck.IsGrounded && canActiveInertia) {
            isInInertia = true;
            inertiaMovementActivated = true; //passe en mode inertie => permet de rester dans ce mode même quand physicInput = 0
        } else if(!inertiaMovementActivated) { //si on appuie plus sur physicInput et qu'on une fable velo
            isInInertia = false;
        }
        /*
        //(facultatif) change le seuil qui permet de décider a quel moment on va trop vite
        if(myGroundCheck.IsGrounded && xInput != 0) {
            currentInertiaThreshold = inertiaThreshold;
        } else {
            currentInertiaThreshold = 0.1f;
        }
        */


        ////// isSliding /////
        // quand on est sur le sol ET qu'on est en inertie, on glisse
        // mouvement exprès dans apply movement
        if(myGroundCheck.IsGrounded && isInInertia) {
            isSliding = true;
        } else {
            isSliding  = false;
        }

        ///// isFlying ////
        //serait quand on est dans les airs et en inertie, on est projeté
        //inclus dans AIR MOVEMENT via savedVelo ?


        ////// //isControledByPhysic ////
        if(physicInput != 0) {
            isControledByPhysic = true;
        } else {
            isControledByPhysic = false;
        }


        ////// isJetpackActivated /////
        if(rocketInput != 0) { //ajouter && waterFuel > 0
            isJetpackActivated = true;
        } else {
            isJetpackActivated = false;
        }


        /*
        if(slideInput != 0 && myGroundCheck.IsGrounded) {
            isSliding = true;
        } else {
            if(isSliding) {
                //Debug.Log("save slide velo");
                //slideVelo = rb.linearVelocity;
            }
            isSliding = false;
        }
        */

        //TO DO : Faire en sorte que dans certains cas (si le player ne se déplace pas au sol) on sorte de l'inertie de façon smooth
        // => inertiaThreshold à 0.01f | quand on est en l'air OU quand on est au sol et xInput = 0;
        //Problème quand on est en InertiaMovement on est pas en AirMovement
        //Faire : Inertia Ground Movement, Inertia Air Movement, Ground Movement, Air Movement ?
        
        //il faudrait simplifier
        //et avoir un vecteur d'inertie (qui se réduit progressivement selon friction(qui pourrait changer a terme selon on ground ou in air) et est influencée par xInput)
        //qui s'ajoute a la newVelo, puis faire les mouvement Air ou Ground.

      
    }



  //FIXED UPDATE
    private void CheckJump() {

        if(jumpBufferCount <= 0) {
            tryToJump = false;
        }

        if(tryToJump && canJump) {
            canJump = false;
            isJumping = true;
            //Debug.Log("isJumping = " + isJumping);
            canCheckGround = false;
            myGroundCheck.CheckGroundEnabled = false;
            isSliding = false;
            tryToJump = false;
            inertiaMovementActivated = false; //a garder ????

            //simplifiable
            newVelocity.Set(rb.linearVelocity.x/ veloDividerWhenJumping, rb.linearVelocity.y/ veloDividerWhenJumping);
            rb.linearVelocity = newVelocity;

            //changer la direction du jump selon le cas
            Vector2 _jumpDirection;
            //si on va vers le haut
            if(rb.linearVelocity.y >= 0) {
                Debug.Log("on va vers le haut");
                _jumpDirection = Vector2.up;
            } else {                
                if(isControledByPhysic || isSliding || isInInertia) {
                    //_jumpDirection = Vector2.Reflect(Vector2.down, myGroundCheck.GroundHitResult.normal);
                    Debug.Log("on va vers l'avant");
                    float _angle = Vector2.Angle(myGroundCheck.GroundHitResult.normal, Vector2.up);
                    _jumpDirection = myGroundCheck.RotateVector(myGroundCheck.GroundHitResult.normal, _angle / 2);
                } else {
                    _jumpDirection = Vector2.up;
                }
                
            }
            //newForce.Set(0.0f, jumpForce + rb.linearVelocity.magnitude * jumpForceMagnitudeMultiplier);
            newForce = _jumpDirection * (jumpForce + rb.linearVelocity.magnitude * jumpForceMagnitudeMultiplier);
            
            rb.AddForce(newForce, ForceMode2D.Impulse);
            newVelocity = rb.linearVelocity;
            xMomentum = rb.linearVelocity.x;
            yMomentum = rb.linearVelocity.y;
            Debug.Log("JUMP xMomentum = " + xMomentum);
            Debug.Log("JUMP newVelocity = " + newVelocity);
        }
    }

    private void UpdateMomentum() {

        /*
        if(myGroundCheck.IsGrounded && !isSliding && !isInInertia) {
            xMomentum = 0;
            yMomentum = 0;
            //isInAirAndTouchingNonWalkableSlope = false;
        } else {

            xMomentum = rb.linearVelocity.x;
            yMomentum = rb.linearVelocity.y;
            
            //if(xMomentum == 0) {
            //    xMomentum = rb.linearVelocity.x;
            //    yMomentum = rb.linearVelocity.y;
            //    //currentmovementSpeed = 0;
            //}
        }
        */

        //Si en Inertia movement, on décrémente l'élan
        //Si on est dans les airs on décrémente aussi
        if(isInInertia || !myGroundCheck.IsGrounded) {
            float _frictionNorm = Mathf.Clamp01(strateFrictionNorm);

            float _xVeloDirection = Mathf.Sign(rb.linearVelocity.x);

            float _xInputInfluence = xInput * _xVeloDirection;
            float _xCoefMult = inputInfluenceOnInertia.Evaluate(_xInputInfluence);

            float _xfrictionCoef = 1 - _frictionNorm * _xCoefMult;
            float yfrictionCoef = 1 - _frictionNorm;
            
            if(savedVelocity.magnitude <= 0.01f) {
                savedVelocity = Vector2.zero;
            } else {
                savedVelocity.Set(savedVelocity.x * _xfrictionCoef, savedVelocity.y * yfrictionCoef);
            }
            

        } else {
            //si on est sur le sol sans élan
            //quand on est en l'air il faut la réduire progressivement, mais si on se lâche dans les airs et qu'on remet le jetpack on derait pas avoir re save la velo ?
            savedVelocity = rb.linearVelocity;
        }

        //on veut réduire la save inertie progressivement jusqu'à 0 quand :
        //on slide (sol + inertia)
        //on est en air control ?
        //on est en jetpack

        //on est en air control et en jetpack en même temps

        Debug.Log("isInInerta (saved velo reduc) ? " + isInInertia);
    }

    private void UpdatePhysicParams() {

        //PHYSIC MAT
        if(myGroundCheck.IsGrounded && !isControledByPhysic && xInput == 0 && !isInInertia && !isJumping) {
            rb.sharedMaterial = maxFriction;
        } else {
            rb.sharedMaterial = minFriction;
        }

        //GRAVITY
        if(myGroundCheck.IsGrounded && !isControledByPhysic && !isInInertia && !isJumping) {
            rb.gravityScale = 0;
        } else {
            rb.gravityScale = strateGravityScale;
        }
    }

    public Vector2 inertiaVelo;

    private void ApplyMovement() {
 

        // PHYSIC MOVEMENT
        //on ne touche pas la velo, la physique s'en charge
        if(isControledByPhysic) {
            //currentmovementSpeed = movementForce;
            newVelocity = rb.linearVelocity;
            Debug.Log("sliding");
            return;
        }

        // TODO SLIDE MOVEMENT (s'inspirer d'INERTIA MOVEMENT)

        /*     


        //INERTIA VELOCITY
        //peut être sur le sol ou dans les airs
        //Vector2 _InertiaVelo = Vector2.zero;
        inertiaVelo = Vector2.zero;
        if(myGroundCheck.IsGrounded && !isSliding && isInInertia) {
            Debug.Log("INERTIA MOOV");
            float _frictionNorm = Mathf.Clamp01(strateFrictionNorm);

            float _xVeloDirection = Mathf.Sign(rb.linearVelocity.x);
            float _inputInfluence = xInput * _xVeloDirection;
            float _coefMult = inputInfluenceOnInertia.Evaluate(_inputInfluence);

            float _frictionCoef = 1 - _frictionNorm * _coefMult;
                                                                
            newVelocity = rb.linearVelocity * _frictionCoef;

            //inertiaVelo = newVelocity * _frictionCoef;
            //newVelocity = inertiaVelo;

        }

        */

        /*
        //AIR MOVEMENT
        //si pas au sol et pas en contact d'une pente trop abrupte, mais l'angle ne doit pas non plus être égal a 0 (c
        if(!myGroundCheck.IsGrounded && !isSliding) { //&& !isInAirAndTouchingNonWalkableSlope
            Debug.Log("AIR MOOV");
  
            float _xMomentumAbs = Mathf.Abs(xMomentum);
            Debug.Log("AIR _xMomentumAbs = " + _xMomentumAbs);
            float _xInputInfluence = xInput * movementForce;

            //de toute façon il faut changer, faire un saut moins arcade, on a toujours la même influence dessus, quelque soit la vitesse en l'air
            float _minClamp = -_xMomentumAbs - movementForce; 
            float _maxClamp = _xMomentumAbs + movementForce;
            float _xNewVeloClamped = Mathf.Clamp(_xInputInfluence, _minClamp, _maxClamp);

            newVelocity.Set(_xNewVeloClamped, rb.linearVelocity.y);
            Debug.Log("Air Moov newVelocity  = " + newVelocity);
            //newVelocity += inertiaVelo;
        }
       */



        //AIR MOVEMENT 2
        if(!myGroundCheck.IsGrounded && !isControledByPhysic) {

            Debug.Log("AIR MOOV");
            
            float _xInputInfluence = xInput * airControlForce;
            float _xMomentumAbs = Mathf.Abs(savedVelocity.x);

            float _minClamp = -_xMomentumAbs - movementForce;
            float _maxClamp = _xMomentumAbs + movementForce;

            float _xNewVeloClamped = Mathf.Clamp(_xInputInfluence, _minClamp, _maxClamp);
            newVelocity.Set(savedVelocity.x + _xNewVeloClamped, rb.linearVelocity.y);

        }


        // BASE MOVEMENT : if is on slope / ground
        else if(myGroundCheck.IsGrounded && !isJumping && !isControledByPhysic ) { //&& !isSliding
            Debug.Log("SOL MOOV");
            currentmovementSpeed = movementForce;
      
            Vector2 _walkDirection = -Vector2.Perpendicular(myGroundCheck.GroundHitResult.normal).normalized;

            
            newVelocity.Set(movementForce * _walkDirection.x * xInput, movementForce * _walkDirection.y * xInput);
            //newVelocity += inertiaVelo;
            Debug.DrawRay(transform.position, newVelocity, Color.cyan);
            float _dir = Mathf.Sign(xInput) != 0 ? Mathf.Sign(xInput) : 0;



            //FIX Décallage avec le sol           
            if(myGroundCheck.IsGroundedButFarFromGround /*&& !isJetpackActivated*/) {

                //float _xInputMinForced = Mathf.Abs(xInput) >= 01f ? Mathf.Abs(xInput) : 0.1f;

                //Vector2 _veloToDown = Vector2.down * correctionSpeed; //* _xInputMinForced;
                Vector2 _veloToDown = Vector2.down * (rb.linearVelocity.magnitude+1) ; //* _xInputMinForced;
                newVelocity = newVelocity + _veloToDown;
                Debug.Log("FIX GROUND DECALAGE");
            }                 
        }

        /*
        */
        //JETPACK MOVEMENT
        //s'ajoute a l'inertia movement ?
        //s'ajoute a air movement ?
        //s'ajoute a walk movement ?
        if(isJetpackActivated) {
            float _yMomentumAbs = Mathf.Abs(yMomentum);
            float _jetpackInputInfluence = rocketInput * jetpackForce;

            float _yNewVeloClamped = Mathf.Clamp(_jetpackInputInfluence, -_yMomentumAbs - jetpackForce, _yMomentumAbs + jetpackForce);
            newVelocity.Set(newVelocity.x, _yNewVeloClamped);

            //float _jetpackAddedVeloClamped = Mathf.Clamp(_jetpackInputInfluence, -maxJetpackForce, maxJetpackForce);
            //newVelocity.Set(newVelocity.x, newVelocity.y + _jetpackInputInfluence);
        }




        

        //APPLY VELOCITY
        //Debug.Log("APPLY VELO");
        //rb.linearVelocity = newVelocity;
        rb.linearVelocity = newVelocity.normalized * Mathf.Clamp(newVelocity.magnitude,0, absoluteVelocityMagnitudeLimit);




    }

 
    private void UpdateVisual() {
        myAnimator.SetFloat(SRAnimators.Animator_Hero1.Parameters.xVelocity, Mathf.Round(rb.linearVelocity.x));
        myAnimator.SetFloat(SRAnimators.Animator_Hero1.Parameters.yVelocity, Mathf.Round(rb.linearVelocity.y));
        myAnimator.SetBool(SRAnimators.Animator_Hero1.Parameters.isGrounded, myGroundCheck.IsGrounded);
        myAnimator.SetBool(SRAnimators.Animator_Hero1.Parameters.isSliding, isControledByPhysic);
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
            if(isControledByPhysic|| isSliding || isInInertia) {
                float _angle = Vector2.Angle(myGroundCheck.GroundHitResult.normal, Vector2.up);
                Gizmos.DrawRay(transform.position, myGroundCheck.RotateVector(myGroundCheck.GroundHitResult.normal, _angle / 2));
            } else {
                Gizmos.DrawRay(transform.position, Vector2.up);
            }

        }

    }
       
}
