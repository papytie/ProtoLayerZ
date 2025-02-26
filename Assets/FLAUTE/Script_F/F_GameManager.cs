using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEditor.ShaderGraph;
using UnityEngine.U2D;
using static F_GameManager;

public class F_GameManager : MonoBehaviour
{

    public enum PlayerMode { character, spaceshipStationary, spaceshipMobile };
    [SerializeField] private PlayerMode myPlayerMode = PlayerMode.character;
    
    [SerializeField] Material defaultMaterial;

    public  Action OnStrateSnaping = null;
    public  Action OnShipTakeof = null;
    public  Action OnShipLanding = null;
    public  Action OnShipEnter = null;
    public  Action OnShipExit = null;
    //public  Action OnShipFlying = null;

    [SerializeField] F_PlayerController4 playerCharacter;
    [SerializeField] F_SpaceshipController playerSpaceship;
    [SerializeField] F_Camera cam;
    F_InputManager inputManager;

    [SerializeField] GameObject allStratesFolder = null;
    [SerializeField] List<F_Strate> allStrates = new List<F_Strate>();
    [SerializeField] int closestStrateIndex = 0;
    [SerializeField] F_Strate currentStrate = null;
    [SerializeField] F_Strate closestStrate = null;
    [SerializeField] F_Strate nextStrate = null;
    [SerializeField] F_Strate previousStrate = null;

    [SerializeField] Color backwardColorFill = new Color();
    [SerializeField] Color backwardColorEdge = new Color();
    [SerializeField] Color closestStrateColorEdgeFill = new Color();
    [SerializeField] Color firstForwardColorEdgeFill = new Color();
    [SerializeField] Color lastForwardColorEdgeFill = new Color();
    [SerializeField] float maxCutofMaskValue = 0.65f;
    [SerializeField] float portalAnimationDuration = 1f;
    [SerializeField] float propagationDelayAnimPortal = 0.2f;

    [SerializeField] int previousDelayMultiplier = 1;
    [SerializeField] int nextDelayMultiplier = 1;

    [SerializeField] Gradient stratesGradientFill = new Gradient();
    [SerializeField] Gradient stratesGradientEdge = new Gradient();

    public float spaceBetweenStrates;
    public int totalStrateNumber;
    public int numberOfForwardStrateToUpdate = 5;//47;
    //public int numberOfBackStrateToUpdate = 2;
    //public int totalNumberOfStratesToUpdate;

    [SerializeField] bool canCheckStrates = false;


    //ACCESSEURS
    public F_InputManager Inputs => inputManager;
    public Material DefaultMaterial => defaultMaterial;
    public F_PlayerController4 PlayerCharacter => playerCharacter;
    public F_SpaceshipController PlayerSpaceship => playerSpaceship;
    public F_Strate ClosestStrate => closestStrate;
    public PlayerMode CurrentPlayerMode => myPlayerMode;
    public float PortalAnimationDuration => portalAnimationDuration;
    public float PropagationDelayAnimPortal => propagationDelayAnimPortal;
    public int PreviousDelayMultiplier { get => previousDelayMultiplier; set => previousDelayMultiplier = value; }
    public int NextDelayMultiplier { get => nextDelayMultiplier; set => nextDelayMultiplier = value; }

    private void Awake() {

        inputManager = GetComponent<F_InputManager>();
        /*
        if(allStrates.Count == 0) {
            SetAllStrateList();
        }
        */
        SetAllStrateList();
        //SetupGradients();

        spaceBetweenStrates = Mathf.Abs(allStrates[0].transform.position.z - allStrates[1].transform.position.z);
        SetupAllStrates();
    }

