using UnityEngine;
using static UnityEngine.UI.Image;

[RequireComponent (typeof(PlayerControls))]
public class PlayerMove : MonoBehaviour
{
    [Header("Player")]
    [SerializeField] float minSpeed = 1;
    [SerializeField] float maxSpeed = 1;
    [SerializeField] float jumpForce = 1;
    [SerializeField] float acceleration = 1;
    [Header("Collisions")]
    [SerializeField] LayerMask groundLayer;
    [SerializeField] float snapDist = 0.1f;
    [SerializeField] float checkDist = 0.5f;
    [SerializeField] int checkNumber = 4;
    [SerializeField] int checkAngle = 180;
    [SerializeField] int angleLimit = 90;
    [Header("Physic")]
    [SerializeField] float groundFriction = 1;
    [SerializeField] float groundBrake = 1;
    [SerializeField] float airFriction = 1;
    [SerializeField] float airBrake = 1;
    [SerializeField] float weightFactor = 1;
    [Header("Debug")]
    [SerializeField] bool showDebug = true;
    
    PlayerControls playerControls;
    CircleCollider2D baseCircleCollider;

    bool isSnapToGround = false;

    float verticalMoveValue;
    float horizontalMoveValue;

    Vector2 groundNormal;

    private void Awake()
    {
        playerControls = GetComponent<PlayerControls>();
        baseCircleCollider = GetComponent<CircleCollider2D>();
    }

    void Update()
    {
        float inputValue = playerControls.Move.ReadValue<float>();
        int moveDirection = horizontalMoveValue > 0 ? 1 : -1;

        if (playerControls.Move.WasPressedThisFrame() || playerControls.Move.IsPressed())
        {
            int inputDirection = inputValue > 0 ? 1 : -1;

            if (inputDirection != moveDirection || Mathf.Abs(horizontalMoveValue) < minSpeed)
            {
                horizontalMoveValue = minSpeed * inputDirection;
            }
            else if (Mathf.Abs(horizontalMoveValue) < maxSpeed)
            {
                horizontalMoveValue += acceleration * Time.deltaTime * inputDirection;
            }
            if (Mathf.Abs(horizontalMoveValue) > maxSpeed) horizontalMoveValue = maxSpeed * inputDirection;
        }
        else if (horizontalMoveValue != 0)
        {
            horizontalMoveValue = Mathf.Abs(horizontalMoveValue) < minSpeed ? 0 : horizontalMoveValue - groundBrake * Time.deltaTime * moveDirection;
        }

        if(playerControls.Jump.WasPressedThisFrame() && isSnapToGround)
        {
            isSnapToGround = false;
            verticalMoveValue += jumpForce;
        }
        
        if (isSnapToGround) 
        {
            GroundMove();
        }

        if (!isSnapToGround)
        {
            GravityPull();
            AerialMove();
        }
    }

    void GroundMove()
    {
        if (!isSnapToGround) return;
        if (horizontalMoveValue == 0) return;

        if (GroundSnapCheck(out Vector3 snapDirection))
        {
            //Todo : check direction

            transform.position += snapDirection * Mathf.Abs(horizontalMoveValue) * Time.deltaTime;
        }
        else
        {
            isSnapToGround = false;
        }
    }

    void AerialMove()
    {
        Vector3 freeMoveVector = new(horizontalMoveValue, verticalMoveValue);

        if (DirectionalCheck(ColliderLocalPosOffset(baseCircleCollider), freeMoveVector.normalized, out Vector3 verticalSnapDirection, out Vector3 verticalSnapPos))
        {
            if (Vector3.Distance(ColliderLocalPosOffset(baseCircleCollider), verticalSnapPos) < freeMoveVector.magnitude)
            {
                if (verticalMoveValue < 0) isSnapToGround = true;

                SetPlayerPositionOnCollider(verticalSnapPos, baseCircleCollider);
                verticalMoveValue = 0;
            }
            else
            {
                transform.position += verticalSnapDirection * freeMoveVector.magnitude * Time.deltaTime;
            }
        }
        else
        {
            transform.position += freeMoveVector * Time.deltaTime;
        }
    }

    void GravityPull()
    {
        verticalMoveValue -= weightFactor * Time.deltaTime;
    }

