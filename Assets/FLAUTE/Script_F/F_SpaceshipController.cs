using System.Net.Sockets;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using static F_GameManager;

public class F_SpaceshipController : MonoBehaviour
{
    [SerializeField] F_GameManager gm;
    [SerializeField] Rigidbody2D rb;
    [SerializeField] GameObject charaSeat;
    [SerializeField] private LayerMask whatIsGround;
    
    [SerializeField] ParticleSystem takeofParticles;
    [SerializeField] ParticleSystem reactorParticles;
    [SerializeField] ParticleSystem canEnterSpaceshipParticles;
    //[SerializeField] ParticleSystem.EmissionModule canEnterSpaceshipEmitModule;
    [SerializeField] ParticleSystem LandingScanParticles;
    [SerializeField] ParticleSystem.MainModule landingScanMainModule;
    [SerializeField] ParticleSystem takeofSequenceParticles;
    //[SerializeField] ParticleSystem.EmissionModule landingScanEmitModule;
   //[SerializeField] float baseLandingScanEmitParticles = 3;
    [SerializeField] Color canLandOnStrateColor = new Color();
    [SerializeField] Color canNotLandOnStrateColor = new Color();


    [SerializeField] float zForce = 8;
    [SerializeField] float xyForce = 20;
    [SerializeField] float minDistToEnterSpaceship = 5;

    [SerializeField] float takeofDuration = 2;
    [SerializeField] float takeofTimer = 0;
    [SerializeField] float durationBeforeSnap = 1;
    [SerializeField] float snapTimer = 0;
    [SerializeField] float disableTakeofDuration = 0.1f;
    [SerializeField] float disableTakeofTimer = 0;

    //public bool isDriveMode = false;
    //public bool isStationaryMode = true;
    [SerializeField] bool canEnterSpaceship = false;
    [SerializeField] bool canLandOnStrate = false;
    [SerializeField] bool isClosestStrateSnaped = true;
    public bool needToPushEngineButtonAgainToTakeof = false;

