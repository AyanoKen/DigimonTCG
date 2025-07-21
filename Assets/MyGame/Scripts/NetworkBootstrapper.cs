using Unity.Netcode;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using Unity.Networking.Transport.Relay;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Core;
using Unity.Services.Authentication;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class NetworkBootstrapper : MonoBehaviour
{
    [SerializeField] private string gameSceneName = "Game";
    [SerializeField] private TMP_InputField joinCodeInput;

    private async void Start()
    {
        await UnityServices.InitializeAsync();
        await AuthenticationService.Instance.SignInAnonymouslyAsync();
    }
    public void StartHost()
    {
        if (NetworkManager.Singleton.StartHost())
        {
            Debug.Log("Hosting game...");
            NetworkManager.Singleton.SceneManager.LoadScene(gameSceneName, LoadSceneMode.Single);
        }
        else
        {
            Debug.LogError("Failed to start Host");
        }
    }

    public void StartClient()
    {
        if (NetworkManager.Singleton.StartClient())
        {
            Debug.Log("Looking for games to join");
        }
        else
        {
            Debug.LogError("Failed to start client");
        }
    }

    public async void StartHostWithRelay()
    {
        try
        {
            var allocation = await RelayService.Instance.CreateAllocationAsync(1);
            var joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            var relayServerData = new RelayServerData(allocation, "dtls");
            RelaySessionInfo.JoinCode = joinCode;
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);

            if (NetworkManager.Singleton.StartHost())
            {
                Debug.Log("Hosting game via Relay. Join Code: " + joinCode);
                NetworkManager.Singleton.SceneManager.LoadScene(gameSceneName, LoadSceneMode.Single);
            }
        }
        catch (RelayServiceException e)
        {
            Debug.LogError("Relay Error: " + e.Message);
        }
    }

    public async void JoinGameWithRelay()
    {
        string joinCode = joinCodeInput.text;

        try
        {
            var joinAlloc = await RelayService.Instance.JoinAllocationAsync(joinCode);
            var relayServerData = new RelayServerData(joinAlloc, "dtls");
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);

            if (NetworkManager.Singleton.StartClient())
            {
                Debug.Log("Joining game with Relay using code: " + joinCode);
            }
        }
        catch (RelayServiceException e)
        {
            Debug.LogError("Relay Join Error: " + e.Message);
        }
    }
}
