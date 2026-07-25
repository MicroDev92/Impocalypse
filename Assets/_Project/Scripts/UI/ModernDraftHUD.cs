using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using _Project.Scripts.Utility;
using Cards;
using Core;
using Core.Events;
using UI.Abstractions;

namespace UI
{
    public class ModernDraftHUD : MonoBehaviour, IDraftHUD
    {
        [Header("Static UI References")]
        [SerializeField] private TMP_Text roundText;
        [SerializeField] private TMP_Text phaseText;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private TMP_Text draftInfoText;

        [Header("Board Resource Counts (unclaimed/total per resource on this map)")]
        [SerializeField] private TMP_Text woodAmount;
        [SerializeField] private TMP_Text carrotAmount;
        [SerializeField] private TMP_Text fishAmount;

        [Header("Settings")]
        [SerializeField] private float statusFadeDelay = 3f;
        [SerializeField] private float statusFadeDuration = 0.5f;

        private IEventBus _eventBus;
        private Coroutine _statusFadeCoroutine;

        public bool IsVisible => gameObject.activeSelf;

        private void Awake()
        {
            ServiceLocator.RegisterService<IDraftHUD>(this);
        }

        private void Start()
        {
            if (ServiceLocator.TryGetService<IEventBus>(out _eventBus))
            {
                _eventBus.Subscribe<UpdateHUDEvent>(OnUpdateHUD);
                _eventBus.Subscribe<ShowStatusEvent>(OnShowStatus);
                _eventBus.Subscribe<RoundStartedEvent>(OnRoundStarted);
                _eventBus.Subscribe<GamePhaseChangedEvent>(OnPhaseChanged);
                _eventBus.Subscribe<CardsDealtEvent>(OnCardsDealt);
                _eventBus.Subscribe<CardConfirmedEvent>(OnCardConfirmed);
                _eventBus.Subscribe<TileOwnerChangedEvent>(OnTileOwnerChanged);
            }

            // Sync initial state after a short delay to ensure GameManager is ready
            StartCoroutine(DeferredSync());
        }

        private IEnumerator DeferredSync()
        {
            yield return null; // Wait one frame for all services to register

            var gm = ServiceLocator.GetService<IGameManager>();
            if (gm != null)
            {
                UpdateHUD(gm.CurrentRound, gm.CurrentPhase, "");
                if (draftInfoText) draftInfoText.gameObject.SetActive(gm.CurrentPhase == GamePhase.Draft);
            }
            UpdateBoardCounts();
        }

        private void OnDestroy()
        {
            if (_eventBus != null)
            {
                _eventBus.Unsubscribe<UpdateHUDEvent>(OnUpdateHUD);
                _eventBus.Unsubscribe<ShowStatusEvent>(OnShowStatus);
                _eventBus.Unsubscribe<RoundStartedEvent>(OnRoundStarted);
                _eventBus.Unsubscribe<GamePhaseChangedEvent>(OnPhaseChanged);
                _eventBus.Unsubscribe<CardsDealtEvent>(OnCardsDealt);
                _eventBus.Unsubscribe<CardConfirmedEvent>(OnCardConfirmed);
                _eventBus.Unsubscribe<TileOwnerChangedEvent>(OnTileOwnerChanged);
            }

            ServiceLocator.UnregisterService<IDraftHUD>();
        }

        public void SetRound(int round)
        {
            if (roundText == null) return;
            int maxRounds = 4;
            var gm = ServiceLocator.GetService<IGameManager>();
            if (gm != null) maxRounds = gm.MaxRounds;
            roundText.text = $"Round {round} / {maxRounds}";
        }

        public void SetPhase(GamePhase phase)
        {
            if (phaseText == null) return;
            _phaseLabel = phase switch
            {
                GamePhase.Lobby => "LOBBY",
                GamePhase.Draft => "DRAFT PHASE",
                GamePhase.Build => "CONSTRUCTION",
                GamePhase.Scoring => "SCORING",
                GamePhase.GameOver => "GAME OVER",
                _ => phase.ToString().ToUpper()
            };

            // Build counts down from phase entry; draft restarts per deal (OnCardsDealt).
            if (phase == GamePhase.Build) StartCountdown(GameTiming.BuildSeconds);
            else if (phase != GamePhase.Draft) StopCountdown();

            RefreshPhaseText();
        }

        // ==========================================
        // PHASE COUNTDOWN (display only - enforcement is master-side)
        // ==========================================

        private string _phaseLabel = "";
        private float _countdown;
        private bool _countdownActive;
        private int _lastShownSecond = -1;

        private void StartCountdown(float seconds)
        {
            _countdown = seconds;
            _countdownActive = true;
            _lastShownSecond = -1;
            RefreshPhaseText();
        }

        private void StopCountdown()
        {
            _countdownActive = false;
            RefreshPhaseText();
        }

