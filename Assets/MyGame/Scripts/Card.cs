using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using TMPro;
using Unity.Netcode;

[System.Serializable]
public class DigivolveCostEntry
{
    public string color;
    public int cost;
}


public class Card : NetworkBehaviour, IPointerClickHandler, IPointerDownHandler, IPointerUpHandler
{
    [SerializeField] private TMP_Text attackButton;
    [SerializeField] private Button blockerButton;

    public int cardId;
    public int ownerId;
    public bool canAttack = false;
    public bool mainEffectUsed = false;
    public GameObject actionPanel;
    public bool isBlocker = false;
    public bool isBlocking = false;
    public bool isSuspended = false;
    public bool isDigivolved = false;

    public int securityAttackCount = 1;
    public int dpBuff = 0;

    public enum Zone
    {
        Deck,
        Hand,
        BreedingEggSlot,
        BreedingActiveSlot,
        BattleArea,
        TamerArea,
        Trash,
        Security,
        None
    }

    public string cardName;
    public string cardType;
    public int? level;
    public string color;
    public string form;
    public string attribute;
    public int? dp;
    public int playCost;
    public Sprite sprite;
    public List<DigivolveCostEntry> digivolveCost;
    public List<Card> inheritedStack = new List<Card>();
    public List<EffectData> effects = new List<EffectData>();
    public List<EffectData> inheritedEffects = new List<EffectData>();

    private bool isZoomed = false;
    private Vector3 originalPosition;
    private Vector3 originalScale;
    private Vector2 originalSize;
    private Transform originalParent;
    private Canvas topCanvas;

    private RectTransform rectTransform;

    public NetworkVariable<Zone> currentZone = new NetworkVariable<Zone>(Zone.None);

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        currentZone.OnValueChanged += OnZoneChanged;

