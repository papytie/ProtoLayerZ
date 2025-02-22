using NUnit.Framework;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using System.Linq;
using System;
using UnityEngine.Windows;
using static UnityEngine.Rendering.DebugUI;

public class F_GroundCheck : MonoBehaviour
{
    GameObject controller;
    CapsuleCollider2D controllerCapsuleCollider;

    [SerializeField] private LayerMask whatIsGround;
    [SerializeField] private Vector3 raycastOrigin;
    [SerializeField] private Vector3 shortRaycastOrigin;
    [SerializeField] private float shortRaycastLength;
    [SerializeField] private float raycastOriginOffset = 0;
    [SerializeField] private float maxSlopeAngle = 90;
    [SerializeField] private float circleOverlapRadius = 0.35f;
    [SerializeField] private RaycastHit2D groundHitResult;
    [SerializeField] private RaycastHit2D groundSecondHitResult;
    [SerializeField] private bool isGrounded;
    [SerializeField] private bool isGroundOverlaped;
    [SerializeField] private bool isGroundedButFarFromGround;
    [SerializeField] private bool checkGroundEnabled = true;
    [SerializeField] private bool noHitResult;

    [SerializeField] float rayLength = 1;
    [SerializeField] float peakDownRayLength = 5;
    [SerializeField] float rayDirectionMaxSpread = 360;
    [SerializeField] float raySpreadGlobalOrientation = 0;
    int numberOfRay = 3;
    [SerializeField] float minDist = 0.1f;

    //DEBUG
    [SerializeField] List<float> allDistances = new List<float>();
    [SerializeField] float currentSlopeAngle;
    
    List<RaycastHit2D> allResults = new List<RaycastHit2D>();
    float facingValue = 1;
    float controllerFacingValue;
    float baseRayDirectionMaxSpread;

    public LayerMask GroundLayer => whatIsGround;
    public bool IsGrounded => isGrounded;
    public bool IsGroundOverlaped => isGroundOverlaped;
    public bool IsGroundedButFarFromGround => isGroundedButFarFromGround;
    public float CircleOverlapRadius => circleOverlapRadius;
    public bool NoHitResult => noHitResult;
    public float CheckedDistance => rayLength;
    public float MaxGroundAngle => maxSlopeAngle;
    public float CurrentSlopeAngle => currentSlopeAngle;

    public RaycastHit2D GroundHitResult => groundHitResult;
    public RaycastHit2D GroundSecondHitResult => groundSecondHitResult;
    public bool CheckGroundEnabled {
        get => checkGroundEnabled;
        set => checkGroundEnabled = value;
    }

    private void Awake() {
        controller = transform.parent.gameObject;
        controllerCapsuleCollider = controller.GetComponentInChildren<CapsuleCollider2D>();
    }

    void Start()
    {
        baseRayDirectionMaxSpread = rayDirectionMaxSpread;
        controllerFacingValue = Mathf.Sign(controller.transform.right.x);
        UpdateRayDirectionAccordingToPLayerFacing();

        //raycastOrigin = transform.position  + transform.up * raycastOriginOffset;
        //shortRaycastOrigin = controller.transform.position - transform.up * controllerCapsuleCollider.size.y;
    }

    // Update is called once per frame
    void Update()
    {

        isGroundOverlaped = Physics2D.OverlapCircle(transform.position, circleOverlapRadius, whatIsGround);


        raycastOrigin = transform.position + transform.up * raycastOriginOffset;
        shortRaycastOrigin = controller.transform.position - transform.up * controllerCapsuleCollider.size.y/2;

        controllerFacingValue = Mathf.Sign(controller.transform.right.x);

        if(facingValue != controllerFacingValue) {
            UpdateRayDirectionAccordingToPLayerFacing();
        }

        if(checkGroundEnabled) {
            CheckGround();
        } else {
            isGrounded = false;
        }

        if(isGrounded) {

        }
    }


