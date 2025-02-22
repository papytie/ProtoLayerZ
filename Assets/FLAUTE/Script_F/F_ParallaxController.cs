using UnityEngine;

public class F_ParallaxController : MonoBehaviour
{
    Vector2 startPos;
    [SerializeField] GameObject cam;
    [SerializeField] float parallaxFactor;

    void Start()
    {
        startPos = (Vector2) transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        Vector2 _positionDifference = (Vector2)cam.transform.position * parallaxFactor;
        transform.position = new Vector3(_positionDifference.x, _positionDifference.y, transform.position.z);
    }
}
