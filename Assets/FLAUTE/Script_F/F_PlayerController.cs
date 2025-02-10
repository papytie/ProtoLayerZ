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

    [SerializeField] float jumpPower = 10;
    [SerializeField] float xMovementPower = 2;


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
        
    }

    // Update is called once per frame
    void Update()
    {
        InputListener();
        CheckIsOnGround();
        UpdateSlideResult();


    }

    void InputListener() {
        xInputValue = move.ReadValue<float>();
    }
    void CheckIsOnGround() {

        bool _isHit = Physics2D.Raycast(transform.position, Vector2.down, checkGroundDist,groundLayer);
        isOnGround = _isHit;
    }

    void UpdateSlideResult() {
        Vector2 _slideVelo = new Vector2(xInputValue * xMovementPower, 0f);
        slideResult = rigid2d.Slide(_slideVelo, Time.deltaTime, slideMov);
    }







    private void FixedUpdate() {
        //HorizontalMovement();
    }


    void Jump(InputAction.CallbackContext _context) {

        if(!isOnGround) return;
        Vector2 _jumpForce = jumpPower * Vector2.up;
        rigid2d.AddForce(_jumpForce, ForceMode2D.Impulse);     
    }

    void HorizontalMovement() {
        Vector2 _xMovementForce = xInputValue * Vector2.right * xMovementPower;
        rigid2d.AddForce(_xMovementForce, ForceMode2D.Force);


    }

    private void OnDrawGizmos() {
        Gizmos.color = isOnGround ? Color.green : Color.red;
        Gizmos.DrawLine(transform.position, transform.position + Vector3.down * checkGroundDist);
    }
}
