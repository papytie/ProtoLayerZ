using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.U2D;
using System.Linq;
using DG.Tweening;

public class F_Strate : MonoBehaviour
{

    [SerializeField] F_GameManager gm;

    [SerializeField] List<GameObject> allActivableObjects = new List<GameObject>();
    [SerializeField] List<GameObject> allConstantObjects = new List<GameObject>();
    [SerializeField] List<SpriteRenderer> allSpriteRenderers = new List<SpriteRenderer>();
    [SerializeField] SpriteMask mySpriteMask;
    [SerializeField] SpriteShapeRenderer spriteShapeRenderer;
    [SerializeField] PolygonCollider2D spriteShapeCollider;
    
    [SerializeField] Material baseMat;

    [SerializeField] F_Strate myNextStrate = null;
    [SerializeField] F_Strate myPreviousStrate = null;
    [SerializeField] float portalAnimationDuration = 1f;
    [SerializeField] float propagationDelayAnimPortal = 0.2f;


    [SerializeField] int mySortingOrderNumber;
    [SerializeField] bool isCurrentStrate = false;
    [SerializeField] bool isClosestStrate = false;
    [SerializeField] bool isPreviousStrate = false;
    [SerializeField] bool isNextStrate = false;
    [SerializeField] bool isDistantStrate = false;
    //[SerializeField] bool isSnapedStrate = false;
    [SerializeField] bool isClosed = false;

    [SerializeField] float mySpriteMaskScale;
    Color currentColor;
    Color targetColor;
    Tween spriteMaskCutoutTween;

    //ACCESSEUR
    public F_GameManager MyGameManger { get => gm; set => gm = value; }
    public int SortingOrderStrate { get => mySortingOrderNumber; set => mySortingOrderNumber = value; }
    public SpriteShapeRenderer SpriteShapeRenderer { get => spriteShapeRenderer; set => spriteShapeRenderer = value; }
    public PolygonCollider2D SpriteShapeCollider { get => spriteShapeCollider; set => spriteShapeCollider = value; }
    public SpriteMask SpriteMask { get => mySpriteMask; set => mySpriteMask = value; }
    public F_Strate MyNextStrate { get => myNextStrate; set => myNextStrate = value; }
    public F_Strate MyPreviousStrate { get => myPreviousStrate; set => myPreviousStrate = value; }


    private void Awake() {

    }


