using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI;

public class NetworkManagerUI : MonoBehaviour
{
    // Start is called before the first frame update
    [SerializeField] private Button ServerButton;
    [SerializeField] private Button HostButton;
    [SerializeField] private Button ClientButton;

    private void Awake()
    {
        ServerButton.onClick.AddListener(() => {
            NetworkManager.Singleton.StartServer();
        });
        HostButton.onClick.AddListener(() => {
            NetworkManager.Singleton.StartHost();
        });
        ClientButton.onClick.AddListener(() => {
            NetworkManager.Singleton.StartClient();
        });
    }

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
