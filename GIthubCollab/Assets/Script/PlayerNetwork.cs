  using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerNetwork : NetworkBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (!IsOwner) return;
        Vector3 moveDir = Vector3.zero;
        if (Input.GetKey(KeyCode.W)) moveDir.y = +1f;
        if (Input.GetKey(KeyCode.S)) moveDir.y = -1f;
        if (Input.GetKey(KeyCode.A)) moveDir.x = -1f;
        if (Input.GetKey(KeyCode.D)) moveDir.x = +1f;

        float movesSpeed = 3f;
        transform.position += moveDir * movesSpeed * Time.deltaTime;  
    }
}
