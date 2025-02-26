using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.U2D;
using System.Linq;
using DG.Tweening;

public class F_Strate : MonoBehaviour
{
    [SerializeField] List<GameObject> allActivableObjects = new List<GameObject>();
    [SerializeField] List<GameObject> allConstantObjects = new List<GameObject>();
    [SerializeField] List<SpriteRenderer> allSpriteRenderers = new List<SpriteRenderer>();
    [SerializeField] SpriteMask mySpriteMask;
    [SerializeField] SpriteShapeRenderer spriteShapeRenderer;
    [SerializeField] PolygonCollider2D spriteShapeCollider;
    
    [SerializeField] Material baseMat;


    [SerializeField] int mySortingOrderNumber;
    [SerializeField] bool isCurrentStrate = false;
    [SerializeField] bool isClosestStrate = false;
    [SerializeField] bool isPreviousStrate = false;
    [SerializeField] bool isNextStrate = false;
    [SerializeField] bool isDistantStrate = false;

    Color currentColor;
    Color targetColor;


    //ACCESSEUR
    public int SortingOrderStrate { get => mySortingOrderNumber; set => mySortingOrderNumber = value; }
    public SpriteShapeRenderer SpriteShapeRenderer { get => spriteShapeRenderer; set => spriteShapeRenderer = value; }
    public PolygonCollider2D SpriteShapeCollider { get => spriteShapeCollider; set => spriteShapeCollider = value; }
    public SpriteMask SpriteMask { get => mySpriteMask; set => mySpriteMask = value; }


    private void Awake() {
        //spriteSphapeRenderer = GetComponentInChildren<SpriteShapeRenderer>();
        //spriteSphapeCollider = GetComponentInChildren<SpriteShapeController>().polygonCollider;
        //mySpriteMask = GetComponentInChildren<SpriteMask>();


    }


    void Start()
    {
        //SetDistantStrate();
        //SetObjectsLists();

        //SetAllSpriteRenderers();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    //TRIGGER BY EVENTS

    public void AnimateColor(Color _targetColorFill, Color _targetColorEdge, float _maxCutofValue) {

        spriteShapeRenderer.materials[0].DOColor(_targetColorFill, 1).SetEase(Ease.OutQuad);
        spriteShapeRenderer.materials[1].DOColor(_targetColorEdge, 1f).SetEase(Ease.OutQuad);
        foreach(SpriteRenderer _renderer in allSpriteRenderers) {
            _renderer.DOColor(_targetColorFill,1).SetEase(Ease.OutQuad);
        }

        if(isPreviousStrate) {
            DOTween.To(() => SpriteMask.alphaCutoff, x => SpriteMask.alphaCutoff = x, _maxCutofValue, 1f);
        } else {
            DOTween.To(() => SpriteMask.alphaCutoff, x => SpriteMask.alphaCutoff = x, 0, 1f);
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
        mySpriteMask.gameObject.SetActive(true);
    }

    public void SetClosestStrate() {
        Debug.Log("set closest strate " + gameObject.name);
        isClosestStrate = true;
        isCurrentStrate = false;
        isPreviousStrate = false;
        isNextStrate = false;
        isDistantStrate = false;

        spriteShapeRenderer.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
        spriteShapeCollider.enabled = true;
        DisableObjects();
        mySpriteMask.gameObject.SetActive(true);
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
        
        DisableObjects();
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
       
        DisableObjects();
        mySpriteMask.gameObject.SetActive(true);
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
        
        DisableObjects();
        mySpriteMask.gameObject.SetActive(true);
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
        for(int i = 0; i < transform.childCount; i++) {
            if(transform.GetChild(i).GetComponent<F_ConstantElement>() == null) {
                allActivableObjects.Add(transform.GetChild(i).gameObject);
            } else {
                allConstantObjects.Add(transform.GetChild(i).gameObject);
            }
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