        UpdateView();
    }

    private void OnDestroy()
    {
        currentZone.OnValueChanged -= OnZoneChanged;
    }

    private void OnZoneChanged(Zone previous, Zone current)
    {
        UpdateView();
    }

    public void UpdateView()
    {
        if (!IsOwner && currentZone == Zone.Hand)
        {
            image.sprite = GameManager.Instance.cardBackSprite;
        }
        else
        {
            image.sprite = sprite;
        }

        Transform newParent = null;
        switch (currentZone.Value)
        {
            case Zone.Hand:
                newParent = IsOwner ? GameManager.Instance.handZone_Bottom : GameManager.Instance.handZone_Top;
                break;
            case Zone.BattleArea:
                newParent = IsOwner ? GameManager.Instance.battleZone_Bottom : GameManager.Instance.battleZone_Top;
                break;
            case Zone.TamerArea:
                newParent = IsOwner ? GameManager.Instance.tamerZone_Bottom : GameManager.Instance.tamerZone_Top;
                break;
            case Zone.BreedingActiveSlot:
                newParent = IsOwner ? GameManager.Instance.breedingZone_Bottom : GameManager.Instance.breedingZone_Top;
                break;
        }

        if (newParent != null)
        {
            transform.SetParent(newParent);
            transform.localScale = Vector3.one;
        }

        GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
    }

    public void NotifyZoneChange(Card.Zone newZone)
    {
        if (IsOwner || IsServer)
        {
            currentZone.Value = newZone;
        }
        else
        {
            NotifyZoneChangeServerRpc((int)newZone);
        }
    }

    [ServerRpc]
    private void NotifyZoneChangeServerRpc(int zoneValue)
    {
        currentZone.Value = (Zone)zoneValue;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            if (transform.childCount > 1)
            {
                return;
            }

            if (currentZone == Zone.BattleArea && ownerId == GameManager.Instance.GetActivePlayer())
            {
                if (canAttack)
                {
                    foreach (var card in FindObjectsOfType<Card>())
                    {
                        if (card != this)
                            card.actionPanel?.SetActive(false);
                    }

                    bool shouldShow = !actionPanel.activeSelf;
                    actionPanel.SetActive(shouldShow);
                }
                else
                {
                    Debug.Log("This Digimon cannot attack right now.");
                }
            }

            if (currentZone == Zone.TamerArea && ownerId == GameManager.Instance.GetActivePlayer() && !mainEffectUsed)
            {
                foreach (var card in FindObjectsOfType<Card>())
                {
                    if (card != this)
                    {
                        card.actionPanel.SetActive(false);
                    }
                }

                bool shouldShow = !actionPanel.activeSelf;
                actionPanel.SetActive(shouldShow);

                attackButton.text = "Activate Effect";
            }
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            ZoomIn();
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            ZoomOut();
        }
    }

    private void ZoomIn()
    {
        if (isZoomed) return;

        isZoomed = true;
        originalPosition = transform.position;
        originalScale = transform.localScale;
        originalParent = transform.parent;
        originalSize = rectTransform.sizeDelta;

        topCanvas = GameObject.Find("Canvas").GetComponent<Canvas>();
        transform.SetParent(topCanvas.transform, true);
        transform.SetAsLastSibling();

        Vector2 centerScreen = new Vector2(Screen.width / 2f, Screen.height / 2f);
        transform.position = centerScreen;

        // Set fixed zoom size
        rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 450);
        rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 660);
    }

    private void ZoomOut()
    {
        if (!isZoomed) return;

        isZoomed = false;
        transform.SetParent(originalParent, true);
        transform.localScale = originalScale;
        transform.position = originalPosition;

        rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, originalSize.x);
        rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, originalSize.y);
    }

    public void AttackSecurity()
    {
        if (currentZone == Zone.TamerArea)
        {
            if (mainEffectUsed)
            {
                Debug.Log("Main Phase effect already used.");
                return;
            }

            Debug.Log("Activating Main Phase effect for Tamer.");
            actionPanel.SetActive(false);

            EffectManager.Instance.TriggerEffects(EffectTrigger.MainPhase, this);
            mainEffectUsed = true;

            return;
        }

        if (!canAttack || ownerId != GameManager.Instance.GetActivePlayer())
        {
            Debug.Log("Cannot Attack Security");
            return;
        }

        canAttack = false;
        actionPanel.SetActive(false);

        BattleLogManager.Instance.AddLog(
                            $"[Security Attack] {cardName} is attacking!",
                            BattleLogManager.LogType.Attack,
                            ownerId);

        EffectManager.Instance.TriggerEffects(EffectTrigger.WhenAttacking, this);

        StartCoroutine(GameManager.Instance.ResolveSecurityAttack(this, securityAttackCount, dpBuff));

        securityAttackCount = 1;
        dpBuff = 0;
        GetComponent<Image>().color = new Color32(0x7E, 0x7E, 0x7E, 0xFF);
    }

    public void HideActionPanel()
    {
        actionPanel?.SetActive(false);
    }

    public bool CanDigivolveFrom(Card baseCard)
    {
        if (baseCard.level == null || this.level == null)
        {
            return false;
        }

        if (this.cardType != "Digimon")
        {
            return false;
        }

        if (baseCard.cardType == "Tamer" || baseCard.cardType == "Option")
        {
            return false;
        }

        if (this.level != baseCard.level + 1)
        {
            return false;
        }

        if (this.digivolveCost == null)
        {
            return false;
        }

        return this.digivolveCost.Any(entry => entry.color == baseCard.color);
    }

    public int GetDigivolveCost(Card baseCard)
    {
        var costEntry = this.digivolveCost.FirstOrDefault(entry => entry.color == baseCard.color);
        return costEntry != null ? costEntry.cost : -1;
    }

    public void InitializeFlagsFromEffects()
    {
        foreach (var effect in effects)
        {
            if (effect.type == EffectType.Blocker)
            {
                isBlocker = true;
            }

            if (isBlocker && blockerButton != null)
            {
                blockerButton.gameObject.SetActive(true);
            }
        }
    }

    public void ActivateBlockerMode()
    {
        if (GameManager.Instance.GetActivePlayer() != ownerId)
        {
            return;
        }

        foreach (var card in FindObjectsOfType<Card>())
        {
            if (card != this &&
                card.ownerId == ownerId &&
                card.isBlocker &&
                card.currentZone == Zone.BattleArea)
            {
                card.isBlocking = false;
            }
        }

        isBlocking = true;
        Debug.Log($"{cardName} Blocker Mode set to: {isBlocking}");
    }

    public void ResetStats()
    {
        dpBuff = 0;
        securityAttackCount = 1;
    }
}
