using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using CardGame.Managers;
using CardGame.Core;
using CardGame.UI;

public class CardDropArea1 : MonoBehaviour, ICardDropArea
{
    [Header("Deck Manager Reference")]
    [SerializeField] private NewDeckManager deckManager;
    [SerializeField] private NewDeckManagerOpp deckManagerOpp;

    [Header("Settings")]
    [SerializeField] private bool playCardOnDrop = true;
    [SerializeField] private bool snapCardToPosition = true;
    [SerializeField] private float adjacentCardDistance = 3f; // Distance to consider cards adjacent (increased from 2f)
    [SerializeField] private bool enableCardBattles = true; // Enable stat comparison and card flipping
    [SerializeField] private bool debugBattles = true; // Log battle detection for debugging
    
    private void Start()
    {
        // Auto-find NewDeckManager if not assigned
        if (deckManager == null)
        {
            deckManager = FindObjectOfType<NewDeckManager>();
            if (deckManager == null)
            {
                Debug.LogWarning("CardDropArea1: NewDeckManager not found! Card play functionality will not work.");
            }
        }
    }
    
    public void OnCardDrop(CardMover cardMover)
    {
        // Snap card to slot position
        if (snapCardToPosition)
        {
            cardMover.transform.position = transform.position;
        }
        
        // Play the card through DeckManager
        if (playCardOnDrop && deckManager != null)
        {
            NewCard card = cardMover.Card;
            
            // Try to find card reference if not set
            if (card == null)
            {
                // Try to refresh the card reference
                cardMover.SendMessage("FindCardReference", SendMessageOptions.DontRequireReceiver);
                card = cardMover.Card;
            }
            
            if (card != null)
            {
                // Check if card is in hand before playing
                if (deckManager.Hand.Contains(card))
                {
                    // Play the card - this will trigger OnCardPlayed event
                    // But we want to keep the card on the board, so we'll handle it differently
                    deckManager.PlayCard(card);
                    
                    // Note: The card GameObject will stay on the board even though it's removed from hand
                    // NewHandUI will try to destroy it, but if it's a CardMover (not NewCardUI), it won't find it
                    Debug.Log($"Card {card.Data.cardName} played from drop area and placed on board");
                    
                    // Mark card as played - prevents further dragging
                    cardMover.SetPlayed(true);
                    
                    // Check for card battles with adjacent cards
                    if (enableCardBattles)
                    {
                        CheckCardBattles(cardMover, card);
                    }
                }
                else
                {
                    Debug.LogWarning($"Card {card.Data.cardName} is not in hand, cannot play");
                }
            }
            else
            {
                Debug.LogWarning("CardMover does not have a NewCard reference! Trying to find it...");
                // Last attempt: try to get card from deck manager by matching
                // This is a fallback - ideally the card should be set when the GameObject is created
            }
        }
        else if (playCardOnDrop && deckManager == null)
        {
            Debug.LogWarning("CardDropArea1: Cannot play card - NewDeckManager not found!");
        }
        
        Debug.Log("Card dropped here");
    }
    
