using UnityEngine;
using UnityEngine.Windows;

public class F_CamTarget : MonoBehaviour
{
    [SerializeField] F_PlayerController3 playerController;

    [SerializeField] private float aheadAmount = 5;
    [SerializeField] private float aheadSpeed = 0.5f;
    private Vector3 destinationPos = Vector3.zero;
    void Start()
    {
        destinationPos = playerController.transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        MoveCamTarget();
    }

    private void MoveCamTarget() {

        float _addedAmountWithVelo = playerController.rb.linearVelocity.magnitude * Time.deltaTime;
        _addedAmountWithVelo *= aheadAmount*20;

        //ajouter une condition si le player est trop éloigné (parce que si on bouge doucement on peut sortir de l'écran)
        if(Mathf.Abs(playerController.xInput) >= 0.2f || playerController.isSliding || playerController.myGroundCheck.IsGrounded == false) {
            Vector3 _veloNormalized = playerController.rb.linearVelocity.normalized;

            float _modifiedAmount = aheadAmount + _addedAmountWithVelo;          
            destinationPos = playerController.transform.position + new Vector3(_modifiedAmount * _veloNormalized.x, _modifiedAmount / 2 * _veloNormalized.y, 0);
        }

        float _addedAheadSpeedWithVelo = playerController.rb.linearVelocity.magnitude * Time.deltaTime;
        _addedAheadSpeedWithVelo /= aheadSpeed*100;
        float _speed = aheadSpeed * Time.deltaTime ;
        transform.position = Vector3.Lerp(transform.position, destinationPos, _speed + _addedAheadSpeedWithVelo);
    }

    private void OnDrawGizmos() {

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 0.5f);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(destinationPos, 0.5f);
    }

    /*
    private void MoveCamTarget() {

        if(Mathf.Abs(xInput) >= 0.2f || isSliding || !isGrounded) {
            Vector3 _veloNormalized = rb.linearVelocity.normalized;
            camTargetDestinationPos = new Vector3(aheadAmount * _veloNormalized.x, aheadAmount / 2 * _veloNormalized.y, 0);
        }

        camTarget.localPosition = Vector3.Lerp(camTarget.localPosition, camTargetDestinationPos, aheadSpeed * Time.deltaTime);
    }
    */
}
