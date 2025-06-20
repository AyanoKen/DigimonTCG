using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class BattleLogManager : MonoBehaviour
{
    public static BattleLogManager Instance;

    public Transform logContainer;
    public GameObject logEntryPrefab;
    public GameObject logPanel;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public enum LogType
    {
        Buff,
        Attack,
        Destroy,
        Effect,
        System
    }

    public void AddLog(string message, LogType type)
    {
        Color color = GetColorForType(type);
        AddLog(message, color);
    }

    public void AddLog(string message, Color color)
    {
        GameObject entry = Instantiate(logEntryPrefab, logContainer);
        TMP_Text text = entry.GetComponent<TMP_Text>();

        text.text = message;
        text.color = color;
    }

    public void ClearLog()
    {
        foreach (Transform child in logContainer)
        {
            Destroy(child.gameObject);
        }
    }

    private Color GetColorForType(LogType type)
    {
        switch (type)
        {
            case LogType.Buff: return Color.white;
            case LogType.Attack: return Color.white;
            case LogType.Destroy: return Color.white;
            case LogType.Effect: return Color.white;
            case LogType.System: return Color.white;
            default: return Color.white;
        }
    }

    public void ToggleBattleLog()
    {
        logPanel.SetActive(!logPanel.activeSelf);
    }
}
