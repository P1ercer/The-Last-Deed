using UnityEngine;

namespace GameAssets.Scripts
{
    public class NpcBehavior : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 2f;
        [SerializeField] private float stoppingDistance = 0.05f;

        private Vector3 destination;
        private Vector3 finalFacingDirection;
        private bool isMoving;
        private bool hasFinalFacingDirection;

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

            Vector3 movementDirection = destination - transform.position;
            movementDirection.y = 0f;

            if (movementDirection.sqrMagnitude > 0.001f)
            {
                transform.forward = Vector3.Lerp(
                    transform.forward,
                    movementDirection.normalized,
                    moveSpeed * Time.deltaTime * 5f);
            }

            if (Vector3.Distance(transform.position, destination) <= stoppingDistance)
            {
                transform.position = destination;
                isMoving = false;

                if (hasFinalFacingDirection)
                {
                    transform.forward = finalFacingDirection;
                }
            }
        }

        public void MoveTo(Vector3 targetPosition)
        {
            destination = targetPosition;
            isMoving = true;
        }

        public void FaceDirection(Vector3 direction)
        {
            direction.y = 0f;

            if (direction.sqrMagnitude <= 0.001f)
            {
                return;
            }

            finalFacingDirection = direction.normalized;
            hasFinalFacingDirection = true;

            if (!isMoving)
            {
                transform.forward = finalFacingDirection;
            }
        }
    }
}