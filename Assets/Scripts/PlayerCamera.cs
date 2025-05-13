using UnityEngine;
using Unity.Netcode;

public class PlayerCamera : NetworkBehaviour
{
    public Camera playerCam;

    void Start()
    {
        if (!IsOwner)
        {
            playerCam.enabled = false;
            GetComponentInChildren<AudioListener>().enabled = false;
        }
    }
}