    void Start()
    {

        OnStrateSnaping += StrateSnaped;
        
        OnShipTakeof += SwitchToSpaceshipMobileMode;

        OnShipLanding += SwitchToSpaceshipStationaryMode;


        OnShipEnter += inputManager.EnableSpaceshipInputs;
        OnShipEnter += inputManager.DisableCharacterInputs;
        OnShipEnter += cam.SwitchTarget;
        OnShipEnter += PlayerSpaceship.PlayerEnterSpaceship;
        OnShipEnter += SwitchToSpaceshipStationaryMode;
        
        OnShipExit += inputManager.DisableSpaceshipInputs;
        OnShipExit += inputManager.EnableCharacterInputs;
        OnShipExit += cam.SwitchTarget;
        OnShipExit += SwitchToCharacterMode;



        //totalNumberOfStratesToUpdate = numberOfForwardStrateToUpdate + numberOfBackStrateToUpdate + 1;
        


    }

    // Update is called once per frame
    void Update()
    {
        if(canCheckStrates) {
            UpdateClosestStrate();
            UpdateStratesColors();
        }
    }


    public void StrateSnaped() {
        closestStrate.SetSnapedStrate();
    }


    public void SwitchToSpaceshipStationaryMode() {

        currentStrate = closestStrate;
        currentStrate.SetCurrentStrate();
        canCheckStrates = false;

        if(myPlayerMode == PlayerMode.character) {
            playerCharacter.transform.position = playerSpaceship.CharaSeat.transform.position;
            playerCharacter.RigidBod.simulated = false;
            playerCharacter.transform.parent = playerSpaceship.transform;

        }else if(myPlayerMode == PlayerMode.spaceshipMobile) {

            //close portal
            //reset multipliers
            previousDelayMultiplier = 1;
            nextDelayMultiplier = 1;
            allStrates[closestStrateIndex].AnimatePortal(false,0);

        }

        myPlayerMode = PlayerMode.spaceshipStationary;


    }

    public void SwitchToSpaceshipMobileMode() {
        myPlayerMode = PlayerMode.spaceshipMobile;
        canCheckStrates = true;
        currentStrate.SetClosestStrate();
        currentStrate = null;

        //open portal
        //reset multiplier
        previousDelayMultiplier = 1;
        nextDelayMultiplier = 1;
        allStrates[closestStrateIndex].AnimatePortal(true,0);
    }

    public void SwitchToCharacterMode() {
        myPlayerMode = PlayerMode.character;
        playerCharacter.RigidBod.simulated = true;
        playerCharacter.transform.parent = null;
    }



    void InitStrates() {

        totalStrateNumber = allStrates.Count;
        closestStrate = null;


        for(int i = 0; i < totalStrateNumber; i++) {


            allStrates[i].MyGameManger = this;
            allStrates[i].SortingOrderStrate = -i;
            allStrates[i].SpriteShapeRenderer = allStrates[i].transform.GetComponentInChildren<SpriteShapeRenderer>();
            allStrates[i].SpriteShapeCollider = allStrates[i].transform.GetComponentInChildren<SpriteShapeController>().polygonCollider;
            allStrates[i].SpriteMask = allStrates[i].transform.GetComponentInChildren<SpriteMask>();
            allStrates[i].SetDistantStrate();
            allStrates[i].AnimateColor(lastForwardColorEdgeFill, lastForwardColorEdgeFill, maxCutofMaskValue);


            if(i == 0) {
                allStrates[i].MyPreviousStrate = null;
                allStrates[i].MyNextStrate = allStrates[i + 1];
            }else if( i == totalStrateNumber-1) {
                allStrates[i].MyPreviousStrate = allStrates[i - 1];
                allStrates[i].MyNextStrate = null;
            } else {
                allStrates[i].MyPreviousStrate = allStrates[i - 1];
                allStrates[i].MyNextStrate = allStrates[i + 1];
            }


            float _zDistBetweenStrateAndPlayer = Mathf.Abs(allStrates[i].transform.position.z - playerSpaceship.transform.position.z);

            if(_zDistBetweenStrateAndPlayer < 0.1f && closestStrate == null) {
                closestStrate = allStrates[i];
                currentStrate = closestStrate;
                closestStrateIndex = i;
                currentStrate.SetCurrentStrate();
            }

        }
        SetPreviousNextStrates();
        UpdateStratesColors();
    }

