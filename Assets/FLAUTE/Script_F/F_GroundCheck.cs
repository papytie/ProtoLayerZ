using NUnit.Framework;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using System.Linq;
using System;

public class F_GroundCheck : MonoBehaviour
{
    GameObject controller;

    [SerializeField] private LayerMask whatIsGround;
    [SerializeField] private float maxSlopeAngle = 90;
    [SerializeField] private RaycastHit2D groundHitResult;
    [SerializeField] private Vector2 slopeNormalPerp;
    [SerializeField] private bool isGrounded;
    [SerializeField] private bool checkGroundEnabled = true;

    [SerializeField] float rayLength = 1;
    [SerializeField] float rayDirectionMaxSpread = 360;
    [SerializeField] float raySpreadGlobalOrientation = 0;
    [SerializeField] int numberOfRay = 4;

    //DEBUG
    [SerializeField] List<float> allDistances = new List<float>();
    [SerializeField] float currentSlopeAngle;
    
    List<RaycastHit2D> allResults = new List<RaycastHit2D>();
    float facingValue = 1;
    float controllerFacingValue;
    float baseRayDirectionMaxSpread;

    public LayerMask GroundLayer => whatIsGround;
    public bool IsGrounded => isGrounded;
    public float CheckedDistance => rayLength;
    public float MaxGroundAngle => maxSlopeAngle;

    public RaycastHit2D GroundHitResult => groundHitResult;
    public Vector2 WalkDirection => slopeNormalPerp;
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
    }

    // Update is called once per frame
    void Update()
    {
        /*
        if(allResults.Count != numberOfRay) {
            UpdateResultList();
        }
        */

        controllerFacingValue = Mathf.Sign(controller.transform.right.x);

        if(facingValue != controllerFacingValue) {
            UpdateRayDirectionAccordingToPLayerFacing();
        }

        if(checkGroundEnabled) {
            UpdateResultList();
            CheckGround();
        } else {
            isGrounded = false;
        }
    }

    void UpdateResultList() {
        //Debug.Log("update list");
        allResults.Clear();
        for (int i = 0; i < numberOfRay; i++) {
            allResults.Add(new RaycastHit2D()) ;
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

    void CheckGround() {
        for(int i = 0; i < allResults.Count; i++) {
            float _angleToRotateVector = i*rayDirectionMaxSpread / (allResults.Count-1) + raySpreadGlobalOrientation;
            allResults[i] = Physics2D.Raycast(transform.position, RotateVector(controller.transform.right, _angleToRotateVector).normalized, rayLength, whatIsGround);
            Debug.DrawRay(transform.position, RotateVector(controller.transform.right, _angleToRotateVector).normalized*rayLength, Color.black);
        }


        //Reorganiser la liste
        allDistances.Clear();
        //allResults.RemoveAll(result => result.distance == 0);
        
        //remove ceux qui ne touchent pas
        allResults.RemoveAll(result => result == false);
        //remove ceux qui n'ont pas un angle pratiquable
        allResults.RemoveAll(result => Vector2.Angle(result.normal, Vector2.up) >= maxSlopeAngle);
        //Organiser du plus près au plus éloigné
        allResults = allResults.OrderBy(result => result.distance).ToList();

        for(int i = 0;i < allResults.Count;i++) {
            allDistances.Add(allResults[i].distance);
            allDistances[i] = (float) System.Math.Round(allDistances[i], 3);
        }

        Debug.Log("allResult Count = " + allResults.Count);


        //Si au moins les deux premiers sont équidistants on va plus loin ET NAVIGABLE
        if(allDistances.Count > 1 && allDistances[0] == allDistances[1]) {

            //Si il y a plus d'éléments dans la liste on compare les suivants au premier elem et on arrête dès qu'ils ne sont plus équidistants, on les éléments restants
            if(allDistances.Count > 2) {
                for(int i = 2; i < allDistances.Count; i++) {
                    if(allDistances[i] != allDistances[0]) {
                        allDistances.RemoveRange(i, allDistances.Count - i);
                        allResults.RemoveRange(i, allDistances.Count - i);
                        break;
                    }
                }
            }

            //DEBUG
            for(int i = 0; i < allResults.Count; i++) {
                Debug.DrawRay(transform.position, (Vector3) allResults[i].point - transform.position, Color.green);
            }

            //on regarde chaque élément, le premier ayant la bonne orientation
            for(int i = 0; i < allResults.Count; i++) {
                if(Mathf.Sign(allResults[i].normal.x) == - controllerFacingValue) {
                    isGrounded = true;
                    groundHitResult = allResults[i];
                    slopeNormalPerp = Vector2.Perpendicular(allResults[i].normal).normalized;
                    //the chosen one
                    Debug.DrawRay(transform.position, (Vector3) allResults[i].point - transform.position, Color.red);
                    return;
                }
            }

            //On arrive ici si les mêmes resutl dist sont sur la même pentes, on en prends alors un au pif, le premier
            isGrounded = true;
            groundHitResult = allResults[0];
            slopeNormalPerp = Vector2.Perpendicular(allResults[0].normal).normalized;
            //the chosen one
            Debug.DrawRay(transform.position, (Vector3) allResults[0].point - transform.position, Color.red);
            return;

        }
        else if(allDistances.Count >0) {

            //DEBUG
            for(int i = 0; i < allResults.Count; i++) {
                Debug.DrawRay(transform.position, (Vector3) allResults[i].point - transform.position, Color.green);
            }

            //Si pas d'équidistant, on prends le premier qu'on sait déjà être navigable
            isGrounded = true;
            groundHitResult = allResults[0];
            slopeNormalPerp = Vector2.Perpendicular(allResults[0].normal).normalized;
            //the chosen one
            Debug.DrawRay(transform.position, (Vector3) allResults[0].point - transform.position, Color.red);
            return;

            /*
            for(int i = 0; i < allResults.Count; i++) {
                currentSlopeAngle = Vector2.Angle(allResults[i].normal, Vector2.up);
                if(currentSlopeAngle < maxSlopeAngle) {
                    isGrounded = true;
                    groundHitResult = allResults[i];
                    slopeNormalPerp = Vector2.Perpendicular(allResults[i].normal).normalized;
                    return;
                }
            }
            */
        } else {
            //DEBUG
            for(int i = 0; i < allResults.Count; i++) {
                Debug.DrawRay(transform.position, (Vector3) allResults[i].point - transform.position, Color.green);
            }

            //arriver ici signifie qu'aucun angle n'était praticable

            Debug.Log("0 RESULTS  = " + allResults.Count);
            currentSlopeAngle = 0;
            isGrounded = false;
            groundHitResult = new RaycastHit2D();
            slopeNormalPerp = Vector2.zero;

        }

      
    }

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

        if(controller != null) {
            Gizmos.DrawWireSphere(transform.position + controller.transform.right * rayLength, 0.15f);
        }
  
    }
}