    void Start()
    {
        mySpriteMaskScale = mySpriteMask.transform.localScale.x;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    //TRIGGER BY EVENTS

    public void AnimateColor(Color _targetColorFill, Color _targetColorEdge, float _maxCutofValue) {

        spriteShapeRenderer.materials[0].DOColor(_targetColorFill, 1).SetEase(Ease.OutQuad);
        spriteShapeRenderer.materials[1].DOColor(_targetColorEdge, 1f).SetEase(Ease.OutQuad);
        
        /*
        foreach(SpriteRenderer _renderer in allSpriteRenderers) {
            _renderer.DOColor(_targetColorFill,1).SetEase(Ease.OutQuad);
        } 
        */

        foreach(GameObject _obj in allConstantObjects) {
            SpriteRenderer _spriteRenderer = _obj.GetComponent<SpriteRenderer>();
            if(_spriteRenderer != null) {
                _spriteRenderer.DOColor(_targetColorFill, 1).SetEase(Ease.OutQuad);
            }
        }

        //FIX Séparer la fonction
        /*
        if(isPreviousStrate) {
            DOTween.To(() => mySpriteMask.alphaCutoff, x => mySpriteMask.alphaCutoff = x, _maxCutofValue, 1f);
        } else {
            DOTween.To(() => mySpriteMask.alphaCutoff, x => mySpriteMask.alphaCutoff = x, 0, 1f);
        }
        */
    }

    public void AnimatedMask(float _maxCutofValue, int _direction) {

        //savoir si j'avance ou je recule pour avoir des valeurs différentes
        //en avançant vers l'avant, d'abord on devient closest 2sec d'anim puis previous 1sec, donc closest finit plus tard et on finit closest (à 0) alors qu'on devrait previous (à _maxCutofValue)
        //la nouvelle anim devrai interrompre celle en cours

        //devient closest strate
        if(_direction == 1) {
            Debug.Log("ANIM MASK next = Full transp on " + gameObject.name);
            if(spriteMaskCutoutTween != null && spriteMaskCutoutTween.IsActive()) {
                spriteMaskCutoutTween.Kill();
            }

            spriteMaskCutoutTween = DOTween.To(() => mySpriteMask.alphaCutoff, x => mySpriteMask.alphaCutoff = x, 0, 2f);
            return;
        }

        //devient previous strate
        if(_direction == -1) {
            Debug.Log("ANIM MASK previous = dissolve on " + gameObject.name);
            if(spriteMaskCutoutTween != null && spriteMaskCutoutTween.IsActive()) {
                spriteMaskCutoutTween.Kill();
            }
            spriteMaskCutoutTween = DOTween.To(() => mySpriteMask.alphaCutoff, x => mySpriteMask.alphaCutoff = x, _maxCutofValue, 1f);//.OnKill(() => mySpriteMask.alphaCutoff = 0);
            return;
        }
      
    }

    public void AnimatePortal(bool _OpenAnim,int _direction) {

        if(isClosed) return;

        //0 means both previous and next
        //1 means next
        //-1 means previous

        if(_direction == 0) {
            if(_OpenAnim) {
                //open anim
                mySpriteMask.transform.DOScale(mySpriteMaskScale, gm.PortalAnimationDuration).SetEase(Ease.OutCubic);
                if(myPreviousStrate != null) {
                    myPreviousStrate.AnimatePortal(true, -1);
                }
                if(myNextStrate != null) {
                    myNextStrate.AnimatePortal(true, 1);
                }
            } else {
                //close anim
                mySpriteMask.transform.DOScale(0, gm.PortalAnimationDuration).SetEase(Ease.OutCubic);
                if(myPreviousStrate != null) {
                    myPreviousStrate.AnimatePortal(false, -1);
                }
                if(myNextStrate != null) {
                    myNextStrate.AnimatePortal(false, 1);
                }
            }
            return;
        }

        if(_direction == 1) {
            if(_OpenAnim) {
                //open anim
                mySpriteMask.transform.DOScale(mySpriteMaskScale, gm.PortalAnimationDuration).SetEase(Ease.OutCubic).SetDelay(gm.PropagationDelayAnimPortal*gm.NextDelayMultiplier);
                gm.NextDelayMultiplier++;
                if(myNextStrate != null) {
                    myNextStrate.AnimatePortal(true, 1);
                }
            } else {
                //close anim
                mySpriteMask.transform.DOScale(0, gm.PortalAnimationDuration).SetEase(Ease.OutCubic).SetDelay(gm.PropagationDelayAnimPortal * gm.NextDelayMultiplier);
                gm.NextDelayMultiplier++;
                if(myNextStrate != null) {
                    myNextStrate.AnimatePortal(false, 1);
                }
            }
            return;
        }

        if(_direction == -1) {
            if(_OpenAnim) {
                //open anim
                mySpriteMask.transform.DOScale(mySpriteMaskScale, gm.PortalAnimationDuration).SetEase(Ease.OutCubic).SetDelay(gm.PropagationDelayAnimPortal * gm.PreviousDelayMultiplier);
                gm.PreviousDelayMultiplier++;
                if(myPreviousStrate != null) {
                    myPreviousStrate.AnimatePortal(true, -1);
                }
            } else {
                //close anim
                mySpriteMask.transform.DOScale(0, gm.PortalAnimationDuration).SetEase(Ease.OutCubic).SetDelay(gm.PropagationDelayAnimPortal * gm.PreviousDelayMultiplier);
                gm.PreviousDelayMultiplier++;
                if(myPreviousStrate != null) {
                    myPreviousStrate.AnimatePortal(false, -1);
                }
            }
            return;
        }
    }


    public void SetSnapedStrate() {
        Debug.Log("set snaped strate " + gameObject.name);
    
        spriteShapeRenderer.maskInteraction = SpriteMaskInteraction.None;
        spriteShapeCollider.enabled = true;


        EnableObjects();
        foreach(SpriteRenderer _renderer in allSpriteRenderers) {
            if(_renderer.GetComponent<F_ConstantElement>() == null) {
                Color _newColor = new Color(_renderer.color.r, _renderer.color.g, _renderer.color.b,1);
                _renderer.DOColor(_newColor, 1).SetEase(Ease.OutQuad);
            }
        }
    }

    public void SetCurrentStrate() {
        Debug.Log("set current strate " + gameObject.name);

        isCurrentStrate = true;
        isClosestStrate = true;
        isPreviousStrate = false;
        isNextStrate = false;
        isDistantStrate = false;


        spriteShapeRenderer.maskInteraction = SpriteMaskInteraction.None;
        spriteShapeCollider.enabled = true;

        EnableObjects();
        foreach(SpriteRenderer _renderer in allSpriteRenderers) {
            if(_renderer.GetComponent<F_ConstantElement>() == null) {
                Color _newColor = new Color(_renderer.color.r, _renderer.color.g, _renderer.color.b, 1);
                _renderer.DOColor(_newColor, 1).SetEase(Ease.OutQuad);
            }
        }
    }

    public void SetClosestStrate() {
        Debug.Log("set closest strate " + gameObject.name);
        isClosestStrate = true;
        isCurrentStrate = false;
        isPreviousStrate = false;
        isNextStrate = false;
        isDistantStrate = false;

        //spriteShapeRenderer.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
        //spriteShapeRenderer.maskInteraction = SpriteMaskInteraction.None;
        //spriteShapeCollider.enabled = true;
        //EnableObjects();
        //DisableObjects();
        //mySpriteMask.gameObject.SetActive(true);
    }

    public void SetPreviousStrate() {
        Debug.Log("set previous strate " + gameObject.name);
        isPreviousStrate = true;
        isCurrentStrate = false;
        isClosestStrate = false;
        isNextStrate = false;
        isDistantStrate = false;

        spriteShapeRenderer.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
        spriteShapeCollider.enabled = false;


        //FADE ET DISABLE
        foreach(SpriteRenderer _renderer in allSpriteRenderers) {
            if(_renderer.GetComponent<F_ConstantElement>() == null) {
                Color _newColor = new Color(_renderer.color.r, _renderer.color.g, _renderer.color.b, 0);
                _renderer.DOColor(_newColor, 0.5f).SetEase(Ease.OutQuad).OnKill(() => allActivableObjects[0].gameObject.SetActive(false));
            }
        }
        //DisableObjects();
        //mySpriteMask.gameObject.SetActive(false);
    }

    public void SetNextStrate() {
        Debug.Log("set next strate " + gameObject.name);
        isNextStrate = true;
        isPreviousStrate = false;
        isCurrentStrate = false;
        isClosestStrate = false;
        isDistantStrate = false;

        spriteShapeRenderer.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
        spriteShapeCollider.enabled = false;

        //FADE ET DISABLE
        foreach(SpriteRenderer _renderer in allSpriteRenderers) {
            if(_renderer.GetComponent<F_ConstantElement>() == null) {
                Color _newColor = new Color(_renderer.color.r, _renderer.color.g, _renderer.color.b, 0);
                _renderer.DOColor(_newColor, 1f).SetEase(Ease.OutQuad).OnKill(() => allActivableObjects[0].gameObject.SetActive(false));
            }
        }
        //mySpriteMask.gameObject.SetActive(true);
    }

    public void SetDistantStrate() {
        Debug.Log("set distant strate " + gameObject.name);
        isDistantStrate = true;
        isNextStrate = false;
        isPreviousStrate = false;
        isCurrentStrate = false;
        isClosestStrate = false;

        spriteShapeRenderer.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
        spriteShapeCollider.enabled = false;

        /*

        //FADE ET DISABLE
        foreach(SpriteRenderer _renderer in allSpriteRenderers) {
            if(_renderer.GetComponent<F_ConstantElement>() != null) {
                Color _newColor = new Color(_renderer.color.r, _renderer.color.g, _renderer.color.b, 0);
                _renderer.DOColor(_newColor, 1).SetEase(Ease.OutQuad).OnKill(() => _renderer.gameObject.SetActive(false));
            }
        }
        */
    }

    public void DisableObjects() {
        foreach(GameObject obj in allActivableObjects) {
            obj.SetActive(false);
        }
    }

    public void EnableObjects() {
        foreach(GameObject obj in allActivableObjects) {
            obj.SetActive(true);
        }
    }

    //[ContextMenu("Set allActivableObjects List")]
    public void SetObjectsLists() {
        allActivableObjects.Clear();
        allConstantObjects.Clear();

  

        foreach(Transform _trans in  transform) {
            if(_trans.GetComponent<F_ConstantElement>() == null) {
                allActivableObjects.Add(_trans.gameObject);
            } 
        }

        GetAllChildTransformsConstantObjects(transform);

        /*
        for(int i = 0; i < transform.childCount; i++) {
            if(transform.GetChild(i).GetComponent<F_ConstantElement>() == null) {
                allActivableObjects.Add(transform.GetChild(i).gameObject);
            } else {
                allConstantObjects.Add(transform.GetChild(i).gameObject);
            }
        }
        */
    }

    void GetAllChildTransformsConstantObjects(Transform _parent) {
        foreach(Transform _child in _parent) {
            if(_child.GetComponent<F_ConstantElement>() != null) {
                allConstantObjects.Add(_child.gameObject);
            }
            GetAllChildTransformsConstantObjects(_child);
        }
    }

    //[ContextMenu("Set allSpriteRendrerers")]
    public void SetAllSpriteRenderers() {

        allSpriteRenderers = transform.GetComponentsInChildren<SpriteRenderer>(true).ToList();

        foreach(SpriteRenderer _render in allSpriteRenderers) {
            _render.sortingOrder = mySortingOrderNumber;
        }

        spriteShapeRenderer.sortingOrder = mySortingOrderNumber;

        mySpriteMask.isCustomRangeActive = true;
        mySpriteMask.backSortingOrder = mySortingOrderNumber - 1;
        mySpriteMask.frontSortingOrder = mySortingOrderNumber + 1;
    }
}
