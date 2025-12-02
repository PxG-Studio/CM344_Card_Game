using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CardGame.Core;
using CardGame.UI;
using CardGame.Managers;

public class CardMoverP1 : MonoBehaviour
{
    private Collider2D col;
    private Vector3 startDragPosition;
    private bool isPlayed = false; // Track if card has been played/dropped on board
    private bool isDragging;
    private bool hasMovedDuringDrag;
    [SerializeField] private float dragThreshold = 0.3f; // Increased from 0.1f for better click sensitivity (prevents accidental drags)
    private Vector3 pointerStartPosition;
    
    [Header("Card Reference")]
    [SerializeField] private NewCard card; // Reference to the NewCard this represents
    [SerializeField] private FateSide ownerSide = FateSide.Player;
    
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
            
            // [CardFront] Diagnostic: Log if collider is missing
            if (col == null)
            {
            }
            
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
        CardGame.Managers.NewDeckManagerP1 deckManager = FindObjectOfType<CardGame.Managers.NewDeckManagerP1>();
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
                        return;
                    }
                }
            }
            
            // If no match found and there's only one card in hand, use it as fallback
            // This helps when card names don't match exactly
            if (card == null && deckManager.Hand.Count == 1)
            {
                card = deckManager.Hand[0];
                return;
            }
        }
        
        // If still not found, only log in Editor (not during play) and only for scene instances (not prefabs)
        #if UNITY_EDITOR
        if (card == null && !gameObject.name.Contains("Prefab") && !Application.isPlaying)
        {
            // Only warn in Editor when not playing - during play, cards may be initialized later
            // Suppress warning for prefabs and during runtime initialization
        }
        #endif
    }
    
    private bool CanInteract => FateFlowController.Instance != null && FateFlowController.Instance.CanAct(ownerSide);
    
    private void OnMouseDown()
    {
        // Don't allow dragging during coin toss (including selection phase)
        if (CoinTossManager.Instance != null && CoinTossManager.Instance.IsInProgress)
        {
            return; // Silently ignore - coin toss is in progress
        }
        
        // Don't allow dragging if card has been played or it's not the player's turn
        if (isPlayed)
        {
            return;
        }
        
        if (!CanInteract)
        {
            return;
        }
        
        // [CardFront] Check if collider exists - OnMouseDown requires Collider2D
        if (col == null)
        {
            return;
        }
        
        isDragging = true;
        hasMovedDuringDrag = false;
        startDragPosition = transform.position;
        pointerStartPosition = GetMousePositionInWorldSpace();
        transform.position = GetMousePositionInWorldSpace();
        
        // Ensure stat text is visible during drag
        NewCardUI cardUI = GetComponent<NewCardUI>();
        if (cardUI == null) cardUI = GetComponentInChildren<NewCardUI>();
        if (cardUI == null) cardUI = GetComponentInParent<NewCardUI>();
        // Note: EnsureStatTextVisible and AreStatsVisuallyVisible methods removed in develop-1 revert
    }

    private void OnMouseDrag()
    {
        // Don't allow dragging if card has been played or it's not the player's turn
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
        
        // Continuously ensure stat text is visible during drag
        NewCardUI cardUI = GetComponent<NewCardUI>();
        if (cardUI == null) cardUI = GetComponentInChildren<NewCardUI>();
        if (cardUI == null) cardUI = GetComponentInParent<NewCardUI>();
        // Note: EnsureStatTextVisible method removed in develop-1 revert
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

        // [CardFront] CRITICAL: Disable ALL colliders on this card (including children) to prevent self-detection
        // Collect all colliders on this GameObject and its children
        Collider2D[] allCardColliders = GetComponentsInChildren<Collider2D>(true);
        bool[] originalEnabledStates = new bool[allCardColliders.Length];
        for (int i = 0; i < allCardColliders.Length; i++)
        {
            originalEnabledStates[i] = allCardColliders[i].enabled;
            allCardColliders[i].enabled = false;
        }
        
        // [CardFront] Use ContactFilter2D to exclude this card's layer or use custom filtering
        ContactFilter2D filter = new ContactFilter2D();
        filter.NoFilter(); // We'll manually filter results instead
        
        // [CardFront] Try multiple detection methods with increasing radius
        Collider2D hitCollider = null;
        List<Collider2D> results = new List<Collider2D>();
        
        // Try point cast first
        Physics2D.OverlapPoint(transform.position, filter, results);
        foreach (Collider2D result in results)
        {
            // Skip if it's part of this card
            if (result.transform == transform || result.transform.IsChildOf(transform) || result.transform == transform.parent)
            {
                continue;
            }
            hitCollider = result;
            break;
        }
        results.Clear();
        
        // [CardFront] Fallback: Try small radius circle cast if point fails
        float searchRadius = 0.1f;
        if (hitCollider == null)
        {
            Physics2D.OverlapCircle(transform.position, searchRadius, filter, results);
            foreach (Collider2D result in results)
            {
                if (result.transform == transform || result.transform.IsChildOf(transform) || result.transform == transform.parent)
                {
                    continue;
                }
                hitCollider = result;
                break;
            }
            results.Clear();
        }
        
        // [CardFront] Fallback: Try larger radius if still not found
        if (hitCollider == null)
        {
            searchRadius = 1.0f;
            Physics2D.OverlapCircle(transform.position, searchRadius, filter, results);
            foreach (Collider2D result in results)
            {
                if (result.transform == transform || result.transform.IsChildOf(transform) || result.transform == transform.parent)
                {
                    continue;
                }
                hitCollider = result;
                break;
            }
            results.Clear();
        }
        
        // [CardFront] Fallback: Try even larger radius (for board tiles that might be spaced apart)
        if (hitCollider == null)
        {
            searchRadius = 2.0f;
            Physics2D.OverlapCircle(transform.position, searchRadius, filter, results);
            foreach (Collider2D result in results)
            {
                if (result.transform == transform || result.transform.IsChildOf(transform) || result.transform == transform.parent)
                {
                    continue;
                }
                hitCollider = result;
                float distance = Vector3.Distance(transform.position, hitCollider.transform.position);
                break;
            }
            results.Clear();
        }
        
        // [CardFront] Re-enable all card colliders
        for (int i = 0; i < allCardColliders.Length; i++)
        {
            allCardColliders[i].enabled = originalEnabledStates[i];
        }
        
        if (hitCollider != null)
        {
            ICardDropArea cardDropArea = hitCollider.GetComponent<ICardDropArea>();
            if (cardDropArea == null)
            {
                cardDropArea = hitCollider.GetComponentInParent<ICardDropArea>();
            }
            if (cardDropArea == null)
            {
                cardDropArea = hitCollider.GetComponentInChildren<ICardDropArea>();
            }
            
            if (cardDropArea != null)
            {
                cardDropArea.OnCardDrop(this);
                hasMovedDuringDrag = false;
                isDragging = false;
                startDragPosition = transform.position;
                return true;
            }
            else
            {
            }
        }
        else
        {
            // [CardFront] Diagnostic: Find all CardDropArea objects
            CardDropArea[] allDropAreas = FindObjectsOfType<CardDropArea>(true);
            if (allDropAreas.Length == 0)
            {
            }
        }

        return false;
    }

    private void EnsureCardReference()
    {
        if (card == null)
        {
            FindCardReference();
            
            if (card == null)
            {
            }
        }
    }
}