    //UPDATE
    void UpdateClosestStrate() {
        //Si le joueur est en mode pilotage (canCheckStrates), checker la distance entre previous, closest et next strates pour mettre a jour 

        //vérifier si on est en bout de chaine et adapter les index
        int _startIndex;
        int _endIndex;

        //si une next strate est possible
        if(closestStrateIndex < totalStrateNumber - 1) {
            _endIndex = closestStrateIndex + 1;
        } else {
            _endIndex = closestStrateIndex;
        }

        //si une previous strate est possible
        if(closestStrateIndex > 0) {
            _startIndex = closestStrateIndex - 1;
        } else {
            _startIndex = closestStrateIndex;
        }

        //Debug.Log("_startIntex = " + _startIndex + " _endIndex = " + _endIndex);

        for(int i = _startIndex; i <= _endIndex; i++) {
            float _zDistBetweenStrateAndShip = Mathf.Abs(allStrates[i].transform.position.z - playerSpaceship.transform.position.z);
            //Debug.Log("_zDist with " + allStrates[i].name + " = " + _zDistBetweenStrateAndPlayer);

            //si proche et que ce n'est pas la même strate déjà connue comme être la closest
            if(_zDistBetweenStrateAndShip < spaceBetweenStrates/2 && allStrates[i].transform.position.z != closestStrate.transform.position.z) {

                //si on a reculé
                if(allStrates[i].transform.position.z < closestStrate.transform.position.z) {
                    Debug.Log("new closest From back");
                    allStrates[_endIndex].SetDistantStrate();
                } else {
                    //si on a avancé, on laisse en previous pour l'instant
                    Debug.Log("new closest From front");
                    //allStrates[_startIndex].SetDistantStrate();
                }


                closestStrate = allStrates[i];
                closestStrate.SetClosestStrate();
                closestStrateIndex = i;
                closestStrate.AnimatedMask(maxCutofMaskValue, 1);
                SetPreviousNextStrates();

                break;
            }
        }
    }

    void SetPreviousNextStrates() {

        //Set previous et next strates, peuvent être NULL si extrémité
        if(closestStrateIndex < totalStrateNumber - 1) {
            nextStrate = allStrates[closestStrateIndex + 1];
            nextStrate.SetNextStrate();
            //nextStrate.AnimatedMask(maxCutofMaskValue,1);

        } else {
            Debug.Log("Closest is FINAL STRATE");
            nextStrate = null;
        }

        if(closestStrateIndex > 0) {
            previousStrate = allStrates[closestStrateIndex - 1];
            previousStrate.SetPreviousStrate();
            previousStrate.AnimateColor(backwardColorFill,backwardColorEdge, maxCutofMaskValue);
            previousStrate.AnimatedMask(maxCutofMaskValue,-1);

        } else {
            Debug.Log("Closest is STRATE 1");
            previousStrate = null;
        }

    }

    void UpdateStratesColors() {

        int _maxIndex;
        for(_maxIndex = closestStrateIndex; _maxIndex < totalStrateNumber && _maxIndex <= closestStrateIndex + numberOfForwardStrateToUpdate; _maxIndex++) {
        }

        for(int i = closestStrateIndex; i < _maxIndex; i++) {
            float _gradientPosition = ((float) i - closestStrateIndex) / (float) numberOfForwardStrateToUpdate;
            allStrates[i].AnimateColor(stratesGradientFill.Evaluate(_gradientPosition), stratesGradientEdge.Evaluate(_gradientPosition), maxCutofMaskValue);
            //allStrates[i].AnimatedMask(maxCutofMaskValue, 1);
        }

    }

    //END UPDATE