    /// <summary>
    /// Checks for adjacent cards and performs stat comparisons, flipping losing cards
    /// </summary>
    private void CheckCardBattles(CardMover placedCardMover, NewCard placedCard)
    {
        if (placedCardMover == null || placedCard == null) return;
        
        if (debugBattles)
        {
            Debug.Log($"CheckCardBattles: Checking battles for {placedCard.Data.cardName} at position {placedCardMover.transform.position}");
        }
        
        Vector3 placedPosition = placedCardMover.transform.position;
        bool placedCardShouldFlip = false;
        GameObject winningCardObject = null; // Track which card beat the placed card
        
        // Find all CardMover and CardMoverOpp components on the board
        CardMover[] allCardMovers = FindObjectsOfType<CardMover>();
        CardMoverOpp[] allCardMoverOpps = FindObjectsOfType<CardMoverOpp>();
        
        if (debugBattles)
        {
            Debug.Log($"CheckCardBattles: Found {allCardMovers.Length} CardMovers and {allCardMoverOpps.Length} CardMoverOpps on board");
        }
        
        // Check against regular CardMovers
        foreach (CardMover otherCardMover in allCardMovers)
        {
            // Skip self
            if (otherCardMover == placedCardMover) continue;
            if (otherCardMover.Card == null) continue;
            
            bool shouldFlip = CheckBattleBetweenCards(placedPosition, placedCard, otherCardMover.transform.position, otherCardMover.Card, otherCardMover.gameObject, placedCardMover.gameObject);
            if (shouldFlip)
            {
                placedCardShouldFlip = true;
                winningCardObject = otherCardMover.gameObject; // Track the winning card
            }
        }
        
        // Check against opponent CardMovers
        foreach (CardMoverOpp otherCardMoverOpp in allCardMoverOpps)
        {
            if (otherCardMoverOpp.Card == null) continue;
            bool shouldFlip = CheckBattleBetweenCards(placedPosition, placedCard, otherCardMoverOpp.transform.position, otherCardMoverOpp.Card, otherCardMoverOpp.gameObject, placedCardMover.gameObject);
            if (shouldFlip)
            {
                placedCardShouldFlip = true;
                winningCardObject = otherCardMoverOpp.gameObject; // Track the winning card
            }
        }
        
        // Flip placed card if it lost
        if (placedCardShouldFlip && winningCardObject != null)
        {
            // Determine capture color: The captured card gets the color of who captured it
            // Use the winning card's color (the card that beat the placed card)
            bool winningCardIsPlayer = IsPlayerCard(winningCardObject);
            Color captureColor = winningCardIsPlayer ? 
                GetPlayerCaptureColor() : GetOpponentCaptureColor();
            FlipCard(placedCardMover, placedCard, captureColor);
        }
    }
    
    /// <summary>
    /// Determines if a card GameObject belongs to the player (vs opponent)
    /// </summary>
    private bool IsPlayerCard(GameObject cardObject)
    {
        if (cardObject == null) return true; // Default to player
        
        // Check if it has CardMover (player) or CardMoverOpp (opponent)
        CardMover cardMover = cardObject.GetComponent<CardMover>();
        if (cardMover != null) return true;
        
        CardMoverOpp cardMoverOpp = cardObject.GetComponent<CardMoverOpp>();
        if (cardMoverOpp != null) return false;
        
        // Check children/parents
        cardMover = cardObject.GetComponentInChildren<CardMover>();
        if (cardMover != null) return true;
        
        cardMoverOpp = cardObject.GetComponentInChildren<CardMoverOpp>();
        if (cardMoverOpp != null) return false;
        
        cardMover = cardObject.GetComponentInParent<CardMover>();
        if (cardMover != null) return true;
        
        cardMoverOpp = cardObject.GetComponentInParent<CardMoverOpp>();
        if (cardMoverOpp != null) return false;
        
        // Default: assume player card
        return true;
    }
    
    /// <summary>
    /// Gets the player's capture color (orange)
    /// </summary>
    private Color GetPlayerCaptureColor()
    {
        // Orange color for player's captured cards (matches card border orange)
        return new Color(1f, 0.5f, 0f, 1f);
    }
    
    /// <summary>
    /// Gets the opponent's capture color (green)
    /// </summary>
    private Color GetOpponentCaptureColor()
    {
        // Green color for opponent's captured cards
        return new Color(0f, 0.8f, 0f, 1f);
    }
    
    /// <summary>
    /// Flips a card (flips it to show the back) with a specific capture color
    /// </summary>
    private void FlipCard(CardMover cardMover, NewCard card, Color captureColor)
    {
        if (cardMover == null)
        {
            Debug.LogWarning("FlipCard: cardMover is null!");
            return;
        }

        // Use the same helper method for consistency
        FlipCardGameObject(cardMover.gameObject, card, captureColor);
    }

