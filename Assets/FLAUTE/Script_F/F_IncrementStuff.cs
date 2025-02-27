using DG.Tweening;
using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UIElements;

public class F_IncrementStuff : MonoBehaviour
{

    [SerializeField] Transform strateContainer;
    [SerializeField] List<GameObject> allObjects = new List<GameObject>();
    [SerializeField] List<SpriteRenderer> spriteRenderers = new List<SpriteRenderer>();
    [SerializeField] float spacing = 1;

    [SerializeField] Color startColor = new Color();
    [SerializeField] Color endColor = new Color();
    

    void Start()
    {
    
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void SetAllObjectList() {
        allObjects.Clear();
        foreach(Transform _trans in strateContainer) {
            allObjects.Add(_trans.gameObject);
        }
    }

    [ContextMenu("Increment Position")]
    void IncrementPosition() {

        SetAllObjectList();
        if(allObjects.Count == 0) return;


        Vector3 _startPos = allObjects[0].transform.position;
        for(int i = 0; i < allObjects.Count; i++) {
            allObjects[i].transform.position = new Vector3(_startPos.x, _startPos.y, spacing * i);
        }

    }


    [SerializeField] float colorIncremCurveSpeed = 5;

    [ContextMenu("Increment Color")]

    void IncrementColor() {

        endColor = Camera.main.backgroundColor;

        for(int i = 0; i < allObjects.Count; i++) {

            float _normalizedCount = ((float) i) / allObjects.Count;
            float _alpha = 1f - Mathf.Exp(-colorIncremCurveSpeed * _normalizedCount);

            Color _lerpedColor = Color.Lerp(startColor, endColor, _alpha);
            SpriteShapeRenderer _renderer = allObjects[i].GetComponentInChildren<SpriteShapeRenderer>();
            _renderer.color = _lerpedColor;

        }

    }

    [ContextMenu("Random Colors SpriteRenderers")]
    void RandomColor() {
        foreach(SpriteRenderer _spriteRenderer in spriteRenderers) {
            _spriteRenderer.color = new Color(Random.value, Random.value, Random.value);
        }
    }
}
