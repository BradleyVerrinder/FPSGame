using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using System.Threading.Tasks;

public class RelayNetworkUI : MonoBehaviour
{
    string joinCode = "";
    string inputJoinCode = "";
    string statusMessage = "";

    async void Start()
    {
        await InitializeUnityServices();
    }

    async Task InitializeUnityServices()
    {
        if (!UnityServices.State.Equals(ServicesInitializationState.Initialized))
        {
            await UnityServices.InitializeAsync();
        }

        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            Debug.Log("Signed in with ID: " + AuthenticationService.Instance.PlayerId);
        }
    }

    async Task StartHostWithRelay()
    {
        try
        {
            var allocation = await RelayService.Instance.CreateAllocationAsync(4); // Max players

            joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            statusMessage = "Join Code: " + joinCode;

            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            transport.SetRelayServerData(
                allocation.RelayServer.IpV4,
                (ushort)allocation.RelayServer.Port,
                allocation.AllocationIdBytes,
                allocation.Key,
                allocation.ConnectionData
            );

            NetworkManager.Singleton.StartHost();
        }
        catch (RelayServiceException e)
        {
            Debug.LogError(e);
            statusMessage = "Relay Host Error: " + e.Message;
        }
    }

    async Task JoinWithRelay(string joinCode)
    {
        try
        {
            var joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);

            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            transport.SetRelayServerData(
                joinAllocation.RelayServer.IpV4,
                (ushort)joinAllocation.RelayServer.Port,
                joinAllocation.AllocationIdBytes,
                joinAllocation.Key,
                joinAllocation.ConnectionData,
                joinAllocation.HostConnectionData
            );

            NetworkManager.Singleton.StartClient();
        }
        catch (RelayServiceException e)
        {
            Debug.LogError(e);
            statusMessage = "Join Error: " + e.Message;
        }
    }

    void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 10, 300, 500));
        try
        {
            GUILayout.Label("Relay Multiplayer FPS");
            GUILayout.Space(10);
    
            if (!NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer)
            {
                if (GUILayout.Button("Start Host (Relay)"))
                {
                    _ = StartHostWithRelay();
                }
    
                GUILayout.Space(20);
                GUILayout.Label("Enter Join Code:");
                inputJoinCode = GUILayout.TextField(inputJoinCode);
    
                if (GUILayout.Button("Join Game"))
                {
                    _ = JoinWithRelay(inputJoinCode);
                }
            }
            else
            {
                if (NetworkManager.Singleton.IsHost)
                {
                    GUILayout.Label("Hosting Game");
                    GUILayout.Label("Join Code: " + joinCode);
                }
                else if (NetworkManager.Singleton.IsClient)
                {
                    GUILayout.Label("Client Connected");
                }
    
                if (GUILayout.Button("Shutdown"))
                {
                    NetworkManager.Singleton.Shutdown();
                }
            }
    
            GUILayout.Space(20);
            GUILayout.Label("Status: " + statusMessage);
        }
        finally
        {
            GUILayout.EndArea();
        }
    }

}