    public void OnCardDropOpp(CardMoverOpp cardMoverOpp)
    {
        // Snap card to slot position
        if (snapCardToPosition)
        {
            cardMoverOpp.transform.position = transform.position;
        }
        
        // Play the card through DeckManager
        if (playCardOnDrop && deckManagerOpp != null)
        {
            NewCard card = cardMoverOpp.Card;
            
            // Try to find card reference if not set
            if (card == null)
            {
                // Try to refresh the card reference
                cardMoverOpp.SendMessage("FindCardReference", SendMessageOptions.DontRequireReceiver);
                card = cardMoverOpp.Card;
            }
            
            if (card != null)
            {
                // Check if card is in hand before playing
                if (deckManagerOpp.Hand.Contains(card))
                {
                    // Play the card - this will trigger OnCardPlayed event
                    // But we want to keep the card on the board, so we'll handle it differently
                    deckManagerOpp.PlayCard(card);
                    
                    // Note: The card GameObject will stay on the board even though it's removed from hand
                    // NewHandUI will try to destroy it, but if it's a CardMover (not NewCardUI), it won't find it
                    Debug.Log($"Card {card.Data.cardName} played from drop area and placed on board");
                    
                    // Mark card as played - prevents further dragging
                    cardMoverOpp.SetPlayed(true);
                    
                    // Check for card battles with adjacent cards
                    if (enableCardBattles)
                    {
                        CheckCardBattlesOpp(cardMoverOpp, card);
                    }
                }
                else
                {
                    Debug.LogWarning($"Card {card.Data.cardName} is not in hand, cannot play");
                }
            }
            else
            {
                Debug.LogWarning("CardMover does not have a NewCard reference! Trying to find it...");
                // Last attempt: try to get card from deck manager by matching
                // This is a fallback - ideally the card should be set when the GameObject is created
            }
        }
        else if (playCardOnDrop && deckManagerOpp == null)
        {
            Debug.LogWarning("CardDropArea1: Cannot play card - NewDeckManager not found!");
        }
        
        Debug.Log("Card dropped here");
    }
    
    /// <summary>
    /// Checks for adjacent cards and performs stat comparisons for opponent cards
    /// </summary>
    private void CheckCardBattlesOpp(CardMoverOpp placedCardMover, NewCard placedCard)
    {
        if (placedCardMover == null || placedCard == null) return;
        
        Vector3 placedPosition = placedCardMover.transform.position;
        bool placedCardShouldFlip = false;
        GameObject winningCardObject = null; // Track which card beat the placed card
        
        // Find all CardMover and CardMoverOpp components on the board
        CardMover[] allCardMovers = FindObjectsOfType<CardMover>();
        CardMoverOpp[] allCardMoverOpps = FindObjectsOfType<CardMoverOpp>();
        
        // Check against regular CardMovers
        foreach (CardMover otherCardMover in allCardMovers)
        {
            if (otherCardMover.Card == null) continue;
            bool shouldFlip = CheckBattleBetweenCards(placedPosition, placedCard, otherCardMover.transform.position, otherCardMover.Card, otherCardMover.gameObject, placedCardMover.gameObject);
            if (shouldFlip)
            {
                placedCardShouldFlip = true;
                winningCardObject = otherCardMover.gameObject; // Track the winning card
            }
        }
        
        // Check against opponent CardMovers
        foreach (CardMoverOpp otherCardMoverOpp in allCardMoverOpps)
        {
            if (otherCardMoverOpp == placedCardMover) continue;
            if (otherCardMoverOpp.Card == null) continue;
            bool shouldFlip = CheckBattleBetweenCards(placedPosition, placedCard, otherCardMoverOpp.transform.position, otherCardMoverOpp.Card, otherCardMoverOpp.gameObject, placedCardMover.gameObject);
            if (shouldFlip)
            {
                placedCardShouldFlip = true;
                winningCardObject = otherCardMoverOpp.gameObject; // Track the winning card
            }
        }
        
        // Flip placed card if it lost
        if (placedCardShouldFlip && winningCardObject != null)
        {
            // Determine capture color: The captured card gets the color of who captured it
            // Use the winning card's color (the card that beat the placed card)
            bool winningCardIsPlayer = IsPlayerCard(winningCardObject);
            Color captureColor = winningCardIsPlayer ? 
                GetPlayerCaptureColor() : GetOpponentCaptureColor();
            FlipCardGameObject(placedCardMover.gameObject, placedCard, captureColor);
        }
    }
    
