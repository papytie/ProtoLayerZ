using Unity.Cinemachine;
using Unity.VisualScripting;
using UnityEngine;

public class F_Camera : MonoBehaviour
{
    [SerializeField] F_GameManager gm;
    CinemachineCamera cinemachineCam;
    CinemachinePositionComposer cinemachinePositionComposer;
    //[SerializeField] F_PlayerController4 playerController;
    //[SerializeField] F_SpaceshipController spaceshipController;

    [SerializeField] Transform currentTarget;
    
    [SerializeField] float xOffset = 5;
     float yOffset = 2.5f;
    [SerializeField] float baseDamping = 2f;
    [SerializeField] float ySign = 0;
    [SerializeField] float yPreviousSign = 0;


    private void Awake() {
        cinemachineCam = GetComponent<CinemachineCamera>();
        cinemachinePositionComposer = GetComponent<CinemachinePositionComposer>();
    }

    void Start()
    {
        currentTarget = cinemachineCam.Follow.transform;
    }

    // Update is called once per frame
    void Update()
    {

        yOffset = xOffset / 4;

        float _angleVeloUp = Vector2.Angle(gm.PlayerCharacter.RigidBod.linearVelocity.normalized, Vector2.up);
        //Debug.Log(_angleVeloUp);
        if(_angleVeloUp>= 80 && _angleVeloUp <=110){
            yOffset = 0;
        }
  
        if(gm.PlayerCharacter.RigidBod.linearVelocity.magnitude > 0.001f) {//Si movement

            if(Mathf.Abs(gm.PlayerCharacter.RigidBod.linearVelocity.y)>0.01f){//si d�placement sur pente
                ySign = Mathf.Sign(gm.PlayerCharacter.RigidBod.linearVelocity.y);
            } else { //d�placement sur sol plat
                ySign = 0;
            }
            //si movement previous sign mis a jour
            yPreviousSign = ySign;
        } else {
            //si pas de mouvement on garde le signe pr�c�dent qui peut �tre -1, 0, 1
            ySign= yPreviousSign;
        }




        // FAIRE LE CAS DU RETOURNEMENT => 6 c'est bien
        // QUAND ON MARCHE NORMALEMENT => 6 c'est trop, la cam n'est plus a l'avant
        
        if(gm.PlayerCharacter.RigidBod.linearVelocity.magnitude > 0 && gm.PlayerCharacter.RigidBod.linearVelocity.magnitude < gm.PlayerCharacter.MovementSpeed / 2) {
            cinemachinePositionComposer.TargetOffset = new Vector2(xOffset, yOffset * ySign);
            cinemachinePositionComposer.Damping = Vector2.one * baseDamping;
            return;
        }

        if(gm.PlayerCharacter.RigidBod.linearVelocity.magnitude >= gm.PlayerCharacter.MovementSpeed / 2 && gm.PlayerCharacter.RigidBod.linearVelocity.magnitude < gm.PlayerCharacter.MovementSpeed * 1.5f) {
            float _xOffsetWithVelo = xOffset + Mathf.Abs(gm.PlayerCharacter.RigidBod.linearVelocity.x) / 2.5f;
            float _yOffsetWithVelo = yOffset + Mathf.Abs(gm.PlayerCharacter.RigidBod.linearVelocity.y) / 2.5f;
            cinemachinePositionComposer.TargetOffset = new Vector2(_xOffsetWithVelo, ySign * _yOffsetWithVelo);
            /*
            float _xDampingWithVelo = baseDamping - playerController.rb.linearVelocity.x / 20;
            float _xNewDamping = _xDampingWithVelo > 0.1f ? _xDampingWithVelo : 0.1f;
            float _yDampingWithVelo = baseDamping - Mathf.Abs(playerController.rb.linearVelocity.y / 20);
            float _yNewDamping = _yDampingWithVelo > 0.1f ? _yDampingWithVelo : 0.1f;
            */
            cinemachinePositionComposer.Damping = Vector2.one * baseDamping/2;
            return;
        }


        if(gm.PlayerCharacter.RigidBod.linearVelocity.magnitude >= gm.PlayerCharacter.MovementSpeed * 1.5f) {
            float _xOffsetWithVelo = xOffset + Mathf.Abs(gm.PlayerCharacter.RigidBod.linearVelocity.x) / 5;
            float _yOffsetWithVelo = yOffset + Mathf.Abs(gm.PlayerCharacter.RigidBod.linearVelocity.y) / 8;
            float _newDamping = Mathf.Clamp(baseDamping - gm.PlayerCharacter.RigidBod.linearVelocity.magnitude / 15, 1f, baseDamping);

            cinemachinePositionComposer.TargetOffset = new Vector2(_xOffsetWithVelo, ySign * _yOffsetWithVelo);
            cinemachinePositionComposer.Damping = new Vector2(_newDamping, _newDamping);
            return;
        } 


    }


    public void SwitchTarget() {
        if(currentTarget.gameObject.GetComponent<F_PlayerController4>() != null) {
            cinemachineCam.Follow = gm.PlayerSpaceship.transform;
        } else {
            cinemachineCam.Follow = gm.PlayerCharacter.transform;
        }

        currentTarget = cinemachineCam.Follow;
    }
}
