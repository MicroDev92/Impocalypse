using System.Collections;
using UnityEngine;
using _Project.Scripts.Utility;
using Cards;
using Core.Events;

/// <summary>
/// Animates card dealing and interaction feedback.
/// Listens for CardsDealtEvent to trigger staggered deal-in animations.
/// Provides select/confirm/disable feedback called by Card.cs.
///
/// Place this component in the GameScene (e.g. on the Services GameObject
/// or alongside CardFactory). Self-registers via ServiceLocator.
/// </summary>
public class CardAnimator : MonoBehaviour
{
    [Header("Deal Animation")]
    [SerializeField] private float dealStaggerDelay = 0.06f;
    [SerializeField] private float dealDuration = 0.35f;
    [SerializeField] private float dealSlideDistance = 200f;

    [Header("Confirm Animation")]
    [SerializeField] private float confirmPunchAmount = 0.15f;
    [SerializeField] private float confirmPunchDuration = 0.3f;

    [Header("Select Animation")]
    [SerializeField] private float selectBounceAmount = 0.08f;
    [SerializeField] private float selectBounceDuration = 0.2f;

    private IEventBus _eventBus;
    private Coroutine _dealSequence;

    private void Awake()
    {
        ServiceLocator.RegisterService<CardAnimator>(this);
    }

    private void Start()
    {
        if (ServiceLocator.TryGetService<IEventBus>(out _eventBus))
        {
            _eventBus.Subscribe<CardsDealtEvent>(OnCardsDealt);
        }
    }

    private void OnDestroy()
    {
        _eventBus?.Unsubscribe<CardsDealtEvent>(OnCardsDealt);
        ServiceLocator.UnregisterService<CardAnimator>();
    }

    private void OnCardsDealt(CardsDealtEvent evt)
    {
        if (_dealSequence != null)
            StopCoroutine(_dealSequence);

        _dealSequence = StartCoroutine(DealInSequence());
    }

    private IEnumerator DealInSequence()
    {
        // Wait two frames for CardFactory to instantiate cards and layout to rebuild
        yield return null;
        yield return null;

        var factory = ServiceLocator.GetService<CardFactory>();
        if (factory?.ActiveCards == null || factory.ActiveCards.Count == 0)
            yield break;

        var cards = factory.ActiveCards;

        // Hide all cards
        foreach (var card in cards)
        {
            if (card == null) continue;
            card.transform.localScale = Vector3.zero;
            GetOrAddCanvasGroup(card).alpha = 0f;
        }

        // Stagger deal-in
        for (int i = 0; i < cards.Count; i++)
        {
            if (cards[i] != null)
                StartCoroutine(AnimateCardIn(cards[i]));

            yield return new WaitForSeconds(dealStaggerDelay);
        }

        _dealSequence = null;
    }

    private IEnumerator AnimateCardIn(Card card)
    {
        var rect = card.GetComponent<RectTransform>();
        var cg = GetOrAddCanvasGroup(card);

        Vector2 targetPos = rect.anchoredPosition;
        Vector2 startPos = targetPos + Vector2.down * dealSlideDistance;
        float startRotation = Random.Range(-8f, 8f);

        rect.anchoredPosition = startPos;
        rect.localRotation = Quaternion.Euler(0f, 0f, startRotation);
        card.transform.localScale = new Vector3(0.6f, 0.6f, 1f);
        cg.alpha = 0f;

        float elapsed = 0f;
        while (elapsed < dealDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / dealDuration);
            float eased = UIAnimator.EaseOutBack(t);
            float fadeT = UIAnimator.EaseOutCubic(Mathf.Clamp01(t * 2f));

            if (rect == null) yield break;

            rect.anchoredPosition = Vector2.LerpUnclamped(startPos, targetPos, eased);
            rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.LerpUnclamped(startRotation, 0f, eased));
            card.transform.localScale = Vector3.LerpUnclamped(new Vector3(0.6f, 0.6f, 1f), Vector3.one, eased);
            cg.alpha = fadeT;

            yield return null;
        }

        if (rect == null) yield break;
        rect.anchoredPosition = targetPos;
        rect.localRotation = Quaternion.identity;
        card.transform.localScale = Vector3.one;
        cg.alpha = 1f;
    }

    // ==========================================
    // PUBLIC FEEDBACK API
    // ==========================================

    public void PlaySelectFeedback(Card card)
    {
        if (card != null)
            StartCoroutine(UIAnimator.PunchScale(card.transform, selectBounceAmount, selectBounceDuration));
    }

    public void PlayConfirmFeedback(Card card)
    {
        if (card != null)
            StartCoroutine(UIAnimator.PunchScale(card.transform, confirmPunchAmount, confirmPunchDuration));
    }

    public void PlayDisableFeedback(Card card)
    {
        if (card != null)
            StartCoroutine(UIAnimator.FadeTo(GetOrAddCanvasGroup(card), 1f, 0.5f, 0.3f));
    }

    // ==========================================
    // UTILITY
    // ==========================================

    private static CanvasGroup GetOrAddCanvasGroup(Card card)
    {
        var cg = card.GetComponent<CanvasGroup>();
        if (cg == null) cg = card.gameObject.AddComponent<CanvasGroup>();
        return cg;
    }
}
