using UnityEngine;

namespace GameAssets.Scripts
{
    public class DecisionStamp : MonoBehaviour
    {
        public enum Decision
        {
            Accept,
            Decline
        }

        [SerializeField] private NpcQueueManager queueManager;
        [SerializeField] private Decision decision;

        public void UseStamp()
        {
            if (queueManager == null)
            {
                Debug.LogWarning(
                    "DecisionStamp requires an NpcQueueManager reference.");

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
    }
}