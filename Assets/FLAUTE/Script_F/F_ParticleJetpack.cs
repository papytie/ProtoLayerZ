using UnityEngine;

public class F_ParticleJetpack : MonoBehaviour
{

    [SerializeField] GameObject player;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        transform.position = player.transform.position;
    }
}