    void CheckGround() {
        
        UpdateResultListCount(numberOfRay);

        isGroundedButFarFromGround = isGrounded && !isGroundOverlaped ? true : false;
        if(isGroundedButFarFromGround) {
            //Debug.Log("isGrounded BUT FAR");
        }

        

        MultipleRaycastsAndDirections(numberOfRay);
        allDistances.Clear();

        // allResults[0] // front
        // allResults[1] // down
        // allResults[2] // back

        //List<int> touchingRaycastIDList = new List<int>(); 

        List<int> touchingRaycastIDList2 = new List<int>(); 
        List<int> touchingGoodAngleRaycastIDList = new List<int>(); 
        List<int> touchingBadAngleRaycastIDList = new List<int>(); 
        
        for(int i = 0; i < numberOfRay; i++) {
            /*
            if(allResults[i] && Vector2.Angle(allResults[i].normal, Vector2.up) < maxSlopeAngle) {
                touchingRaycastIDList.Add(i);
            }
            */
            if(allResults[i]) {
                touchingRaycastIDList2.Add(i);
                if(Vector2.Angle(allResults[i].normal, Vector2.up) < maxSlopeAngle) {
                    touchingGoodAngleRaycastIDList.Add(i);
                } else {
                    touchingBadAngleRaycastIDList.Add(i);
                }
              
            }
        }

      


        //Debug.Log("COUNT = " + touchingRaycastIDList.Count);

        float _frontPointDist;
        float _downPointDist;
        float _backPointDist;

        //A FIX : on peut grimper sur des mauvaises penter grace au long raycast vers le bas qui touche un sol plat 
        //SPECIAL CASE : try to climb bad angle
        //if(touchingGoodAngleRaycastIDList.Count == 1 && )

        if(touchingGoodAngleRaycastIDList.Count == 0) {
            Debug.Log("COUNT 0");
       
            if(isGroundOverlaped) {
                Debug.Log("GROUND OVERLAP");

        
                RaycastHit2D _shortCastFront = Physics2D.Raycast(shortRaycastOrigin,controller.transform.right, shortRaycastLength,whatIsGround);
                Debug.DrawRay(shortRaycastOrigin, controller.transform.right * shortRaycastLength,Color.red);
                RaycastHit2D _shortCastBack = Physics2D.Raycast(shortRaycastOrigin,-controller.transform.right, shortRaycastLength,whatIsGround);
                Debug.DrawRay(shortRaycastOrigin, -controller.transform.right * shortRaycastLength,Color.red);
                
                if(_shortCastFront && Vector2.Angle(_shortCastFront.normal, Vector2.up) < maxSlopeAngle) {
                    Debug.DrawRay(shortRaycastOrigin, controller.transform.right * _shortCastFront.distance, Color.green);
                    /*
                    float _dotProduct = Vector2.Dot(controller.transform.right, _shortCastFront.normal);
                    // If dotProduct > 0, the character is climbing, if < 0, it's descending
                    if(_dotProduct > 0) {
                        Debug.Log("Climbing the slope");
                    } else if(_dotProduct < 0) {
                        Debug.Log("Descending the slope");
                    }
                    */
                    RaycastHit2D _fakeHitResult = allResults[0];
                    _fakeHitResult.point = new Vector2(0, 0);
                    _fakeHitResult.normal = Vector2.up;
                    allResults[0] = _fakeHitResult;
                    SetIsGrounded(true, 0);
                    Debug.Log("SPECIAL PEAK MONTER");
                    return;
                } else if(_shortCastBack && Vector2.Angle(_shortCastBack.normal, Vector2.up) < maxSlopeAngle) {
                    Debug.DrawRay(shortRaycastOrigin, -controller.transform.right * _shortCastFront.distance, Color.green);

                    RaycastHit2D _fakeHitResult = allResults[0];
                    _fakeHitResult.point = new Vector2(0, 0);
                    _fakeHitResult.normal = controller.transform.right;
                    allResults[0] = _fakeHitResult;
                    SetIsGrounded(true, 0);
                    Debug.Log("SPECIAL PEAK DESCENDRE");
                    return;
                } else {
                    SetIsGrounded(false, 0);
                    Debug.Log("ANGLE NOPE");
                    return;
                }  
            } else {
                Debug.Log("GROUND PAS OVERLAP");
                //0 useless quand grounded false, mais flemme de faire surcharge
                SetIsGrounded(false, 0);
                return;
            }
        } 

        if(touchingGoodAngleRaycastIDList.Count == 1) {

            //CAS DE MAUVAISE PENTE qu'on grimpe jusqu'au bout du raycast Down grace au down result praticable et aux collisions
            // si front point Ou back point plus près que down point, on va vers le bas, sinon rien 
            // on réoriente vers le bas
            if(touchingBadAngleRaycastIDList.Count >=1 && touchingGoodAngleRaycastIDList.Contains(1)) {

                _downPointDist = Vector2.Distance(allResults[1].point, transform.position);


                //Si le front touche un mauvais angle
                if(touchingBadAngleRaycastIDList.Contains(0)) {
                    _frontPointDist = Vector2.Distance(allResults[0].point, transform.position);
                    //si front plus près que down, on va vers le bas
                    if(_frontPointDist < _downPointDist - minDist) {
                        /*
                        */
                        RaycastHit2D _fakeHitResult = allResults[0];
                        _fakeHitResult.point = new Vector2(0, 0);
                        _fakeHitResult.normal = controller.transform.right;
                        allResults[0] = _fakeHitResult;
                        SetIsGrounded(true, 0);
                        Debug.Log("SPECIAL CASE BAD ANGLE = FRONT");
                        //SetIsGrounded(false, 0);
                        return;
                    } else {
                        SetIsGrounded(true, touchingGoodAngleRaycastIDList[0]);
                        return;
                    }
                }

                //Si le back touche un mauvais angle
                if(touchingBadAngleRaycastIDList.Contains(2)) {
                    _backPointDist = Vector2.Distance(allResults[2].point, transform.position);
                    //si back plus près que down, on va vers le bas
                    if(_backPointDist < _downPointDist - minDist) {
                        RaycastHit2D _fakeHitResult = allResults[0];
                        _fakeHitResult.point = new Vector2(0, 0);
                        _fakeHitResult.normal = controller.transform.right;
                        allResults[0] = _fakeHitResult;
                        SetIsGrounded(true, 0);
                        Debug.Log("SPECIAL CASE BAD ANGLE = BACK");
                        /*
                        */
                        //SetIsGrounded(false, 0);
                        return;
                    } else {
                        SetIsGrounded(true, touchingGoodAngleRaycastIDList[0]);
                        return;
                    }
                }

                /*
                _frontPointDist = Vector2.Distance(allResults[0].point, transform.position);
                _downPointDist = Vector2.Distance(allResults[1].point, transform.position);

                RaycastHit2D _fakeHitResult = allResults[0];
                _fakeHitResult.point = new Vector2(0, 0);
                _fakeHitResult.normal = controller.transform.right;
                allResults[0] = _fakeHitResult;
                SetIsGrounded(true, 0);
                Debug.Log("SPECIAL CASE BAD ANGLE");
                return;
                */

            } else {
                SetIsGrounded(true, touchingGoodAngleRaycastIDList[0]);
                return;
            }
        } 
        
        if(touchingGoodAngleRaycastIDList.Count == 2) {
            if(touchingGoodAngleRaycastIDList.Contains(0) && touchingGoodAngleRaycastIDList.Contains(1)) {
                //front et down touch
                _frontPointDist = Vector2.Distance(allResults[0].point, transform.position);
                _downPointDist = Vector2.Distance(allResults[1].point, transform.position);

                //front prio grace a mindist
                if(_frontPointDist - minDist < _downPointDist) {
                    SetIsGrounded(true, 0);
                    return;
                } else {
                    SetIsGrounded(true, 1);
                    return;
                }

            } else if(touchingGoodAngleRaycastIDList.Contains(1) && touchingGoodAngleRaycastIDList.Contains(2)) {
                //down et back touch

                _downPointDist = Vector2.Distance(allResults[1].point, transform.position);
                _backPointDist = Vector2.Distance(allResults[2].point, transform.position);

                //down prio grace a mindist
                if(_downPointDist - minDist < _backPointDist) {
                    SetIsGrounded(true, 1);
                    return;
                } else {
                    SetIsGrounded(true, 2);
                    return;
                }

            } else {
                //front et back touch
                _frontPointDist = Vector2.Distance(allResults[0].point, transform.position);
                _backPointDist = Vector2.Distance(allResults[2].point, transform.position);

                //front prio grace a mindist
                if(_frontPointDist - minDist < _backPointDist) {
                    SetIsGrounded(true, 0);
                    return;
                } else {
                    SetIsGrounded(true, 2);
                    return;
                }
            }
        } 
        
        if(touchingGoodAngleRaycastIDList.Count == 3) {
            //si les 3 touche, on les compare tous en donnant prio au front
            _frontPointDist = Vector2.Distance(allResults[0].point, transform.position);
            _downPointDist = Vector2.Distance(allResults[1].point, transform.position);
            _backPointDist = Vector2.Distance(allResults[2].point, transform.position);           

            //front prio, puis down, puis back
            if(_frontPointDist - minDist < _downPointDist && _frontPointDist - minDist < _backPointDist) {
                SetIsGrounded(true, 0);
                return;
            } else if(_downPointDist - minDist < _backPointDist) {
                SetIsGrounded(true, 1);
                return;
            } else {
                SetIsGrounded(true, 2);
                return;
            }
        }



        Debug.Log("NNNNOOOOOOOONN j'ai zappé des cas");









        //remove ceux qui ne touchent pas
        allResults.RemoveAll(result => result == false);
        UpdateDistanceList();





        //////////// CASE 1 /////////////
        //si aucun ne touche ET et le cercle overlap pas 
        if(allResults.Count == 0 && !isGroundOverlaped) {
            // on est en l'air
            isGrounded = false;
            SetRaycastResults();
            return;
        }


        //////////// CASE 2 /////////////
        //si aucun ne touche ET ground overlap circle => on est sur un pic
        //le pic est soit navigable soit non

        //On tire qu'un raycast plus long vers le bas 
        if(allResults.Count == 0 && isGroundOverlaped) {

            allResults.Clear();
            allResults.Add(Physics2D.Raycast(raycastOrigin, Vector2.down, peakDownRayLength, whatIsGround));
            Debug.DrawRay(raycastOrigin, Vector2.down * peakDownRayLength, Color.black);

            // si ça touche et que l'angle est bon
            if(allResults[0] && Vector2.Angle(allResults[0].normal, Vector2.up) < maxSlopeAngle) {
                //on essaie d'aller vers le front
                //CAS SPECIAL

                //faire comme si on touchait un sol plat pour aller tout droit
                RaycastHit2D _fakeHitResult = allResults[0];
                _fakeHitResult.point = new Vector2(0, 0);
                _fakeHitResult.normal = Vector2.up;
       

                isGrounded = true;
                noHitResult = true;
                groundHitResult = _fakeHitResult;
                groundSecondHitResult = new RaycastHit2D();
                currentSlopeAngle = 0;
                Debug.DrawRay(raycastOrigin, Vector2.Perpendicular(_fakeHitResult.normal) * rayLength*2, Color.cyan);

                //SetRaycastResults();
                return;
            } else {
                //ça touche pas ou angle pas bon
                isGrounded = false;
                SetRaycastResults();
                return;
            }

        }


        //////////// CASE 3 /////////////
        //Au moins un résultat touche => soit on est sur une pente navigable soit non

        //remove ceux qui n'ont pas un angle pratiquable
        allResults.RemoveAll(result => Vector2.Angle(result.normal, Vector2.up) >= maxSlopeAngle);
        
        //si plus d'éléments
        if(allResults.Count == 0) {
            //on est sur une pente pas navigable
            isGrounded = false;
            SetRaycastResults();
            return;
        }

    }


