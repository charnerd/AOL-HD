using UnityEngine;

public class TextFollowPlayer : MonoBehaviour
{
    [Header("Target & Positioning")]
    public Transform player;
    public Vector3 offset = new Vector3(0, 2f, 0);

    // Update is called once per frame
    void Update()
    {
        transform.position = player.position + offset;
    }
}
