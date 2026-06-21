using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using _Project.Scripts.Utility;
using Core;
using Core.Events;

/// <summary>
/// Controls the card panel's show/hide behavior and dynamic card sizing.
/// All layout components (HLG, ContentSizeFitter, Image, CanvasGroup) are pre-configured in the Editor.
/// Attach this to the CardPanel GameObject.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class CardPanelLayout : MonoBehaviour
{
    [Header("Layout Settings")]
    [SerializeField] private float cardWidth = 90f;
    [SerializeField] private float cardHeight = 120f;
    [SerializeField] private float spacing = -20f;
    [SerializeField] private float padding = 12f;

    [Header("Fade Settings")]
    [SerializeField] private float fadeDuration = 0.3f;

    private CanvasGroup _canvasGroup;
    private IEventBus _eventBus;
    private Coroutine _fadeCoroutine;

    private void Awake()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
    }

    private void Start()
    {
        if (ServiceLocator.TryGetService<IEventBus>(out _eventBus))
        {
            _eventBus.Subscribe<GamePhaseChangedEvent>(OnPhaseChanged);
        }
    }

    private void OnDestroy()
    {
        if (_eventBus != null)
        {
            _eventBus.Unsubscribe<GamePhaseChangedEvent>(OnPhaseChanged);
        }
    }

    private void OnPhaseChanged(GamePhaseChangedEvent evt)
    {
        if (evt.NewPhase == GamePhase.Draft)
            Show();
        else
            Hide();
    }

    public void Show()
    {
        gameObject.SetActive(true);
        if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
        _fadeCoroutine = StartCoroutine(FadeTo(1f));
    }

    public void Hide()
    {
        if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
        _fadeCoroutine = StartCoroutine(FadeTo(0f, deactivateOnComplete: true));
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
        _canvasGroup.interactable = target > 0.5f;
        _canvasGroup.blocksRaycasts = target > 0.5f;

        if (deactivateOnComplete && target <= 0f)
            gameObject.SetActive(false);

        _fadeCoroutine = null;
    }

    /// <summary>
    /// Ensures each card child has proper sizing via LayoutElement.
    /// Dynamically scales cards to fit the available panel width.
    /// Called by CardFactory after creating cards.
    /// </summary>
    public void RefreshCardSizes()
    {
        int activeCount = 0;
        foreach (Transform child in transform)
        {
            if (child.gameObject.activeSelf) activeCount++;
        }

        if (activeCount == 0) return;

        var rect = GetComponent<RectTransform>();
        float availableWidth = rect.rect.width;

        if (availableWidth <= 0f)
        {
            availableWidth = Screen.width * (rect.anchorMax.x - rect.anchorMin.x);
        }

        availableWidth -= padding * 2f;

        float fitWidth = (availableWidth - (activeCount - 1) * spacing) / activeCount;
        float actualWidth = Mathf.Clamp(fitWidth, 40f, cardWidth);
        float actualHeight = cardHeight * (actualWidth / cardWidth);

        foreach (Transform child in transform)
        {
            if (!child.gameObject.activeSelf) continue;

            var le = child.GetComponent<LayoutElement>();
            if (le == null) le = child.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = actualWidth;
            le.preferredHeight = actualHeight;
            le.minWidth = actualWidth;
            le.minHeight = actualHeight;
        }

        rect.sizeDelta = new Vector2(rect.sizeDelta.x, actualHeight + padding * 2f);
        LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
    }
}
