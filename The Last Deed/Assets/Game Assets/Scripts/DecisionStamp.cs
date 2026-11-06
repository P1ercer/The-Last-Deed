using TMPro;
using UnityEngine;
using UnityEngine.XR;
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
        [SerializeField] private float npcHoverDistance = 1.5f;

        [Header("XR Controller Input")]
        [SerializeField, Range(0f, 1f)] private float triggerPressThreshold = 0.75f;

        private bool wasLeftTriggerPressed;
        private bool wasRightTriggerPressed;

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

        private void LateUpdate()
        {
            bool leftTriggerJustPressed = CheckControllerTrigger(
                XRNode.LeftHand,
                ref wasLeftTriggerPressed);

            bool rightTriggerJustPressed = CheckControllerTrigger(
                XRNode.RightHand,
                ref wasRightTriggerPressed);

            if (!leftTriggerJustPressed && !rightTriggerJustPressed)
            {
                return;
            }

            if (stampInteractable == null ||
                (!stampInteractable.isSelected && !stampInteractable.isHovered) ||
                !IsNpcCloseEnough())
            {
                return;
            }

            UseStamp();
        }

        private bool CheckControllerTrigger(
            XRNode controllerNode,
            ref bool wasTriggerPressed)
        {
            InputDevice controller =
                InputDevices.GetDeviceAtXRNode(controllerNode);

            if (!controller.isValid)
            {
                wasTriggerPressed = false;
                return false;
            }

            if (!controller.TryGetFeatureValue(
                    CommonUsages.trigger,
                    out float triggerValue))
            {
                wasTriggerPressed = false;
                return false;
            }

            bool isTriggerPressed =
                triggerValue >= triggerPressThreshold;

            bool wasTriggerJustPressed =
                isTriggerPressed && !wasTriggerPressed;

            wasTriggerPressed = isTriggerPressed;

            return wasTriggerJustPressed;
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

            GameObject stampedObject =
                queueManager.GetCurrentDecisionObject();

            if (stampedObject == null)
            {
                return;
            }

            bool decisionCompleted = decision == Decision.Accept
                ? queueManager.AcceptNpc()
                : queueManager.DeclineNpc();

            if (decisionCompleted)
            {
                CreateDecisionLabel(stampedObject);
            }
        }

        private void CreateDecisionLabel(GameObject stampedObject)
        {
            GameObject labelObject = new GameObject("Decision Label");

            labelObject.transform.position = transform.position;
            labelObject.transform.SetParent(stampedObject.transform, true);

            Camera playerCamera = Camera.main;

            if (playerCamera != null)
            {
                labelObject.transform.rotation =
                    Quaternion.LookRotation(
                        labelObject.transform.position -
                        playerCamera.transform.position);
            }
            else
            {
                labelObject.transform.rotation = transform.rotation;
            }

            TextMesh textMesh = labelObject.AddComponent<TextMesh>();

            textMesh.text = decision == Decision.Accept
                ? "ACCEPT"
                : "DECLINED";

            textMesh.color = decision == Decision.Accept
                ? Color.green
                : Color.red;

            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.alignment = TextAlignment.Center;
            textMesh.fontSize = 48;
            textMesh.characterSize = 0.01f;
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