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

public class CardDropArea : MonoBehaviour, ICardDropArea
{
    [Header("Deck Manager Reference")]
    [SerializeField] private NewDeckManagerP1 deckManagerP1;
    [SerializeField] private NewDeckManagerP2 deckManagerP2;

    [Header("Settings")]
    [SerializeField] private bool playCardOnDrop = true;
    [SerializeField] private bool snapCardToPosition = true;
    [SerializeField] private Vector3 cardScaleOnBoard = Vector3.one; // Leave at (1,1,1) to auto-match drop area size
    [SerializeField, Range(0.5f, 1.2f)] private float cardScaleFillPercent = 0.9f;
    [SerializeField] private SpriteRenderer tileSpriteRenderer;
    // [SerializeField] private float adjacentCardDistance = 3f; // Distance to consider cards adjacent (increased from 2f) - Currently unused, kept for future use
    [SerializeField] private bool enableCardBattles = true; // Enable stat comparison and card flipping
    [SerializeField] private bool debugBattles = true; // Log battle detection for debugging
    
    [Header("Ripple Effect Settings")]
    [SerializeField] private bool useRippleEffect = true; // Enable ripple/chain flip effect
    [SerializeField] private float rippleDelayPerUnit = 0.15f; // Delay between flips per unit of distance
    [SerializeField] private float rippleBaseDelay = 0.1f; // Base delay before first flip starts
    
    [Header("Managers")]
    private ScoreManager scoreManager;
    private GameEndManager gameEndManager;
    
    // Board occupancy tracking
    [SerializeField] private GameObject occupyingCard;
    public bool IsOccupied => occupyingCard != null;
    
    /// <summary>
    /// Gets the card GameObject currently occupying this drop area (null if empty)
    /// </summary>
    public GameObject GetOccupyingCard() => occupyingCard;
    
    // Track cards played this turn (cannot be captured during same turn)
    private HashSet<GameObject> cardsPlayedThisTurn = new HashSet<GameObject>();
    
    // Track cards currently being processed in chain captures (to prevent infinite loops)
    private HashSet<GameObject> cardsInCurrentChain = new HashSet<GameObject>();
    
    // Track if chains are in progress
    private int activeChainCount = 0;
    
    // [CardFront] Game statistics tracking (static to track across all instances)
    private static int gameCardsPlayed = 0;
    private static int gameCapturesMade = 0;
    private static int gameLongestChain = 0;
    private static int currentChainLength = 0;
    
    /// <summary>
    /// Gets the number of cards played this game
    /// </summary>
    public static int GetCardsPlayed() => gameCardsPlayed;
    
    /// <summary>
    /// Gets the number of captures made this game
    /// </summary>
    public static int GetCapturesMade() => gameCapturesMade;
    
    /// <summary>
    /// Gets the longest chain capture length this game
    /// </summary>
    public static int GetLongestChain() => gameLongestChain;
    
    /// <summary>
    /// Resets game statistics (called at start of new game)
    /// </summary>
    public static void ResetGameStatistics()
    {
        gameCardsPlayed = 0;
        gameCapturesMade = 0;
        gameLongestChain = 0;
        currentChainLength = 0;
        // Game statistics reset
    }
    
    /// <summary>
    /// Resets this CardDropArea instance for a new game (clears occupying card and turn tracking)
    /// </summary>
    public void ResetForNewGame()
    {
        // Clear occupying card reference
        occupyingCard = null;
        
        // Clear turn tracking
        cardsPlayedThisTurn.Clear();
        cardsInCurrentChain.Clear();
        activeChainCount = 0;
        
        if (debugBattles)
        {
            // Reset for new game
        }
    }

    private bool CanCardAct(FateSide side)
    {
        if (FateFlowController.Instance == null) return true;
        return FateFlowController.Instance.CanAct(side);
    }
    
    private void Start()
    {
        // [CardFront] CRITICAL: Ensure Collider2D exists for Physics2D.OverlapPoint detection
        Collider2D existingCollider = GetComponent<Collider2D>();
        if (existingCollider == null)
        {
            existingCollider = GetComponentInChildren<Collider2D>();
        }
        
        if (existingCollider == null)
        {
            // No Collider2D found - add one automatically
            // Try to use SpriteRenderer bounds to size the collider
            if (tileSpriteRenderer == null)
            {
                tileSpriteRenderer = GetComponent<SpriteRenderer>();
            }
            
            if (tileSpriteRenderer != null && tileSpriteRenderer.sprite != null)
            {
                // Use BoxCollider2D to match sprite bounds
                BoxCollider2D boxCollider = gameObject.AddComponent<BoxCollider2D>();
                boxCollider.size = tileSpriteRenderer.bounds.size;
                boxCollider.isTrigger = true; // Allow physics detection but not collision blocking
                // Auto-added BoxCollider2D
            }
            else
            {
                // Fallback: add a default sized collider
                BoxCollider2D boxCollider = gameObject.AddComponent<BoxCollider2D>();
                boxCollider.size = new Vector2(1f, 1f); // Default 1x1 unit size
                boxCollider.isTrigger = true;
                Debug.LogWarning($"[CardDropArea] Auto-added default BoxCollider2D to '{gameObject.name}' (no SpriteRenderer found for sizing). Size manually in Inspector if needed.");
            }
        }
        else
        {
            // Collider exists - ensure it's enabled and set as trigger if needed
            if (!existingCollider.enabled)
            {
                existingCollider.enabled = true;
                // Enabled existing Collider2D
            }
            
            // Ensure it's a trigger (doesn't block physics, but can be detected)
            if (existingCollider is BoxCollider2D boxCol)
            {
                boxCol.isTrigger = true;
            }
            else if (existingCollider is CircleCollider2D circleCol)
            {
                circleCol.isTrigger = true;
            }
            
            // Collider2D verified
        }
        
        if (tileSpriteRenderer == null)
        {
            tileSpriteRenderer = GetComponent<SpriteRenderer>();
        }
        
        // Auto-find NewDeckManagerP1 if not assigned
        if (deckManagerP1 == null)
        {
            deckManagerP1 = FindObjectOfType<NewDeckManagerP1>();
            if (deckManagerP1 == null)
            {
                Debug.LogWarning("CardDropArea: NewDeckManagerP1 not found! Card play functionality will not work.");
            }
        }
        
        // Auto-find NewDeckManagerP2 if not assigned
        if (deckManagerP2 == null)
        {
            deckManagerP2 = FindObjectOfType<NewDeckManagerP2>();
            if (deckManagerP2 == null)
            {
                Debug.LogWarning("CardDropArea: NewDeckManagerP2 not found! P2 card play functionality will not work.");
            }
        }
        
        // Auto-find ScoreManager
        if (scoreManager == null)
        {
            scoreManager = FindObjectOfType<ScoreManager>();
            if (scoreManager == null)
            {
                Debug.LogWarning("CardDropArea: ScoreManager not found! Scoring will not work.");
            }
        }
        
        // Auto-find GameEndManager
        if (gameEndManager == null)
        {
            gameEndManager = FindObjectOfType<GameEndManager>();
            if (gameEndManager == null)
            {
                Debug.LogWarning("CardDropArea: GameEndManager not found! Game end detection will not work.");
            }
        }
        
        if (FateFlowController.Instance != null)
        {
            FateFlowController.Instance.OnFateChanged += HandleFateWindowShift;
        }
    }
    
