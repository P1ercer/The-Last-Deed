using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace GameAssets.Scripts
{
    public class NpcQueueManager : MonoBehaviour
    {
        [Header("NPC")]
        [SerializeField] private NpcBehavior npcPrefab;
        [SerializeField] private int startingNpcCount = 4;
        [SerializeField] private float distanceBetweenNpcs = 1.5f;

        [Header("Queue Positions")]
        [SerializeField] private Transform frontOfTable;
        [SerializeField] private Transform spawnPoint;
        [SerializeField] private Transform queueDirection;

        [Header("Table Object")]
        [SerializeField] private GameObject decisionObjectPrefab;
        [SerializeField] private Transform tablePlacementPoint;
        [SerializeField] private Collider tableSurfaceCollider;
        [SerializeField] private float objectPlacementDuration = 1.25f;

        [Header("NPC Departure")]
        [SerializeField] private float departureDistance = 3f;
        [SerializeField] private float departureDuration = 2f;

        [Header("Decision Settings")]
        [SerializeField] private float decisionCooldown = 0.25f;

        [Header("Events")]
        [SerializeField] private UnityEvent onNpcAccepted;
        [SerializeField] private UnityEvent onNpcDeclined;

        [Header("Queue Direction Gizmo")]
        [SerializeField] private bool showQueueDirectionGizmo = true;
        [SerializeField] private Color queueDirectionColor = Color.blue;
        [SerializeField] private float queueDirectionArrowLength = 2f;
        [SerializeField] private float queueDirectionArrowHeadLength = 0.4f;
        [SerializeField] private float queueDirectionArrowHeadWidth = 0.2f;

        private readonly List<NpcBehavior> npcs =
            new List<NpcBehavior>();

        private float nextDecisionTime;

        private void Start()
        {
            if (npcPrefab == null ||
                frontOfTable == null ||
                spawnPoint == null ||
                decisionObjectPrefab == null ||
                tablePlacementPoint == null)
            {
                Debug.LogError(
                    "NpcQueueManager requires an NPC prefab, Front Of Table, " +
                    "Spawn Point, decision object prefab, and table placement point.",
                    this);

                enabled = false;
                return;
            }

            if (tableSurfaceCollider == null)
            {
                Debug.LogWarning(
                    "NpcQueueManager is using Table Placement Point as the " +
                    "fallback tabletop surface.",
                    this);
            }

            startingNpcCount = Mathf.Max(1, startingNpcCount);
            distanceBetweenNpcs = Mathf.Max(0.1f, distanceBetweenNpcs);

            for (int i = 0; i < startingNpcCount; i++)
            {
                SpawnNpc(i);
            }
        }

        private void Update()
        {
            if (npcs.Count == 0)
            {
                return;
            }

            NpcBehavior currentNpc = npcs[0];

            if (currentNpc == null ||
                currentNpc.IsMoving ||
                currentNpc.IsPlacingObject ||
                currentNpc.PlacedObject != null)
            {
                return;
            }

            currentNpc.PlaceObjectOnTable(
                decisionObjectPrefab,
                tablePlacementPoint,
                tableSurfaceCollider,
                objectPlacementDuration); // Added missing argument
        }

        private void SpawnNpc(int queueIndex)
        {
            Vector3 spawnPosition = spawnPoint.position;

            if (queueIndex > 0)
            {
                spawnPosition += GetQueueDirection() *
                                 (queueIndex * distanceBetweenNpcs);
            }

            NpcBehavior npc = Instantiate(
                npcPrefab,
                spawnPosition,
                npcPrefab.transform.rotation);

            npcs.Add(npc);
            npc.MoveTo(GetQueuePosition(queueIndex));
            npc.FaceDirection(-GetQueueDirection());
        }

        private Vector3 GetQueuePosition(int queueIndex)
        {
            return frontOfTable.position +
                   GetQueueDirection() *
                   (queueIndex * distanceBetweenNpcs);
        }

        private Vector3 GetQueueDirection()
        {
            Transform directionTransform = queueDirection != null
                ? queueDirection
                : frontOfTable;

            if (directionTransform == null)
            {
                return Vector3.forward;
            }

            Vector3 direction = directionTransform.forward;
            direction.y = 0f;

            if (direction.sqrMagnitude <= 0.001f)
            {
                return Vector3.forward;
            }

            return direction.normalized;
        }

        private void MoveQueueForward()
        {
            Vector3 facingDirection = -GetQueueDirection();

            for (int i = 0; i < npcs.Count; i++)
            {
                if (npcs[i] != null)
                {
                    npcs[i].MoveTo(GetQueuePosition(i));    
                    npcs[i].FaceDirection(facingDirection);
                }
            }
        }

        public bool AcceptNpc()
        {
            if (!CanMakeDecision())
            {
                return false;
            }

            ResolveCurrentNpc();
            onNpcAccepted?.Invoke();

            return true;
        }

        public bool DeclineNpc()
        {
            if (!CanMakeDecision())
            {
                return false;
            }

            ResolveCurrentNpc();
            onNpcDeclined?.Invoke();

            return true;
        }

        private bool CanMakeDecision()
        {
            if (npcs.Count == 0 ||
                npcs[0] == null ||
                npcs[0].PlacedObject == null ||
                npcs[0].IsPlacingObject ||
                Time.time < nextDecisionTime)
            {
                return false;
            }

            nextDecisionTime = Time.time + decisionCooldown;
            return true;
        }

        private void ResolveCurrentNpc()
        {
            NpcBehavior currentNpc = npcs[0];

            if (currentNpc != null)
            {
                currentNpc.TakePlacedObject();
            }

            npcs.RemoveAt(0);

            if (currentNpc != null)
            {
                currentNpc.DepartAndDisappear(
                    -GetQueueDirection(),
                    departureDistance,
                    departureDuration);
            }

            SpawnNpc(npcs.Count);
            MoveQueueForward();
        }

        public NpcBehavior GetCurrentNpc()
        {
            if (npcs.Count == 0)
            {
                return null;
            }

            return npcs[0];
        }

        public GameObject GetCurrentDecisionObject()
        {
            NpcBehavior currentNpc = GetCurrentNpc();

            if (currentNpc == null)
            {
                return null;
            }

            return currentNpc.PlacedObject;
        }

        private void OnDrawGizmos()
        {
            if (!showQueueDirectionGizmo)
            {
                return;
            }

            Vector3 arrowStart = frontOfTable != null
                ? frontOfTable.position
                : transform.position;

            Vector3 direction = GetQueueDirection();
            Vector3 sideDirection =
                Vector3.Cross(Vector3.up, direction).normalized;

            Vector3 arrowEnd =
                arrowStart + direction * queueDirectionArrowLength;

            Vector3 arrowHeadBase =
                arrowEnd - direction * queueDirectionArrowHeadLength;

            Vector3 leftArrowHead =
                arrowHeadBase + sideDirection * queueDirectionArrowHeadWidth;

            Vector3 rightArrowHead =
                arrowHeadBase - sideDirection * queueDirectionArrowHeadWidth;

            Gizmos.color = queueDirectionColor;

            Gizmos.DrawLine(arrowStart, arrowEnd);
            Gizmos.DrawLine(arrowEnd, leftArrowHead);
            Gizmos.DrawLine(arrowEnd, rightArrowHead);
            Gizmos.DrawSphere(arrowStart, 0.05f);
        }
    }
}