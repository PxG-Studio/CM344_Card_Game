using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CardGame.Core;
using CardGame.UI;

public class CardMoverOpp : MonoBehaviour
{
    private Collider2D col;
    private Vector3 startDragPosition;
    private bool isPlayed = false; // Track if card has been played/dropped on board
    
    [Header("Card Reference")]
    [SerializeField] private NewCard card; // Reference to the NewCard this represents
    
    public NewCard Card => card;
    public bool IsPlayed => isPlayed;
    
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

    void Start()
    {
        col = GetComponent<Collider2D>();
        
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
        if (card == null)
        {
            // Only log once per GameObject and only in development builds
            #if UNITY_EDITOR
            if (!hasLoggedWarning)
            {
                // Suppress warning if this is a prefab instance that will be initialized later
                if (!gameObject.name.Contains("Prefab"))
                {
                    Debug.LogWarning($"CardMover on {gameObject.name}: Could not find NewCard reference. Card will not be playable until reference is set. You can assign it manually in Inspector or ensure NewCardUI component exists.");
                }
                hasLoggedWarning = true;
            }
            #endif
        }
    }
    
    private bool hasLoggedWarning = false;
    private void OnMouseDown()
    {
        // Don't allow dragging if card has been played
        if (isPlayed) return;
        
        startDragPosition = transform.position;
        transform.position = GetMousePositionInWorldSpace();
    }

    private void OnMouseDrag()
    {
        // Don't allow dragging if card has been played
        if (isPlayed) return;
        
        transform.position = GetMousePositionInWorldSpace();
    }
    private void OnMouseUp()
    {
        // Try to find card reference again in case it wasn't set at Start
        if (card == null)
        {
            FindCardReference();
        }
        
        col.enabled = false;
        Collider2D hitCollider = Physics2D.OverlapPoint(transform.position);
        col.enabled = true;
         if (hitCollider != null && hitCollider.TryGetComponent(out ICardDropArea cardDropArea))
         {
            cardDropArea.OnCardDropOpp(this);
         }
         else
         {
            transform.position = startDragPosition;
         }
    }

    public Vector3 GetMousePositionInWorldSpace()
    {
        Vector3 p = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        p.z = 0f;
        return p;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