    void SetPlayerPositionOnCollider(Vector3 position, CircleCollider2D collider)
    {
        transform.position = position - collider.offset.ToVector3();
    }

    Vector3 ColliderLocalPosOffset(CircleCollider2D collider)
    {
        return transform.position + collider.offset.ToVector3();
    }

    bool DirectionalCheck(Vector2 origin, Vector2 moveVector, out Vector3 snapDirection, out Vector3 snapPos, bool debug = false)
    {
        RaycastHit2D colliderHit = Physics2D.CircleCast(origin, baseCircleCollider.radius, moveVector.normalized, checkDist, groundLayer);
        snapPos = colliderHit.centroid + colliderHit.normal * snapDist;
        snapDirection = (snapPos - ColliderLocalPosOffset(baseCircleCollider)).normalized;

        if (colliderHit && Vector2.Angle(colliderHit.normal, Vector2.up) <= angleLimit)
        {
            if (debug)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(snapPos, baseCircleCollider.radius);
                Gizmos.DrawSphere(colliderHit.point, .1f);
            }

            groundNormal = colliderHit.normal;
            return true;
        }

        if (debug)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(ColliderLocalPosOffset(baseCircleCollider) + moveVector.normalized.ToVector3() * checkDist, baseCircleCollider.radius);
        }
        return false;
    }

    bool GroundSnapCheck(out Vector3 snapDirection, bool debug = false)
    {
        int direction = horizontalMoveValue < 0 ? -1 : 1;

        RaycastHit2D groundHit = Physics2D.Raycast(ColliderLocalPosOffset(baseCircleCollider), -groundNormal, baseCircleCollider.radius + checkDist, groundLayer);
        groundNormal = groundHit.normal;

        Vector2 groundPerp = -Vector2.Perpendicular(groundNormal) * direction;
        Vector3 startVector = Quaternion.AngleAxis(checkAngle / 2 * direction, Vector3.forward) * groundPerp * checkDist;

        for (int i = 1; i <= checkNumber; i++)
        {
            Vector3 endVector = Quaternion.AngleAxis(-checkAngle * direction / checkNumber, Vector3.forward) * startVector;
            Vector3 startPos = ColliderLocalPosOffset(baseCircleCollider) + startVector;
            Vector3 endPos = ColliderLocalPosOffset(baseCircleCollider) + endVector;

            if (debug)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(startPos, baseCircleCollider.radius);
            }

            RaycastHit2D colliderHit = Physics2D.CircleCast(startPos, baseCircleCollider.radius, endPos - startPos, (endPos - startPos).magnitude, groundLayer);

            if (colliderHit && Vector2.Angle(colliderHit.normal, Vector2.up) <= angleLimit)
            {
                Vector3 snapPos = colliderHit.centroid + colliderHit.normal * snapDist;
                snapDirection = (snapPos - ColliderLocalPosOffset(baseCircleCollider)).normalized;

                if (debug)
                {
                    Gizmos.color = Color.red;
                    Gizmos.DrawWireSphere(snapPos, baseCircleCollider.radius);
                    Gizmos.DrawSphere(colliderHit.point, .1f);
                }

                groundNormal = colliderHit.normal;

                return true;
            }

            startVector = endVector;
            
            if (debug)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(endPos, baseCircleCollider.radius);
            }
        }
        snapDirection = Vector3.zero;
        return false;
    }

    private void OnDrawGizmos()
    {        
        if (Application.isPlaying)
        {
            Vector3 freeMoveVector = new Vector3(horizontalMoveValue, verticalMoveValue);

            Gizmos.color = Color.green;
            //Gizmos.DrawRay(ColliderLocalPosOffset(baseCircleCollider), freeMoveVector.normalized);

            if (isSnapToGround && horizontalMoveValue != 0 && showDebug)
            {
                GroundSnapCheck(out Vector3 direction, showDebug);
            }
            if (!isSnapToGround && verticalMoveValue != 0 && showDebug)
            {
                DirectionalCheck(ColliderLocalPosOffset(baseCircleCollider), freeMoveVector.normalized, out Vector3 verticalSnapDirection, out Vector3 verticalSnapPos, showDebug);
            }
        }
    }
}