    void UpdateResultListCount(int _number) {
        //Debug.Log("update list");
        allResults.Clear();
        for (int i = 0; i < _number; i++) {
            allResults.Add(new RaycastHit2D()) ;
        }
    }

    void UpdateDistanceList() {
        allDistances.Clear();
        for(int i = 0; i < allResults.Count; i++) {
            allDistances.Add(allResults[i].distance);
            //allDistances[i] = (float) System.Math.Round(allDistances[i], 3);

            // 100 = truncate to second decimal
            //allDistances[i] = Mathf.Floor(allDistances[i] * 1000) / 1000;
        }
    }

    void MultipleRaycastsAndDirections(int _count) {
        for(int i = 0; i < _count; i++) {
            float _angleToRotateVector = i * rayDirectionMaxSpread / (allResults.Count - 1) + raySpreadGlobalOrientation;
            allResults[i] = Physics2D.Raycast(raycastOrigin, RotateVector(controller.transform.right, _angleToRotateVector).normalized, rayLength, whatIsGround);
            Debug.DrawRay(raycastOrigin, RotateVector(controller.transform.right, _angleToRotateVector).normalized * rayLength, Color.black);
        }
    }


    void UpdateRayDirectionAccordingToPLayerFacing() {
        if(controllerFacingValue > 0) {
            rayDirectionMaxSpread = baseRayDirectionMaxSpread;
        } else {
            rayDirectionMaxSpread = -baseRayDirectionMaxSpread;
        }
        facingValue = controllerFacingValue;
    }