    //ACCESSEURS
    public bool CanEnterSpaceship => canEnterSpaceship;
    public bool CanLandOnStrate => canLandOnStrate;
    public GameObject CharaSeat => charaSeat;


    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        landingScanMainModule = LandingScanParticles.main;
        //landingScanEmitModule = LandingScanParticles.emission;
        //INIT
        //isDriveMode = false;
        //isStationaryMode = true;
        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic;
        takeofParticles.Stop();
        reactorParticles.Stop();
        //landingScanEmitModule.rateOverTime = 0;
        LandingScanParticles.Stop();
    }

    // Update is called once per frame
    void Update()
    {
        if(gm.CurrentPlayerMode == PlayerMode.spaceshipMobile) {
            CheckClosestStrateSnaped();
            CheckCanLandOnStrate();
        } else {
            canLandOnStrate = false;
            //LandingScanParticles.Stop();
        }


        if(gm.CurrentPlayerMode == PlayerMode.spaceshipStationary) {         
            CheckSpaceshipTakeof();
        } 

        if(gm.CurrentPlayerMode == PlayerMode.character) {
            CheckCanEnterSpaceship();
        } else {
            canEnterSpaceship = false;
            canEnterSpaceshipParticles.Stop();
        }
        UpdateTimers();
        //little fix to avoid takeof effect when landing
        if(needToPushEngineButtonAgainToTakeof && gm.Inputs.SpaceshipEngineInput == 0 && disableTakeofTimer >= disableTakeofDuration) {
            needToPushEngineButtonAgainToTakeof = false;
        }
    }

    void FixedUpdate() {

        if(gm.CurrentPlayerMode == PlayerMode.spaceshipMobile) {
            ApplyMovement(); //set isClosestStrateSnaped
        }

    }

    //TRIGGER BY INPUT

    public void TryToLandOnStrate(InputAction.CallbackContext _context) {
        if(gm.CurrentPlayerMode == PlayerMode.spaceshipMobile && canLandOnStrate) {
            needToPushEngineButtonAgainToTakeof = true;
            rb.linearVelocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Kinematic;
            reactorParticles.Stop();

            //landingScanEmitModule.rateOverTime = 0;
            Debug.Log("TryToLandOnStrate LandingScanParticles STROP");
            LandingScanParticles.Stop();

            gm.OnShipLanding.Invoke();
        }
    }

    public void TryToQuitSpaceship(InputAction.CallbackContext _context) {
        if(gm.CurrentPlayerMode == PlayerMode.spaceshipStationary) {
            gm.OnShipExit.Invoke();
        }
    }

   


    //TRIGGER BY EVENT
    public void PlayerEnterSpaceship() { //trigger in player controler
        //stationary mode
    }


    //



    //UPDATE

    void UpdateTimers() {

        //DISABLE TAKEOF TIMER
        if(needToPushEngineButtonAgainToTakeof) {
            disableTakeofTimer = disableTakeofTimer >= disableTakeofDuration ? disableTakeofDuration : disableTakeofTimer + Time.deltaTime;
        } else {
            disableTakeofTimer = 0;
        }


        //TAKEOF TIMER
        if(gm.CurrentPlayerMode == PlayerMode.spaceshipStationary && !needToPushEngineButtonAgainToTakeof) {
            if(gm.Inputs.SpaceshipEngineInput != 0) {
                takeofTimer = takeofTimer >= takeofDuration ? takeofDuration : takeofTimer + Time.deltaTime;
                takeofSequenceParticles.gameObject.SetActive(true);
            } else {
                takeofTimer = 0;
                takeofSequenceParticles.gameObject.SetActive(false);
            }
        } else {
            takeofTimer = 0;
            takeofSequenceParticles.gameObject.SetActive(false);
        }


        //SNAP STRATE TIMER
        if(gm.CurrentPlayerMode == PlayerMode.spaceshipMobile) {
            if(gm.Inputs.ShipInputZ == 0) {
                snapTimer = snapTimer >= durationBeforeSnap ? durationBeforeSnap : snapTimer + Time.deltaTime;
            } else {
                snapTimer = 0;
                //snaping particles ?
            }
        } else {
            snapTimer = 0;
        }
   
    }


    void CheckClosestStrateSnaped() {
        float _closestZPosition = gm.ClosestStrate.transform.position.z;
        
        if( _closestZPosition != transform.position.z) {
            isClosestStrateSnaped = false;
            //landingScanEmitModule.rateOverTime = 0;
            Debug.Log("CheckClosestStrateSnaped LandingScanParticles STROP");
            LandingScanParticles.Stop();
        } else {

            if(!isClosestStrateSnaped) {
                LandingScanParticles.Play();
                gm.OnStrateSnaping.Invoke();
            }
            isClosestStrateSnaped = true;
            
            //landingScanEmitModule.rateOverTime = baseLandingScanEmitParticles;
            //Debug.Log("CheckClosestStrateSnaped LandingScanParticles PLAY");
           
        }
    }

    void CheckSpaceshipTakeof() {
        //décolage, hold input

        if(takeofTimer >= takeofDuration) {
            //isDriveMode = true;
            //isStationaryMode = false;
            needToPushEngineButtonAgainToTakeof = true;
            rb.bodyType = RigidbodyType2D.Dynamic;
            takeofParticles.Play();
            reactorParticles.Play();
            //landingScanEmitModule.rateOverTime = baseLandingScanEmitParticles;
            LandingScanParticles.Play();
            Debug.Log("CheckSpaceshipTakeof LandingScanParticles PLAY");

            gm.OnShipTakeof.Invoke();
        }

    }

    void CheckCanEnterSpaceship() {

        if(gm.CurrentPlayerMode != PlayerMode.character) {
            canEnterSpaceship = false;
            canEnterSpaceshipParticles.Stop();
            return;
        }

        if(Vector3.Distance(transform.position, gm.PlayerCharacter.transform.position) <= minDistToEnterSpaceship) {
            canEnterSpaceship = true;
            canEnterSpaceshipParticles.Play();
        } else {
            canEnterSpaceship = false;
            canEnterSpaceshipParticles.Stop();
        }
    }



    void CheckCanLandOnStrate() {

        if(isClosestStrateSnaped) {

            bool _hitGround = Physics2D.OverlapCircle(transform.position, minDistToEnterSpaceship, whatIsGround);
            if(_hitGround) {
                canLandOnStrate = false;
                landingScanMainModule.startColor = canNotLandOnStrateColor;
            } else {
                canLandOnStrate = true;
                landingScanMainModule.startColor = canLandOnStrateColor;
            }
            
        } else { //si closest strate pas snaped
            canLandOnStrate = false;
            landingScanMainModule.startColor = Color.white;

        }
    }

    //FIXED UPDATE
    void ApplyMovement() {

        //SNAP ON STRATE
        //ajouter un timer pendant que l'input == 0 avant d'essayer de snap
        if(snapTimer >= durationBeforeSnap && !isClosestStrateSnaped) {
            float _targetZPosition = gm.ClosestStrate.transform.position.z;
            float _newZ = Mathf.Lerp(transform.position.z, _targetZPosition, zForce * Time.fixedDeltaTime);
            transform.position = new Vector3(transform.position.x, transform.position.y, _newZ);
            
            if(Mathf.Abs(transform.position.z - _targetZPosition)<=0.01f) {
                transform.position = new Vector3(transform.position.x, transform.position.y, _targetZPosition);
            } 
        }


        //2D movement
        float _xForceFactor = Mathf.Abs(rb.linearVelocity.x);
        float _xForceToAdd = gm.Inputs.ShipInputXY.x * xyForce / (1f + Mathf.Log(_xForceFactor + 1f));
        float _yForceFactor = Mathf.Abs(rb.linearVelocity.y);
        float _yForceToAdd = gm.Inputs.ShipInputXY.y * xyForce / (1f + Mathf.Log(_yForceFactor + 1f));
        Vector2 _xyForceToAdd = new Vector2(_xForceToAdd, _yForceToAdd);
        rb.AddForce(_xyForceToAdd, ForceMode2D.Force);


        //Z Movement
        float _zForceToAdd = gm.Inputs.ShipInputZ * zForce * Time.fixedDeltaTime;
        Vector3 _newPos = Vector3.zero;
        _newPos.z = _zForceToAdd;
        transform.position += _newPos;
    }






    private void OnDrawGizmos() {
        Gizmos.color = canEnterSpaceship ? Color.green: Color.red;
        Gizmos.DrawWireSphere(transform.position, minDistToEnterSpaceship);

    }
}
