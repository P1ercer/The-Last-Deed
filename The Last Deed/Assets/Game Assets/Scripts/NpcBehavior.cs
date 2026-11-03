using UnityEngine;

namespace GameAssets.Scripts
{
    public class NpcBehavior : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 2f;
        [SerializeField] private float stoppingDistance = 0.05f;

        private Vector3 destination;
        private bool isMoving;

        private void Update()
        {
            if (!isMoving)
            {
                return;
            }

            transform.position = Vector3.MoveTowards(
                transform.position,
                destination,
                moveSpeed * Time.deltaTime);

            Vector3 direction = destination - transform.position;
            direction.y = 0f;

            if (direction.sqrMagnitude > 0.001f)
            {
                transform.forward = Vector3.Lerp(
                    transform.forward,
                    direction.normalized,
                    moveSpeed * Time.deltaTime);
            }

            if (Vector3.Distance(transform.position, destination) <= stoppingDistance)
            {
                transform.position = destination;
                isMoving = false;
            }
        }

        public void MoveTo(Vector3 targetPosition)
        {
            destination = targetPosition;
            isMoving = true;
        }
    }
}