using UnityEngine;

public class F_Clone : MonoBehaviour
{
    [SerializeField] SpriteRenderer parentSpriteRenderer;
    SpriteRenderer mySpriteRenderer;

    [SerializeField] float cloneOpacity = 0;

    void Start()
    {
        parentSpriteRenderer = transform.parent.GetComponent<SpriteRenderer>();
        mySpriteRenderer = GetComponent<SpriteRenderer>();
    }

    // Update is called once per frame
    void Update()
    {
        mySpriteRenderer.sprite = parentSpriteRenderer.sprite;
        mySpriteRenderer.color = new Color(parentSpriteRenderer.color.r, parentSpriteRenderer.color.g, parentSpriteRenderer.color.b,cloneOpacity);
    }
}
