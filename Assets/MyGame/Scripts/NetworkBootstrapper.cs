using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NetworkBootstrapper : MonoBehaviour
{
    [SerializeField] private string gameSceneName = "Game";
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
}
