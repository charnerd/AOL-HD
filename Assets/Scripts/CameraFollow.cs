using UnityEngine;

public class CameraFollow : MonoBehaviour
{

    private Transform player;

    private Vector3 tempPos;

    [SerializeField]
    private float minX, maxX;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        player = GameObject.FindWithTag("Player").transform;
    }

    // Update is called once per frame
    void LateUpdate()
    {
        
        tempPos = transform.position;
        tempPos.x = player.position.x;
        tempPos.y = player.position.y;

        transform.position = tempPos;

    }
} // class
