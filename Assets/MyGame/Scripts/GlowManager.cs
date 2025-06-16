using UnityEngine;

public class GlowManager : MonoBehaviour
{
    public static GlowManager Instance;

    public GameObject glowPrefab;

    private GameObject activeGlow;
    private Transform currentTarget;

    private void Awake()
    {
        Instance = this;
    }

    public void ShowGlow(Transform target)
    {
        if (currentTarget == target)
        {
            return;
        }

        HideGlow();

        activeGlow = Instantiate(glowPrefab, target);
        activeGlow.transform.SetAsLastSibling();
        activeGlow.SetActive(true);
        currentTarget = target;
    }

    public void HideGlow()
    {
        if (activeGlow != null)
        {
            Destroy(activeGlow);
            activeGlow = null;
            currentTarget = null;
        }
    }
}
