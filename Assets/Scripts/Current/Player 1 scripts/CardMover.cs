using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CardGame.Core;
using CardGame.UI;
using CardGame.Managers;

public class CardMover : MonoBehaviour
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
            Debug.LogWarning($"[CardMover] No Collider2D found on '{gameObject.name}'. OnMouseDown() will not work. Add a Collider2D (BoxCollider2D recommended) for drag functionality.");
        }
        else
        {
            Debug.Log($"[CardMover] Found Collider2D '{col.name}' on '{gameObject.name}'. Drag functionality should work.");
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
        CardGame.Managers.NewDeckManager deckManager = FindObjectOfType<CardGame.Managers.NewDeckManager>();
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
        
        // If still not found, only log in Editor (not during play) and only for scene instances (not prefabs)
        #if UNITY_EDITOR
        if (card == null && !gameObject.name.Contains("Prefab") && !Application.isPlaying)
        {
            // Only warn in Editor when not playing - during play, cards may be initialized later
            // Suppress warning for prefabs and during runtime initialization
            Debug.LogWarning($"CardMover on {gameObject.name}: Could not find NewCard reference. Card will not be playable until reference is set. You can assign it manually in Inspector or ensure NewCardUI component exists.");
        }
        #endif
    }
    
    private bool CanInteract => FateFlowController.Instance != null && FateFlowController.Instance.CanAct(ownerSide);
    
    private void OnMouseDown()
    {
        // [CardFront] Diagnostic logging
        Debug.Log($"[CardMover] OnMouseDown CALLED for '{gameObject.name}'. isPlayed: {isPlayed}, CanInteract: {CanInteract}, collider: {(col != null ? col.name : "null")}");
        
        // Don't allow dragging if card has been played or it's not the player's turn
        if (isPlayed)
        {
            Debug.LogWarning($"[CardMover] Cannot drag '{gameObject.name}' - card has been played.");
            return;
        }
        
        if (!CanInteract)
        {
            Debug.LogWarning($"[CardMover] Cannot drag '{gameObject.name}' - not player's turn. CurrentFate: {(FateFlowController.Instance != null ? FateFlowController.Instance.CurrentFate.ToString() : "null")}");
            return;
        }
        
        // [CardFront] Check if collider exists - OnMouseDown requires Collider2D
        if (col == null)
        {
            Debug.LogError($"[CardMover] OnMouseDown called but no Collider2D found on '{gameObject.name}'. OnMouseDown requires a Collider2D component!");
            return;
        }
        
        Debug.Log($"[CardMover] Starting drag for '{gameObject.name}'");
        isDragging = true;
        hasMovedDuringDrag = false;
        startDragPosition = transform.position;
        pointerStartPosition = GetMousePositionInWorldSpace();
        transform.position = GetMousePositionInWorldSpace();
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
            Debug.Log($"[CardMover] AttemptDrop: Cannot interact for '{gameObject.name}' - bypassTurnCheck: {bypassTurnCheck}");
            return false;
        }

        EnsureCardReference();

        // [CardFront] Diagnostic: Log the drop attempt position
        Debug.Log($"[CardMover] AttemptDrop: Checking drop at position {transform.position} for '{gameObject.name}'");

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
                Debug.Log($"[CardMover] AttemptDrop: Found drop area '{hitCollider.name}' at distance {distance:F2} using large radius search");
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
            Debug.Log($"[CardMover] AttemptDrop: Found collider '{hitCollider.name}' at position {hitCollider.transform.position} (NOT self)");
            
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
                Debug.Log($"[CardMover] AttemptDrop: Successfully found ICardDropArea on '{hitCollider.name}'. Calling OnCardDrop...");
                cardDropArea.OnCardDrop(this);
                hasMovedDuringDrag = false;
                isDragging = false;
                startDragPosition = transform.position;
                return true;
            }
            else
            {
                Debug.LogWarning($"[CardMover] AttemptDrop: Found collider '{hitCollider.name}' but it does NOT have ICardDropArea component! Card cannot be dropped here.");
            }
        }
        else
        {
            Debug.LogWarning($"[CardMover] AttemptDrop: No collider found at position {transform.position}. CardDropArea1 objects may be missing Collider2D components or in different layer/physics space.");
            
            // [CardFront] Diagnostic: Find all CardDropArea1 objects and log their positions
            CardDropArea1[] allDropAreas = FindObjectsOfType<CardDropArea1>(true);
            if (allDropAreas.Length > 0)
            {
                Debug.Log($"[CardMover] AttemptDrop: Found {allDropAreas.Length} CardDropArea1 object(s) in scene:");
                foreach (CardDropArea1 dropArea in allDropAreas)
                {
                    Collider2D dropCol = dropArea.GetComponent<Collider2D>();
                    float distance = Vector3.Distance(transform.position, dropArea.transform.position);
                    Debug.Log($"[CardMover] AttemptDrop:   - '{dropArea.gameObject.name}' at {dropArea.transform.position}, distance: {distance:F2}, has Collider2D: {dropCol != null}");
                }
            }
            else
            {
                Debug.LogError("[CardMover] AttemptDrop: No CardDropArea1 objects found in scene! Cards cannot be placed on board.");
            }
        }

        return false;
    }

    private void EnsureCardReference()
    {
        if (card == null)
        {
            Debug.Log($"[CardMover] EnsureCardReference: Card is null for '{gameObject.name}'. Attempting to find via FindCardReference()...");
            FindCardReference();
            
            if (card != null)
            {
                Debug.Log($"[CardMover] EnsureCardReference: Successfully found card '{card.Data.cardName}' for '{gameObject.name}'");
            }
            else
            {
                Debug.LogWarning($"[CardMover] EnsureCardReference: Could not find card reference for '{gameObject.name}'. Card may not be placeable.");
            }
        }
        else
        {
            Debug.Log($"[CardMover] EnsureCardReference: Card reference already set to '{card.Data.cardName}' for '{gameObject.name}'");
        }
    }
}
