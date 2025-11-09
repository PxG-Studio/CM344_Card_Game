using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CardDropArea1 : MonoBehaviour, ICardDropArea
{
    public void OnCardDrop(CardMover card)
    {
            card.transform.position = transform.position;
            Debug.Log("Card dropped here");
    }
}
