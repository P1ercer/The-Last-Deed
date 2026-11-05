using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GameAssets.Scripts
{
    public class DecisionStamp : MonoBehaviour
    {
        public enum Decision
        {
            Accept,
            Decline
        }

        [Header("Decision")]
        [SerializeField] private NpcQueueManager queueManager;
        [SerializeField] private Decision decision;

        [Header("Hover Interaction")]
        [SerializeField] private XRBaseInteractable stampInteractable;
        [SerializeField] private InputActionReference stampInput;
        [SerializeField] private float npcHoverDistance = 1.5f;

        [Header("Gizmo")]
        [SerializeField] private bool showGizmoDuringPlay = true;
        [SerializeField] private bool showGizmoInEditMode = true;
        [SerializeField] private float gizmoTextHeight = 0.25f;
        [SerializeField] private float gizmoTextSize = 18f;
        [SerializeField] private float gizmoRadius = 1.5f;

        private void Awake()
        {
            FindStampInteractable();

            if (stampInteractable == null)
            {
                Debug.LogWarning(
                    "DecisionStamp could not find an XRBaseInteractable on this object, its children, or its parent.",
                    this);
            }
        }

        private void OnEnable()
        {
            if (stampInput != null && stampInput.action != null)
            {
                stampInput.action.performed += OnStampInputPerformed;
            }
        }

        private void OnDisable()
        {
            if (stampInput != null && stampInput.action != null)
            {
                stampInput.action.performed -= OnStampInputPerformed;
            }
        }

        private void OnStampInputPerformed(InputAction.CallbackContext context)
        {
            if (stampInteractable == null ||
                !stampInteractable.isHovered)
            {
                return;
            }

            if (!IsNpcCloseEnough())
            {
                return;
            }

            UseStamp();
        }

        private bool IsNpcCloseEnough()
        {
            if (queueManager == null)
            {
                return false;
            }

            NpcBehavior currentNpc = queueManager.GetCurrentNpc();

            if (currentNpc == null)
            {
                return false;
            }

            float distance = Vector3.Distance(
                transform.position,
                currentNpc.transform.position);

            return distance <= npcHoverDistance;
        }

        public void UseStamp()
        {
            if (queueManager == null)
            {
                Debug.LogWarning(
                    "DecisionStamp requires an NpcQueueManager reference.",
                    this);

                return;
            }

            if (!IsNpcCloseEnough())
            {
                return;
            }

            if (decision == Decision.Accept)
            {
                queueManager.AcceptNpc();
            }
            else
            {
                queueManager.DeclineNpc();
            }
        }

        private void FindStampInteractable()
        {
            if (stampInteractable != null)
            {
                return;
            }

            stampInteractable = GetComponent<XRBaseInteractable>();

            if (stampInteractable == null)
            {
                stampInteractable = GetComponentInChildren<XRBaseInteractable>();
            }

            if (stampInteractable == null)
            {
                stampInteractable = GetComponentInParent<XRBaseInteractable>();
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            FindStampInteractable();
        }

        private void OnDrawGizmos()
        {
            if (!Application.isPlaying && !showGizmoInEditMode)
            {
                return;
            }

            if (Application.isPlaying && !showGizmoDuringPlay)
            {
                return;
            }

            Color gizmoColor = decision == Decision.Accept
                ? Color.green
                : Color.red;

            string label = decision == Decision.Accept
                ? "ACCEPT"
                : "DECLINE";

            Gizmos.color = new Color(
                gizmoColor.r,
                gizmoColor.g,
                gizmoColor.b,
                0.25f);

            Gizmos.DrawWireSphere(
                transform.position,
                gizmoRadius);

            GUIStyle labelStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = Mathf.Max(1, Mathf.RoundToInt(gizmoTextSize)),
                alignment = TextAnchor.MiddleCenter
            };

            labelStyle.normal.textColor = gizmoColor;

            Vector3 labelPosition =
                transform.position +
                Vector3.up * gizmoTextHeight;

            Handles.Label(labelPosition, label, labelStyle);
        }
#endif
    }
}