    /// <summary>
    /// Helper method to check battle between two cards
    /// Returns true if placed card should be flipped (lost)
    /// </summary>
    private bool CheckBattleBetweenCards(Vector3 placedPos, NewCard placedCard, Vector3 otherPos, NewCard otherCard, GameObject otherCardObject, GameObject placedCardObject)
    {
        float distance = Vector3.Distance(placedPos, otherPos);
        if (debugBattles)
        {
            Debug.Log($"CheckBattleBetweenCards: {placedCard.Data.cardName} vs {otherCard.Data.cardName} - Distance: {distance}, Threshold: {adjacentCardDistance}");
        }
        // Use small epsilon to account for floating point precision
        // Also allow cards slightly over threshold (within 0.1 units) to account for positioning
        float epsilon = 0.1f;
        if (distance > adjacentCardDistance + epsilon) 
        {
            if (debugBattles)
            {
                Debug.Log($"  → Distance {distance} exceeds threshold {adjacentCardDistance} (with {epsilon} tolerance), skipping battle");
            }
            return false;
        }
        
        if (debugBattles && distance > adjacentCardDistance)
        {
            Debug.Log($"  → Distance {distance} slightly over threshold {adjacentCardDistance}, but within tolerance - proceeding with battle");
        }
        
        Vector3 direction = (otherPos - placedPos).normalized;
        int placedCardStat = 0;
        int otherCardStat = 0;
        string directionName = "";
        
        float dotRight = Vector3.Dot(direction, Vector3.right);
        float dotUp = Vector3.Dot(direction, Vector3.up);
        float absRight = Mathf.Abs(dotRight);
        float absUp = Mathf.Abs(dotUp);
        
        if (absRight > absUp)
        {
            if (dotRight > 0)
            {
                placedCardStat = placedCard.CurrentRightStat;
                otherCardStat = otherCard.CurrentLeftStat;
                directionName = "right";
            }
            else
            {
                placedCardStat = placedCard.CurrentLeftStat;
                otherCardStat = otherCard.CurrentRightStat;
                directionName = "left";
            }
        }
        else
        {
            if (dotUp > 0)
            {
                placedCardStat = placedCard.CurrentTopStat;
                otherCardStat = otherCard.CurrentDownStat;
                directionName = "top";
            }
            else
            {
                placedCardStat = placedCard.CurrentDownStat;
                otherCardStat = otherCard.CurrentTopStat;
                directionName = "down";
            }
        }
        
        if (debugBattles)
        {
            Debug.Log($"  → Stat comparison: {placedCard.Data.cardName} {directionName} stat = {placedCardStat}, {otherCard.Data.cardName} opposing stat = {otherCardStat}");
        }
        
        if (placedCardStat > otherCardStat)
        {
            // Determine capture color: The captured card gets the color of who captured it
            // If player (orange) captures → show orange
            // If opponent (green) captures → show green
            bool placedCardIsPlayer = IsPlayerCard(placedCardObject);
            
            // Use the capturer's color (the placed card that won)
            Color captureColor = placedCardIsPlayer ? 
                GetPlayerCaptureColor() : GetOpponentCaptureColor();
            
            // Convert direction name to FlipDirection enum
            CardGame.UI.FlipDirection flipDir = CardGame.UI.FlipDirection.Right; // Default
            switch (directionName.ToLower())
            {
                case "left":
                    flipDir = CardGame.UI.FlipDirection.Left;
                    break;
                case "right":
                    flipDir = CardGame.UI.FlipDirection.Right;
                    break;
                case "top":
                    flipDir = CardGame.UI.FlipDirection.Top;
                    break;
                case "down":
                    flipDir = CardGame.UI.FlipDirection.Down;
                    break;
            }
            
            FlipCardGameObject(otherCardObject, otherCard, captureColor, flipDir);
            Debug.Log($"✅ Card Battle: {placedCard.Data.cardName} ({placedCardStat}) > {otherCard.Data.cardName} ({otherCardStat}) in {directionName} direction. {otherCard.Data.cardName} captured with {captureColor}!");
            return false; // Placed card won, don't flip it
        }
        else if (otherCardStat > placedCardStat)
        {
            Debug.Log($"Card Battle: {otherCard.Data.cardName} ({otherCardStat}) > {placedCard.Data.cardName} ({placedCardStat}) in {directionName} direction. {placedCard.Data.cardName} should flip!");
            return true; // Placed card lost, should flip
        }
        
        if (debugBattles)
        {
            Debug.Log($"  → Tie! Both stats are equal ({placedCardStat} = {otherCardStat}), no capture");
        }
        return false; // Tie, no flip
    }
    
