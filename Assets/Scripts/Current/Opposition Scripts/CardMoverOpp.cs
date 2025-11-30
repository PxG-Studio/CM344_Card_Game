using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CardGame.Core;
using CardGame.UI;
using CardGame.Managers;

public class CardMoverP2 : MonoBehaviour
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
    [SerializeField] private FateSide ownerSide = FateSide.P2;
    
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

    void Awake()
    {
        // Force-enable collider early for tests
        EnsureCollider();
        if (col != null)
        {
            col.enabled = true; // Force-enable for tests
        }
    }

    void Start()
    {
        EnsureCollider();
        startDragPosition = transform.position;
        
        // Try to find card reference automatically if not set
        if (card == null)
        {
            FindCardReference();
        }
    }

    /// <summary>
    /// Ensures the collider is set up. Can be called manually if Start() hasn't been called yet.
    /// Made public so tests can call it directly.
    /// </summary>
    public void EnsureCollider()
    {
        // Always check and set up collider (col field might be null even if component exists)
        // First check the field
        if (col == null)
        {
            col = GetComponent<Collider2D>();
        }
        
        // If still null, check children (card might have collider on child GameObject)
        if (col == null)
        {
            col = GetComponentInChildren<Collider2D>(true); // Include inactive children
        }
        
        // If still null, check parent
        if (col == null)
        {
            col = GetComponentInParent<Collider2D>();
        }
        
        // If still null, add a collider (for test scenarios)
        if (col == null)
        {
            // Try to find a sprite renderer or rect transform to size the collider appropriately
            SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponentInChildren<SpriteRenderer>(true);
            }
            
            RectTransform rectTransform = GetComponent<RectTransform>();
            if (rectTransform == null)
            {
                rectTransform = GetComponentInChildren<RectTransform>(true);
            }
            
            BoxCollider2D newCollider = gameObject.AddComponent<BoxCollider2D>();
            col = newCollider;
            
            if (newCollider != null)
            {
                newCollider.isTrigger = true;
                newCollider.enabled = true;
                
                // Auto-size based on sprite or rect transform if available
                if (spriteRenderer != null && spriteRenderer.sprite != null)
                {
                    newCollider.size = spriteRenderer.bounds.size;
                }
                else if (rectTransform != null)
                {
                    Vector2 size = rectTransform.sizeDelta;
                    newCollider.size = size;
                }
                
            }
        }
        
        // Verify collider is properly configured and enabled
        if (col != null)
        {
            if (!col.isTrigger)
            {
                col.isTrigger = true;
            }
            if (!col.enabled)
            {
                col.enabled = true;
            }
            
            // Ensure the GameObject the collider is on is active
            if (col.gameObject != null && !col.gameObject.activeInHierarchy)
            {
                col.gameObject.SetActive(true);
            }
        }
        else
        {
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
        CardGame.Managers.NewDeckManagerP2 deckManager = FindObjectOfType<CardGame.Managers.NewDeckManagerP2>();
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
        
        // If still not found, only log in Editor (not during play)
        #if UNITY_EDITOR
        if (card == null && !gameObject.name.Contains("Prefab"))
        {
            // Suppress warning if this is a prefab instance that will be initialized later
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
        
        // Don't allow dragging if card has been played or it's not the opponent's turn
        if (isPlayed)
        {
            return;
        }
        
        if (!CanInteract)
        {
            return;
        }
        
        // [CardFront] Check if collider exists - OnMouseDown requires Collider2D
        EnsureCollider();
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
        if (cardUI != null)
        {
            cardUI.EnsureStatTextVisible();
            string cardName = card?.Data?.cardName ?? gameObject.name;
            bool statsVisible = cardUI.AreStatsVisuallyVisible();
            if (!statsVisible)
            {
            }
        }
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
        
        // Continuously ensure stat text is visible during drag
        NewCardUI cardUI = GetComponent<NewCardUI>();
        if (cardUI == null) cardUI = GetComponentInChildren<NewCardUI>();
        if (cardUI == null) cardUI = GetComponentInParent<NewCardUI>();
        if (cardUI != null)
        {
            cardUI.EnsureStatTextVisible();
        }
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

    /// <summary>
    /// Gets the camera to use for Player 2 card interactions.
    /// Tries to find a Player 2 specific camera, falls back to Camera.main.
    /// </summary>
    private Camera GetPlayer2Camera()
    {
        // Try to find Player 2 specific camera first
        Camera[] allCameras = FindObjectsOfType<Camera>();
        foreach (Camera cam in allCameras)
        {
            if (cam.name.Contains("Player2") || cam.name.Contains("Opponent") || cam.name.Contains("P2"))
            {
                if (cam.enabled)
                {
                    return cam;
                }
            }
        }
        
        // Fall back to Camera.main
        if (Camera.main != null && Camera.main.enabled)
        {
            return Camera.main;
        }
        
        // Last resort: find any enabled camera
        foreach (Camera cam in allCameras)
        {
            if (cam.enabled)
            {
                return cam;
            }
        }
        
        return null;
    }

    public Vector3 GetMousePositionInWorldSpace()
    {
        Camera camera = GetPlayer2Camera();
        if (camera == null)
        {
            return Vector3.zero;
        }
        
        Vector3 p = camera.ScreenToWorldPoint(Input.mousePosition);
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

        // Ensure collider is set up before attempting drop
        EnsureCollider();

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
        EnsureCollider(); // Ensure collider is set up before using it

        // [CardFront] Enhanced drop detection with better logging and layer mask support
        if (col == null)
        {
            return false;
        }

        // Temporarily disable our collider to avoid self-collision
        bool wasEnabled = col.enabled;
        col.enabled = false;
        
        // Use a small radius to catch nearby drop areas (more forgiving than exact point)
        float checkRadius = 0.5f;
        Collider2D hitCollider = Physics2D.OverlapCircle(transform.position, checkRadius);
        
        // Also try exact point check as fallback
        if (hitCollider == null)
        {
            hitCollider = Physics2D.OverlapPoint(transform.position);
        }
        
        // Restore collider state
        col.enabled = wasEnabled;
        
        if (hitCollider != null)
        {
            // Try to get ICardDropArea component
            ICardDropArea cardDropArea = hitCollider.GetComponent<ICardDropArea>();
            if (cardDropArea == null)
            {
                // Try parent
                cardDropArea = hitCollider.GetComponentInParent<ICardDropArea>();
            }
            
            if (cardDropArea != null)
            {
                cardDropArea.OnCardDropP2(this);
                hasMovedDuringDrag = false;
                isDragging = false;
                startDragPosition = transform.position;
                return true;
            }
            else
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
        }
    }
}
