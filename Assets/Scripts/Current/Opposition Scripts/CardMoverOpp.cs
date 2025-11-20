using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CardGame.Core;
using CardGame.UI;
using CardGame.Managers;

public class CardMoverOpp : MonoBehaviour
{
    private Collider2D col;
    private Vector3 startDragPosition;
    private bool isPlayed = false; // Track if card has been played/dropped on board
    private bool isDragging;
    private bool hasMovedDuringDrag;
    [SerializeField] private float dragThreshold = 0.1f;
    private Vector3 pointerStartPosition;
    
    [Header("Card Reference")]
    [SerializeField] private NewCard card; // Reference to the NewCard this represents
    [SerializeField] private FateSide ownerSide = FateSide.Opponent;
    
    public NewCard Card => card;
    public bool IsPlayed => isPlayed;
    public FateSide OwnerSide => ownerSide;
    
    /// <summary>
    /// Mark this card as played - prevents further dragging
    /// </summary>
    public void SetPlayed(bool played)
    {
        isPlayed = played;
    }
    
    // Method to set the card reference
    public void SetCard(NewCard newCard)
    {
        card = newCard;
    }
    
    public void RefreshHomePosition()
    {
        startDragPosition = transform.position;
    }

    void Start()
    {
        col = GetComponent<Collider2D>();
        startDragPosition = transform.position;
        
        // Try to find card reference automatically if not set
        if (card == null)
        {
            FindCardReference();
        }
    }
    
    public void FindCardReference()
    {
        // Try to find NewCardUI component on this GameObject
        NewCardUI cardUI = GetComponent<NewCardUI>();
        if (cardUI != null && cardUI.Card != null)
        {
            card = cardUI.Card;
            return;
        }
        
        // Try to find NewCardUI component in children
        cardUI = GetComponentInChildren<NewCardUI>();
        if (cardUI != null && cardUI.Card != null)
        {
            card = cardUI.Card;
            return;
        }
        
        // Try to find NewCardUI component in parent
        cardUI = GetComponentInParent<NewCardUI>();
        if (cardUI != null && cardUI.Card != null)
        {
            card = cardUI.Card;
            return;
        }
        
        // Try to find card by name matching with NewDeckManager
        // This is a fallback for 2D cards that don't have NewCardUI
        CardGame.Managers.NewDeckManagerOpp deckManager = FindObjectOfType<CardGame.Managers.NewDeckManagerOpp>();
        if (deckManager != null && deckManager.Hand != null && deckManager.Hand.Count > 0)
        {
            // Try to match by GameObject name or some identifier
            // This is a workaround - ideally the card should be set when created
            string cardName = gameObject.name;
            string cleanName = cardName.Replace("(Clone)", "").Replace("Prefab", "").Replace("NewCardPrefab", "").Trim();
            
            // First, try exact or partial name matching
            foreach (var handCard in deckManager.Hand)
            {
                if (handCard.Data != null && handCard.Data.cardName != null)
                {
                    // Try to match card name (remove "Prefab" or "Clone" suffixes)
                    if (cleanName.Contains(handCard.Data.cardName) || 
                        handCard.Data.cardName.Contains(cleanName) ||
                        cleanName.Equals(handCard.Data.cardName, System.StringComparison.OrdinalIgnoreCase))
                    {
                        card = handCard;
                        Debug.Log($"CardMover: Found card {handCard.Data.cardName} by name matching");
                        return;
                    }
                }
            }
            
            // If no match found and there's only one card in hand, use it as fallback
            // This helps when card names don't match exactly
            if (card == null && deckManager.Hand.Count == 1)
            {
                card = deckManager.Hand[0];
                Debug.Log($"CardMover: Using only card in hand: {card.Data.cardName}");
                return;
            }
        }
        
        // If still not found, only log in Editor (not during play)
        #if UNITY_EDITOR
        if (card == null && !gameObject.name.Contains("Prefab"))
        {
            // Suppress warning if this is a prefab instance that will be initialized later
            Debug.LogWarning($"CardMoverOpp on {gameObject.name}: Could not find NewCard reference. Card will not be playable until reference is set. You can assign it manually in Inspector or ensure NewCardUI component exists.");
        }
        #endif
    }
    
    private bool CanInteract => FateFlowController.Instance != null && FateFlowController.Instance.CanAct(ownerSide);
    
    private void OnMouseDown()
    {
        // Don't allow dragging if card has been played or it's not the opponent's turn
        if (isPlayed || !CanInteract) return;
        
        isDragging = true;
        hasMovedDuringDrag = false;
        startDragPosition = transform.position;
        pointerStartPosition = GetMousePositionInWorldSpace();
        transform.position = GetMousePositionInWorldSpace();
    }

    private void OnMouseDrag()
    {
        // Don't allow dragging if card has been played or it's not the opponent's turn
        if (isPlayed || !CanInteract || !isDragging) return;
        
        Vector3 currentPointer = GetMousePositionInWorldSpace();
        if (!hasMovedDuringDrag)
        {
            float distance = Vector3.Distance(pointerStartPosition, currentPointer);
            if (distance >= dragThreshold)
            {
                hasMovedDuringDrag = true;
            }
        }
        transform.position = currentPointer;
    }
    private void OnMouseUp()
    {
        if (!isDragging)
        {
            return;
        }

        isDragging = false;

        if (!hasMovedDuringDrag)
        {
            ReturnToStartPosition();
            return;
        }

        if (!CanInteract)
        {
            ReturnToStartPosition();
            return;
        }
        if (!AttemptDrop(bypassTurnCheck: false))
        {
            ReturnToStartPosition();
        }
    }

    public Vector3 GetMousePositionInWorldSpace()
    {
        Vector3 p = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        p.z = 0f;
        return p;
    }

    public void ReturnToStartPosition()
    {
        transform.position = startDragPosition;
        hasMovedDuringDrag = false;
    }

    public bool AutomationAttemptDrop(Vector3 worldPosition, bool bypassTurnGate = true)
    {
        if (isPlayed)
        {
            return false;
        }

        if (!bypassTurnGate && !CanInteract)
        {
            return false;
        }

        Vector3 previousPosition = transform.position;
        Vector3 previousStart = startDragPosition;

        transform.position = worldPosition;
        startDragPosition = previousPosition;
        bool result = AttemptDrop(bypassTurnGate);

        if (!result)
        {
            transform.position = previousPosition;
            startDragPosition = previousStart;
        }

        return result;
    }

    private bool AttemptDrop(bool bypassTurnCheck)
    {
        if (!bypassTurnCheck && !CanInteract)
        {
            return false;
        }

        EnsureCardReference();

        col.enabled = false;
        Collider2D hitCollider = Physics2D.OverlapPoint(transform.position);
        col.enabled = true;
        if (hitCollider != null && hitCollider.TryGetComponent(out ICardDropArea cardDropArea))
        {
            cardDropArea.OnCardDropOpp(this);
            hasMovedDuringDrag = false;
            isDragging = false;
            startDragPosition = transform.position;
            return true;
        }

        return false;
    }

    private void EnsureCardReference()
    {
        if (card == null)
        {
            FindCardReference();
        }
    }
}