        private void Update()
        {
            if (!_countdownActive) return;

            _countdown -= Time.deltaTime;
            if (_countdown <= 0f)
            {
                _countdown = 0f;
                _countdownActive = false;
            }

            int second = Mathf.CeilToInt(_countdown);
            if (second == _lastShownSecond && _countdownActive) return; // once per second
            _lastShownSecond = second;
            RefreshPhaseText();
        }

        private void RefreshPhaseText()
        {
            if (phaseText == null) return;

            if (_countdownActive)
            {
                int t = Mathf.CeilToInt(_countdown);
                phaseText.text = $"{_phaseLabel}  {t / 60}:{t % 60:00}";
            }
            else
            {
                phaseText.text = _phaseLabel;
            }
        }

        public void SetStatus(string status)
        {
            if (statusText == null) return;
            statusText.text = status;
            statusText.alpha = 1f;
            if (_statusFadeCoroutine != null) StopCoroutine(_statusFadeCoroutine);
            if (gameObject.activeInHierarchy && !string.IsNullOrEmpty(status))
                _statusFadeCoroutine = StartCoroutine(FadeStatusAfterDelay());
        }

        private IEnumerator FadeStatusAfterDelay()
        {
            yield return new WaitForSeconds(statusFadeDelay);
            float elapsed = 0f;
            while (elapsed < statusFadeDuration)
            {
                elapsed += Time.deltaTime;
                if (statusText != null) statusText.alpha = Mathf.Lerp(1f, 0f, elapsed / statusFadeDuration);
                yield return null;
            }
            if (statusText != null) statusText.alpha = 0f;
            _statusFadeCoroutine = null;
        }

        public void UpdateHUD(int round, GamePhase phase, string status)
        {
            SetRound(round);
            SetPhase(phase);
            SetStatus(status);
        }

        public void Show() => gameObject.SetActive(true);
        public void Hide() => gameObject.SetActive(false);

        private void OnTileOwnerChanged(TileOwnerChangedEvent evt)
        {
            // A claim changes how many resource tiles are still up for grabs.
            UpdateBoardCounts();
        }

        /// <summary>
        /// Shows map-level resource availability: how many Wood/Carrot/Fish tiles this
        /// grid rolled, and how many are still unclaimed ("unclaimed/total"). Draft info -
        /// per-player counts live in the player cards, not here.
        /// </summary>
        private void UpdateBoardCounts()
        {
            var grid = ServiceLocator.GetService<IGridService>();
            if (grid == null) return;

            SetBoardCount(woodAmount, grid, TileData.TileType.Wood);
            SetBoardCount(carrotAmount, grid, TileData.TileType.Carrot);
            SetBoardCount(fishAmount, grid, TileData.TileType.Fish);
        }

        private static void SetBoardCount(TMP_Text text, IGridService grid, TileData.TileType type)
        {
            if (text == null) return;

            var tiles = grid.GetTilesByResourceType(type);
            int total = tiles.Count;
            int unclaimed = 0;
            foreach (var t in tiles)
                if (!t.IsOwned) unclaimed++;

            text.text = $"{unclaimed}/{total}";
        }

        private void OnUpdateHUD(UpdateHUDEvent evt) => UpdateHUD(evt.Round, evt.Phase, evt.Status);
        private void OnShowStatus(ShowStatusEvent evt) => SetStatus(evt.Message);
        private void OnRoundStarted(RoundStartedEvent evt) => SetRound(evt.RoundNumber);
        private void OnPhaseChanged(GamePhaseChangedEvent evt) 
        {
            SetPhase(evt.NewPhase);
            if (draftInfoText) draftInfoText.gameObject.SetActive(evt.NewPhase == GamePhase.Draft);
            UpdateBoardCounts();
        }

        private void OnCardsDealt(CardsDealtEvent evt)
        {
            UpdateDraftInfo(evt.SerializedCards?.Length ?? 0);
            StartCountdown(GameTiming.DraftPickSeconds); // fresh 45s per hand
        }
        private void OnCardConfirmed(CardConfirmedEvent evt)
        {
            if (ServiceLocator.TryGetService<CardSelectionHandler>(out var handler))
            {
                var factory = ServiceLocator.GetService<CardFactory>();
                int cardsInHand = factory?.ActiveCards?.Count ?? 0;
                UpdateDraftInfo(cardsInHand, handler.RemainingPicks);
            }
        }

        private void UpdateDraftInfo(int count, int picks = -1)
        {
            if (draftInfoText == null) return;
            if (picks == -1)
            {
                if (ServiceLocator.TryGetService<CardSelectionHandler>(out var handler))
                    picks = handler.RemainingPicks;
            }
            draftInfoText.text = $"Cards: {count} | Picks: {picks}";
        }
    }
}
