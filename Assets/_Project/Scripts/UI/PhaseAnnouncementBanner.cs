using System.Collections;
using TMPro;
using UnityEngine;
using Core.Events;
using _Project.Scripts.Utility;
using Core;

namespace UI
{
    public class PhaseAnnouncementBanner : MonoBehaviour
    {
        [SerializeField] private TMP_Text phaseText;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private float displayDuration = 2f;
        [SerializeField] private float fadeDuration = 0.5f;

        private IEventBus _eventBus;

        private void Start()
        {
            if (canvasGroup != null) canvasGroup.alpha = 0f;
            
            if (ServiceLocator.TryGetService<IEventBus>(out _eventBus))
            {
                _eventBus.Subscribe<GamePhaseChangedEvent>(OnPhaseChanged);
            }
        }

        private void OnDestroy()
        {
            _eventBus?.Unsubscribe<GamePhaseChangedEvent>(OnPhaseChanged);
        }

        private void OnPhaseChanged(GamePhaseChangedEvent evt)
        {
            StopAllCoroutines();
            StartCoroutine(ShowBanner(evt.NewPhase));
        }

        private IEnumerator ShowBanner(GamePhase phase)
        {
            if (phaseText != null)
            {
                phaseText.text = phase switch
                {
                    GamePhase.Draft => "DRAFT PHASE",
                    GamePhase.Build => "CONSTRUCTION",
                    GamePhase.Scoring => "SCORING",
                    GamePhase.GameOver => "GAME OVER",
                    _ => phase.ToString().ToUpper()
                };
            }

            // Fade In
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeDuration);
                yield return null;
            }
            canvasGroup.alpha = 1f;

            yield return new WaitForSeconds(displayDuration);

            // Fade Out
            elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
                yield return null;
            }
            canvasGroup.alpha = 0f;
        }
    }
}
