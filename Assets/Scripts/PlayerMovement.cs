using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public float speed = 5f;
    private bool isMovementEnabled = true; // Flag to control movement

    // start - called before first frame update
    void Start()
    {

    }

    // update - called once per frame
    void Update()
    {
        // Only move if movement is enabled
        if (!isMovementEnabled) return;

        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        Vector2 pos = transform.position;

        pos.x += h * speed * Time.deltaTime;
        pos.y += v * speed * Time.deltaTime;

        transform.position = pos;
    }

    // Public method to disable movement
    public void DisableMovement()
    {
        isMovementEnabled = false;
        Debug.Log("Player movement disabled");
    }

    // Public method to enable movement
    public void EnableMovement()
    {
        isMovementEnabled = true;
        Debug.Log("Player movement enabled");
    }

    // Public method to check if movement is enabled
    public bool IsMovementEnabled()
    {
        return isMovementEnabled;
    }
}