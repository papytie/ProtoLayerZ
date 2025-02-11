using DG.Tweening;
using System.Collections;
using Unity.VisualScripting;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;
using UnityEngine.InputSystem;
public class F_PlayerController : MonoBehaviour
{
    Controls controls;
    InputAction move;
    InputAction jump;
    InputAction slide;

    Rigidbody2D rigid2d;
    [SerializeField] Rigidbody2D.SlideMovement slideMov = new Rigidbody2D.SlideMovement();
    [SerializeField] Rigidbody2D.SlideResults slideResult;

    [SerializeField] float xInputValue = 0;
    [SerializeField] bool slideInput = false;
    [SerializeField] bool jumpInput = false;
    [SerializeField] bool isOnGround = false;
    [SerializeField] float checkGroundDist = 1;

    [SerializeField] LayerMask groundLayer;
    RaycastHit2D hitResult2D;

    [SerializeField] float jumpPower = 10;
    [SerializeField] float xMovementPower = 2;
    [SerializeField] float xAirMovementPower = 2;
    
    [SerializeField] bool canSlide = true;
    [SerializeField] bool canCheckGround = true;
    //float canSlideStartTime;
    [SerializeField] float canCheckGroundMaxTime = 0.1f;
    [SerializeField] float canCheckGroundCurrentTime;

    [SerializeField] Vector2 gravityForceBase = new Vector2 (0,-9.81f);

    public float strateGravityScale = 1;
    Vector2 directionOnGround = Vector2.zero;

    private void Awake() {
        controls = new Controls();
        move = controls.MainCharacterMap.Walking;
        jump = controls.MainCharacterMap.Jumping;
        slide = controls.MainCharacterMap.Sliding;

        move.Enable();
        jump.Enable();
        slide.Enable();

        jump.performed += Jump;

        rigid2d = GetComponent<Rigidbody2D>();
    }
    void Start()
    {
        SwitchToPhysicMode();
    }

    // Update is called once per frame
    void Update()
    {
        InputListener();

        if(canCheckGround) {
            CheckIsOnGround();
        } else {
            isOnGround = false;
        }
 

        if(isOnGround) {
            if(slideMov.useSimulationMove == true) {
                SwitchToKinematicMode();
            }
            UpdateSlideResult();
        }
 

        if(!canCheckGround) {
            UpdateCanCheckGround();        
        }

        if(!isOnGround) {
            if(slideMov.useSimulationMove == false) {
                SwitchToPhysicMode();
            }
        }
    }

    void InputListener() {
        xInputValue = move.ReadValue<float>();
    }
    void CheckIsOnGround() {

        hitResult2D = Physics2D.Raycast(transform.position, Vector2.down, checkGroundDist, groundLayer);
        isOnGround = hitResult2D;
        //rigid2d.gravityScale = isOnGround ? 0 : strateGravityScale;
    }
    Vector2 slideVelocity = Vector2.zero;
    void UpdateSlideResult() {
        //slideMov.gravity = strateGravityScale * gravityForceBase;

        //TEST
        if(xInputValue != 0) {
            slideMov.startPosition = transform.position;
        } 
     

        directionOnGround = -Vector2.Perpendicular(hitResult2D.normal);
;
        if(isOnGround) {
            slideVelocity = directionOnGround * xInputValue * xMovementPower;
        } else {
            slideVelocity = Vector2.right * xInputValue * xMovementPower;
        }


        slideResult = rigid2d.Slide(slideVelocity, Time.deltaTime, slideMov);


    }


    void UpdateCanCheckGround() {
        
        canCheckGroundCurrentTime += Time.deltaTime;

        if(canCheckGroundCurrentTime >= canCheckGroundMaxTime) {
            canCheckGround = true;
            canCheckGroundCurrentTime = 0;
        }
    }




    private void FixedUpdate() {
        //HorizontalMovement();
        if(!isOnGround) {
            //AirControl();
        }
    }

    void SwitchToPhysicMode() {
        Debug.Log("physics");

        rigid2d.bodyType = RigidbodyType2D.Dynamic;
        rigid2d.gravityScale = strateGravityScale;

        directionOnGround = -Vector2.Perpendicular(hitResult2D.normal);
        ;
        if(isOnGround) {
            slideVelocity = directionOnGround * xInputValue * xMovementPower;
        } else {
            slideVelocity = Vector2.right * xInputValue * xMovementPower;
        }
        rigid2d.linearVelocity = slideVelocity;

        slideMov.useSimulationMove = true;
        slideMov.gravity = Vector2.zero;
    }

    void SwitchToKinematicMode() {
        Debug.Log("Kinematic");
        slideVelocity = rigid2d.linearVelocity;
        rigid2d.Slide(slideVelocity, Time.deltaTime, slideMov);
        rigid2d.bodyType = RigidbodyType2D.Kinematic;
        rigid2d.gravityScale = 0;
        rigid2d.linearVelocity = Vector2.zero;
        slideMov.useSimulationMove = false;
        slideMov.gravity = strateGravityScale * gravityForceBase;
    }
    void Jump(InputAction.CallbackContext _context) {
        Debug.Log("jump !");

        if(!isOnGround) return;

        /*
        canSlide = false;
        rigid2d.linearVelocity = Vector2.zero;

        */

        SwitchToPhysicMode();
        canCheckGround = false;
        Vector2 _jumpForce = jumpPower * Vector2.up;
        rigid2d.AddForce(_jumpForce, ForceMode2D.Impulse);     

    }

  

    void AirControl() {
        Vector2 _xMovementForce = xInputValue * Vector2.right * xMovementPower;
        rigid2d.AddForce(_xMovementForce, ForceMode2D.Force);
    }

    private void OnDrawGizmos() {
        Gizmos.color = isOnGround ? Color.green : Color.red;
        Gizmos.DrawLine(transform.position, transform.position + Vector3.down * checkGroundDist);
        //Gizmos.DrawLine(transform.position, hitResult2D.point);


        Gizmos.color = Color.yellow;
        Vector3 _lineEndPos3D = new Vector3( hitResult2D.normal.x, hitResult2D.normal.y, 0);
        _lineEndPos3D *= 10;
        Gizmos.DrawLine(transform.position, transform.position + _lineEndPos3D);

        Gizmos.color = Color.magenta;
        Vector2 _directionOnGround = -Vector2.Perpendicular(hitResult2D.normal);
        Vector3 _lineEndPosDir3D = new Vector3(_directionOnGround.x, _directionOnGround.y, 0);
        _lineEndPosDir3D*= 10;
        Gizmos.DrawLine(transform.position, transform.position + _lineEndPosDir3D * xInputValue);
    }
}
