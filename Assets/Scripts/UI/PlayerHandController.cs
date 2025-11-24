using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using CardGame.Core;
using CardGame.Managers;
using CardGame.Factories;

namespace CardGame.UI
{
    /// <summary>
    /// Shared hand controller that manages Player 1 hand visuals.
    /// NewHandP1UI now derives from this class to keep backwards compatibility.
    /// </summary>
    public class PlayerHandController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private NewCardUI cardPrefab;
        [SerializeField] private Transform cardContainer;
        [SerializeField] private RectTransform handContainer;
        [SerializeField] private FanHandUI fanLayout;

        [Header("Fan Layout Settings")]
        [SerializeField] private float legacyCardSpacing = 180f; // kept only so old scenes don't lose values
        [SerializeField] private float legacyMaxSpread = 900f;
        [SerializeField] private float legacyArcHeight = 80f;
        [SerializeField] private float legacyRotationAngle = 12f;
        [SerializeField] private Color handHighlightColor = new Color(0.98f, 0.78f, 0.19f, 1f);

        protected readonly List<NewCardUI> cardUIList = new();
        protected NewDeckManagerP1 deckManager;

        private static NewCardUI staticCardPrefab;
        private bool isSubscribed;

        public NewDeckManagerP1 DeckManager => deckManager;

        public static NewCardUI DefaultCardPrefab => staticCardPrefab;

        protected virtual void Awake()
        {
            CacheCardPrefabReference();
            SetupHandHierarchy();
        }

        protected virtual void Start()
        {
            deckManager = FindObjectOfType<NewDeckManagerP1>();
            if (deckManager != null)
            {
                deckManager.OnCardDrawn += HandleCardDrawn;
                deckManager.OnCardPlayed += HandleCardPlayed;
                deckManager.OnCardDiscarded += HandleCardDiscarded;
                isSubscribed = true;
            }
            else
            {
                Debug.LogWarning("[P1HandController] Unable to find NewDeckManagerP1 in scene.");
            }
        }

        protected virtual void OnDestroy()
        {
            if (deckManager != null && isSubscribed)
            {
                deckManager.OnCardDrawn -= HandleCardDrawn;
                deckManager.OnCardPlayed -= HandleCardPlayed;
                deckManager.OnCardDiscarded -= HandleCardDiscarded;
            }

            cardUIList.Clear();
        }

        public NewCard GetCardForUI(NewCardUI cardUI)
        {
            if (cardUI == null)
            {
                return null;
            }

            if (cardUIList.Contains(cardUI) && cardUI.Card != null)
            {
                return cardUI.Card;
            }

            int index = cardUIList.IndexOf(cardUI);
            if (index >= 0 && deckManager != null && deckManager.Hand != null && index < deckManager.Hand.Count)
            {
                return deckManager.Hand[index];
            }

            return null;
        }

        public int GetCardCount()
        {
            return cardUIList.Count;
        }

        public NewCard GetCardForUIByIndex(int index)
        {
            if (index >= 0 && index < cardUIList.Count)
            {
                NewCardUI cardUI = cardUIList[index];
                if (cardUI != null)
                {
                    if (cardUI.Card != null)
                    {
                        return cardUI.Card;
                    }

                    if (deckManager != null && deckManager.Hand != null && index < deckManager.Hand.Count)
                    {
                        return deckManager.Hand[index];
                    }
                }
            }

            return null;
        }

        public virtual void AddCardToHand(NewCard card)
        {
            if (card == null)
            {
                Debug.LogError("[P1HandController] Cannot add null card to hand.");
                return;
            }

            NewCardUI prefab = ResolveCardPrefab();
            Transform parent = cardContainer != null ? cardContainer : transform;

            if (prefab == null || parent == null)
            {
                Debug.LogError("[P1HandController] Missing prefab or parent container for card UI.");
                return;
            }

            float revealDelay = 0f;
            if (cardUIList.Count > 0 && prefab.autoFlipOnReveal)
            {
                revealDelay = cardUIList.Count * 0.1f;
            }

            NewCardUI cardUI = CardFactory.CreateCardUI(card, prefab, parent, revealDelay);
            if (cardUI == null || cardUI.Card == null)
            {
                Debug.LogError("[P1HandController] Failed to create card UI.");
                return;
            }

            cardUI.OnCardPlayed += HandleCardUIPlayed;
            cardUI.ApplyHandStyle(handHighlightColor);
            cardUIList.Add(cardUI);
            Debug.Log($"[P1HandController] Added '{card.Data.cardName}' to hand container '{handContainer?.name}' (card parent: '{cardUI.transform.parent?.name}')");
            ArrangeCards();
        }

        public virtual void RemoveCardFromHand(NewCard card)
        {
            if (card == null)
            {
                return;
            }

            NewCardUI cardUI = cardUIList.Find(c => c != null && c.Card != null && c.Card.InstanceID == card.InstanceID);
            if (cardUI == null)
            {
                return;
            }

            CleanupCardUI(cardUI);
            cardUIList.Remove(cardUI);
            ArrangeCards();
        }

        public virtual void ClearHand()
        {
            foreach (NewCardUI cardUI in cardUIList)
            {
                if (cardUI != null)
                {
                    CleanupCardUI(cardUI);
                    Destroy(cardUI.gameObject);
                }
            }

            cardUIList.Clear();
            ArrangeCards();
        }

