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

    [SerializeField] private LayerMask whatIsGround;
    [SerializeField] private Vector3 raycastOrigin;
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
    [SerializeField] float minFrontPointDistToDirectChoice = 0.5f;

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
    }

    void Start()
    {
        baseRayDirectionMaxSpread = rayDirectionMaxSpread;
        controllerFacingValue = Mathf.Sign(controller.transform.right.x);
        UpdateRayDirectionAccordingToPLayerFacing();

        raycastOrigin = transform.position  + transform.up * raycastOriginOffset;
    }

    // Update is called once per frame
    void Update()
    {

        isGroundOverlaped = Physics2D.OverlapCircle(transform.position, circleOverlapRadius, whatIsGround);


        raycastOrigin = transform.position + transform.up * raycastOriginOffset;
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

        List<int> touchingRaycastIDList = new List<int>(); 
        
        for(int i = 0; i < numberOfRay; i++) {
            if(allResults[i] && Vector2.Angle(allResults[i].normal, Vector2.up) < maxSlopeAngle) {
                touchingRaycastIDList.Add(i);
            }
        }

        Debug.Log(touchingRaycastIDList.Count);

        float _frontPointDist;
        float _downPointDist;
        float _backPointDist;


        if(touchingRaycastIDList.Count == 0) {
            //aucun touche
            //ajouter le cas de peak

            //si on est sur une peak, on caste beaucoup plus bas
            if(isGroundOverlaped) {

                RaycastHit2D _newCast = Physics2D.Raycast(raycastOrigin, Vector2.down, peakDownRayLength, whatIsGround);
                // si ça touche et que l'angle est bon
                if(_newCast && Vector2.Angle(_newCast.normal, Vector2.up) < maxSlopeAngle) {
                    //on essaie d'aller vers le front
                    //CAS SPECIAL

                    //faire comme si on touchait un sol plat pour aller tout droit
                    //RaycastHit2D _fakeHitResult = allResults[0];
                    //_fakeHitResult.point = new Vector2(0, 0);
                    //_fakeHitResult.normal = Vector2.up;
                    allResults[0] = _newCast;
                    SetIsGrounded(true, 0);
                    return;
                } else {
                    SetIsGrounded(false, 0);
                    return;
                }

            } else {
                //0 useless quand grounded false, mais flemme de faire surcharge
                SetIsGrounded(false, 0);
                return;
            }
        } 
        
        if(touchingRaycastIDList.Count == 1) {
            //ajouter le cas de peak
            //si 1 seul touche, on le prends
            SetIsGrounded(true, touchingRaycastIDList[0]);
            return;
        } 
        
        if(touchingRaycastIDList.Count == 2) {
            if(touchingRaycastIDList.Contains(0) && touchingRaycastIDList.Contains(1)) {
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

            } else if(touchingRaycastIDList.Contains(1) && touchingRaycastIDList.Contains(2)) {
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
        
        if(touchingRaycastIDList.Count == 3) {
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

        //ici au moins une pente est navigable
        /*
        //Organiser du plus près au plus éloigné
        allResults = allResults.OrderBy(result => result.distance).ToList();
        UpdateDistanceList();

        //C'est tout, on garde les 3 résultats max trié du plus près au plus éloigné
        isGrounded = true;
        SetRaycastResults();
        return;
        */
        /*
        //Si plusieurs Resultats avec pentes navigables Et les deux premières Equidistantes

        if(allDistances.Count > 1 && allDistances[0] == allDistances[1]) {

            //Si il y a plus d'éléments dans la liste on compare les suivants au premier elem et on arrête dès qu'ils ne sont plus équidistants
            //on supprime les éléments restants
            if(allDistances.Count > 2) {
                for(int i = 2; i < allDistances.Count; i++) {
                    if(allDistances[i] != allDistances[0]) {
                        allDistances.RemoveRange(i, allDistances.Count - i);
                        allResults.RemoveRange(i, allDistances.Count - i);
                        break;
                    }
                }
            }

            //on regarde chaque élément, le premier ayant la bonne orientation
            for(int i = 0; i < allResults.Count; i++) {
                if(Mathf.Sign(allResults[i].normal.x) == - controllerFacingValue) {
                    
                    //si i != 0, on supprime les resultats précédents
                    if(i != 0) { 
                        allDistances.RemoveRange(0, i-1);
                        allResults.RemoveRange(0, i - 1);
                    }
                    
                    //normalement ça revient a allResults[0] avec la suppression de précédents
                    //groundHitResult = allResults[i];

                    isGrounded = true;
                    SetRaycastResults();
                    return;
                }
            }

            //On arrive ici si les mêmes results distances sont sur la même pentes, on en prends alors un au pif, le premier
            isGrounded = true;
            SetRaycastResults();
            return;

        }
        else if(allDistances.Count >0) {

            //Si pas d'équidistant, on prends le premier qu'on sait déjà être navigable
            isGrounded = true;
            SetRaycastResults();
            return;

        } else {

            Debug.Log("PLUS CENSE ARRIVE ICIIIIII");
            //arriver ici signifie qu'aucun angle n'était praticable

            isGrounded = false;
            SetRaycastResults();
            return;
        } 
        
        */
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



    Vector2 RotateVector(Vector2 v, float angleDegrees) {
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

        if(controller != null) {
            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(raycastOrigin + controller.transform.right * rayLength, 0.05f);
        }
  
    }
}
