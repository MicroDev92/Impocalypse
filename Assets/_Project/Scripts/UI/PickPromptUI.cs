using TMPro;
using UnityEngine;
using UnityEngine.UI;
using _Project.Scripts.Utility;
using Cards;
using Core;
using Core.Events;

namespace UI
{
    /// <summary>
    /// The draft's call to action: "Pick 2 cards — Selected 1/2", plus the Confirm
    /// button that commits them.
    ///
    /// Picks used to submit the instant the second card was clicked, which left
    /// players unsure whether their click had registered and gave them no way to
    /// change their mind. An explicit confirm makes the commit deliberate.
    /// </summary>
    public class PickPromptUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TMP_Text promptText;
        [SerializeField] private Button confirmButton;
        [SerializeField] private TMP_Text confirmLabel;

        private IEventBus _eventBus;
        private CardSelectionHandler _selection;

        private void Start()
        {
            ServiceLocator.TryGetService(out _selection);

            if (ServiceLocator.TryGetService<IEventBus>(out _eventBus))
            {
                _eventBus.Subscribe<PickSelectionChangedEvent>(OnSelectionChanged);
                _eventBus.Subscribe<GamePhaseChangedEvent>(OnPhaseChanged);
                _eventBus.Subscribe<CardsDealtEvent>(OnCardsDealt);
            }

            if (confirmButton != null)
                confirmButton.onClick.AddListener(OnConfirmClicked);

            SetVisible(false);
        }

        private void OnDestroy()
        {
            if (_eventBus != null)
            {
                _eventBus.Unsubscribe<PickSelectionChangedEvent>(OnSelectionChanged);
                _eventBus.Unsubscribe<GamePhaseChangedEvent>(OnPhaseChanged);
                _eventBus.Unsubscribe<CardsDealtEvent>(OnCardsDealt);
            }

            if (confirmButton != null)
                confirmButton.onClick.RemoveListener(OnConfirmClicked);
        }

        private void OnPhaseChanged(GamePhaseChangedEvent evt) => SetVisible(evt.NewPhase == GamePhase.Draft);

        private void OnCardsDealt(CardsDealtEvent evt)
        {
            SetVisible(true);
            Refresh();
        }

        private void OnSelectionChanged(PickSelectionChangedEvent evt) => Refresh();

        private void OnEnable() => Refresh();

        /// <summary>
        /// Reads the handler rather than trusting an event payload.
        ///
        /// Confirm used to be driven purely by whatever the last event happened to
        /// carry, so a CardsDealtEvent arriving after a selection left the button dead
        /// while the model considered the picks perfectly submittable. That matches the
        /// intermittent "cannot confirm" from playtest 1: deriving from the single
        /// source of truth removes the whole class of drift rather than one ordering
        /// of it.
        /// </summary>
        private void Refresh()
        {
            if (_selection == null) ServiceLocator.TryGetService(out _selection);

            int required = _selection?.MaxPicks ?? 2;
            int selected = _selection?.ConfirmedCards?.Count ?? 0;
            bool canSubmit = _selection?.CanSubmit ?? false;

            if (promptText != null)
            {
                promptText.text = canSubmit
                    ? $"Confirm your {required} picks"
                    : $"Pick {required} cards — selected {selected}/{required}";
            }

            if (confirmButton != null)
            {
                confirmButton.interactable = canSubmit;
                // Dim rather than hide: a button that vanishes is a button players
                // stop looking for.
                var canvasGroup = confirmButton.GetComponent<CanvasGroup>();
                if (canvasGroup != null) canvasGroup.alpha = canSubmit ? 1f : 0.45f;
            }

            if (confirmLabel != null)
                confirmLabel.text = "Confirm Picks";
        }

        private void OnConfirmClicked()
        {
            if (_selection == null) ServiceLocator.TryGetService(out _selection);
            _selection?.SubmitPicks();
        }

        private void SetVisible(bool visible)
        {
            if (gameObject.activeSelf != visible)
                gameObject.SetActive(visible);
        }
    }
}