    void SetAllStrateList() {
        allStrates.Clear();
        foreach(Transform _childObject in allStratesFolder.transform) {
            allStrates.Add(_childObject.GetComponent<F_Strate>());
        }
    }

    [ContextMenu("Setup All Strates")]

    void SetupAllStrates() {
        //CALL IN AWAKE
        SetAllStrateList();
        InitStrates();

        foreach(F_Strate _strate in allStrates) {

            //need initStrates avant pour avoir le sorting order
            _strate.SetAllSpriteRenderers();
            _strate.SetObjectsLists();
        }     
    }

    [ContextMenu("Setup Gradients")]
    void SetupGradients() {

       
        float _step = 1 / numberOfForwardStrateToUpdate;

        //GRADIENT FILL
        //color keys
        GradientColorKey _gradientFillColorKey1 = new GradientColorKey(closestStrateColorEdgeFill, 0);
        GradientColorKey _gradientFillColorKey2 = new GradientColorKey(firstForwardColorEdgeFill,_step);
        GradientColorKey _gradientFillColorKey3 = new GradientColorKey(lastForwardColorEdgeFill, 1);

        GradientColorKey[] _allFillColorKeys = new GradientColorKey[3];
        _allFillColorKeys[0] = _gradientFillColorKey1;
        _allFillColorKeys[1] = _gradientFillColorKey2;
        _allFillColorKeys[2] = _gradientFillColorKey3;

        //alpha keys
        GradientAlphaKey _gradientFillAlphaKey1 = new GradientAlphaKey(closestStrateColorEdgeFill.a, 0);
        GradientAlphaKey _gradientFillAlphaKey2 = new GradientAlphaKey(firstForwardColorEdgeFill.a,_step);
        GradientAlphaKey _gradientFillAlphaKey3 = new GradientAlphaKey(lastForwardColorEdgeFill.a, 1);

        GradientAlphaKey[] _allFillAlphaKeys = new GradientAlphaKey[3];
        _allFillAlphaKeys[0] = _gradientFillAlphaKey1;
        _allFillAlphaKeys[1] = _gradientFillAlphaKey2;
        _allFillAlphaKeys[2] = _gradientFillAlphaKey3;

        stratesGradientFill.SetKeys(_allFillColorKeys, _allFillAlphaKeys);

        //GRADIENT EDGE
        //color keys
        GradientColorKey _gradientEdgeColorKey1 = new GradientColorKey(closestStrateColorEdgeFill, 0);
        GradientColorKey _gradientEdgeColorKey2 = new GradientColorKey(firstForwardColorEdgeFill,_step);
        GradientColorKey _gradientEdgeColorKey3 = new GradientColorKey(lastForwardColorEdgeFill, 1);

        GradientColorKey[] _allEdgeColorKeys = new GradientColorKey[3];
        _allEdgeColorKeys[0] = _gradientEdgeColorKey1;
        _allEdgeColorKeys[1] = _gradientEdgeColorKey2;
        _allEdgeColorKeys[2] = _gradientEdgeColorKey3;

        //alpha keys
        GradientAlphaKey _gradientEdgeAlphaKey1 = new GradientAlphaKey(closestStrateColorEdgeFill.a, 0);
        GradientAlphaKey _gradientEdgeAlphaKey2 = new GradientAlphaKey(firstForwardColorEdgeFill.a, _step);
        GradientAlphaKey _gradientEdgeAlphaKey3 = new GradientAlphaKey(lastForwardColorEdgeFill.a, 1);

        GradientAlphaKey[] _allEdgeAlphaKeys = new GradientAlphaKey[3];
        _allEdgeAlphaKeys[0] = _gradientEdgeAlphaKey1;
        _allEdgeAlphaKeys[1] = _gradientEdgeAlphaKey2;
        _allEdgeAlphaKeys[2] = _gradientEdgeAlphaKey3;

        stratesGradientEdge.SetKeys(_allEdgeColorKeys, _allEdgeAlphaKeys);
    }
}
