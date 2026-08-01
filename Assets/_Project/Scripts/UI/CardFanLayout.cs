using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using _Project.Scripts.Utility;
using Core;
using Core.Events;
using Cards;

/// <summary>
/// Arranges child cards in a hand-held fan/arc shape.
/// Center card is straight, outer cards fan outward with rotation.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class CardFanLayout : MonoBehaviour
{
    [Header("Fan Shape")]
    [SerializeField] private float fanAngle = 30f;
    [SerializeField] private float arcHeight = 70f;
    [SerializeField] private float cardSpacing = 90f;
    [SerializeField] private float maxFanWidth = 700f;

    [Header("Position")]
    [Tooltip("How high above the bottom of the screen the hand sits. Increase to lift the fan clear of the board.")]
    [SerializeField] private float bottomOffset = 130f;

    [Header("Visibility")]
    [SerializeField] private float fadeDuration = 0.3f;

    private CanvasGroup _canvasGroup;
    private RectTransform _rect;
    private IEventBus _eventBus;
    private Coroutine _fadeCoroutine;

    private void Awake()
    {
        _rect = GetComponent<RectTransform>();
        _canvasGroup = GetComponent<CanvasGroup>();
        if (_canvasGroup == null)
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();

        // Anchor the hand to bottom-center. A zero-height rect with a centered pivot
        // means the cards (center-anchored in RefreshLayout) sit exactly bottomOffset
        // above the screen bottom - tune bottomOffset to lift the fan clear of the board.
        _rect.anchorMin = new Vector2(0.5f, 0f);
        _rect.anchorMax = new Vector2(0.5f, 0f);
        _rect.pivot = new Vector2(0.5f, 0.5f);
        _rect.anchoredPosition = new Vector2(0, bottomOffset);
        _rect.sizeDelta = new Vector2(maxFanWidth, 0f);
    }

    private void Start()
    {
        if (ServiceLocator.TryGetService<IEventBus>(out _eventBus))
        {
            _eventBus.Subscribe<GamePhaseChangedEvent>(OnPhaseChanged);
            _eventBus.Subscribe<CardsDealtEvent>(OnCardsDealt);
        }

        StartCoroutine(DeferredSync());
    }

    private IEnumerator DeferredSync()
    {
        yield return null;
        var gm = ServiceLocator.GetService<IGameManager>();
        if (gm != null)
        {
            if (gm.CurrentPhase == GamePhase.Draft) Show();
            else Hide();
        }
    }

    private void OnDestroy()
    {
        if (_eventBus != null)
        {
            _eventBus.Unsubscribe<GamePhaseChangedEvent>(OnPhaseChanged);
            _eventBus.Unsubscribe<CardsDealtEvent>(OnCardsDealt);
        }
    }

    private void OnPhaseChanged(GamePhaseChangedEvent evt)
    {
        if (evt.NewPhase == GamePhase.Draft) Show();
        else Hide();
    }

    private void OnCardsDealt(CardsDealtEvent evt)
    {
        StartCoroutine(RefreshAfterFrame());
    }

    private IEnumerator RefreshAfterFrame()
    {
        yield return null;
        yield return null;
        RefreshLayout();
    }

    public void RefreshLayout()
    {
        var activeChildren = new List<Transform>();
        foreach (Transform child in transform)
        {
            if (child.gameObject.activeSelf) activeChildren.Add(child);
        }

        if (activeChildren.Count == 0) return;

        // Sort children by coordinate for stable fan order
        activeChildren.Sort((a, b) => {
            var cardA = a.GetComponent<Card>();
            var cardB = b.GetComponent<Card>();
            if (cardA != null && cardB != null) return string.Compare(cardA.coordinate, cardB.coordinate);
            return string.Compare(a.name, b.name);
        });

        int count = activeChildren.Count;
        float totalWidth = (count - 1) * cardSpacing;
        float effectiveSpacing = totalWidth > maxFanWidth ? maxFanWidth / (count - 1) : cardSpacing;
        float startAngle = count > 1 ? fanAngle / 2f : 0f;

        for (int index = 0; index < count; index++)
        {
            var child = activeChildren[index];

            // Fan math is measured from the hand's center, so each card must be
            // center-anchored. The card prefab is corner-anchored (0,0), which would
            // shove the whole fan off to the left - force center anchoring here.
            var childRect = child.GetComponent<RectTransform>();
            if (childRect != null)
            {
                childRect.anchorMin = childRect.anchorMax = new Vector2(0.5f, 0.5f);
                childRect.pivot = new Vector2(0.5f, 0.5f);
            }

            float normalized = count > 1 ? (2f * index / (count - 1)) - 1f : 0f;
            float x = count > 1 ? (index - (count - 1) * 0.5f) * effectiveSpacing : 0f;
            float y = arcHeight * (1f - normalized * normalized);
            float angle = count > 1 ? Mathf.Lerp(startAngle, -startAngle, (float)index / (count - 1)) : 0f;

            var card = child.GetComponent<Card>();
            if (card != null)
            {
                card.SetLayoutPosition(new Vector2(x, y), Quaternion.Euler(0f, 0f, angle));
            }
            else
            {
                var rt = child.GetComponent<RectTransform>();
                rt.anchoredPosition = new Vector2(x, y);
                rt.localRotation = Quaternion.Euler(0f, 0f, angle);
            }

        }

        ApplyFanDepth(activeChildren);
    }

    /// <summary>
    /// Draw order for the fan: outermost cards at the back, centre card on top.
    ///
    /// This must be a STRICT TOTAL ORDER. The previous version derived the sibling
    /// index from distance-to-centre directly, which handed symmetric pairs (e.g.
    /// index 5 and 7 of 12) the same value - and repeated SetSiblingIndex calls with
    /// the same index make Unity reshuffle siblings, so a card could end up drawn
    /// behind both of its neighbours. Sorting first and assigning sequentially in a
    /// separate pass keeps every index unique and avoids mutating sibling order
    /// while positioning.
    /// </summary>
    private static void ApplyFanDepth(List<Transform> cards)
    {
        int count = cards.Count;
        float centre = (count - 1) * 0.5f;

        var drawOrder = new List<int>(count);
        for (int i = 0; i < count; i++) drawOrder.Add(i);

        drawOrder.Sort((a, b) =>
        {
            int byDistance = Mathf.Abs(b - centre).CompareTo(Mathf.Abs(a - centre)); // farthest first
            return byDistance != 0 ? byDistance : a.CompareTo(b);                    // stable tie-break
        });

        for (int depth = 0; depth < count; depth++)
        {
            var child = cards[drawOrder[depth]];
            child.SetSiblingIndex(depth);
            child.GetComponent<Card>()?.SetRestSiblingIndex(depth);
        }
    }

    public void RefreshCardSizes() => RefreshLayout();

    public void Show()
    {
        gameObject.SetActive(true);
        if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
        _fadeCoroutine = StartCoroutine(FadeTo(1f));
    }

    public void Hide()
    {
        if (!gameObject.activeInHierarchy) return;
        if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
        _fadeCoroutine = StartCoroutine(FadeTo(0f, true));
    }

    private IEnumerator FadeTo(float target, bool deactivateOnComplete = false)
    {
        if (_canvasGroup == null) yield break;
        float start = _canvasGroup.alpha;
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            _canvasGroup.alpha = Mathf.Lerp(start, target, elapsed / fadeDuration);
            yield return null;
        }
        _canvasGroup.alpha = target;
        _canvasGroup.interactable = _canvasGroup.blocksRaycasts = target > 0.5f;
        if (deactivateOnComplete && target <= 0f) gameObject.SetActive(false);
        _fadeCoroutine = null;
    }
}
