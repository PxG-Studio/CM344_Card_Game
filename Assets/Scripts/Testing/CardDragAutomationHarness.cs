using System.Collections;
using UnityEngine;

namespace CardGame.Testing
{
    /// <summary>
    /// Simple automation harness that replays deterministic drag/drop scenarios in play mode.
    /// Attach this to an empty GameObject, wire the test cards + drop areas,
    /// and set <see cref="autoRunOnStart"/> to true to execute on scene load.
    /// </summary>
    public class CardDragAutomationHarness : MonoBehaviour
    {
        [System.Serializable]
        public class DragScenario
        {
            [Tooltip("Label used in logs to identify the scenario.")]
            public string label = "Scenario";

            [Tooltip("Player-side card to move. Leave null to use the opponent card slot instead.")]
            public CardMover playerCard;

            [Tooltip("Opponent-side card to move when playerCard is null.")]
            public CardMoverOpp opponentCard;

            [Tooltip("Target drop area the card should end up on.")]
            public CardDropArea1 targetDropArea;

            [Tooltip("Optional positional offset (world units) to apply before attempting the drop.")]
            public Vector3 positionOffset = Vector3.zero;

            [Tooltip("When false the scenario will respect FateFlow turn gating.")]
            public bool bypassTurnGate = true;

            [Tooltip("How long to wait after this scenario completes before running the next one.")]
            public float postDelay = 0.25f;

            /// <summary>
            /// Returns whichever card slot is populated for this scenario.
            /// </summary>
            public Object GetCardObject()
            {
                if (playerCard != null) return playerCard;
                if (opponentCard != null) return opponentCard;
                return null;
            }
        }

        [Header("Playback Settings")]
        [SerializeField] private bool autoRunOnStart = true;
        [SerializeField, Tooltip("Extra delay inserted before the first scenario runs.")]
        private float initialDelay = 0.3f;
        [SerializeField, Tooltip("Optional gizmo drawing for scenario targets.")]
        private bool drawDebugGizmos = true;

        [Header("Scenarios")]
        [SerializeField] private DragScenario[] scenarios = System.Array.Empty<DragScenario>();

        private Coroutine automationRoutine;

        private void Start()
        {
            if (autoRunOnStart && scenarios != null && scenarios.Length > 0)
            {
                automationRoutine = StartCoroutine(RunScenarios());
            }
        }

        /// <summary>
        /// Allows QA to trigger the scripted drag run on demand (e.g. via context menu).
        /// </summary>
        [ContextMenu("Run Drag Scenarios")]
        public void TriggerOnce()
        {
            if (automationRoutine != null)
            {
                StopCoroutine(automationRoutine);
            }

            automationRoutine = StartCoroutine(RunScenarios());
        }

        private IEnumerator RunScenarios()
        {
            yield return new WaitForSeconds(initialDelay);

            if (scenarios == null || scenarios.Length == 0)
            {
                Debug.LogWarning("[CardDragAutomationHarness] No scenarios configured.");
                automationRoutine = null;
                yield break;
            }

            foreach (DragScenario scenario in scenarios)
            {
                if (scenario == null)
                {
                    continue;
                }

                bool success = ExecuteScenario(scenario);
                if (!success)
                {
                    Debug.LogWarning($"[CardDragAutomationHarness] Scenario '{scenario.label}' failed. " +
                                     "Check card references, turn gating, and drop-area colliders.");
                }

                float wait = Mathf.Max(0f, scenario.postDelay);
                if (wait > 0f)
                {
                    yield return new WaitForSeconds(wait);
                }
            }

            automationRoutine = null;
        }

        private bool ExecuteScenario(DragScenario scenario)
        {
            if (scenario.targetDropArea == null)
            {
                Debug.LogWarning($"[CardDragAutomationHarness] Scenario '{scenario.label}' has no drop area assigned.");
                return false;
            }

            Vector3 dropPoint = scenario.targetDropArea.transform.position + scenario.positionOffset;
            bool result = false;
            if (scenario.playerCard != null)
            {
                result = scenario.playerCard.AutomationAttemptDrop(dropPoint, scenario.bypassTurnGate);
            }
            else if (scenario.opponentCard != null)
            {
                result = scenario.opponentCard.AutomationAttemptDrop(dropPoint, scenario.bypassTurnGate);
            }
            else
            {
                Debug.LogWarning($"[CardDragAutomationHarness] Scenario '{scenario.label}' is missing a card reference.");
                return false;
            }

            if (result)
            {
                Debug.Log($"[CardDragAutomationHarness] Scenario '{scenario.label}' succeeded at {dropPoint}.");
            }

            return result;
        }

        private void OnDrawGizmosSelected()
        {
            if (!drawDebugGizmos || scenarios == null)
            {
                return;
            }

            Gizmos.color = new Color(1f, 0.65f, 0f, 0.6f);
            foreach (DragScenario scenario in scenarios)
            {
                if (scenario?.targetDropArea == null) continue;
                Vector3 pos = scenario.targetDropArea.transform.position + scenario.positionOffset;
                Gizmos.DrawWireSphere(pos, 0.15f);
            }
        }
    }
}

