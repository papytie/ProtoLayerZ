using DG.Tweening.Core.Easing;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor.ShaderGraph;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Windows;

public class F_InputManager : MonoBehaviour
{

    [SerializeField] F_GameManager gameManager;

    Controls controls;

    List<InputAction> characterInputActions = new List<InputAction>();
    List<InputAction> spaceshipInputActions = new List<InputAction>();

    //Character actions
    InputAction characterMove;
    InputAction jump;
    InputAction slide;
    InputAction rocket;
    InputAction interact;

    //Spaceship actions
    InputAction zSpaceshipMove;
    InputAction xySpaceshipMove;
    InputAction spaceshipEngine;
    InputAction eject;

    [SerializeField] float xCharacterInput;
    [SerializeField] float characterPhysicInput;
    [SerializeField] float characterRocketInput;
    [SerializeField] float characterInteractInput;

    [SerializeField] float zSpaceshipInput;
    [SerializeField] Vector2 xySpaceshipInput;
    [SerializeField] float spaceshipEngineInput;
    [SerializeField] float ejectFromSpaceshipInput;

    //ACCESSEURS
    public float CharInputX => xCharacterInput;
    //public InputAction Jump => jump;
    public float CharPhysicInput => characterPhysicInput;
    public float CharRocketInput => characterRocketInput;
    public float CharInteractInput => characterInteractInput;

    public float ShipInputZ => zSpaceshipInput;
    public Vector2 ShipInputXY => xySpaceshipInput;
    public float SpaceshipEngineInput => spaceshipEngineInput;
    public float ShipEjectInput => ejectFromSpaceshipInput;

    public List<InputAction> CharInputActions => characterInputActions;
    public List<InputAction> ShipInputActions => spaceshipInputActions;

    private void Awake() {

        gameManager = GetComponent<F_GameManager>();
        controls = new Controls();

        characterMove = controls.MainCharacterMap.Walking;
        jump = controls.MainCharacterMap.Jumping;
        slide = controls.MainCharacterMap.Sliding;
        rocket = controls.MainCharacterMap.Rocket;
        interact = controls.MainCharacterMap.Interact;

        zSpaceshipMove = controls.SpaceshipMap.Zmovement;
        xySpaceshipMove = controls.SpaceshipMap._2DMovement;
        spaceshipEngine = controls.SpaceshipMap.LandingTakeOff;
        eject = controls.SpaceshipMap.EjectPlayer;


        characterInputActions.Add(characterMove);
        characterInputActions.Add(jump);
        characterInputActions.Add(slide);
        characterInputActions.Add(rocket);
        characterInputActions.Add(interact);

        spaceshipInputActions.Add(zSpaceshipMove);
        spaceshipInputActions.Add(xySpaceshipMove);
        spaceshipInputActions.Add(spaceshipEngine);
        spaceshipInputActions.Add(eject);
    }

    void Start()
    {
        EnableCharacterInputs();
    }

    // Update is called once per frame
    void Update()
    {
        CheckInput();
    }

    private void CheckInput() {
        //Character
        xCharacterInput = characterMove.ReadValue<float>();
        characterPhysicInput = slide.ReadValue<float>();
        characterRocketInput = rocket.ReadValue<float>();
        characterInteractInput = interact.ReadValue<float>();

        //Spaceship
        zSpaceshipInput = zSpaceshipMove.ReadValue<float>();
        xySpaceshipInput = xySpaceshipMove.ReadValue<Vector2>();
        spaceshipEngineInput = spaceshipEngine.ReadValue<float>();
        ejectFromSpaceshipInput = eject.ReadValue<float>();
    }

    public void EnableSpaceshipInputs() {
        Debug.Log("SPACESHIP INPUTS");
        zSpaceshipMove.Enable();
        xySpaceshipMove.Enable();
        spaceshipEngine.Enable();
        eject.Enable();

        spaceshipEngine.performed += gameManager.PlayerSpaceship.TryToLandOnStrate;
        eject.performed += gameManager.PlayerSpaceship.TryToQuitSpaceship;
    }

    public void DisableSpaceshipInputs() {
        zSpaceshipMove.Disable();
        xySpaceshipMove.Disable();
        spaceshipEngine.Disable();
        eject.Disable();

        spaceshipEngine.performed -= gameManager.PlayerSpaceship.TryToLandOnStrate;
        eject.performed -= gameManager.PlayerSpaceship.TryToQuitSpaceship;
    }

    public void EnableCharacterInputs() {
        characterMove.Enable();
        jump.Enable();
        slide.Enable();
        rocket.Enable();
        interact.Enable();

        jump.performed += gameManager.PlayerCharacter.TryToJump;
        interact.performed += gameManager.PlayerCharacter.TryToInteract;
    }

    public void DisableCharacterInputs() {
        characterMove.Disable();
        jump.Disable();
        slide.Disable();
        rocket.Disable();
        interact.Disable();

        jump.performed -= gameManager.PlayerCharacter.TryToJump;
        interact.performed -= gameManager.PlayerCharacter.TryToInteract;
    }
}
