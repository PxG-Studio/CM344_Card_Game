using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using CardGame.Managers;
using CardGame.Core;

public class CardDropArea1 : MonoBehaviour, ICardDropArea
{
    [Header("Deck Manager Reference")]
    [SerializeField] private NewDeckManager deckManager;
    [SerializeField] private NewDeckManagerOpp deckManagerOpp;

    [Header("Settings")]
    [SerializeField] private bool playCardOnDrop = true;
    [SerializeField] private bool snapCardToPosition = true;
    
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
}