        protected virtual void ArrangeCards()
        {
            if (fanLayout == null)
            {
                SetupHandHierarchy();
            }

            fanLayout?.UpdateFan();
        }

        private void HandleCardDrawn(NewCard card)
        {
            AddCardToHand(card);
        }

        private void HandleCardPlayed(NewCard card)
        {
            RemoveCardFromHand(card);
        }

        private void HandleCardDiscarded(NewCard card)
        {
            RemoveCardFromHand(card);
        }

        private void HandleCardUIPlayed(NewCardUI cardUI)
        {
            if (deckManager == null || cardUI == null || cardUI.Card == null)
            {
                return;
            }

            deckManager.PlayCard(cardUI.Card);
            ApplyCardEffects(cardUI.Card);
        }

        private void ApplyCardEffects(NewCard card)
        {
            if (card?.Data?.effects == null)
            {
                return;
            }

            foreach (var effect in card.Data.effects)
            {
                Debug.Log($"Applying effect: {effect.effectType} with value {effect.effectValue}");
            }
        }

        private void CleanupCardUI(NewCardUI cardUI)
        {
            cardUI.OnCardPlayed -= HandleCardUIPlayed;
            cardUI.ResetVisualStyle();
        }

        private void CacheCardPrefabReference()
        {
            if (cardPrefab != null && staticCardPrefab == null)
            {
                staticCardPrefab = cardPrefab;
            }
            else if (cardPrefab == null && staticCardPrefab != null)
            {
                cardPrefab = staticCardPrefab;
            }
        }

        private NewCardUI ResolveCardPrefab()
        {
            if (cardPrefab != null)
            {
                return cardPrefab;
            }

            if (staticCardPrefab != null)
            {
                cardPrefab = staticCardPrefab;
            }

            return cardPrefab;
        }

        private void SetupHandHierarchy()
        {
            Debug.Log("[P1HandController] SetupHandHierarchy starting...");
            if (handContainer == null)
            {
                handContainer = EnsureHandContainer(null);
            }
            else
            {
                ConfigureHandContainer(handContainer);
            }

            if (handContainer != null)
            {
                cardContainer = EnsureFanLayout(handContainer).transform;
            }

            if (fanLayout == null && cardContainer != null)
            {
                fanLayout = cardContainer.GetComponent<FanHandUI>();
            }

            if (fanLayout != null)
            {
                fanLayout.autoUpdate = false;
            }

            Debug.Log($"[P1HandController] Hand container: {handContainer?.name}, card container: {cardContainer?.name}, fan layout: {fanLayout?.name}");
        }

        private RectTransform EnsureHandContainer(RectTransform overrideParent)
        {
            RectTransform parent = overrideParent != null ? overrideParent : FindDefaultParent();
            if (parent == null)
            {
                return null;
            }

            RectTransform container = parent.Find("P1HandContainer") as RectTransform;
            if (container == null)
            {
                GameObject go = new GameObject("P1HandContainer", typeof(RectTransform));
                go.transform.SetParent(parent, false);
                container = go.GetComponent<RectTransform>();
            }

            ConfigureHandContainer(container);
            return container;
        }

        private RectTransform FindDefaultParent()
        {
            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                return transform as RectTransform;
            }

            Transform newCardRoot = canvas.transform.Find("NewCardUI");
            RectTransform target = newCardRoot != null
                ? newCardRoot.GetComponent<RectTransform>()
                : canvas.GetComponent<RectTransform>();

            return target;
        }

        private void ConfigureHandContainer(RectTransform container)
        {
            container.anchorMin = new Vector2(0.5f, 0f);
            container.anchorMax = new Vector2(0.5f, 0f);
            container.pivot = new Vector2(0.5f, 0f);
            container.anchoredPosition = new Vector2(0f, 60f);
            container.sizeDelta = new Vector2(0f, 300f);

            CanvasGroup cg = container.GetComponent<CanvasGroup>() ?? container.gameObject.AddComponent<CanvasGroup>();
            cg.alpha = 1f;
            cg.blocksRaycasts = true;
            cg.interactable = true;

            HorizontalLayoutGroup layout = container.GetComponent<HorizontalLayoutGroup>() ?? container.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.enabled = false;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            ContentSizeFitter fitter = container.GetComponent<ContentSizeFitter>() ?? container.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;
        }

        private RectTransform EnsureFanLayout(RectTransform container)
        {
            RectTransform fanRoot = container.Find("FanLayout") as RectTransform;
            if (fanRoot == null)
            {
                GameObject go = new GameObject("FanLayout", typeof(RectTransform));
                go.transform.SetParent(container, false);
                fanRoot = go.GetComponent<RectTransform>();
            }

            fanRoot.anchorMin = new Vector2(0.5f, 0f);
            fanRoot.anchorMax = new Vector2(0.5f, 0f);
            fanRoot.pivot = new Vector2(0.5f, 0f);
            fanRoot.anchoredPosition = Vector2.zero;
            fanRoot.sizeDelta = Vector2.zero;

            FanHandUI fan = fanRoot.GetComponent<FanHandUI>();
            if (fan == null)
            {
                fan = fanRoot.gameObject.AddComponent<FanHandUI>();
            }

            return fanRoot;
        }
    }
}

