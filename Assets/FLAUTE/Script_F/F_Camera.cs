using Unity.Cinemachine;
using UnityEngine;

public class F_Camera : MonoBehaviour
{

    CinemachinePositionComposer cinemachinePositionComposer;
    [SerializeField] F_PlayerController3 playerController;
    
    [SerializeField] float xOffset = 5;
     float yOffset = 2.5f;
    [SerializeField] float baseDamping = 2f;
    [SerializeField] float ySign = 0;
    [SerializeField] float yPreviousSign = 0;


    private void Awake() {
        cinemachinePositionComposer = GetComponent<CinemachinePositionComposer>();
    }

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {

        yOffset = xOffset / 2;

  
        if(playerController.rb.linearVelocity.magnitude > 0.001f) {//Si movement

            if(Mathf.Abs(playerController.rb.linearVelocity.y)>0.01f){//si déplacement sur pente
                ySign = Mathf.Sign(playerController.rb.linearVelocity.y);
            } else { //déplacement sur sol plat
                ySign = 0;
            }

            //si movement previous sign mis a jour
            yPreviousSign = ySign;
        } else {
            //si pas de mouvement on garde le signe précédent qui peut être -1, 0, 1
            ySign= yPreviousSign;
        }

        // FAIRE LE CAS DU RETOURNEMENT => 6 c'est bien
        // QUAND ON MARCHE NORMALEMENT => 6 c'est trop, la cam n'est plus a l'avant

        if(playerController.rb.linearVelocity.magnitude > playerController.MovementSpeed /2) {
            Debug.Log("TRUE");
            float _xOffsetWithVelo = xOffset + Mathf.Abs(playerController.rb.linearVelocity.x) / 5;
            float _yOffsetWithVelo = yOffset + Mathf.Abs(playerController.rb.linearVelocity.y) / 8;
            cinemachinePositionComposer.TargetOffset = new Vector2(_xOffsetWithVelo, ySign * _yOffsetWithVelo);



            //float _xDampingWithVelo = baseDamping - playerController.rb.linearVelocity.x / 20;
            //float _xNewDamping = _xDampingWithVelo > 0.1f ? _xDampingWithVelo : 0.1f;

            //float _yDampingWithVelo = baseDamping - Mathf.Abs(playerController.rb.linearVelocity.y / 20);
            //float _yNewDamping = _yDampingWithVelo > 0.1f ? _yDampingWithVelo : 0.1f;


            float _newDamping = Mathf.Clamp(baseDamping - playerController.rb.linearVelocity.magnitude / 15, 1f, baseDamping);
            cinemachinePositionComposer.Damping = new Vector2(_newDamping, _newDamping);
            //cinemachinePositionComposer.Damping = new Vector2(_xNewDamping, _yNewDamping);
        } else {
            Debug.Log("FAKSE");
            cinemachinePositionComposer.TargetOffset = new Vector2(xOffset,  yOffset * ySign);
            cinemachinePositionComposer.Damping = Vector2.one * baseDamping;
        }


    }
}
