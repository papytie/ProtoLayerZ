using UnityEngine;

[RequireComponent(typeof(PlayerControls), typeof(Animator), typeof(Rigidbody2D))]
[RequireComponent(typeof(CapsuleCollider2D))]
public class PlayerController : MonoBehaviour
{
    PlayerControls inputs;
    Animator animator;
    Rigidbody2D playerRigidbody;
    CapsuleCollider2D capsuleCollider;

    float moveValue;
    bool isJumping;
    bool isSliding;

    [SerializeField] float moveSpeed = 1.0f;
    [SerializeField] float checkDist = 10f;
    [SerializeField] LayerMask ground;

    RaycastHit2D hit;

    private void Awake()
    {
        inputs = GetComponent<PlayerControls>();
        animator = GetComponent<Animator>();
        playerRigidbody = GetComponent<Rigidbody2D>();
        capsuleCollider = GetComponent<CapsuleCollider2D>();
    }

    private void Update()
    {
        moveValue = inputs.Move.ReadValue<float>();
        isJumping = inputs.Jump.ReadValue<bool>();
        isSliding = inputs.Slide.ReadValue<bool>();

        hit = Physics2D.Raycast(transform.position, Vector2.down, checkDist, ground);

        if(moveValue != 0f && hit)
        {
            playerRigidbody.linearVelocity = moveSpeed * moveValue * -Vector2.Perpendicular(hit.normal) ;
            
        }
    }

    private void OnDrawGizmos()
    {
        if(Application.isPlaying)
        {
            if (hit)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(transform.position, hit.point);
                Gizmos.DrawSphere(hit.point, 0.1f);    
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(hit.point, hit.point + hit.normal*2);
                Gizmos.color = Color.blue;
                Gizmos.DrawLine(transform.position, new Vector2(transform.position.x, transform.position.y) + (moveSpeed * moveValue * -Vector2.Perpendicular(hit.normal)));
            }
            else
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(transform.position, transform.position + new Vector3(0f,-checkDist,0f));
            }
            
        }
    }
}
