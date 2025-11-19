using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using CardGame.Managers;
using CardGame.Core;
using CardGame.UI;

/// <summary>
/// Represents a card that needs to be flipped with ripple effect
/// </summary>
public class FlipTarget
{
    public GameObject cardObject;
    public NewCard card;
    public Color captureColor;
    public CardGame.UI.FlipDirection direction;
    public float distance;
    public Vector3 position;
    
    public FlipTarget(GameObject obj, NewCard c, Color color, CardGame.UI.FlipDirection dir, float dist, Vector3 pos)
    {
        cardObject = obj;
        card = c;
        captureColor = color;
        direction = dir;
        distance = dist;
        position = pos;
    }
}

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
    
    [Header("Ripple Effect Settings")]
    [SerializeField] private bool useRippleEffect = true; // Enable ripple/chain flip effect
    [SerializeField] private float rippleDelayPerUnit = 0.15f; // Delay between flips per unit of distance
    [SerializeField] private float rippleBaseDelay = 0.1f; // Base delay before first flip starts
    
    [Header("Managers")]
    private ScoreManager scoreManager;
    private GameEndManager gameEndManager;
    
    // Track cards played this turn (cannot be captured during same turn)
    private HashSet<GameObject> cardsPlayedThisTurn = new HashSet<GameObject>();
    
    // Track cards currently being processed in chain captures (to prevent infinite loops)
    private HashSet<GameObject> cardsInCurrentChain = new HashSet<GameObject>();
    
    // Track if chains are in progress
    private int activeChainCount = 0;
    
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
        
        // Auto-find ScoreManager
        if (scoreManager == null)
        {
            scoreManager = FindObjectOfType<ScoreManager>();
            if (scoreManager == null)
            {
                Debug.LogWarning("CardDropArea1: ScoreManager not found! Scoring will not work.");
            }
        }
        
        // Auto-find GameEndManager
        if (gameEndManager == null)
        {
            gameEndManager = FindObjectOfType<GameEndManager>();
            if (gameEndManager == null)
            {
                Debug.LogWarning("CardDropArea1: GameEndManager not found! Game end detection will not work.");
            }
        }
        
        // Subscribe to GameManager events to clear turn tracking
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnTurnStarted += ClearTurnTracking;
            GameManager.Instance.OnTurnEnded += ClearTurnTracking;
        }
    }
    
    private void OnDestroy()
    {
        // Unsubscribe from events
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnTurnStarted -= ClearTurnTracking;
            GameManager.Instance.OnTurnEnded -= ClearTurnTracking;
        }
    }
    
    /// <summary>
    /// Clears the tracking of cards played this turn
    /// </summary>
    private void ClearTurnTracking()
    {
        cardsPlayedThisTurn.Clear();
        if (debugBattles)
        {
            Debug.Log("Turn tracking cleared - new cards can now be captured");
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
                    
                    // Track card as played this turn (cannot be captured during same turn)
                    cardsPlayedThisTurn.Add(cardMover.gameObject);
                    
                    // Check board occupancy after card is placed
                    CheckBoardOccupancy();
                    
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
        List<FlipTarget> flipTargets = new List<FlipTarget>();
        FlipTarget placedCardFlipTarget = null;
        
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
            
            FlipTarget target = CheckBattleBetweenCardsForRipple(placedPosition, placedCard, otherCardMover.transform.position, otherCardMover.Card, otherCardMover.gameObject, placedCardMover.gameObject);
            if (target != null)
            {
                flipTargets.Add(target);
            }
        }
        
        // Check against opponent CardMovers
        foreach (CardMoverOpp otherCardMoverOpp in allCardMoverOpps)
        {
            if (otherCardMoverOpp.Card == null) continue;
            FlipTarget target = CheckBattleBetweenCardsForRipple(placedPosition, placedCard, otherCardMoverOpp.transform.position, otherCardMoverOpp.Card, otherCardMoverOpp.gameObject, placedCardMover.gameObject);
            if (target != null)
            {
                flipTargets.Add(target);
            }
        }
        
        // Check if placed card should flip (lost to another card)
        /*foreach (CardMover otherCardMover in allCardMovers)
        {
            if (otherCardMover == placedCardMover) continue;
            if (otherCardMover.Card == null) continue;
            
            bool placedCardLost = CheckBattleBetweenCards(placedPosition, placedCard, otherCardMover.transform.position, otherCardMover.Card, otherCardMover.gameObject, placedCardMover.gameObject);
            if (placedCardLost)
            {
                bool winningCardIsPlayer = IsPlayerCard(otherCardMover.gameObject);
                Color captureColor = winningCardIsPlayer ? 
                    GetPlayerCaptureColor() : GetOpponentCaptureColor();
                float distance = Vector3.Distance(placedPosition, otherCardMover.transform.position);
                placedCardFlipTarget = new FlipTarget(placedCardMover.gameObject, placedCard, captureColor, CardGame.UI.FlipDirection.Right, distance, placedPosition);
                break;
            }
        }
        */
        // Check opponent cards for placed card loss
        if (placedCardFlipTarget == null)
        {
            foreach (CardMoverOpp otherCardMoverOpp in allCardMoverOpps)
            {
                if (otherCardMoverOpp.Card == null) continue;
                bool placedCardLost = CheckBattleBetweenCards(placedPosition, placedCard, otherCardMoverOpp.transform.position, otherCardMoverOpp.Card, otherCardMoverOpp.gameObject, placedCardMover.gameObject);
                if (placedCardLost)
                {
                    bool winningCardIsPlayer = IsPlayerCard(otherCardMoverOpp.gameObject);
                    Color captureColor = winningCardIsPlayer ? 
                        GetPlayerCaptureColor() : GetOpponentCaptureColor();
                    float distance = Vector3.Distance(placedPosition, otherCardMoverOpp.transform.position);
                    placedCardFlipTarget = new FlipTarget(placedCardMover.gameObject, placedCard, captureColor, CardGame.UI.FlipDirection.Right, distance, placedPosition);
                    break;
                }
            }
        }
        
        // Add placed card flip target if it lost
        if (placedCardFlipTarget != null)
        {
            flipTargets.Add(placedCardFlipTarget);
        }
        
        // Execute ripple effect if we have any flips
        if (flipTargets.Count > 0)
        {
            if (useRippleEffect)
            {
                StartCoroutine(ExecuteRippleFlips(flipTargets, placedPosition));
            }
            else
            {
                // Old behavior: flip all at once
                foreach (var target in flipTargets)
                {
                    FlipCardGameObject(target.cardObject, target.card, target.captureColor, target.direction);
                }
            }
        }
    }
    
    /// <summary>
    /// Determines if a card GameObject belongs to the player (vs opponent)
    /// Checks both component type (CardMover vs CardMoverOpp) and border color (for captured cards)
    /// </summary>
    private bool IsPlayerCard(GameObject cardObject)
    {
        if (cardObject == null) return true; // Default to player
        
        // First, check border color to determine ownership (for captured cards)
        // This takes priority because a captured card belongs to whoever captured it
        NewCardUI cardUI = cardObject.GetComponent<NewCardUI>();
        if (cardUI == null)
        {
            cardUI = cardObject.GetComponentInChildren<NewCardUI>();
        }
        if (cardUI == null)
        {
            cardUI = cardObject.GetComponentInParent<NewCardUI>();
        }
        
        if (cardUI != null)
        {
            // Check the card's background color to determine ownership
            // Use reflection to access private cardBackground field
            var cardBackgroundField = typeof(NewCardUI).GetField("cardBackground",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (cardBackgroundField != null)
            {
                var cardBackground = cardBackgroundField.GetValue(cardUI);
                if (cardBackground != null)
                {
                    Color borderColor = Color.white;
                    
                    // Get color from SpriteRenderer or Image
                    SpriteRenderer bgSR = cardBackground as SpriteRenderer;
                    if (bgSR != null)
                    {
                        borderColor = bgSR.color;
                    }
                    else
                    {
                        UnityEngine.UI.Image bgImg = cardBackground as UnityEngine.UI.Image;
                        if (bgImg != null)
                        {
                            borderColor = bgImg.color;
                        }
                    }
                    
                    // Compare with capture colors to determine ownership
                    Color playerColor = GetPlayerCaptureColor();
                    Color opponentColor = GetOpponentCaptureColor();
                    
                    // Check if border color matches player's capture color (orange)
                    // Use a small tolerance for color comparison
                    float colorTolerance = 0.1f;
                    if (Mathf.Abs(borderColor.r - playerColor.r) < colorTolerance &&
                        Mathf.Abs(borderColor.g - playerColor.g) < colorTolerance &&
                        Mathf.Abs(borderColor.b - playerColor.b) < colorTolerance)
                    {
                        return true; // Player's captured card (orange)
                    }
                    
                    // Check if border color matches opponent's capture color (green)
                    if (Mathf.Abs(borderColor.r - opponentColor.r) < colorTolerance &&
                        Mathf.Abs(borderColor.g - opponentColor.g) < colorTolerance &&
                        Mathf.Abs(borderColor.b - opponentColor.b) < colorTolerance)
                    {
                        return false; // Opponent's captured card (green)
                    }
                }
            }
        }
        
        // Fallback: Check if it has CardMover (player) or CardMoverOpp (opponent)
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
                    
                    // Track card as played this turn (cannot be captured during same turn)
                    cardsPlayedThisTurn.Add(cardMoverOpp.gameObject);
                    
                    // Check board occupancy after card is placed
                    CheckBoardOccupancy();
                    
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
        List<FlipTarget> flipTargets = new List<FlipTarget>();
        FlipTarget placedCardFlipTarget = null;
        
        // Find all CardMover and CardMoverOpp components on the board
        CardMover[] allCardMovers = FindObjectsOfType<CardMover>();
        CardMoverOpp[] allCardMoverOpps = FindObjectsOfType<CardMoverOpp>();
        
        // Check against regular CardMovers
        foreach (CardMover otherCardMover in allCardMovers)
        {
            if (otherCardMover.Card == null) continue;
            FlipTarget target = CheckBattleBetweenCardsForRipple(placedPosition, placedCard, otherCardMover.transform.position, otherCardMover.Card, otherCardMover.gameObject, placedCardMover.gameObject);
            if (target != null)
            {
                flipTargets.Add(target);
            }
        }
        
        // Check against opponent CardMovers
        foreach (CardMoverOpp otherCardMoverOpp in allCardMoverOpps)
        {
            if (otherCardMoverOpp == placedCardMover) continue;
            if (otherCardMoverOpp.Card == null) continue;
            FlipTarget target = CheckBattleBetweenCardsForRipple(placedPosition, placedCard, otherCardMoverOpp.transform.position, otherCardMoverOpp.Card, otherCardMoverOpp.gameObject, placedCardMover.gameObject);
            if (target != null)
            {
                flipTargets.Add(target);
            }
        }
        
        // Check if placed card should flip (lost to another card)
        /*foreach (CardMover otherCardMover in allCardMovers)
        {
            if (otherCardMover.Card == null) continue;
            bool placedCardLost = CheckBattleBetweenCards(placedPosition, placedCard, otherCardMover.transform.position, otherCardMover.Card, otherCardMover.gameObject, placedCardMover.gameObject);
            if (placedCardLost)
            {
                bool winningCardIsPlayer = IsPlayerCard(otherCardMover.gameObject);
                Color captureColor = winningCardIsPlayer ? 
                    GetPlayerCaptureColor() : GetOpponentCaptureColor();
                float distance = Vector3.Distance(placedPosition, otherCardMover.transform.position);
                placedCardFlipTarget = new FlipTarget(placedCardMover.gameObject, placedCard, captureColor, CardGame.UI.FlipDirection.Right, distance, placedPosition);
                break;
            }
        }
        */
        // Check opponent cards for placed card loss
       /* if (placedCardFlipTarget == null)
        {
            foreach (CardMoverOpp otherCardMoverOpp in allCardMoverOpps)
            {
                if (otherCardMoverOpp == placedCardMover) continue;
                if (otherCardMoverOpp.Card == null) continue;
                bool placedCardLost = CheckBattleBetweenCards(placedPosition, placedCard, otherCardMoverOpp.transform.position, otherCardMoverOpp.Card, otherCardMoverOpp.gameObject, placedCardMover.gameObject);
                if (placedCardLost)
                {
                    bool winningCardIsPlayer = IsPlayerCard(otherCardMoverOpp.gameObject);
                    Color captureColor = winningCardIsPlayer ? 
                        GetPlayerCaptureColor() : GetOpponentCaptureColor();
                    float distance = Vector3.Distance(placedPosition, otherCardMoverOpp.transform.position);
                    placedCardFlipTarget = new FlipTarget(placedCardMover.gameObject, placedCard, captureColor, CardGame.UI.FlipDirection.Right, distance, placedPosition);
                    break;
                }
            }
        }
        */
        // Add placed card flip target if it lost
        if (placedCardFlipTarget != null)
        {
            flipTargets.Add(placedCardFlipTarget);
        }
        
        // Execute ripple effect if we have any flips
        if (flipTargets.Count > 0)
        {
            if (useRippleEffect)
            {
                StartCoroutine(ExecuteRippleFlips(flipTargets, placedPosition));
            }
            else
            {
                // Old behavior: flip all at once
                foreach (var target in flipTargets)
                {
                    FlipCardGameObject(target.cardObject, target.card, target.captureColor, target.direction);
                }
            }
        }
    }
    
    /// <summary>
    /// Helper method to check battle between two cards
    /// Only checks orthogonal neighbors (top, bottom, left, right) - no diagonals
    /// Returns true if placed card should be flipped (lost)
    /// </summary>
    private bool CheckBattleBetweenCards(Vector3 placedPos, NewCard placedCard, Vector3 otherPos, NewCard otherCard, GameObject otherCardObject, GameObject placedCardObject)
    {
        // Don't battle cards that belong to the same player
        bool placedCardIsPlayer = IsPlayerCard(placedCardObject);
        bool otherCardIsPlayer = IsPlayerCard(otherCardObject);
        
        if (placedCardIsPlayer == otherCardIsPlayer)
        {
            if (debugBattles)
            {
                Debug.Log($"CheckBattleBetweenCards: {placedCard.Data.cardName} and {otherCard.Data.cardName} belong to the same player, skipping battle");
            }
            return false; // Same player, no battle
        }
        
        Vector3 delta = otherPos - placedPos;
        float deltaX = Mathf.Abs(delta.x);
        float deltaY = Mathf.Abs(delta.y); // Y is vertical (up/down)
        
        // Only check directly adjacent cards (orthogonal neighbors)
        // Cards must be aligned on same row OR same column, and within 1 grid cell
        bool isOrthogonalNeighbor = false;
        string directionName = "";
        int placedCardStat = 0;
        int otherCardStat = 0;
        
        // Check if cards are on the same row (Y/Z aligned) - horizontal neighbors
        if (deltaY < 0.5f && deltaX > 0.1f && deltaX <= adjacentCardDistance + 0.1f)
        {
            isOrthogonalNeighbor = true;
            if (delta.x > 0)
            {
                // Other card is to the RIGHT of placed card
                placedCardStat = placedCard.CurrentRightStat;
                otherCardStat = otherCard.CurrentLeftStat;
                directionName = "right";
            }
            else
            {
                // Other card is to the LEFT of placed card
                placedCardStat = placedCard.CurrentLeftStat;
                otherCardStat = otherCard.CurrentRightStat;
                directionName = "left";
            }
        }
        // Check if cards are on the same column (X aligned) - vertical neighbors
        else if (deltaX < 0.5f && deltaY > 0.1f && deltaY <= adjacentCardDistance + 0.1f)
        {
            isOrthogonalNeighbor = true;
            if (delta.y > 0)
            {
                // Other card is ABOVE (top) of placed card
                placedCardStat = placedCard.CurrentTopStat;
                otherCardStat = otherCard.CurrentDownStat;
                directionName = "top";
            }
            else
            {
                // Other card is BELOW (bottom) of placed card
                placedCardStat = placedCard.CurrentDownStat;
                otherCardStat = otherCard.CurrentTopStat;
                directionName = "down";
            }
        }
        
        if (!isOrthogonalNeighbor)
        {
            if (debugBattles)
            {
                Debug.Log($"CheckBattleBetweenCards: {placedCard.Data.cardName} vs {otherCard.Data.cardName} - Not orthogonal neighbor (deltaX: {deltaX}, deltaY: {deltaY}), skipping battle");
            }
            return false;
        }
        
        if (debugBattles)
        {
            Debug.Log($"  → Stat comparison: {placedCard.Data.cardName} {directionName} stat = {placedCardStat}, {otherCard.Data.cardName} opposing stat = {otherCardStat}");
        }
        
        if (placedCardStat > otherCardStat)
        {
            // Placed card won - if using ripple effect, don't flip immediately (will be handled by ripple)
            // Only flip immediately if ripple effect is disabled
            if (!useRippleEffect)
            {
                // Determine capture color: The captured card gets the color of who captured it
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
            }
            else
            {
                // Using ripple effect - just log, don't flip (will be handled by ripple)
                if (debugBattles)
                {
                    Debug.Log($"  → {placedCard.Data.cardName} ({placedCardStat}) > {otherCard.Data.cardName} ({otherCardStat}) in {directionName} direction. Will be captured in ripple effect.");
                }
            }
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
        
        // Notify ScoreManager of the capture
        if (scoreManager != null)
        {
            bool isPlayerCapture = IsPlayerCard(cardObject);
            // Note: The card is being captured, so the capture color determines who gets the score
            bool isPlayerScoring = (captureColor == GetPlayerCaptureColor());
            scoreManager.AddScore(isPlayerScoring);
        }
    }
    
    /// <summary>
    /// Checks battle between cards and returns a FlipTarget if the other card should be flipped
    /// Used for ripple effect - collects flip targets instead of flipping immediately
    /// Only checks orthogonal neighbors (top, bottom, left, right)
    /// </summary>
    private FlipTarget CheckBattleBetweenCardsForRipple(Vector3 placedPos, NewCard placedCard, Vector3 otherPos, NewCard otherCard, GameObject otherCardObject, GameObject placedCardObject)
    {
        // Don't battle cards that belong to the same player
        bool placedCardIsPlayer = IsPlayerCard(placedCardObject);
        bool otherCardIsPlayer = IsPlayerCard(otherCardObject);
        
        if (placedCardIsPlayer == otherCardIsPlayer)
        {
            return null; // Same player, no battle
        }
        
        Vector3 delta = otherPos - placedPos;
        float deltaX = Mathf.Abs(delta.x);
        float deltaY = Mathf.Abs(delta.y); // Y is vertical (up/down)
        
        // Only check directly adjacent cards (orthogonal neighbors)
        bool isOrthogonalNeighbor = false;
        string directionName = "";
        int placedCardStat = 0;
        int otherCardStat = 0;
        
        // Check if cards are on the same row (Y/Z aligned) - horizontal neighbors
        if (deltaY < 0.5f && deltaX > 0.1f && deltaX <= adjacentCardDistance + 0.1f)
        {
            isOrthogonalNeighbor = true;
            if (delta.x > 0)
            {
                // Other card is to the RIGHT of placed card
                placedCardStat = placedCard.CurrentRightStat;
                otherCardStat = otherCard.CurrentLeftStat;
                directionName = "right";
            }
            else
            {
                // Other card is to the LEFT of placed card
                placedCardStat = placedCard.CurrentLeftStat;
                otherCardStat = otherCard.CurrentRightStat;
                directionName = "left";
            }
        }
        // Check if cards are on the same column (X aligned) - vertical neighbors
        else if (deltaX < 0.5f && deltaY > 0.1f && deltaY <= adjacentCardDistance + 0.1f)
        {
            isOrthogonalNeighbor = true;
            if (delta.y > 0)
            {
                // Other card is ABOVE (top) of placed card
                placedCardStat = placedCard.CurrentTopStat;
                otherCardStat = otherCard.CurrentDownStat;
                directionName = "top";
            }
            else
            {
                // Other card is BELOW (bottom) of placed card
                placedCardStat = placedCard.CurrentDownStat;
                otherCardStat = otherCard.CurrentTopStat;
                directionName = "down";
            }
        }
        
        if (!isOrthogonalNeighbor)
        {
            return null; // Not an orthogonal neighbor, no battle
        }
        
        // If placed card wins, other card should flip
        if (placedCardStat > otherCardStat)
        {
            Color captureColor = placedCardIsPlayer ? 
                GetPlayerCaptureColor() : GetOpponentCaptureColor();
            
            // Convert direction name to FlipDirection enum
            CardGame.UI.FlipDirection flipDir = CardGame.UI.FlipDirection.Right;
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
            
            // Calculate distance for ripple effect timing
            float distance = Vector3.Distance(placedPos, otherPos);
            return new FlipTarget(otherCardObject, otherCard, captureColor, flipDir, distance, otherPos);
        }
        
        return null; // No flip needed
    }
    
    /// <summary>
    /// Executes ripple effect by chaining flips with delays based on distance
    /// </summary>
    private IEnumerator ExecuteRippleFlips(List<FlipTarget> flipTargets, Vector3 sourcePosition)
    {
        if (flipTargets == null || flipTargets.Count == 0) yield break;
        
        // Sort by distance from source (closest first for ripple effect)
        flipTargets.Sort((a, b) => a.distance.CompareTo(b.distance));
        
        if (debugBattles)
        {
            Debug.Log($"ExecuteRippleFlips: Starting ripple effect with {flipTargets.Count} cards. Base delay: {rippleBaseDelay}s, Delay per unit: {rippleDelayPerUnit}s");
        }
        
        // Wait for base delay before starting
        yield return new WaitForSeconds(rippleBaseDelay);
        
        // Increment active chain count for initial ripple
        activeChainCount++;
        if (gameEndManager != null)
        {
            gameEndManager.SetChainsInProgress(true);
        }
        
        // Flip each card with increasing delay based on distance
        float lastDistance = 0f;
        foreach (var target in flipTargets)
        {
            // Calculate delay based on distance difference from previous card
            float distanceDelta = target.distance - lastDistance;
            float delay = distanceDelta * rippleDelayPerUnit;
            
            if (delay > 0f)
            {
                yield return new WaitForSeconds(delay);
            }
            
            // Execute the flip
            FlipCardGameObject(target.cardObject, target.card, target.captureColor, target.direction);
            
            // Wait for the flip animation to complete before checking for chain captures
            // Flip animation takes about 1 second total (0.5s flip to back + 0.5s flip back to front)
            // Use a safe delay to ensure animation completes
            yield return new WaitForSeconds(1.1f);
            
            // Check if this newly captured card can capture others (chain capture)
            CheckChainCapture(target.cardObject, target.card);
            
            lastDistance = target.distance;
        }
        
        if (debugBattles)
        {
            Debug.Log($"ExecuteRippleFlips: Ripple effect complete!");
        }
        
        // Decrement active chain count
        activeChainCount--;
        if (activeChainCount <= 0)
        {
            activeChainCount = 0;
            if (gameEndManager != null)
            {
                gameEndManager.SetChainsInProgress(false);
            }
        }
    }
    
    /// <summary>
    /// Checks board occupancy to determine if the board is full
    /// </summary>
    private void CheckBoardOccupancy()
    {
        // Count total CardDropArea1 instances (total board spaces)
        CardDropArea1[] allDropAreas = FindObjectsOfType<CardDropArea1>();
        int totalSpaces = allDropAreas.Length;
        
        // Count occupied spaces (spaces with cards on them)
        int occupiedSpaces = 0;
        
        // Find all cards on the board
        CardMover[] allCardMovers = FindObjectsOfType<CardMover>();
        CardMoverOpp[] allCardMoverOpps = FindObjectsOfType<CardMoverOpp>();
        
        // Check each drop area to see if it has a card nearby
        foreach (CardDropArea1 dropArea in allDropAreas)
        {
            bool hasCard = false;
            
            // Check CardMovers
            foreach (CardMover cardMover in allCardMovers)
            {
                if (cardMover.Card != null)
                {
                    float distance = Vector3.Distance(dropArea.transform.position, cardMover.transform.position);
                    if (distance < 0.5f) // Cards are considered on a space if within 0.5 units
                    {
                        hasCard = true;
                        break;
                    }
                }
            }
            
            // Check CardMoverOpps
            if (!hasCard)
            {
                foreach (CardMoverOpp cardMoverOpp in allCardMoverOpps)
                {
                    if (cardMoverOpp.Card != null)
                    {
                        float distance = Vector3.Distance(dropArea.transform.position, cardMoverOpp.transform.position);
                        if (distance < 0.5f)
                        {
                            hasCard = true;
                            break;
                        }
                    }
                }
            }
            
            if (hasCard)
            {
                occupiedSpaces++;
            }
        }
        
        if (debugBattles)
        {
            Debug.Log($"Board occupancy: {occupiedSpaces}/{totalSpaces} spaces filled");
        }
        
        // Check if board is full
        if (occupiedSpaces >= totalSpaces && totalSpaces > 0)
        {
            Debug.Log("Board is full! Last card has been placed.");
            if (gameEndManager != null)
            {
                gameEndManager.CheckGameEnd();
            }
        }
    }
    
    /// <summary>
    /// Checks if a newly captured card can capture adjacent cards (chain capture)
    /// </summary>
    private void CheckChainCapture(GameObject capturedCard, NewCard card)
    {
        if (capturedCard == null || card == null) return;
        
        // Skip if card is already in current chain (prevent infinite loops)
        if (cardsInCurrentChain.Contains(capturedCard))
        {
            if (debugBattles)
            {
                Debug.Log($"CheckChainCapture: {card.Data.cardName} already in current chain, skipping");
            }
            return;
        }
        
        // Skip if card was played this turn (same-turn protection rule)
        if (cardsPlayedThisTurn.Contains(capturedCard))
        {
            if (debugBattles)
            {
                Debug.Log($"CheckChainCapture: {card.Data.cardName} was played this turn, cannot be captured");
            }
            return;
        }
        
        // Add to current chain
        cardsInCurrentChain.Add(capturedCard);
        
        Vector3 cardPosition = capturedCard.transform.position;
        List<FlipTarget> chainFlipTargets = new List<FlipTarget>();
        
        // Find all cards on the board
        CardMover[] allCardMovers = FindObjectsOfType<CardMover>();
        CardMoverOpp[] allCardMoverOpps = FindObjectsOfType<CardMoverOpp>();
        
        // Check adjacent cards
        foreach (CardMover otherCardMover in allCardMovers)
        {
            if (otherCardMover.Card == null) continue;
            if (otherCardMover.gameObject == capturedCard) continue; // Skip self
            
            // Skip if in current chain or played this turn
            if (cardsInCurrentChain.Contains(otherCardMover.gameObject)) continue;
            if (cardsPlayedThisTurn.Contains(otherCardMover.gameObject)) continue;
            
            // Only check battles if cards belong to different players (after capture)
            bool capturedCardIsPlayer = IsPlayerCard(capturedCard);
            bool otherCardIsPlayer = IsPlayerCard(otherCardMover.gameObject);
            
            // Skip if both cards belong to same player (no battle)
            if (capturedCardIsPlayer == otherCardIsPlayer) continue;
            
            FlipTarget target = CheckBattleBetweenCardsForRipple(
                cardPosition, card,
                otherCardMover.transform.position, otherCardMover.Card,
                otherCardMover.gameObject, capturedCard);
            
            if (target != null)
            {
                chainFlipTargets.Add(target);
            }
        }
        
        foreach (CardMoverOpp otherCardMoverOpp in allCardMoverOpps)
        {
            if (otherCardMoverOpp.Card == null) continue;
            if (otherCardMoverOpp.gameObject == capturedCard) continue; // Skip self
            
            // Skip if in current chain or played this turn
            if (cardsInCurrentChain.Contains(otherCardMoverOpp.gameObject)) continue;
            if (cardsPlayedThisTurn.Contains(otherCardMoverOpp.gameObject)) continue;
            
            // Only check battles if cards belong to different players (after capture)
            bool capturedCardIsPlayer = IsPlayerCard(capturedCard);
            bool otherCardIsPlayer = IsPlayerCard(otherCardMoverOpp.gameObject);
            
            // Skip if both cards belong to same player (no battle)
            if (capturedCardIsPlayer == otherCardIsPlayer) continue;
            
            FlipTarget target = CheckBattleBetweenCardsForRipple(
                cardPosition, card,
                otherCardMoverOpp.transform.position, otherCardMoverOpp.Card,
                otherCardMoverOpp.gameObject, capturedCard);
            
            if (target != null)
            {
                chainFlipTargets.Add(target);
            }
        }
        
        // If we found chain captures, execute them
        if (chainFlipTargets.Count > 0)
        {
            if (debugBattles)
            {
                Debug.Log($"Chain capture triggered! {card.Data.cardName} can capture {chainFlipTargets.Count} adjacent cards");
            }
            
            // Increment active chain count
            activeChainCount++;
            if (gameEndManager != null)
            {
                gameEndManager.SetChainsInProgress(true);
            }
            
            // Execute chain captures with ripple effect
            StartCoroutine(ExecuteChainCaptureRipple(chainFlipTargets, cardPosition));
        }
        else
        {
            // No more chain captures, remove from current chain
            cardsInCurrentChain.Remove(capturedCard);
        }
    }
    
    /// <summary>
    /// Executes chain captures with ripple effect, then checks for further chains
    /// </summary>
    private IEnumerator ExecuteChainCaptureRipple(List<FlipTarget> flipTargets, Vector3 sourcePosition)
    {
        if (flipTargets == null || flipTargets.Count == 0) yield break;
        
        // Sort by distance
        flipTargets.Sort((a, b) => a.distance.CompareTo(b.distance));
        
        if (debugBattles)
        {
            Debug.Log($"ExecuteChainCaptureRipple: Starting chain capture ripple with {flipTargets.Count} cards");
        }
        
        // Execute each flip with ripple timing
        float lastDistance = 0f;
        foreach (var target in flipTargets)
        {
            float distanceDelta = target.distance - lastDistance;
            float delay = distanceDelta * rippleDelayPerUnit;
            
            if (delay > 0f)
            {
                yield return new WaitForSeconds(delay);
            }
            
            // Execute the flip
            FlipCardGameObject(target.cardObject, target.card, target.captureColor, target.direction);
            
            // Wait for the flip animation to complete before checking for next chain
            // Flip animation takes about 1 second total (0.5s flip to back + 0.5s flip back to front)
            yield return new WaitForSeconds(1.1f); // Wait for animation
            
            // Check if this newly captured card can capture others (recursive chain)
            CheckChainCapture(target.cardObject, target.card);
            
            lastDistance = target.distance;
        }
        
        // Decrement active chain count when this chain level is done
        activeChainCount--;
        if (activeChainCount <= 0)
        {
            activeChainCount = 0;
            cardsInCurrentChain.Clear(); // Clear chain tracking when all chains complete
            if (gameEndManager != null)
            {
                gameEndManager.SetChainsInProgress(false);
            }
        }
        
        if (debugBattles)
        {
            Debug.Log($"ExecuteChainCaptureRipple: Chain capture ripple complete!");
        }
    }
}