    void SetIsGrounded(bool _isGrounded, int _index) {

        isGrounded = _isGrounded;

        if(isGrounded) {
            noHitResult = false;
            groundHitResult = allResults[_index];
            Debug.DrawRay(raycastOrigin, (Vector3) groundHitResult.point - raycastOrigin, Color.red);
            currentSlopeAngle = Vector2.Angle(groundHitResult.normal, Vector2.up);
        } else {

            //NOOO CHOSEN ONE
            noHitResult = true;
            currentSlopeAngle = 0;
            groundHitResult = new RaycastHit2D();
        }
    }

    private void SetRaycastResults() {


        if(isGrounded) {
            noHitResult = false;
            groundHitResult = allResults[0];
            Debug.DrawRay(raycastOrigin, (Vector3) groundHitResult.point - raycastOrigin, Color.red);
            currentSlopeAngle = Vector2.Angle(groundHitResult.normal, Vector2.up);
        } else {

            //NOOO CHOSEN ONE
            noHitResult = true;
            currentSlopeAngle = 0;
            groundHitResult = new RaycastHit2D();
        }
    }
    /*
    private void SetRaycastResults() {


        if(isGrounded) {
            noHitResult = false;
            groundHitResult = allResults[0]; //liste épurée et triée avant de sorte a ce que ce soit toujours le premier élément a choisir
            //THE CHOSEN ONE
            Debug.DrawRay(raycastOrigin, (Vector3) groundHitResult.point - raycastOrigin, Color.red);
            currentSlopeAngle = Vector2.Angle(groundHitResult.normal, Vector2.up);

            //FOR STUCK In angle aigu hole, second result backup for player controller movement fix
            if(allResults.Count > 1) {
           
                groundSecondHitResult = allResults[1];
                Debug.DrawRay(raycastOrigin, (Vector3) groundSecondHitResult.point - raycastOrigin, Color.yellow);
            } else {
                groundSecondHitResult = new RaycastHit2D();
            }
        } 
        else {

            //NOOO CHOSEN ONE
            noHitResult = true;
            currentSlopeAngle = 0;
            groundHitResult = new RaycastHit2D();
            groundSecondHitResult = new RaycastHit2D();
        }     
    }

    */



    public Vector2 RotateVector(Vector2 v, float angleDegrees) {
        float angleRadians = angleDegrees * Mathf.Deg2Rad; // Convert degrees to radians
        float cos = Mathf.Cos(angleRadians);
        float sin = Mathf.Sin(angleRadians);

        return new Vector2(
            v.x * cos - v.y * sin,
            v.x * sin + v.y * cos
        );
    }

    private void OnDrawGizmos() {

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, circleOverlapRadius);

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(shortRaycastOrigin, 0.05f);
        

        if(controller != null) {
            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(raycastOrigin + controller.transform.right * rayLength, 0.05f);
        }
  
    }
}