    private void OnDestroy()
    {
        if (FateFlowController.Instance != null)
        {
            FateFlowController.Instance.OnFateChanged -= HandleFateWindowShift;
        }
    }
    
    /// <summary>
    /// Applies scaling so a card visually fills the tile. If cardScaleOnBoard is left at Vector3.one, it derives the size from the drop area scale.
    /// </summary>
    private void ApplyCardScale(Transform cardTransform)
    {
        if (cardTransform == null) return;
        
        if (cardScaleOnBoard != Vector3.one)
        {
            cardTransform.localScale = cardScaleOnBoard;
            if (debugBattles)
            {
                // Applied explicit scale override
            }
            return;
        }
        
        bool scaledViaRenderers = false;
        if (tileSpriteRenderer != null)
        {
            SpriteRenderer cardSprite = cardTransform.GetComponentInChildren<SpriteRenderer>();
            if (cardSprite != null)
            {
                float tileSize = Mathf.Min(tileSpriteRenderer.bounds.size.x, tileSpriteRenderer.bounds.size.y);
                float cardSize = Mathf.Max(cardSprite.bounds.size.x, cardSprite.bounds.size.y);
                if (tileSize > 0f && cardSize > 0.0001f)
                {
                    float targetWorldSize = tileSize * cardScaleFillPercent;
                    float scaleMultiplier = targetWorldSize / cardSize;
                    Vector3 localScale = cardTransform.localScale;
                    float newZ = Mathf.Approximately(localScale.z, 0f) ? 1f : localScale.z;
                    Vector3 newScale = new Vector3(localScale.x * scaleMultiplier, localScale.y * scaleMultiplier, newZ);
                    cardTransform.localScale = newScale;
                    scaledViaRenderers = true;
                    if (debugBattles)
                    {
                        // Scaled via renderer bounds
                    }
                }
            }
        }
        
        if (!scaledViaRenderers)
        {
            float tileScale = Mathf.Min(transform.lossyScale.x, transform.lossyScale.y);
            float finalScale = tileScale * cardScaleFillPercent;
            float currentZ = Mathf.Approximately(cardTransform.localScale.z, 0f) ? 1f : cardTransform.localScale.z;
            Vector3 fallbackScale = new Vector3(finalScale, finalScale, currentZ);
            cardTransform.localScale = fallbackScale;
            if (debugBattles)
            {
                // Applied fallback scale
            }
        }
    }
    
    // [CardFront] Static tracking to prevent duplicate logs when multiple CardDropArea instances exist
    private static FateSide lastClearedFate = (FateSide)(-1); // Invalid initial value
    
    /// <summary>
    /// Clears the tracking of cards played this turn
    /// </summary>
    private void ClearTurnTracking(FateSide currentFate)
    {
        cardsPlayedThisTurn.Clear();
        
        // [CardFront] Only log once per fate change, not once per CardDropArea instance
        if (debugBattles && lastClearedFate != currentFate)
        {
            lastClearedFate = currentFate;
            // Turn tracking cleared
        }
    }
    
    private void HandleFateWindowShift(FateSide side)
    {
        ClearTurnTracking(side);
    }
    
    public void OnCardDrop(CardMoverP1 cardMover)
    {
        if (cardMover == null)
        {
            return;
        }

        if (!CanCardAct(cardMover.OwnerSide))
        {
            if (debugBattles)
            {
                // Cannot play card - incorrect Fate Window
            }
            cardMover.ReturnToStartPosition();
            return;
        }

        if (IsOccupied)
        {
            if (debugBattles)
            {
                // Tile already occupied
            }
            cardMover.ReturnToStartPosition();
            return;
        }
        
        if (snapCardToPosition)
        {
            cardMover.transform.position = transform.position;
            cardMover.RefreshHomePosition();
        }
        
        ApplyCardScale(cardMover.transform);
        
        // Try to find deckManagerP1 if it's null (runtime fallback)
        if (deckManagerP1 == null)
        {
            deckManagerP1 = FindObjectOfType<NewDeckManagerP1>();
        }
        
        if (playCardOnDrop && deckManagerP1 != null)
        {
            NewCard card = cardMover.Card;
            
            // [CardFront] Ensure card reference is set before attempting placement (same logic as Flame Witch)
            if (card == null)
            {
                // Card reference is null, attempting to find via FindCardReference
                cardMover.SendMessage("FindCardReference", SendMessageOptions.DontRequireReceiver);
                card = cardMover.Card;
            }
            
            // [CardFront] Additional fallback: Try to get card from NewCardUI component if CardMoverP1 still doesn't have it
            if (card == null)
            {
                NewCardUI cardUI = cardMover.GetComponent<NewCardUI>();
                if (cardUI == null) cardUI = cardMover.GetComponentInChildren<NewCardUI>();
                if (cardUI == null) cardUI = cardMover.GetComponentInParent<NewCardUI>();
                
                if (cardUI != null && cardUI.Card != null)
                {
                    card = cardUI.Card;
                    cardMover.SetCard(card); // Sync it to CardMoverP1 for future use
                    // Found card via NewCardUI and synced
                }
            }
            
            if (card != null && deckManagerP1.Hand.Contains(card))
            {
                deckManagerP1.PlayCard(card);
                // Card played from drop area
                
                // [CardFront] Track cards played for statistics
                gameCardsPlayed++;
                
                cardMover.SetPlayed(true);
                cardsPlayedThisTurn.Add(cardMover.gameObject);
                occupyingCard = cardMover.gameObject;
                
                CheckBoardOccupancy();
                
                // [CardFront] Check if game should end (all cards played - both players have no cards left)
                // Delay check by one frame to ensure hand removal events have fully propagated
                if (GameEndManager.Instance != null)
                {
                    StartCoroutine(DelayedGameEndCheck());
                }
                
                if (enableCardBattles)
                {
                    CheckCardBattlesP1(cardMover, card);
                }

                GameManager.Instance?.NotifyCardPlaced(this, card);
                FateFlowController.Instance?.AdvanceFateFlow();
            }
            else if (card == null)
            {
                Debug.LogError($"[CardDropArea] Card reference is still null for '{cardMover.gameObject.name}' after all fallback attempts. Cannot play card. Ensure SyncCardReferenceToMovers() was called in NewCardUI.Initialize().");
                cardMover.ReturnToStartPosition();
            }
            else if (!deckManagerP1.Hand.Contains(card))
            {
                Debug.LogWarning($"[CardDropArea] Card '{card.Data.cardName}' is not in hand for '{cardMover.gameObject.name}'. Hand contains {deckManagerP1.Hand.Count} cards. Card may have already been played.");
                cardMover.ReturnToStartPosition();
            }
        }
        else if (playCardOnDrop && deckManagerP1 == null)
        {
            Debug.LogWarning("CardDropArea: Cannot play card - NewDeckManagerP1 not found!");
            cardMover.ReturnToStartPosition();
        }
        
        // Card dropped
    }
    