    /// <summary>
    /// Flips a card GameObject (helper for opponent cards) with a specific capture color
    /// </summary>
    private void FlipCardGameObject(GameObject cardObject, NewCard card, Color captureColor)
    {
        FlipCardGameObject(cardObject, card, captureColor, CardGame.UI.FlipDirection.Right); // Default direction
    }

    /// <summary>
    /// Flips a card GameObject with directional flip animation
    /// </summary>
    private void FlipCardGameObject(GameObject cardObject, NewCard card, Color captureColor, CardGame.UI.FlipDirection direction)
    {
        if (cardObject == null)
        {
            Debug.LogWarning("FlipCardGameObject: cardObject is null!");
            return;
        }

        NewCardUI cardUI = cardObject.GetComponent<NewCardUI>();
        if (cardUI == null)
        {
            cardUI = cardObject.GetComponentInChildren<NewCardUI>();
        }
        if (cardUI == null)
        {
            cardUI = cardObject.GetComponentInParent<NewCardUI>();
        }

        if (cardUI == null)
        {
            Debug.LogWarning($"FlipCardGameObject: Could not find NewCardUI on {cardObject.name} or its children/parents!");
            return;
        }

        CardGame.UI.CardFlipAnimation flipAnim = cardUI.GetComponent<CardGame.UI.CardFlipAnimation>();
        if (flipAnim == null)
        {
            flipAnim = cardObject.GetComponent<CardGame.UI.CardFlipAnimation>();
        }
        if (flipAnim == null)
        {
            flipAnim = cardObject.GetComponentInChildren<CardGame.UI.CardFlipAnimation>();
        }
        if (flipAnim == null)
        {
            flipAnim = cardObject.GetComponentInParent<CardGame.UI.CardFlipAnimation>();
        }

        if (flipAnim == null)
        {
            Debug.LogWarning($"FlipCardGameObject: Could not find CardFlipAnimation on {cardObject.name} or {cardUI.name}! Card cannot flip.");
            return;
        }

        if (!flipAnim.IsSetupValid())
        {
            Debug.LogWarning($"FlipCardGameObject: CardFlipAnimation on {cardObject.name} is not set up correctly! Containers missing. Card cannot flip.");
            return;
        }

        flipAnim.CaptureCard(captureColor, direction);
        Debug.Log($"✅ Captured card {card.Data.cardName} with border color {captureColor} (flip direction: {direction})");
    }
}
