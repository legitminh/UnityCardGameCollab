using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class PlayerNetwork : NetworkBehaviour
{
    //private NetworkVariable<int> randomNumber = new NetworkVariable<int>(1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    [SerializeField] Transform serverBall;
    public struct NetworkData : INetworkSerializable
    {
        public int _int;
        public bool _bool;
        public FixedString32Bytes _keyPressed;
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref _int);
            serializer.SerializeValue(ref _bool);
            serializer.SerializeValue(ref _keyPressed);

        }
    }
    private NetworkVariable<NetworkData> randomNumber = new NetworkVariable<NetworkData>(
         new NetworkData{
             _int = 56,
             _bool = true,
             _keyPressed = "none",
         },
         NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner
        
        );
    // Start is called before the first frame update
    void Start()
    {
        if (IsOwner)
        {
            Transform spawnedObjectTransform = Instantiate(serverBall, new Vector3(3,0,0), Quaternion.identity);
            spawnedObjectTransform.GetComponent<NetworkObject>().Spawn(true);
        }   
    }
    public override void OnNetworkSpawn()
    {
        randomNumber.OnValueChanged += (NetworkData previousValue, NetworkData newValue) =>
        {
            Debug.Log(OwnerClientId + " ; " + randomNumber.Value._int + randomNumber.Value._bool + randomNumber.Value._keyPressed);
        };
    } 
    // Update is called once per frame
    void Update()
    {

        

        if (!IsOwner) return; // if machine is owner of object then they can control it (pass this point)
        Vector3 moveDir = Vector3.zero;
        if (Input.GetKey(KeyCode.W)) moveDir.y = +1f;
        if (Input.GetKey(KeyCode.S)) moveDir.y = -1f;
        if (Input.GetKey(KeyCode.A)) moveDir.x = -1f;
        if (Input.GetKey(KeyCode.D)) moveDir.x = +1f;



        if (Input.GetKeyDown(KeyCode.R))
        {
            TestServerRpc();  
            //randomNumber.Value = new NetworkData {
            //    _int = Random.Range(0, 100),
            //    _bool = false,
            //    _keyPressed = randomNumber.Value._keyPressed,

            //};            
        }


        float movesSpeed = 3f;
        transform.position += moveDir * movesSpeed * Time.deltaTime;  
    }
    [ServerRpc]
    private void TestServerRpc()
    {
        Debug.Log("TestServerRpc " + OwnerClientId);
    }
}
