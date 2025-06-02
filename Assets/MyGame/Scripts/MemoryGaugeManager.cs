using UnityEngine;
using UnityEngine.UI;

public class MemoryGaugeManager : MonoBehaviour
{
    [SerializeField] private GameObject[] positiveMemorySlots;
    [SerializeField] private GameObject[] negativeMemorySlots;
    [SerializeField] private GameObject zeroMemorySlot;

    private int currentMemory = 0;

    public void SetMemory(int value)
    {
        currentMemory = Mathf.Clamp(value, -10, 10);

        DisableAllOutlines();

        if (currentMemory > 0)
        {
            positiveMemorySlots[currentMemory - 1].GetComponent<Outline>().enabled = true;
        }
        else if (currentMemory < 0)
        {
            negativeMemorySlots[Mathf.Abs(currentMemory) - 1].GetComponent<Outline>().enabled = true;
        }
        else
        {
            zeroMemorySlot.GetComponent<Outline>().enabled = true;
        }
    }

    private void DisableAllOutlines()
    {
        foreach (var slot in positiveMemorySlots)
        {
            slot.GetComponent<Outline>().enabled = false;
        }

        foreach (var slot in negativeMemorySlots)
        {
            slot.GetComponent<Outline>().enabled = false;
        }

        zeroMemorySlot.GetComponent<Outline>().enabled = false;
    }

    public int GetCurrentMemory()
    {
        return currentMemory;
    }
}
