using UnityEngine;

[RequireComponent (typeof(PlayerControls))]

public class CustomPhysicMovement : MonoBehaviour
{
    PlayerControls playerControls;
    CircleCollider2D baseCircleCollider;

    Vector2 velocityVector;

    [SerializeField] ContactFilter2D collisionFilter = new ContactFilter2D();

    [SerializeField] float acceleration = 50;
    [SerializeField] float minSpeed = 1;
    [SerializeField] float maxVelocity = 10;
    [SerializeField] float airFriction = 50;
    [SerializeField] float groundFriction = 1;
    [SerializeField] float gravity = 1;
    [SerializeField] float jumpForce = 1;
    [SerializeField] float collisionCheckDistanceMult = 2;
    [SerializeField] float groundSnapDistance = 1;

    bool isJumping = false;
    bool isOnGround = false;
    float currentSpeed = 0;

    private void Awake()
    {
        playerControls = GetComponent<PlayerControls>();
        baseCircleCollider = GetComponentInChildren<CircleCollider2D>();
    }

    private void Update()
    {
        float moveValue = playerControls.Move.ReadValue<float>();
        int horizontalDirection;

        if (playerControls.Jump.WasPressedThisFrame() && isOnGround)
        {
            velocityVector.y += jumpForce;
            isJumping = true;
            isOnGround = false;
        }
        else if (!isOnGround) 
        {
            velocityVector.y -= gravity * Time.deltaTime;
        }

        //only apply when move input is pressed
        if (moveValue != 0)
        {
            //define input direction
            horizontalDirection = moveValue > 0 ? 1 : -1;
            //give a base speed when turning or standing still
            if (velocityVector.x * horizontalDirection < minSpeed) velocityVector.x = minSpeed * horizontalDirection;
            //clamp max velocity
            else if (velocityVector.x * horizontalDirection >= maxVelocity) velocityVector.x = maxVelocity * horizontalDirection;
            //apply acceleration on fixed delta time
            else velocityVector.x += moveValue * acceleration * Time.deltaTime;
        }

        //air friction effect if no input is pressed
        else if(Mathf.Abs(velocityVector.x) > 0)
        {
            //define current movement direction
            horizontalDirection = velocityVector.x > 0 ? 1 : -1;
            //apply friction to slow X vector
            velocityVector.x += airFriction * Time.deltaTime * -horizontalDirection;
            //clamp vector X to never fall down minSpeed value 
            if(Mathf.Abs(velocityVector.x) < minSpeed) velocityVector.x = 0;
        }

        RaycastHit2D[] collisionsHits = new RaycastHit2D[4];

        Physics2D.CircleCast(baseCircleCollider.transform.position, baseCircleCollider.radius, new (velocityVector.x, 0), collisionFilter, collisionsHits, velocityVector.magnitude * Time.fixedDeltaTime * collisionCheckDistanceMult);
        Physics2D.CircleCast(baseCircleCollider.transform.position.ToVector2() + velocityVector * Time.fixedDeltaTime * collisionCheckDistanceMult, baseCircleCollider.radius, Vector2.down, collisionFilter, collisionsHits, velocityVector.magnitude * Time.fixedDeltaTime * collisionCheckDistanceMult);

        foreach (var hit in collisionsHits)
        {
            if (hit)
            {
                if (!isOnGround)
                {
                    isOnGround = true;
                }                
            }    
        }

        //Apply velocity vector after all adjustments
        if (velocityVector.magnitude != 0)
        {
            transform.position += velocityVector.ToVector3() * Time.deltaTime;
        }

    }

    private void OnDrawGizmos()
    {
        if (Application.isPlaying)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(baseCircleCollider.transform.position.ToVector2() + new Vector2(velocityVector.x, 0) * Time.fixedDeltaTime * collisionCheckDistanceMult, baseCircleCollider.radius);
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(baseCircleCollider.transform.position.ToVector2() + new Vector2(velocityVector.x, 0) * Time.fixedDeltaTime * collisionCheckDistanceMult + Vector2.down * velocityVector.magnitude * Time.fixedDeltaTime * collisionCheckDistanceMult, baseCircleCollider.radius);
        }
    }
}
