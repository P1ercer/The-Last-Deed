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

        [Header("Decision Settings")]
        [SerializeField] private float decisionCooldown = 0.25f;

        [Header("Events")]
        [SerializeField] private UnityEvent onNpcAccepted;
        [SerializeField] private UnityEvent onNpcDeclined;

        private readonly List<NpcBehavior> npcs = new List<NpcBehavior>();

        private float nextDecisionTime;

        private void Start()
        {
            if (npcPrefab == null ||
                frontOfTable == null ||
                spawnPoint == null)
            {
                Debug.LogError(
                    "NpcQueueManager requires an NPC prefab, front-of-table position, and spawn point.");

                enabled = false;
                return;
            }

            if (queueDirection == null)
            {
                queueDirection = frontOfTable;
            }

            startingNpcCount = Mathf.Max(1, startingNpcCount);
            distanceBetweenNpcs = Mathf.Max(0.1f, distanceBetweenNpcs);

            for (int i = 0; i < startingNpcCount; i++)
            {
                SpawnNpc(i);
            }
        }

        private void SpawnNpc(int queueIndex)
        {
            Vector3 spawnPosition = spawnPoint.position;

            if (queueIndex > 0)
            {
                spawnPosition += queueDirection.forward *
                                 (queueIndex * distanceBetweenNpcs);
            }

            NpcBehavior npc = Instantiate(
                npcPrefab,
                spawnPosition,
                npcPrefab.transform.rotation);

            npcs.Add(npc);
            npc.MoveTo(GetQueuePosition(queueIndex));
        }

        private Vector3 GetQueuePosition(int queueIndex)
        {
            return frontOfTable.position +
                   queueDirection.forward *
                   (queueIndex * distanceBetweenNpcs);
        }

        private void MoveQueueForward()
        {
            for (int i = 0; i < npcs.Count; i++)
            {
                if (npcs[i] != null)
                {
                    npcs[i].MoveTo(GetQueuePosition(i));
                }
            }
        }

        public void AcceptNpc()
        {
            if (!CanMakeDecision())
            {
                return;
            }

            ResolveCurrentNpc();
            onNpcAccepted?.Invoke();
        }

        public void DeclineNpc()
        {
            if (!CanMakeDecision())
            {
                return;
            }

            ResolveCurrentNpc();
            onNpcDeclined?.Invoke();
        }

        private bool CanMakeDecision()
        {
            if (npcs.Count == 0 || Time.time < nextDecisionTime)
            {
                return false;
            }

            nextDecisionTime = Time.time + decisionCooldown;
            return true;
        }

        private void ResolveCurrentNpc()
        {
            NpcBehavior currentNpc = npcs[0];
            npcs.RemoveAt(0);

            if (currentNpc != null)
            {
                Destroy(currentNpc.gameObject);
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
    }
}