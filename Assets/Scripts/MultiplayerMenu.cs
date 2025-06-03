using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

public class MultiplayerMenu : MonoBehaviour
{
    public TMP_InputField ipInput;

    public void Host()
    {
        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        transport.SetConnectionData("0.0.0.0", 7777);
        NetworkManager.Singleton.StartHost();
    }

    public void Join()
    {
        string ip = ipInput.text;
        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        transport.SetConnectionData(ip, 7777);
        NetworkManager.Singleton.StartClient();
    }
}
