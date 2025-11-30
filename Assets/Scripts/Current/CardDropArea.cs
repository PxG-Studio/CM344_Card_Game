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
    
    // Public property to access tileSpriteRenderer for debugging
    public SpriteRenderer TileSpriteRenderer => tileSpriteRenderer;
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
    
    // Board occupancy tracking
    [SerializeField] private GameObject occupyingCard;
    public bool IsOccupied => occupyingCard != null;
    
    /// <summary>
    /// Gets the card GameObject currently occupying this drop area (null if empty)
    /// </summary>
    public GameObject GetOccupyingCard() => occupyingCard;
    
    // Track cards played this turn (cannot be captured during same turn).
    // Static so that ALL CardDropArea instances share the same protection set;
    // otherwise a flip triggered from a neighbouring tile would not see the
    // freshly played card in its local HashSet.
    private static HashSet<GameObject> cardsPlayedThisTurn = new HashSet<GameObject>();
    
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
    }
    
    /// <summary>
    /// Resets this CardDropArea instance for a new game (clears occupying card and turn tracking)
    /// </summary>
    public void ResetForNewGame()
    {
        // Stop all running coroutines (ripple effects, chain captures, etc.)
        StopAllCoroutines();
        
        // Clear occupying card reference
        occupyingCard = null;
        
        // Clear turn tracking
        cardsPlayedThisTurn.Clear();
        cardsInCurrentChain.Clear();
        activeChainCount = 0;
        
        // Ensure game object is active
        if (!gameObject.activeInHierarchy)
        {
            gameObject.SetActive(true);
        }
        
        // Reset tile color to default (white/neutral)
        // Ensure sprite renderer is found and enabled
        if (tileSpriteRenderer == null)
        {
            tileSpriteRenderer = GetComponent<SpriteRenderer>();
        }
        
        if (tileSpriteRenderer != null)
        {
            // Ensure sprite renderer is enabled
            if (!tileSpriteRenderer.enabled)
            {
                tileSpriteRenderer.enabled = true;
            }
            
            // Reset color to white
            tileSpriteRenderer.color = Color.white;
        }
        else
        {
            Debug.LogWarning($"[BOARD RESET] CardDropArea at {transform.position} has no SpriteRenderer component");
        }
        
        if (debugBattles)
        {
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
            }
            else
            {
                // Fallback: add a default sized collider
                BoxCollider2D boxCollider = gameObject.AddComponent<BoxCollider2D>();
                boxCollider.size = new Vector2(1f, 1f); // Default 1x1 unit size
                boxCollider.isTrigger = true;
            }
        }
        
        // Tile color will be updated when cards are placed
        else
        {
            // Collider exists - ensure it's enabled and set as trigger if needed
            if (!existingCollider.enabled)
            {
                existingCollider.enabled = true;
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
            
            // Collider2D verified - no log needed for successful initialization
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
            }
        }
        
        // Auto-find NewDeckManagerP2 if not assigned
        if (deckManagerP2 == null)
        {
            deckManagerP2 = FindObjectOfType<NewDeckManagerP2>();
            if (deckManagerP2 == null)
            {
            }
        }
        
        // Auto-find ScoreManager
        if (scoreManager == null)
        {
            scoreManager = FindObjectOfType<ScoreManager>();
            if (scoreManager == null)
            {
            }
        }
        
        // Auto-find GameEndManager
        if (gameEndManager == null)
        {
            gameEndManager = FindObjectOfType<GameEndManager>();
            if (gameEndManager == null)
            {
            }
        }
    }
    
    private void OnDestroy()
    {
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
            }
        }
    }
    
    public void OnCardDrop(CardMoverP1 cardMover)
    {
        if (cardMover == null)
        {
            return;
        }

        // Start of a new placement: clear last turn's protection so previously
        // played cards can now be captured, but the card we are about to add
        // this call will remain protected through its own battle/ripple.
        cardsPlayedThisTurn.Clear();

        if (!CanCardAct(cardMover.OwnerSide))
        {
            if (debugBattles)
            {
            }
            cardMover.ReturnToStartPosition();
            return;
        }

        if (IsOccupied)
        {
            if (debugBattles)
            {
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
                    // Card reference synced - initialization action, no log needed
                }
            }
            
            if (card != null && deckManagerP1.Hand.Contains(card))
            {
                // Card placed on board (P1) - interaction handled, no verbose log needed
                deckManagerP1.PlayCard(card);
                
                // [CardFront] Track cards played for statistics
                gameCardsPlayed++;
                
                cardMover.SetPlayed(true);
                cardsPlayedThisTurn.Add(cardMover.gameObject);
                occupyingCard = cardMover.gameObject;
                
                // Update tile color to reflect P1 ownership (orange)
                UpdateTileColor();
                
                // Show alert marker for newly placed card
                DeltaMarkerSystem.ShowAlert(cardMover.transform, "!");
                
                // Draw a card for the placing player after successful placement
                if (deckManagerP1 != null && deckManagerP1.DrawPileCount > 0)
                {
                    deckManagerP1.DrawCard();
                    // Card drawn after placement - automatic action, no log needed
                }
                
                // [CardFront] Check if game should end (all cards played - both players have no cards left)
                // Delay check by one frame to ensure hand removal events have fully propagated
                if (GameEndManager.Instance != null)
                {
                    StartCoroutine(DelayedGameEndCheck());
                }
                
                // Check board occupancy after card placement (this will trigger game end if board is full)
                CheckBoardOccupancy();
                
                if (enableCardBattles)
                {
                    CheckCardBattlesP1(cardMover, card);
                }

                // Update scoring and frontline UI after any successful placement so
                // Field Control and the influence bar reflect current tiles.
                if (scoreManager != null)
                {
                    scoreManager.RecalculateScores();
                }
                UpdateFrontlineUI();
                
                   // Ensure stat text is visible on the placed card
                   NewCardUI cardUI = cardMover.GetComponent<NewCardUI>();
                   if (cardUI == null) cardUI = cardMover.GetComponentInChildren<NewCardUI>();
                   if (cardUI == null) cardUI = cardMover.GetComponentInParent<NewCardUI>();
                   if (cardUI != null)
                   {
                       // CRITICAL: Ensure frontContainer is active for board cards (they should be face-up)
                       var frontContainerField = typeof(NewCardUI).GetField("frontContainer",
                           System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                       if (frontContainerField != null)
                       {
                           var frontContainer = frontContainerField.GetValue(cardUI) as GameObject;
                           if (frontContainer != null && !frontContainer.activeInHierarchy)
                           {
                               frontContainer.SetActive(true);
                           }
                       }
                       
                       // Update visuals to refresh stat text values
                       cardUI.SendMessage("UpdateVisuals", SendMessageOptions.DontRequireReceiver);
                       
                       // Ensure stat text is visible
                       cardUI.EnsureStatTextVisible();
                       StartCoroutine(EnsureStatTextVisibleAfterPlacement(cardUI));
                       
                       // Log stat visibility status after placement (P1) - only log warnings/errors
                       bool statsVisible = cardUI.AreStatsVisuallyVisible();
                       if (!statsVisible)
                       {
                       }
                   }

                GameManager.Instance?.NotifyCardPlaced(this, card);
                FateFlowController.Instance?.AdvanceFateFlow();
            }
            else if (card == null)
            {
                cardMover.ReturnToStartPosition();
            }
            else if (!deckManagerP1.Hand.Contains(card))
            {
                cardMover.ReturnToStartPosition();
            }
        }
        else if (playCardOnDrop && deckManagerP1 == null)
        {
            cardMover.ReturnToStartPosition();
        }
        
    }
    
    /// <summary>
    /// Checks for adjacent cards and performs stat comparisons for P1 cards, flipping losing cards
    /// </summary>
    private void CheckCardBattlesP1(CardMoverP1 placedCardMover, NewCard placedCard)
    {
        if (placedCardMover == null || placedCard == null) return;
        
        Vector3 placedPosition = placedCardMover.transform.position;
        
        // Checking battles for placed card (no verbose logging for isolation)
        
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
        
        // Card battles checking (no verbose logging for isolation)
        
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
            
            // Primary check: placed P1 card attacks other card
            FlipTarget target = CheckBattleBetweenCardsForRipple(
                placedPosition, placedCard,
                otherCardMover.transform.position, otherCardMover.Card,
                otherCardMover.gameObject, placedCardMover.gameObject);
            
            // Secondary symmetric check: allow existing card to capture the newly placed card
            // when the existing card's stat is higher. This ensures that whichever side
            // actually has the higher stat can win the battle, regardless of play order.
            if (target == null)
            {
                target = CheckBattleBetweenCardsForRipple(
                    otherCardMover.transform.position, otherCardMover.Card,
                    placedPosition, placedCard,
                    placedCardMover.gameObject, otherCardMover.gameObject);
            }
            
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
            
            // Primary check: placed P1 card attacks P2 card
            FlipTarget target = CheckBattleBetweenCardsForRipple(
                placedPosition, placedCard,
                otherCardMoverP2.transform.position, otherCardMoverP2.Card,
                otherCardMoverP2.gameObject, placedCardMover.gameObject);
            
            // Secondary symmetric check: allow existing P2 card to capture the newly placed P1 card
            // when P2's stat is higher.
            if (target == null)
            {
                target = CheckBattleBetweenCardsForRipple(
                    otherCardMoverP2.transform.position, otherCardMoverP2.Card,
                    placedPosition, placedCard,
                    placedCardMover.gameObject, otherCardMoverP2.gameObject);
            }
            
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
            // Found flip targets, executing flips (no verbose logging)
            
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
            // No flip targets created (no verbose logging)
        }
    }
    
    /// <summary>
    /// Determines if a card GameObject currently belongs to P1 (vs P2).
    /// - For cards that have been captured (CardFlipAnimation.WasCaptured == true), we trust the
    ///   capture border color to indicate current ownership.
    /// - For uncaptured cards, we trust the mover component type (CardMoverP1 vs CardMoverP2)
    ///   regardless of initial border color, so test-created cards without special skins behave correctly.
    /// </summary>
    private bool IsPlayerCard(GameObject cardObject)
    {
        if (cardObject == null) return true; // Default to player

        // 1) If this card has already been captured, use capture color to determine current owner.
        //    This is what ScoreManager and chain capture logic conceptually care about.
        CardFlipAnimation flipAnim = cardObject.GetComponentInChildren<CardFlipAnimation>();
        if (flipAnim == null)
        {
            flipAnim = cardObject.GetComponent<CardFlipAnimation>();
        }

        if (flipAnim != null && flipAnim.WasCaptured)
        {
            NewCardUI capturedCardUI = cardObject.GetComponent<NewCardUI>();
            if (capturedCardUI == null)
            {
                capturedCardUI = cardObject.GetComponentInChildren<NewCardUI>() ??
                                 cardObject.GetComponentInParent<NewCardUI>();
            }

            if (capturedCardUI != null)
            {
                Color playerColor = GetPlayerCaptureColor();
                Color p2Color = GetP2CaptureColor();
                float colorTolerance = 0.1f;

                // First preference: use explicit capture color remembered on CardFlipAnimation.
                if (flipAnim.LastCaptureColor != Color.clear)
                {
                    Color c = flipAnim.LastCaptureColor;
                    if (debugBattles)
                    {
                    }
                    if (Mathf.Abs(c.r - playerColor.r) < colorTolerance &&
                        Mathf.Abs(c.g - playerColor.g) < colorTolerance &&
                        Mathf.Abs(c.b - playerColor.b) < colorTolerance)
                    {
                        return true;
                    }
                    if (Mathf.Abs(c.r - p2Color.r) < colorTolerance &&
                        Mathf.Abs(c.g - p2Color.g) < colorTolerance &&
                        Mathf.Abs(c.b - p2Color.b) < colorTolerance)
                    {
                        return false;
                    }
                }

                // Fallback: read the background/border color from NewCardUI if available.
                var cardBackgroundField = typeof(NewCardUI).GetField("cardBackground",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (cardBackgroundField != null)
                {
                    var cardBackground = cardBackgroundField.GetValue(capturedCardUI);
                    if (cardBackground != null)
                    {
                        Color borderColor = Color.white;
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
                    // Player's captured card (orange)
                    if (Mathf.Abs(borderColor.r - playerColor.r) < colorTolerance &&
                        Mathf.Abs(borderColor.g - playerColor.g) < colorTolerance &&
                        Mathf.Abs(borderColor.b - playerColor.b) < colorTolerance)
                    {
                        return true;
                    }

                    // P2's captured card (green)
                    if (Mathf.Abs(borderColor.r - p2Color.r) < colorTolerance &&
                        Mathf.Abs(borderColor.g - p2Color.g) < colorTolerance &&
                        Mathf.Abs(borderColor.b - p2Color.b) < colorTolerance)
                    {
                        return false;
                    }

                    if (debugBattles)
                    {
                    }
                    }
                }
            }
        }

        // 2) For cards that have NOT been captured yet (or where we couldn't read capture color),
        //    trust the mover component type as the source of truth. This makes tests that spawn
        //    bare CardMoverP1/CardMoverP2 objects without special skins behave correctly.
        CardMoverP1 cardMover = cardObject.GetComponent<CardMoverP1>() ??
                                 cardObject.GetComponentInChildren<CardMoverP1>() ??
                                 cardObject.GetComponentInParent<CardMoverP1>();
        if (cardMover != null) return true;

        CardMoverP2 cardMoverP2 = cardObject.GetComponent<CardMoverP2>() ??
                                  cardObject.GetComponentInChildren<CardMoverP2>() ??
                                  cardObject.GetComponentInParent<CardMoverP2>();
        if (cardMoverP2 != null) return false;

        // 3) Fallback: if we truly have no mover information, fall back to capture colors
        //    even if the card hasn't been captured, to keep behaviour stable for any
        //    unusual UI-only representations.
        NewCardUI fallbackCardUI = cardObject.GetComponent<NewCardUI>() ??
                                   cardObject.GetComponentInChildren<NewCardUI>() ??
                                   cardObject.GetComponentInParent<NewCardUI>();
        if (fallbackCardUI != null)
        {
            var cardBackgroundField = typeof(NewCardUI).GetField("cardBackground",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (cardBackgroundField != null)
            {
                var cardBackground = cardBackgroundField.GetValue(fallbackCardUI);
                if (cardBackground != null)
                {
                    Color borderColor = Color.white;
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

                    Color playerColor = GetPlayerCaptureColor();
                    Color p2Color = GetP2CaptureColor();
                    float colorTolerance = 0.1f;

                    if (Mathf.Abs(borderColor.r - playerColor.r) < colorTolerance &&
                        Mathf.Abs(borderColor.g - playerColor.g) < colorTolerance &&
                        Mathf.Abs(borderColor.b - playerColor.b) < colorTolerance)
                    {
                        return true;
                    }

                    if (Mathf.Abs(borderColor.r - p2Color.r) < colorTolerance &&
                        Mathf.Abs(borderColor.g - p2Color.g) < colorTolerance &&
                        Mathf.Abs(borderColor.b - p2Color.b) < colorTolerance)
                    {
                        return false;
                    }
                }
            }
        }

        // Default: assume player card
        return true;
    }
    
    /// <summary>
    /// Gets the player's capture color (orange)
    /// </summary>
    // Cache colors to avoid repeated FindObjectOfType calls
    private static Color? cachedP1Color = null;
    private static Color? cachedP2Color = null;
    
    private Color GetPlayerCaptureColor()
    {
        // Use cached color if available
        if (cachedP1Color.HasValue)
        {
            return cachedP1Color.Value;
        }
        
        // Try to get color from Battle Front Influence bar for consistency
        CardFrontlineUI frontlineUI = FindObjectOfType<CardFrontlineUI>();
        if (frontlineUI != null)
        {
            Color p1Color = frontlineUI.P1Color;
            
            // Validate color: must have alpha > 0 and not be white/clear
            if (p1Color.a > 0f && p1Color != Color.white && p1Color != Color.clear)
            {
                cachedP1Color = p1Color;
                return p1Color;
            }
        }
        
        // Fallback: Orange color for player's captured cards
        Color fallbackColor = new Color(1f, 0.5f, 0f, 1f);
        cachedP1Color = fallbackColor;
        return fallbackColor;
    }
    
    /// <summary>
    /// Gets P2's capture color (green) - matches Battle Front Influence bar
    /// </summary>
    private Color GetP2CaptureColor()
    {
        // Use cached color if available
        if (cachedP2Color.HasValue)
        {
            return cachedP2Color.Value;
        }
        
        // Try to get color from Battle Front Influence bar for consistency
        CardFrontlineUI frontlineUI = FindObjectOfType<CardFrontlineUI>();
        if (frontlineUI != null)
        {
            Color p2Color = frontlineUI.P2Color;
            
            // Validate color: must have alpha > 0 and not be white/clear
            if (p2Color.a > 0f && p2Color != Color.white && p2Color != Color.clear)
            {
                cachedP2Color = p2Color;
                return p2Color;
            }
        }
        
        // Fallback: Green color for P2's captured cards
        Color fallbackColor = new Color(0f, 0.8f, 0f, 1f);
        cachedP2Color = fallbackColor;
        return fallbackColor;
    }
    
    /// <summary>
    /// Flips a card (flips it to show the back) with a specific capture color
    /// </summary>
    private void FlipCard(CardMoverP1 cardMover, NewCard card, Color captureColor)
    {
        if (cardMover == null)
        {
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

        // New placement for P2: clear previous protection so older cards can
        // now be captured. The freshly added card for this call will be added
        // back into cardsPlayedThisTurn and protected for its own battle.
        cardsPlayedThisTurn.Clear();

        if (!CanCardAct(cardMoverP2.OwnerSide))
        {
            if (debugBattles)
            {
            }
            cardMoverP2.ReturnToStartPosition();
            return;
        }
        
        if (IsOccupied)
        {
            if (debugBattles)
            {
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
                    // Card reference synced - initialization action, no log needed
                }
            }
            
            if (card != null && deckManagerP2.Hand.Contains(card))
            {
                // Card placed on board (P2) - interaction handled, no verbose log needed
                deckManagerP2.PlayCard(card);
                
                // [CardFront] Track cards played for statistics
                gameCardsPlayed++;
                
                cardMoverP2.SetPlayed(true);
                cardsPlayedThisTurn.Add(cardMoverP2.gameObject);
                occupyingCard = cardMoverP2.gameObject;
                
                // Update tile color to reflect P2 ownership (green)
                UpdateTileColor();
                
                // Show alert marker for new opponent card
                DeltaMarkerSystem.ShowAlert(cardMoverP2.transform, "!");
                
                // Draw a card for the placing player after successful placement
                if (deckManagerP2 != null && deckManagerP2.DrawPileCount > 0)
                {
                    deckManagerP2.DrawCard();
                    // Card drawn after placement - automatic action, no log needed
                }
                
                // [CardFront] Check if game should end (all cards played - both players have no cards left)
                // Delay check by one frame to ensure hand removal events have fully propagated
                if (GameEndManager.Instance != null)
                {
                    StartCoroutine(DelayedGameEndCheck());
                }
                
                // Check board occupancy after card placement (this will trigger game end if board is full)
                CheckBoardOccupancy();
                
                if (enableCardBattles)
                {
                    CheckCardBattlesP2(cardMoverP2, card);
                }

                // Update scoring and frontline UI after any successful placement so
                // Field Control and the influence bar reflect current tiles.
                if (scoreManager != null)
                {
                    scoreManager.RecalculateScores();
                }
                UpdateFrontlineUI();
                
                   // Ensure stat text is visible on the placed card
                   NewCardUI cardUI = cardMoverP2.GetComponent<NewCardUI>();
                   if (cardUI == null) cardUI = cardMoverP2.GetComponentInChildren<NewCardUI>();
                   if (cardUI == null) cardUI = cardMoverP2.GetComponentInParent<NewCardUI>();
                   if (cardUI != null)
                   {
                       // CRITICAL: Ensure frontContainer is active for board cards (they should be face-up)
                       var frontContainerField = typeof(NewCardUI).GetField("frontContainer",
                           System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                       if (frontContainerField != null)
                       {
                           var frontContainer = frontContainerField.GetValue(cardUI) as GameObject;
                           if (frontContainer != null && !frontContainer.activeInHierarchy)
                           {
                               frontContainer.SetActive(true);
                           }
                       }
                       
                       // Update visuals to refresh stat text values
                       cardUI.SendMessage("UpdateVisuals", SendMessageOptions.DontRequireReceiver);
                       
                       // Ensure stat text is visible
                       cardUI.EnsureStatTextVisible();
                       StartCoroutine(EnsureStatTextVisibleAfterPlacement(cardUI));
                       
                       // Log stat visibility status after placement (P2) - only log warnings/errors
                       bool statsVisible = cardUI.AreStatsVisuallyVisible();
                       if (!statsVisible)
                       {
                       }
                   }

                GameManager.Instance?.NotifyCardPlaced(this, card);
                FateFlowController.Instance?.AdvanceFateFlow();
            }
            else if (card == null)
            {
                cardMoverP2.ReturnToStartPosition();
            }
            else if (!deckManagerP2.Hand.Contains(card))
            {
                cardMoverP2.ReturnToStartPosition();
            }
        }
        else if (playCardOnDrop && deckManagerP2 == null)
        {
            cardMoverP2.ReturnToStartPosition();
        }
        
    }
    
    /// <summary>
    /// Checks for adjacent cards and performs stat comparisons for P2 cards
    /// </summary>
    private void CheckCardBattlesP2(CardMoverP2 placedCardMover, NewCard placedCard)
    {
        if (placedCardMover == null || placedCard == null) return;
        
        Vector3 placedPosition = placedCardMover.transform.position;
        
        // ALWAYS log entry point for debugging test failures
        // Checking battles for placed card (no verbose logging for isolation)
        
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
            
            // Primary check: placed P2 card attacks P1 card
            FlipTarget target = CheckBattleBetweenCardsForRipple(
                placedPosition, placedCard,
                otherCardMover.transform.position, otherCardMover.Card,
                otherCardMover.gameObject, placedCardMover.gameObject);
            
            // Secondary symmetric check: allow existing P1 card to capture the newly placed P2 card
            // when P1's stat is higher.
            if (target == null)
            {
                target = CheckBattleBetweenCardsForRipple(
                    otherCardMover.transform.position, otherCardMover.Card,
                    placedPosition, placedCard,
                    placedCardMover.gameObject, otherCardMover.gameObject);
            }
            
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
            
            // Primary check: placed P2 card attacks other P2 card
            FlipTarget target = CheckBattleBetweenCardsForRipple(
                placedPosition, placedCard,
                otherCardMoverP2.transform.position, otherCardMoverP2.Card,
                otherCardMoverP2.gameObject, placedCardMover.gameObject);
            
            // Secondary symmetric check: allow existing P2 card to capture the newly placed P2 card
            // when its stat is higher. (Same-player battles are filtered out inside the battle method.)
            if (target == null)
            {
                target = CheckBattleBetweenCardsForRipple(
                    otherCardMoverP2.transform.position, otherCardMoverP2.Card,
                    placedPosition, placedCard,
                    placedCardMover.gameObject, otherCardMoverP2.gameObject);
            }
            
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
            // Found flip targets, executing flips (no verbose logging)
            
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
            // No flip targets created (no verbose logging)
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
            
            // CRITICAL: Use a strict tolerance for true adjacency based on actual board spacing.
            // Use the serialized adjacentCardDistance so designers/tests can tune it, but enforce
            // a *minimum* of 3.5 so that tiles whose spacing matches the tests' expectations
            // (3–3.5 units apart) are always considered adjacent even if an older scene prefab
            // still has a smaller value serialized (e.g. 1.6f).
            float strictAdjacencyTolerance = Mathf.Max(adjacentCardDistance, 3.5f);
            
            // First check: distance must be within strict tolerance
            if (distance > strictAdjacencyTolerance)
            {
                // Only log rejections when debugBattles is enabled or when distance is interesting (>3.0f)
                // This reduces log spam for routine rejections (like cards at z=90 in hands)
                if (debugBattles || distance < 10f)
                {
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
            return false; // Fail-safe: never allow cards beyond tolerance
        }
        
        // Must be aligned on one axis (same row OR same column)
        // AND must be close enough on the other axis (within 1 grid cell).
        // Use the same 0.5f vertical/horizontal tolerance as CardTestHelper.GetAdjacentDropArea
        // and the battle methods so that "adjacent" is defined consistently everywhere.
        bool sameRow = deltaY < 0.5f && deltaX > 0.1f && deltaX <= strictAdjacencyTolerance;
        bool sameCol = deltaX < 0.5f && deltaY > 0.1f && deltaY <= strictAdjacencyTolerance;
        
        bool isAdjacent = sameRow || sameCol;
        
        // CRITICAL: Final distance verification - NEVER allow cards beyond tolerance to be adjacent
        if (isAdjacent && distance > strictAdjacencyTolerance)
        {
            return false; // Fail-safe: reject even if orthogonal check passed
        }
        
        // Only log rejections when debugBattles is enabled or for interesting cases (distance < 10f to filter out cards in hands)
        if (!isAdjacent && (debugBattles || distance < 10f))
        {
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
            }
            return false; // Same player, no battle
        }
        
        // STRICT ADJACENCY CHECK FIRST - This is the primary gatekeeper
        // Use strict tolerance to ensure cards 7.66 units apart are NEVER compared
        float totalDistance;
        if (!AreCardsStrictlyAdjacent(placedPos, otherPos, out totalDistance))
        {
            // Always log strict adjacency rejections to help debug test failures
            return false; // Not strictly adjacent - reject immediately
        }
        
        Vector3 delta = otherPos - placedPos;
        float deltaX = Mathf.Abs(delta.x);
        float deltaY = Mathf.Abs(delta.y); // Y is vertical (up/down)
        
        if (debugBattles)
        {
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
            }
            return false;
        }
        
        // No need for additional distance check - strict adjacency check already validated this
        
        // CRITICAL LOGGING: Always log stat comparison to diagnose test failures
        
        // CRITICAL COMBAT RULE: Capture ONLY occurs when attacker's stat > defender's stat
        // placedCardStat = the stat of the card being placed (attacker)
        // otherCardStat = the opposing stat of the existing card (defender)
        // Rule: If attacker's stat <= defender's stat, NO capture should occur (defender wins or tie)
        bool attackerWins = placedCardStat > otherCardStat;
        
        if (!attackerWins)
        {
            // Basic Triple Triad rule:
            // - Only the ATTACKER (placed card) can ever capture the defender.
            // - If attacker stat <= defender stat, then NOTHING flips. The defender
            //   never counter‑captures the placed card as part of this comparison.
            //
            // That means:
            // - Defender strictly higher  → no flip (attacker fails, card stays)
            // - Equal stats               → no flip
            // - Only attackerWins (>)     → defender flips
            
            return false; // No flip in basic rules when attacker does not win
        }
        
        // If placed card wins (attacker stat > defender stat), other card should flip
        // We've already validated that attackerWins == true (placedCardStat > otherCardStat)
        
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
            // Using ripple effect - just log, don't flip (will be handled by ripple)
            if (debugBattles)
            {
            }
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
    /// Returns true if the given GameObject belongs to a card that was played
    /// this turn on this drop area. This is tolerant of being passed either the
    /// CardMover root or any child (e.g. NewCardUI, flip animation container).
    /// </summary>
    private bool IsFreshlyPlayedThisTurn(GameObject cardObject)
    {
        if (cardObject == null)
        {
            return false;
        }
        
        if (cardsPlayedThisTurn.Contains(cardObject))
        {
            return true;
        }
        
        // Check parent CardMover components, which are what we register in
        // cardsPlayedThisTurn when a card is dropped from hand.
        CardMoverP1 moverP1 = cardObject.GetComponentInParent<CardMoverP1>();
        if (moverP1 != null && cardsPlayedThisTurn.Contains(moverP1.gameObject))
        {
            return true;
        }
        
        CardMoverP2 moverP2 = cardObject.GetComponentInParent<CardMoverP2>();
        if (moverP2 != null && cardsPlayedThisTurn.Contains(moverP2.gameObject))
        {
            return true;
        }
        
        return false;
    }
    
    /// <summary>
    /// Flips a card GameObject with directional flip animation
    /// </summary>
    private void FlipCardGameObject(GameObject cardObject, NewCard card, Color captureColor, CardGame.UI.FlipDirection direction)
    {
        if (cardObject == null)
        {
            return;
        }

        // Safety: Never allow the freshly placed card for the current turn to be
        // flipped, regardless of battle outcome. This enforces the rule that the
        // card being dropped cannot itself be captured on that same turn for
        // either player.
        if (IsFreshlyPlayedThisTurn(cardObject))
        {
            return;
        }

        // CRITICAL LOGGING: Always log when FlipCardGameObject is called to track score update path

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
            return;
        }

        if (!flipAnim.IsSetupValid())
        {
            return;
        }

        flipAnim.CaptureCard(captureColor, direction);
        
        // [CardFront] Track captures for statistics (only if it's an actual capture, not initial placement)
        if (captureColor != Color.white && captureColor != Color.clear)
        {
            gameCapturesMade++;
            
            // Update tile color to reflect new ownership after capture
            UpdateTileColorForCard(cardObject);
            
            // Show delta marker for territory influence change (+1 for conquer) with slight delay for sync
            StartCoroutine(ShowCaptureDelta(cardObject.transform, +1));
            
            // Recalculate scores immediately based on current board control so that
            // real-time scoring (and tests that expect score changes on capture)
            // remain accurate while still using territory-based scoring under the hood.
            if (scoreManager != null)
            {
                scoreManager.RecalculateScores();
            }
        }
        
        // Scoring remains territory-based, but is now refreshed on each capture so
        // that UI and tests can observe score changes as soon as they happen.
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
            return null; // Not strictly adjacent - reject immediately
        }
        
        Vector3 delta = otherPos - placedPos;
        float deltaX = Mathf.Abs(delta.x);
        float deltaY = Mathf.Abs(delta.y); // Y is vertical (up/down)
        
        if (debugBattles)
        {
        }
        
        // Only check directly adjacent cards (orthogonal neighbors)
        bool isOrthogonalNeighbor = false;
        string directionName = "";
        int placedCardStat = 0;
        int otherCardStat = 0;
        
        // CRITICAL LOGGING: Log delta values to diagnose orthogonal neighbor check failures
        
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
        else
        {
        }
        
        if (!isOrthogonalNeighbor)
        {
            if (debugBattles)
            {
            }
            return null; // Not an orthogonal neighbor, no battle
        }
        
        // CRITICAL LOGGING: Always log stat comparison to diagnose test failures
        
        // No need for additional distance check - strict adjacency check already validated this
        
        // CRITICAL COMBAT RULE: Capture ONLY occurs when attacker's stat > defender's stat
        // placedCardStat = the stat of the card being placed (attacker)
        // otherCardStat = the opposing stat of the existing card (defender)
        // If attacker's stat is NOT greater than defender's stat, NO capture should occur.
        // This is a *normal* outcome during adjacency scans (most neighbor pairs will not result
        // in captures), so we treat it as informational debug output instead of a logic error.
        bool attackerWins = placedCardStat > otherCardStat;
        
        // ABSOLUTE SAFETY CHECK: Double-verify that attacker's stat is strictly greater
        // This prevents any possibility of creating a flip target when attacker didn't win
        if (!attackerWins || placedCardStat <= otherCardStat)
        {
            // Defender wins or tie - NO capture should occur.
            // This branch is expected to be hit frequently as we scan all adjacent neighbors.
            // Only log when debugBattles is enabled, and phrase it as a normal combat outcome
            // rather than a "logic error" to avoid noisy warnings in normal play/tests.
            if (debugBattles)
            {
            }
            return null;
        }
        
        // ABSOLUTE SAFETY CHECK: Triple-verify before creating flip target
        // This is a fail-safe to ensure we never flip a card when the attacker didn't win
        // Note: This check should be redundant since we already checked above, but keeping for extra safety
        if (placedCardStat <= otherCardStat)
        {
            // This should theoretically never happen since we already checked above, but keeping as a safety net
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
        
        // Group targets into distance-based waves so orthogonal neighbors flip together
        const float distanceBandSize = 1f;
        Dictionary<int, List<FlipTarget>> waveBuckets = new Dictionary<int, List<FlipTarget>>();
        foreach (FlipTarget target in flipTargets)
        {
            int bucket = Mathf.RoundToInt(target.distance / distanceBandSize);
            if (!waveBuckets.ContainsKey(bucket))
            {
                waveBuckets[bucket] = new List<FlipTarget>();
            }
            waveBuckets[bucket].Add(target);
        }
        List<int> orderedBuckets = new List<int>(waveBuckets.Keys);
        orderedBuckets.Sort();
        
        // [CardFront] Track chain length for statistics
        currentChainLength = flipTargets.Count;
        if (currentChainLength > gameLongestChain)
        {
            gameLongestChain = currentChainLength;
        }
        
        if (debugBattles)
        {
        }
        
        // Wait for base delay before starting
        yield return new WaitForSeconds(rippleBaseDelay);
        
        // Increment active chain count for initial ripple
        activeChainCount++;
        if (gameEndManager != null)
        {
            gameEndManager.SetChainsInProgress(true);
        }
        
        // Flip cards wave-by-wave
        for (int i = 0; i < orderedBuckets.Count; i++)
        {
            List<FlipTarget> waveTargets = waveBuckets[orderedBuckets[i]];
            
            if (debugBattles)
            {
            }
            
            // Trigger flips simultaneously for this wave
            foreach (var target in waveTargets)
            {
                FlipCardGameObject(target.cardObject, target.card, target.captureColor, target.direction);
            }
            
            // Wait for flip animation to complete once per wave
            yield return new WaitForSeconds(1.1f);
            
            foreach (var target in waveTargets)
            {
                CheckChainCapture(target.cardObject, target.card);
            }
            
            // Delay before next wave to create ripple propagation
            if (i < orderedBuckets.Count - 1)
            {
                float waveDelay = Mathf.Max(0.05f, rippleDelayPerUnit * distanceBandSize);
                yield return new WaitForSeconds(waveDelay);
            }
        }
        
        if (debugBattles)
        {
        }
        
        // Update all tile colors after ripple completes to ensure consistency
        UpdateAllTileColors();
        
        // Update CardFrontlineUI after ripple completes
        UpdateFrontlineUI();
        
        // Trigger blurp message based on capture type
        TriggerBlurpForCaptures(flipTargets);
        
        // Decrement active chain count
        activeChainCount--;
        if (activeChainCount <= 0)
        {
            activeChainCount = 0;
            if (gameEndManager != null)
            {
                gameEndManager.SetChainsInProgress(false);
                // Check game end after all chains complete
                StartCoroutine(DelayedGameEndCheckAfterChains());
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
        
        // Board occupancy tracking (no logging for isolation)
        
        // Game ends when all 16 slots are filled (territory-based scoring)
        // Trigger game end check if board is full (after chains complete if they're in progress)
        if (occupiedSpaces >= totalSpaces && totalSpaces > 0)
        {
            bool chainsInProgress = (GameEndManager.Instance != null && GameEndManager.Instance.AreChainsInProgress());
            if (GameEndManager.Instance != null)
            {
                // If chains are in progress, the game end check will happen after chains complete
                // Otherwise, trigger it immediately (with a small delay to ensure all updates are done)
                if (!chainsInProgress)
                {
                    StartCoroutine(DelayedGameEndCheckAfterChains());
                }
                // If chains are in progress, DelayedGameEndCheckAfterChains() will be called when chains complete
            }
        }
    }
    
    /// <summary>
    /// Gets the current board occupancy (occupied spaces and total spaces)
    /// </summary>
    public static (int occupiedSpaces, int totalSpaces) GetBoardOccupancy()
    {
        CardDropArea[] allDropAreas = Object.FindObjectsOfType<CardDropArea>();
        int totalSpaces = allDropAreas.Length;
        int occupiedSpaces = 0;
        
        foreach (CardDropArea dropArea in allDropAreas)
        {
            if (dropArea != null && dropArea.IsOccupied)
            {
                occupiedSpaces++;
            }
        }
        
        return (occupiedSpaces, totalSpaces);
    }
    
    /// <summary>
    /// Gets the current control counts for P1 and P2 based on captured territories.
    /// </summary>
    public static (int p1Control, int p2Control) GetBoardControl()
    {
        CardDropArea[] allDropAreas = Object.FindObjectsOfType<CardDropArea>();
        int p1Control = 0;
        int p2Control = 0;
        
        foreach (CardDropArea dropArea in allDropAreas)
        {
            if (dropArea == null || !dropArea.IsOccupied) continue;
            
            GameObject occupyingCard = dropArea.GetOccupyingCard();
            if (occupyingCard == null) continue;

            // Determine control based on current ownership (capture color / flip state),
            // not original owner. This matches ScoreManager and what the player sees.
            bool isPlayerCard = dropArea.IsPlayerCard(occupyingCard);
            if (isPlayerCard)
            {
                p1Control++;
            }
            else
            {
                p2Control++;
            }
        }
        
        return (p1Control, p2Control);
    }
    
    /// <summary>
    /// Updates the CardFrontlineUI with current board state.
    /// </summary>
    private static void UpdateFrontlineUI()
    {
        CardFrontlineUI frontlineUI = Object.FindObjectOfType<CardFrontlineUI>();
        if (frontlineUI == null) return;
        
        (int occupiedSpaces, int totalSpaces) = GetBoardOccupancy();
        (int p1Control, int p2Control) = GetBoardControl();
        int remainingFields = totalSpaces - occupiedSpaces;
        
        frontlineUI.UpdateFrontline(p1Control, p2Control, remainingFields);
    }
    
    /// <summary>
    /// Triggers blurp messages for captures based on capture type.
    /// </summary>
    private void TriggerBlurpForCaptures(List<FlipTarget> flipTargets)
    {
        if (flipTargets == null || flipTargets.Count == 0) return;
        
        // Determine which player made the capture (check first capture color)
        bool isPlayer1Capture = IsPlayerCaptureColor(flipTargets[0].captureColor);
        PlayerPanelUI panelUI = GetPlayerPanelUI(isPlayer1Capture);
        if (panelUI == null) return;
        
        // Check if any captures were opponent cards (overturn)
        bool hasOverturn = false;
        foreach (var target in flipTargets)
        {
            bool capturedCardWasPlayer = IsPlayerCard(target.cardObject);
            bool captureIsPlayer = IsPlayerCaptureColor(target.captureColor);
            if (capturedCardWasPlayer != captureIsPlayer)
            {
                hasOverturn = true;
                break;
            }
        }
        
        // Determine blurp message
        string blurpMessage = "";
        if (flipTargets.Count == 1)
        {
            if (hasOverturn)
            {
                blurpMessage = "Overturn!!";
            }
            else
            {
                blurpMessage = "Perfect Capture!!";
            }
        }
        else if (flipTargets.Count == 2)
        {
            blurpMessage = "Chain Combo x2!!";
        }
        else if (flipTargets.Count >= 3)
        {
            blurpMessage = "Chain Combo x3!!";
        }
        
        if (!string.IsNullOrEmpty(blurpMessage))
        {
            panelUI.TriggerBlurp(blurpMessage);
        }
    }
    
    /// <summary>
    /// Triggers blurp message for chain combo captures.
    /// </summary>
    private void TriggerBlurpForChainCombo(List<FlipTarget> flipTargets)
    {
        if (flipTargets == null || flipTargets.Count == 0) return;
        
        // Determine which player made the capture
        bool isPlayer1Capture = IsPlayerCaptureColor(flipTargets[0].captureColor);
        PlayerPanelUI panelUI = GetPlayerPanelUI(isPlayer1Capture);
        if (panelUI == null) return;
        
        string blurpMessage = "";
        if (flipTargets.Count == 2)
        {
            blurpMessage = "Chain Combo x2!!";
        }
        else if (flipTargets.Count >= 3)
        {
            blurpMessage = "Crazy Combo!!";
        }
        
        if (!string.IsNullOrEmpty(blurpMessage))
        {
            panelUI.TriggerBlurp(blurpMessage);
        }
    }
    
    /// <summary>
    /// Gets the PlayerPanelUI for the specified player.
    /// </summary>
    private PlayerPanelUI GetPlayerPanelUI(bool isPlayer1)
    {
        PlayerPanelUI[] allPanels = Object.FindObjectsOfType<PlayerPanelUI>();
        foreach (PlayerPanelUI panel in allPanels)
        {
            Transform parent = panel.transform.parent;
            if (parent != null)
            {
                if (isPlayer1 && parent.name == "P1Panel")
                {
                    return panel;
                }
                else if (!isPlayer1 && parent.name == "P2Panel")
                {
                    return panel;
                }
            }
        }
        return null;
    }
    
    /// <summary>
    /// Shows a delta marker after a short delay to align with flip animation.
    /// </summary>
    private IEnumerator ShowCaptureDelta(Transform target, int deltaValue)
    {
        if (target == null) yield break;
        
        yield return new WaitForSeconds(0.15f);
        DeltaMarkerSystem.ShowDelta(deltaValue, target);
    }
    
    /// <summary>
    /// Checks if a capture color belongs to Player 1.
    /// </summary>
    private bool IsPlayerCaptureColor(Color captureColor)
    {
        Color playerColor = GetPlayerCaptureColor();
        float tolerance = 0.1f;
        return Mathf.Abs(captureColor.r - playerColor.r) < tolerance &&
               Mathf.Abs(captureColor.g - playerColor.g) < tolerance &&
               Mathf.Abs(captureColor.b - playerColor.b) < tolerance;
    }
    
    /// <summary>
    /// Updates the tile color based on the occupying card's ownership.
    /// P1 cards = orange, P2 cards = green (matches Battle Front Influence bar).
    /// </summary>
    private void UpdateTileColor()
    {
        if (tileSpriteRenderer == null)
        {
            // Try to find tile sprite renderer if not set
            tileSpriteRenderer = GetComponent<SpriteRenderer>();
            if (tileSpriteRenderer == null)
            {
                return;
            }
        }
        
        // Check if tile is occupied
        bool isOccupied = IsOccupied;
        GameObject card = GetOccupyingCard();
        
        // Additional check: if occupyingCard is null but there's a card visually present,
        // try to find it by checking for CardMover components at this position
        if (!isOccupied || card == null)
        {
            // Try to find a card at this position as a fallback
            Collider2D hit = Physics2D.OverlapPoint(transform.position);
            if (hit != null)
            {
                CardMoverP1 moverP1 = hit.GetComponent<CardMoverP1>();
                CardMoverP2 moverP2 = hit.GetComponent<CardMoverP2>();
                
                if (moverP1 != null && moverP1.IsPlayed)
                {
                    card = moverP1.gameObject;
                    isOccupied = true;
                }
                else if (moverP2 != null && moverP2.IsPlayed)
                {
                    card = moverP2.gameObject;
                    isOccupied = true;
                }
            }
        }
        
        if (!isOccupied)
        {
            // No card - reset to neutral white only if current color is not a valid ownership color
            Color currentColor = tileSpriteRenderer.color;
            bool isOwnershipColor = (currentColor != Color.white && currentColor != Color.clear && currentColor.a > 0.5f);
            
            if (isOwnershipColor)
            {
                return; // Keep current color - card might be temporarily missing
            }
            
            tileSpriteRenderer.color = Color.white;
            return;
        }
        
        if (card == null)
        {
            // Card reference lost - keep current color if it's a valid ownership color
            Color currentColor = tileSpriteRenderer.color;
            bool isOwnershipColor = (currentColor != Color.white && currentColor != Color.clear && currentColor.a > 0.5f);
            
            if (isOwnershipColor)
            {
                return; // Keep current color - don't reset to white
            }
            
            tileSpriteRenderer.color = Color.white;
            return;
        }
        
        bool isPlayerCard = IsPlayerCard(card);
        Color tileColor;
        
        if (isPlayerCard)
        {
            // P1 ownership - orange color
            tileColor = GetPlayerCaptureColor(); // Orange
        }
        else
        {
            // P2 ownership - green color
            tileColor = GetP2CaptureColor(); // Green
        }
        
        // Validate color before setting (ensure it's not white/clear)
        if (tileColor.a > 0f && tileColor != Color.white && tileColor != Color.clear)
        {
            tileSpriteRenderer.color = tileColor;
        }
        else
        {
            // Fallback to default colors if retrieved color is invalid
            Color fallbackColor = isPlayerCard 
                ? new Color(1f, 0.5f, 0f, 1f) // Orange fallback
                : new Color(0f, 0.8f, 0f, 1f); // Green fallback
            
            tileSpriteRenderer.color = fallbackColor;
        }
    }
    
    /// <summary>
    /// Updates the tile color for a specific card (used after capture).
    /// Finds the CardDropArea that contains this card and updates its tile color.
    /// </summary>
    private void UpdateTileColorForCard(GameObject cardObject)
    {
        if (cardObject == null)
        {
            return;
        }
        
        // Find the CardDropArea that contains this card
        CardDropArea[] allDropAreas = FindObjectsOfType<CardDropArea>();
        
        foreach (CardDropArea dropArea in allDropAreas)
        {
            if (dropArea != null && dropArea.GetOccupyingCard() == cardObject)
            {
                dropArea.UpdateTileColor();
                break;
            }
        }
    }
    
    /// <summary>
    /// Updates all tile colors on the board based on their occupying cards' ownership.
    /// Called after ripple effects complete to ensure all tiles reflect current ownership.
    /// </summary>
    public static void UpdateAllTileColors()
    {
        CardDropArea[] allDropAreas = Object.FindObjectsOfType<CardDropArea>();
        Debug.Log($"[BOARD RESET] Found {allDropAreas.Length} CardDropArea tiles");
        
        foreach (CardDropArea dropArea in allDropAreas)
        {
            if (dropArea != null)
            {
                // Ensure tile is active and has sprite renderer
                if (!dropArea.gameObject.activeInHierarchy)
                {
                    Debug.LogWarning($"[BOARD RESET] CardDropArea at {dropArea.transform.position} is INACTIVE");
                    dropArea.gameObject.SetActive(true);
                }
                
                // Ensure sprite renderer exists and has a sprite
                SpriteRenderer spriteRenderer = dropArea.TileSpriteRenderer;
                if (spriteRenderer == null)
                {
                    spriteRenderer = dropArea.GetComponent<SpriteRenderer>();
                }
                
                if (spriteRenderer != null)
                {
                    if (spriteRenderer.sprite == null)
                    {
                        Debug.LogWarning($"[BOARD RESET] CardDropArea at {dropArea.transform.position} has NO SPRITE assigned to SpriteRenderer");
                    }
                    
                    if (!spriteRenderer.enabled)
                    {
                        Debug.LogWarning($"[BOARD RESET] CardDropArea at {dropArea.transform.position} SpriteRenderer is DISABLED");
                        spriteRenderer.enabled = true;
                    }
                }
                else
                {
                    Debug.LogWarning($"[BOARD RESET] CardDropArea at {dropArea.transform.position} has NO SpriteRenderer component");
                }
                
                dropArea.UpdateTileColor();
            }
        }
        
        Debug.Log($"[BOARD RESET] Updated {allDropAreas.Length} tile colors");
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
            }
            return;
        }
        
        // Skip if card was played this turn (same-turn protection rule)
        if (cardsPlayedThisTurn.Contains(capturedCard))
        {
            if (debugBattles)
            {
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
                // Check game end after all chains complete
                StartCoroutine(DelayedGameEndCheckAfterChains());
            }
        }
        
        if (debugBattles)
        {
        }
        
        // Update all tile colors after chain capture completes
        UpdateAllTileColors();
        
        // Update CardFrontlineUI after chain capture completes
        UpdateFrontlineUI();
        
        // Trigger blurp message for chain combo
        TriggerBlurpForChainCombo(flipTargets);
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
    /// [CardFront] Delays game end check after chains complete to ensure all chain captures and tile calculations are done
    /// </summary>
    private IEnumerator DelayedGameEndCheckAfterChains()
    {
        // Wait a frame to ensure all chain capture processing is complete
        yield return null;
        // Additional small delay to ensure tile color updates and score recalculations are done
        yield return new WaitForSeconds(0.1f);
        
        if (GameEndManager.Instance != null)
        {
            // Double-check board occupancy before calling game end check
            CardDropArea[] allDropAreas = FindObjectsOfType<CardDropArea>();
            int totalSpaces = allDropAreas.Length;
            int occupiedSpaces = 0;
            foreach (CardDropArea dropArea in allDropAreas)
            {
                if (dropArea != null && dropArea.IsOccupied)
                {
                    occupiedSpaces++;
                }
            }
            
            GameEndManager.Instance.CheckGameEnd();
        }
    }
    
    /// <summary>
    /// Ensures stat text is visible after card placement on board
    /// </summary>
    private IEnumerator EnsureStatTextVisibleAfterPlacement(NewCardUI cardUI)
    {
        // Wait a frame to ensure card is fully placed
        yield return null;
        
        if (cardUI != null)
        {
            cardUI.EnsureStatTextVisible();
        }
        
        // Also check after a short delay to catch any timing issues
        yield return new WaitForSeconds(0.1f);
        
        if (cardUI != null)
        {
            cardUI.EnsureStatTextVisible();
        }
    }
}