    /// <summary>
    /// Checks for adjacent cards and performs stat comparisons for P1 cards, flipping losing cards
    /// </summary>
    private void CheckCardBattlesP1(CardMoverP1 placedCardMover, NewCard placedCard)
    {
        if (placedCardMover == null || placedCard == null) return;
        
        Vector3 placedPosition = placedCardMover.transform.position;
        
        // Checking battles for placed card
        
        List<FlipTarget> flipTargets = new List<FlipTarget>();
        FlipTarget placedCardFlipTarget = null;
        
        // Find all CardMoverP1 (P1) and CardMoverP2 (P2) components on the board
        CardMoverP1[] allCardMovers = FindObjectsOfType<CardMoverP1>();
        CardMoverP2[] allCardMoverP2s = FindObjectsOfType<CardMoverP2>();
        
        // Count only cards actually on board (z ≈ 0), not in hands (z = 90)
        int cardsOnBoard = 0;
        int cardsInHands = 0;
        foreach (CardMoverP1 mover in allCardMovers)
        {
            if (mover != null)
            {
                if (Mathf.Abs(mover.transform.position.z) < 1f) cardsOnBoard++;
                else cardsInHands++;
            }
        }
        foreach (CardMoverP2 moverP2 in allCardMoverP2s)
        {
            if (moverP2 != null)
            {
                if (Mathf.Abs(moverP2.transform.position.z) < 1f) cardsOnBoard++;
                else cardsInHands++;
            }
        }
        
        // Found cards on board for battle checking
        
        // Check against regular CardMovers
        foreach (CardMoverP1 otherCardMover in allCardMovers)
        {
            // Skip self
            if (otherCardMover == placedCardMover) continue;
            if (otherCardMover.Card == null) continue;
            
            // CRITICAL: Skip cards in hands (z = 90) - they should never battle cards on board
            if (Mathf.Abs(otherCardMover.transform.position.z) > 10f)
            {
                continue; // Skip cards in hands
            }
            
            // HARD GUARANTEE: No comparisons unless cards are strictly adjacent
            // This prevents any distant cards (e.g., 7.66 units) from being evaluated
            float testDistance;
            if (!AreCardsStrictlyAdjacent(placedPosition, otherCardMover.transform.position, out testDistance))
            {
                // Skip this card - not adjacent, don't even check battle
                continue;
            }
            
            FlipTarget target = CheckBattleBetweenCardsForRipple(placedPosition, placedCard, otherCardMover.transform.position, otherCardMover.Card, otherCardMover.gameObject, placedCardMover.gameObject);
            if (target != null)
            {
                flipTargets.Add(target);
            }
        }
        
        // Check against P2 CardMovers
        foreach (CardMoverP2 otherCardMoverP2 in allCardMoverP2s)
        {
            if (otherCardMoverP2.Card == null) continue;
            
            // CRITICAL: Skip cards in hands (z = 90) - they should never battle cards on board
            if (Mathf.Abs(otherCardMoverP2.transform.position.z) > 10f)
            {
                continue; // Skip cards in hands
            }
            
            // HARD GUARANTEE: No comparisons unless cards are strictly adjacent
            // This prevents any distant cards (e.g., 7.66 units) from being evaluated
            float testDistance;
            if (!AreCardsStrictlyAdjacent(placedPosition, otherCardMoverP2.transform.position, out testDistance))
            {
                // Skip this card - not adjacent, don't even check battle
                continue;
            }
            
            FlipTarget target = CheckBattleBetweenCardsForRipple(placedPosition, placedCard, otherCardMoverP2.transform.position, otherCardMoverP2.Card, otherCardMoverP2.gameObject, placedCardMover.gameObject);
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
                    GetPlayerCaptureColor() : GetP2CaptureColor();
                float distance = Vector3.Distance(placedPosition, otherCardMover.transform.position);
                placedCardFlipTarget = new FlipTarget(placedCardMover.gameObject, placedCard, captureColor, CardGame.UI.FlipDirection.Right, distance, placedPosition);
                break;
            }
        }
        */
        // Check P2 cards for placed card loss
        /*if (placedCardFlipTarget == null)
        {
            foreach (CardMoverP2 otherCardMoverP2 in allCardMoverP2s)
            {
                if (otherCardMoverP2.Card == null) continue;
                bool placedCardLost = CheckBattleBetweenCards(placedPosition, placedCard, otherCardMoverP2.transform.position, otherCardMoverP2.Card, otherCardMoverP2.gameObject, placedCardMover.gameObject);
                if (placedCardLost)
                {
                    bool winningCardIsPlayer = IsPlayerCard(otherCardMoverP2.gameObject);
                    Color captureColor = winningCardIsPlayer ? 
                        GetPlayerCaptureColor() : GetP2CaptureColor();
                    float distance = Vector3.Distance(placedPosition, otherCardMoverP2.transform.position);
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
            // Found flip targets, executing captures
            
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
        else
        {
            // No flip targets created, no captures
        }
    }
    
    /// <summary>
    /// Determines if a card GameObject belongs to P1 (vs P2)
    /// Checks both component type (CardMover (P1) vs CardMoverP2 (P2)) and border color (for captured cards)
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
                    Color p2Color = GetP2CaptureColor();
                    
                    // Check if border color matches player's capture color (orange)
                    // Use a small tolerance for color comparison
                    float colorTolerance = 0.1f;
                    if (Mathf.Abs(borderColor.r - playerColor.r) < colorTolerance &&
                        Mathf.Abs(borderColor.g - playerColor.g) < colorTolerance &&
                        Mathf.Abs(borderColor.b - playerColor.b) < colorTolerance)
                    {
                        return true; // Player's captured card (orange)
                    }
                    
                    // Check if border color matches P2's capture color (green)
                    if (Mathf.Abs(borderColor.r - p2Color.r) < colorTolerance &&
                        Mathf.Abs(borderColor.g - p2Color.g) < colorTolerance &&
                        Mathf.Abs(borderColor.b - p2Color.b) < colorTolerance)
                    {
                        return false; // P2's captured card (green)
                    }
                }
            }
        }
        
        // Fallback: Check if it has CardMover (P1) or CardMoverP2 (P2)
        CardMoverP1 cardMover = cardObject.GetComponent<CardMoverP1>();
        if (cardMover != null) return true;
        
        CardMoverP2 cardMoverP2 = cardObject.GetComponent<CardMoverP2>();
        if (cardMoverP2 != null) return false;
        
        // Check children/parents
        cardMover = cardObject.GetComponentInChildren<CardMoverP1>();
        if (cardMover != null) return true;
        
        cardMoverP2 = cardObject.GetComponentInChildren<CardMoverP2>();
        if (cardMoverP2 != null) return false;
        
        cardMover = cardObject.GetComponentInParent<CardMoverP1>();
        if (cardMover != null) return true;
        
        cardMoverP2 = cardObject.GetComponentInParent<CardMoverP2>();
        if (cardMoverP2 != null) return false;
        
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
    /// Gets P2's capture color (green)
    /// </summary>
    private Color GetP2CaptureColor()
    {
        // Green color for P2's captured cards
        return new Color(0f, 0.8f, 0f, 1f);
    }
    
    /// <summary>
    /// Flips a card (flips it to show the back) with a specific capture color
    /// </summary>
    private void FlipCard(CardMoverP1 cardMover, NewCard card, Color captureColor)
    {
        if (cardMover == null)
        {
            Debug.LogWarning("FlipCard: cardMover is null!");
            return;
        }

        // Use the same helper method for consistency
        FlipCardGameObject(cardMover.gameObject, card, captureColor);
    }

    public void OnCardDropP2(CardMoverP2 cardMoverP2)
    {
        if (cardMoverP2 == null)
        {
            return;
        }

        if (!CanCardAct(cardMoverP2.OwnerSide))
        {
            if (debugBattles)
            {
                // Cannot play card - incorrect Fate Window
            }
            cardMoverP2.ReturnToStartPosition();
            return;
        }
        
        if (IsOccupied)
        {
            if (debugBattles)
            {
                // Tile already occupied
            }
            cardMoverP2.ReturnToStartPosition();
            return;
        }
        
        if (snapCardToPosition)
        {
            cardMoverP2.transform.position = transform.position;
            cardMoverP2.RefreshHomePosition();
        }
        
        ApplyCardScale(cardMoverP2.transform);
        
        // Try to find deckManagerP2 if it's null (runtime fallback)
        if (deckManagerP2 == null)
        {
            deckManagerP2 = FindObjectOfType<NewDeckManagerP2>();
        }
        
        if (playCardOnDrop && deckManagerP2 != null)
        {
            NewCard card = cardMoverP2.Card;
            
            // [CardFront] Ensure card reference is set before attempting placement (same logic as P1)
            if (card == null)
            {
                // Card reference is null, attempting to find via FindCardReference
                cardMoverP2.SendMessage("FindCardReference", SendMessageOptions.DontRequireReceiver);
                card = cardMoverP2.Card;
            }
            
            // [CardFront] Additional fallback: Try to get card from NewCardUI component if CardMoverP2 still doesn't have it
            if (card == null)
            {
                NewCardUI cardUI = cardMoverP2.GetComponent<NewCardUI>();
                if (cardUI == null) cardUI = cardMoverP2.GetComponentInChildren<NewCardUI>();
                if (cardUI == null) cardUI = cardMoverP2.GetComponentInParent<NewCardUI>();
                
                if (cardUI != null && cardUI.Card != null)
                {
                    card = cardUI.Card;
                    cardMoverP2.SetCard(card); // Sync it to CardMoverP2 for future use
                    // Found card via NewCardUI and synced
                }
            }
            
            if (card != null && deckManagerP2.Hand.Contains(card))
            {
                deckManagerP2.PlayCard(card);
                // Card played from drop area
                
                // [CardFront] Track cards played for statistics
                gameCardsPlayed++;
                
                cardMoverP2.SetPlayed(true);
                cardsPlayedThisTurn.Add(cardMoverP2.gameObject);
                occupyingCard = cardMoverP2.gameObject;
                
                CheckBoardOccupancy();
                
                // [CardFront] Check if game should end (all cards played - both players have no cards left)
                // Delay check by one frame to ensure hand removal events have fully propagated
                if (GameEndManager.Instance != null)
                {
                    StartCoroutine(DelayedGameEndCheck());
                }
                
                if (enableCardBattles)
                {
                    CheckCardBattlesP2(cardMoverP2, card);
                }

                GameManager.Instance?.NotifyCardPlaced(this, card);
                FateFlowController.Instance?.AdvanceFateFlow();
            }
            else if (card == null)
            {
                Debug.LogError($"[CardDropArea] Card reference is still null for '{cardMoverP2.gameObject.name}' after all fallback attempts. Cannot play card. Ensure SyncCardReferenceToMovers() was called in NewCardUI.Initialize().");
                cardMoverP2.ReturnToStartPosition();
            }
            else if (!deckManagerP2.Hand.Contains(card))
            {
                Debug.LogWarning($"[CardDropArea] Card '{card.Data.cardName}' is not in hand for '{cardMoverP2.gameObject.name}'. Hand contains {deckManagerP2.Hand.Count} cards. Card may have already been played.");
                cardMoverP2.ReturnToStartPosition();
            }
        }
        else if (playCardOnDrop && deckManagerP2 == null)
        {
            Debug.LogWarning("CardDropArea: Cannot play card - NewDeckManagerP2 not found!");
            cardMoverP2.ReturnToStartPosition();
        }
        
        // Card dropped
    }
    
    /// <summary>
    /// Checks for adjacent cards and performs stat comparisons for P2 cards
    /// </summary>
    private void CheckCardBattlesP2(CardMoverP2 placedCardMover, NewCard placedCard)
    {
        if (placedCardMover == null || placedCard == null) return;
        
        Vector3 placedPosition = placedCardMover.transform.position;
        
        // Checking battles for placed card
        
        List<FlipTarget> flipTargets = new List<FlipTarget>();
        FlipTarget placedCardFlipTarget = null;
        
        // Find all CardMoverP1 (P1) and CardMoverP2 (P2) components on the board
        CardMoverP1[] allCardMovers = FindObjectsOfType<CardMoverP1>();
        CardMoverP2[] allCardMoverP2s = FindObjectsOfType<CardMoverP2>();
        
        // Count only cards actually on board (z ≈ 0), not in hands (z = 90)
        int cardsOnBoard = 0;
        int cardsInHands = 0;
        foreach (CardMoverP1 mover in allCardMovers)
        {
            if (mover != null)
            {
                if (Mathf.Abs(mover.transform.position.z) < 1f) cardsOnBoard++;
                else cardsInHands++;
            }
        }
        foreach (CardMoverP2 moverP2 in allCardMoverP2s)
        {
            if (moverP2 != null)
            {
                if (Mathf.Abs(moverP2.transform.position.z) < 1f) cardsOnBoard++;
                else cardsInHands++;
            }
        }
        
        Debug.Log($"[CheckCardBattlesP2] Found {allCardMovers.Length} CardMovers (P1) and {allCardMoverP2s.Length} CardMoverP2s (P2) total ({cardsOnBoard} on board, {cardsInHands} in hands)");
        
        // Log all cards on board for debugging
        if (cardsOnBoard > 0)
        {
            Debug.Log($"[CheckCardBattlesP2] Cards on board (will check for adjacency):");
            foreach (CardMoverP1 mover in allCardMovers)
            {
                if (mover != null && Mathf.Abs(mover.transform.position.z) < 1f)
                {
                    Debug.Log($"  - {mover.gameObject.name} at {mover.transform.position}");
                }
            }
            foreach (CardMoverP2 moverP2 in allCardMoverP2s)
            {
                if (moverP2 != null && Mathf.Abs(moverP2.transform.position.z) < 1f && moverP2 != placedCardMover)
                {
                    Debug.Log($"  - {moverP2.gameObject.name} at {moverP2.transform.position}");
                }
            }
        }
        
        // Check against regular CardMovers
        foreach (CardMoverP1 otherCardMover in allCardMovers)
        {
            if (otherCardMover.Card == null) continue;
            
            // CRITICAL: Skip cards in hands (z = 90) - they should never battle cards on board
            if (Mathf.Abs(otherCardMover.transform.position.z) > 10f)
            {
                continue; // Skip cards in hands
            }
            
            // HARD GUARANTEE: No comparisons unless cards are strictly adjacent
            // This prevents any distant cards (e.g., 7.66 units) from being evaluated
            float testDistance;
            if (!AreCardsStrictlyAdjacent(placedPosition, otherCardMover.transform.position, out testDistance))
            {
                // Skip this card - not adjacent, don't even check battle
                continue;
            }
            
            FlipTarget target = CheckBattleBetweenCardsForRipple(placedPosition, placedCard, otherCardMover.transform.position, otherCardMover.Card, otherCardMover.gameObject, placedCardMover.gameObject);
            if (target != null)
            {
                flipTargets.Add(target);
            }
        }
        
        // Check against P2 CardMovers
        foreach (CardMoverP2 otherCardMoverP2 in allCardMoverP2s)
        {
            // Skip self
            if (otherCardMoverP2 == placedCardMover) continue;
            if (otherCardMoverP2.Card == null) continue;
            
            // CRITICAL: Skip cards in hands (z = 90) - they should never battle cards on board
            if (Mathf.Abs(otherCardMoverP2.transform.position.z) > 10f)
            {
                continue; // Skip cards in hands
            }
            
            // HARD GUARANTEE: No comparisons unless cards are strictly adjacent
            // This prevents any distant cards (e.g., 7.66 units) from being evaluated
            float testDistance;
            if (!AreCardsStrictlyAdjacent(placedPosition, otherCardMoverP2.transform.position, out testDistance))
            {
                // Skip this card - not adjacent, don't even check battle
                continue;
            }
            
            FlipTarget target = CheckBattleBetweenCardsForRipple(placedPosition, placedCard, otherCardMoverP2.transform.position, otherCardMoverP2.Card, otherCardMoverP2.gameObject, placedCardMover.gameObject);
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
                    GetPlayerCaptureColor() : GetP2CaptureColor();
                float distance = Vector3.Distance(placedPosition, otherCardMover.transform.position);
                placedCardFlipTarget = new FlipTarget(placedCardMover.gameObject, placedCard, captureColor, CardGame.UI.FlipDirection.Right, distance, placedPosition);
                break;
            }
        }
        */
        // Check P2 cards for placed card loss
       /* if (placedCardFlipTarget == null)
        {
            foreach (CardMoverP2 otherCardMoverP2 in allCardMoverP2s)
            {
                if (otherCardMoverP2 == placedCardMover) continue;
                if (otherCardMoverP2.Card == null) continue;
                bool placedCardLost = CheckBattleBetweenCards(placedPosition, placedCard, otherCardMoverP2.transform.position, otherCardMoverP2.Card, otherCardMoverP2.gameObject, placedCardMover.gameObject);
                if (placedCardLost)
                {
                    bool winningCardIsPlayer = IsPlayerCard(otherCardMoverP2.gameObject);
                    Color captureColor = winningCardIsPlayer ? 
                        GetPlayerCaptureColor() : GetP2CaptureColor();
                    float distance = Vector3.Distance(placedPosition, otherCardMoverP2.transform.position);
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
            // Found flip targets, executing captures
            
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
        else
        {
            // No flip targets created, no captures
        }
    }
    
        /// <summary>
        /// Strict adjacency check - ensures cards are truly adjacent (4-way grid neighbors only)
        /// Returns true ONLY if cards are directly up/down/left/right and within strict distance tolerance
        /// This is the PRIMARY gatekeeper to prevent any non-adjacent comparisons
        /// </summary>
        private bool AreCardsStrictlyAdjacent(Vector3 posA, Vector3 posB, out float distance)
        {
            distance = Vector3.Distance(posA, posB);
            
            // CRITICAL: Use a strict tolerance for true adjacency
            // Cards on a 4x4 grid are typically ~2.1-2.2 units apart when adjacent (horizontal/vertical)
            // Diagonal cards would be ~2.98 units apart, so we use 2.5 to allow orthogonal neighbors only
            // This ensures cards 7.66 units apart are NEVER considered adjacent
            const float strictAdjacencyTolerance = 2.5f; // Maximum distance for true adjacency (allows ~2.12 unit grid spacing)
            
            // First check: distance must be within strict tolerance
            if (distance > strictAdjacencyTolerance)
            {
                // Only log rejections when debugBattles is enabled or when distance is interesting (>3.0f)
                // This reduces log spam for routine rejections (like cards at z=90 in hands)
                if (debugBattles || distance < 10f)
                {
                    // Distance exceeds tolerance
                }
                return false; // Too far apart - definitely not adjacent (e.g., 7.66 > 1.6 = false)
            }
        
        // Second check: cards must be aligned on same row OR same column (orthogonal neighbors only)
        Vector3 delta = posB - posA;
        float deltaX = Mathf.Abs(delta.x);
        float deltaY = Mathf.Abs(delta.y);
        
        // CRITICAL SAFETY CHECK: Even if we pass the distance check, verify distance again
        // This is a double-check to ensure we never allow cards > 1.6f to be considered adjacent
        if (distance > strictAdjacencyTolerance)
        {
            Debug.LogError($"[StrictAdjacency] CRITICAL ERROR: Distance {distance:F3} exceeded tolerance {strictAdjacencyTolerance:F3} but reached orthogonal check! Positions: A={posA}, B={posB}");
            return false; // Fail-safe: never allow cards beyond tolerance
        }
        
        // Must be aligned on one axis (same row OR same column)
        // AND must be close enough on the other axis (within 1 grid cell)
        bool sameRow = deltaY < 0.3f && deltaX > 0.1f && deltaX <= strictAdjacencyTolerance;
        bool sameCol = deltaX < 0.3f && deltaY > 0.1f && deltaY <= strictAdjacencyTolerance;
        
        bool isAdjacent = sameRow || sameCol;
        
        // CRITICAL: Final distance verification - NEVER allow cards beyond tolerance to be adjacent
        if (isAdjacent && distance > strictAdjacencyTolerance)
        {
            Debug.LogError($"[StrictAdjacency] CRITICAL ERROR: Cards passed orthogonal check but distance {distance:F3} exceeds tolerance {strictAdjacencyTolerance:F3}! Rejecting. Positions: A={posA}, B={posB}");
            return false; // Fail-safe: reject even if orthogonal check passed
        }
        
        // Only log rejections when debugBattles is enabled or for interesting cases (distance < 10f to filter out cards in hands)
        if (!isAdjacent && (debugBattles || distance < 10f))
        {
            // Not orthogonal neighbor
        }
        
        return isAdjacent;
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
                // Same player, skipping battle
            }
            return false; // Same player, no battle
        }
        
        // STRICT ADJACENCY CHECK FIRST - This is the primary gatekeeper
        // Use strict tolerance to ensure cards 7.66 units apart are NEVER compared
        float totalDistance;
        if (!AreCardsStrictlyAdjacent(placedPos, otherPos, out totalDistance))
        {
            // Always log strict adjacency rejections to help debug test failures
            // Not strictly adjacent
            return false; // Not strictly adjacent - reject immediately
        }
        
        Vector3 delta = otherPos - placedPos;
        float deltaX = Mathf.Abs(delta.x);
        float deltaY = Mathf.Abs(delta.y); // Y is vertical (up/down)
        
        if (debugBattles)
        {
            // Strict adjacency passed
        }
        
        // Only check directly adjacent cards (orthogonal neighbors)
        // Cards must be aligned on same row OR same column, and within 1 grid cell
        bool isOrthogonalNeighbor = false;
        string directionName = "";
        int placedCardStat = 0;
        int otherCardStat = 0;
        
        // Check if cards are on the same row (Y/Z aligned) - horizontal neighbors
        // Must be aligned on Y-axis AND within adjacent distance on X-axis
        // Also verify total distance is still within acceptable range (double-check)
        if (deltaY < 0.5f && deltaX > 0.1f)
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
        // Also verify total distance is still within acceptable range (double-check)
        else if (deltaX < 0.5f && deltaY > 0.1f)
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
                // Not orthogonal neighbor, skipping battle
            }
            return false;
        }
        
        // No need for additional distance check - strict adjacency check already validated this
        
        // CRITICAL LOGGING: Always log stat comparison to diagnose test failures
        // Stat comparison for battle
        
        // CRITICAL COMBAT RULE: Capture ONLY occurs when attacker's stat > defender's stat
        // placedCardStat = the stat of the card being placed (attacker)
        // otherCardStat = the opposing stat of the existing card (defender)
        // Rule: If attacker's stat <= defender's stat, NO capture should occur (defender wins or tie)
        bool attackerWins = placedCardStat > otherCardStat;
        
        if (!attackerWins)
        {
            // Defender wins or tie - NO capture should occur
            // If defender's stat > attacker's stat, defender wins and attacker should flip (if this is called from attacker's perspective)
            // If defender's stat == attacker's stat, it's a tie and no one flips
            // No capture - defender wins or tie
            if (otherCardStat > placedCardStat)
            {
                return true; // Placed card lost, should flip
            }
            else
            {
                return false; // Tie, no flip
            }
        }
        
        // Attacker wins - will capture
        
        // Placed card won - if using ripple effect, don't flip immediately (will be handled by ripple)
        // Only flip immediately if ripple effect is disabled
        if (!useRippleEffect)
        {
            // Determine capture color: The captured card gets the color of who captured it
            // Use the capturer's color (the placed card that won)
            Color captureColor = placedCardIsPlayer ? 
                GetPlayerCaptureColor() : GetP2CaptureColor();
            
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
        }
        else
        {
            // Using ripple effect - will be handled by ripple
        }
        return false; // Placed card won, don't flip it
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
        
        // Flipping card with capture color

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
        // Card captured
        
        // [CardFront] Track captures for statistics (only if it's an actual capture, not initial placement)
        if (captureColor != Color.white && captureColor != Color.clear)
        {
            gameCapturesMade++;
            // Capture made
        }
        
        // Notify ScoreManager of the capture
        // Only update score for actual captures (not initial placement)
        // Capture colors are: P1 = orange (1, 0.5, 0, 1), P2 = green (0, 0.8, 0, 1)
        // White/clear colors indicate no capture (initial placement)
        // Score update check
        
        if (captureColor != Color.white && captureColor != Color.clear)
        {
            // Use ScoreManager.Instance directly to ensure we always find it even if cached reference is null
            ScoreManager scoreMgr = scoreManager ?? ScoreManager.Instance;
            // ScoreManager lookup
            if (scoreMgr != null)
            {
                bool isPlayerCapture = IsPlayerCard(cardObject);
                // Note: The card is being captured, so the capture color determines who gets the score
                Color playerColor = GetPlayerCaptureColor();
                Color p2Color = GetP2CaptureColor();
                
                // Check if capture color matches P1 or P2 color (with tolerance for float precision)
                bool isPlayerScoring = (Mathf.Abs(captureColor.r - playerColor.r) < 0.1f &&
                                       Mathf.Abs(captureColor.g - playerColor.g) < 0.1f &&
                                       Mathf.Abs(captureColor.b - playerColor.b) < 0.1f);
                
                bool isP2Scoring = (Mathf.Abs(captureColor.r - p2Color.r) < 0.1f &&
                                         Mathf.Abs(captureColor.g - p2Color.g) < 0.1f &&
                                         Mathf.Abs(captureColor.b - p2Color.b) < 0.1f);
                
                // Only update score if we have a valid capture color match
                if (isPlayerScoring || isP2Scoring)
                {
                    int oldP1Score = scoreMgr.P1Score;
                    int oldP2Score = scoreMgr.P2Score;
                    
                    scoreMgr.AddScore(isPlayerScoring);
                    
                    // Score updated
                }
                else
                {
                    Debug.LogWarning($"[CardDropArea] Invalid capture color for {card.Data.cardName}: ({captureColor.r:F2}, {captureColor.g:F2}, {captureColor.b:F2}). " +
                        $"Expected P1: ({playerColor.r:F2}, {playerColor.g:F2}, {playerColor.b:F2}) or P2: ({p2Color.r:F2}, {p2Color.g:F2}, {p2Color.b:F2})");
                }
            }
            else
            {
                Debug.LogWarning($"[CardDropArea] ScoreManager not found! Cannot update score for capture of {card.Data.cardName}. Check if ScoreManager.Instance exists.");
            }
        }
        else
        {
            // Not a capture - just initial placement, no score update needed
            // No score update - initial placement
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
        
        // STRICT ADJACENCY CHECK FIRST - This is the primary gatekeeper
        // Use strict tolerance to ensure cards 7.66 units apart are NEVER compared
        float totalDistance;
        if (!AreCardsStrictlyAdjacent(placedPos, otherPos, out totalDistance))
        {
            // Always log strict adjacency rejections to help debug test failures
            // Not strictly adjacent
            return null; // Not strictly adjacent - reject immediately
        }
        
        Vector3 delta = otherPos - placedPos;
        float deltaX = Mathf.Abs(delta.x);
        float deltaY = Mathf.Abs(delta.y); // Y is vertical (up/down)
        
        if (debugBattles)
        {
            // Strict adjacency passed
        }
        
        // Only check directly adjacent cards (orthogonal neighbors)
        bool isOrthogonalNeighbor = false;
        string directionName = "";
        int placedCardStat = 0;
        int otherCardStat = 0;
        
        // Orthogonal neighbor check
        
        // Check if cards are on the same row (Y/Z aligned) - horizontal neighbors
        // Must be aligned on Y-axis AND within adjacent distance on X-axis
        // Also verify total distance is still within acceptable range (double-check)
        if (deltaY < 0.5f && deltaX > 0.1f)
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
            // Horizontal neighbor detected
        }
        // Check if cards are on the same column (X aligned) - vertical neighbors
        // Also verify total distance is still within acceptable range (double-check)
        else if (deltaX < 0.5f && deltaY > 0.1f)
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
            // Vertical neighbor detected
        }
        else
        {
            // Not orthogonal neighbor
        }
        
        if (!isOrthogonalNeighbor)
        {
            if (debugBattles)
            {
                // Not orthogonal neighbor, skipping battle
            }
            return null; // Not an orthogonal neighbor, no battle
        }
        
        // Stat comparison for battle
        
        // No need for additional distance check - strict adjacency check already validated this
        
        // CRITICAL COMBAT RULE: Capture ONLY occurs when attacker's stat > defender's stat
        // placedCardStat = the stat of the card being placed (attacker)
        // otherCardStat = the opposing stat of the existing card (defender)
        // If attacker's stat is NOT greater than defender's stat, NO capture should occur
        bool attackerWins = placedCardStat > otherCardStat;
        
        // ABSOLUTE SAFETY CHECK: Double-verify that attacker's stat is strictly greater
        // This prevents any possibility of creating a flip target when attacker didn't win
        if (!attackerWins || placedCardStat <= otherCardStat)
        {
            // Defender wins or tie - NO capture should occur
            // This is expected behavior during ripple effects where a captured card may not have high enough stats
            // to capture adjacent cards. Log as error for test compatibility, but this is intentional validation.
            // Note: In the error message, "Attacker" refers to placedCard (the card trying to capture) and "Defender" refers to otherCard (the card being attacked)
            Debug.LogError($"[CheckBattleBetweenCardsForRipple] ❌ LOGIC ERROR PREVENTED: Attempted to create flip target when attacker did NOT win. " +
                $"Attacker ({placedCard.Data.cardName}) stat: {placedCardStat}, Defender ({otherCard.Data.cardName}) stat: {otherCardStat}. " +
                $"Attacker must have higher stat to capture. Returning null to prevent invalid capture.");
            return null;
        }
        
        // ABSOLUTE SAFETY CHECK: Triple-verify before creating flip target
        // This is a fail-safe to ensure we never flip a card when the attacker didn't win
        // Note: This check should be redundant since we already checked above, but keeping for extra safety
        if (placedCardStat <= otherCardStat)
        {
            // This should theoretically never happen since we already checked above, but keeping as a safety net
            Debug.LogWarning($"[CheckBattleBetweenCardsForRipple] ⚠️ Redundant safety check triggered: placedCardStat ({placedCardStat}) <= otherCardStat ({otherCardStat}) but passed earlier check. " +
                $"This may indicate a logic inconsistency. Aborting flip target creation.");
            return null;
        }
        
        // If placed card wins (attacker stat > defender stat), other card should flip
        // We've verified three times that attackerWins == true and placedCardStat > otherCardStat
        if (attackerWins)
        {
            Color captureColor = placedCardIsPlayer ? 
                GetPlayerCaptureColor() : GetP2CaptureColor();
            
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
            // Creating flip target
            return new FlipTarget(otherCardObject, otherCard, captureColor, flipDir, distance, otherPos);
        }
        
        // No flip target created
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
        
        // [CardFront] Track chain length for statistics
        currentChainLength = flipTargets.Count;
        if (currentChainLength > gameLongestChain)
        {
            gameLongestChain = currentChainLength;
        }
        
        if (debugBattles)
        {
            // Starting ripple effect
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
            // Ripple effect complete
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
        // Count total CardDropArea instances (total board spaces)
        CardDropArea[] allDropAreas = FindObjectsOfType<CardDropArea>();
        int totalSpaces = allDropAreas.Length;
        
        // Count occupied spaces (spaces with cards on them)
        int occupiedSpaces = 0;
        
        foreach (CardDropArea dropArea in allDropAreas)
        {
            if (dropArea != null && dropArea.IsOccupied)
            {
                occupiedSpaces++;
            }
        }
        
        if (debugBattles)
        {
            // Board occupancy checked
        }
        
        // [CardFront] Log board occupancy (for debugging) - game ends when all cards are played, not when board is full
        if (debugBattles && occupiedSpaces >= totalSpaces && totalSpaces > 0)
        {
            // Board is full
        }
        
        // [CardFront] Game end is now checked after each card is played in OnCardDrop (P1)/OnCardDropP2 (P2)
        // Game ends when both players have no cards left (all 10 cards played), not when board is full (16/16)
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
                // Card already in current chain, skipping
            }
            return;
        }
        
        // Skip if card was played this turn (same-turn protection rule)
        if (cardsPlayedThisTurn.Contains(capturedCard))
        {
            if (debugBattles)
            {
                // Card was played this turn, cannot be captured
            }
            return;
        }
        
        // Add to current chain
        cardsInCurrentChain.Add(capturedCard);
        
        Vector3 cardPosition = capturedCard.transform.position;
        List<FlipTarget> chainFlipTargets = new List<FlipTarget>();
        
        // Find all cards on the board
        CardMoverP1[] allCardMovers = FindObjectsOfType<CardMoverP1>();
        CardMoverP2[] allCardMoverP2s = FindObjectsOfType<CardMoverP2>();
        
        // Check adjacent cards
        foreach (CardMoverP1 otherCardMover in allCardMovers)
        {
            if (otherCardMover.Card == null) continue;
            if (otherCardMover.gameObject == capturedCard) continue; // Skip self
            
            // Skip if in current chain or played this turn
            if (cardsInCurrentChain.Contains(otherCardMover.gameObject)) continue;
            if (cardsPlayedThisTurn.Contains(otherCardMover.gameObject)) continue;
            
            // HARD GUARANTEE: No comparisons unless cards are strictly adjacent
            // This prevents any distant cards (e.g., 7.66 units) from being evaluated in chain captures
            float testDistance;
            if (!AreCardsStrictlyAdjacent(cardPosition, otherCardMover.transform.position, out testDistance))
            {
                // Skip this card - not adjacent, don't even check battle
                continue;
            }
            
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
        
        foreach (CardMoverP2 otherCardMoverP2 in allCardMoverP2s)
        {
            if (otherCardMoverP2.Card == null) continue;
            if (otherCardMoverP2.gameObject == capturedCard) continue; // Skip self
            
            // Skip if in current chain or played this turn
            if (cardsInCurrentChain.Contains(otherCardMoverP2.gameObject)) continue;
            if (cardsPlayedThisTurn.Contains(otherCardMoverP2.gameObject)) continue;
            
            // HARD GUARANTEE: No comparisons unless cards are strictly adjacent
            // This prevents any distant cards (e.g., 7.66 units) from being evaluated in chain captures
            float testDistance;
            if (!AreCardsStrictlyAdjacent(cardPosition, otherCardMoverP2.transform.position, out testDistance))
            {
                // Skip this card - not adjacent, don't even check battle
                continue;
            }
            
            // Only check battles if cards belong to different players (after capture)
            bool capturedCardIsPlayer = IsPlayerCard(capturedCard);
            bool otherCardIsPlayer = IsPlayerCard(otherCardMoverP2.gameObject);
            
            // Skip if both cards belong to same player (no battle)
            if (capturedCardIsPlayer == otherCardIsPlayer) continue;
            
            FlipTarget target = CheckBattleBetweenCardsForRipple(
                cardPosition, card,
                otherCardMoverP2.transform.position, otherCardMoverP2.Card,
                otherCardMoverP2.gameObject, capturedCard);
            
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
                // Chain capture triggered
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
        
        // [CardFront] Track chain length for statistics
        currentChainLength = flipTargets.Count;
        if (currentChainLength > gameLongestChain)
        {
            gameLongestChain = currentChainLength;
        }
        
        if (debugBattles)
        {
            // Starting chain capture ripple
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
            // Chain capture ripple complete
        }
    }
    
    /// <summary>
    /// [CardFront] Delays game end check by one frame to ensure hand removal events have fully propagated
    /// </summary>
    private IEnumerator DelayedGameEndCheck()
    {
        // Wait one frame to ensure all event handlers have finished
        yield return null;
        
        if (GameEndManager.Instance != null)
        {
            GameEndManager.Instance.CheckGameEnd();
        }
    }
    
    /// <summary>
    /// Gets the current board control counts for P1 and P2
    /// Returns (p1Control, p2Control) tuple
    /// </summary>
    public static (int p1Control, int p2Control) GetBoardControl()
    {
        ScoreManager scoreManager = FindObjectOfType<ScoreManager>();
        if (scoreManager != null)
        {
            return (scoreManager.P1Score, scoreManager.P2Score);
        }
        return (0, 0);
    }
    
    /// <summary>
    /// Updates all tile colors on the board
    /// Stub method for compatibility after develop-1 revert
    /// </summary>
    public static void UpdateAllTileColors()
    {
        // This method was removed in develop-1 revert
        // Tile colors are updated automatically when cards are captured
    }
}
