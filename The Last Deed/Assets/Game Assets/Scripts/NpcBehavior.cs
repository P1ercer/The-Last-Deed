using System.Collections;
using UnityEngine;

namespace GameAssets.Scripts
{
    public class NpcBehavior : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float moveSpeed = 2f;
        [SerializeField] private float stoppingDistance = 0.05f;

        [Header("Object Placement")]
        [SerializeField] private Transform itemHoldPoint;
        [SerializeField] private float itemArcHeight = 0.25f;

        private Vector3 destination;
        private Vector3 finalFacingDirection;
        private bool isMoving;
        private bool hasFinalFacingDirection;
        private bool isPlacingObject;

        public bool IsMoving => isMoving;
        public bool IsPlacingObject => isPlacingObject;
        public GameObject PlacedObject { get; private set; }

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

        public void PlaceObjectOnTable(
            GameObject objectPrefab,
            Transform tablePlacementPoint,
            float placementDuration)
        {
            if (isPlacingObject ||
                PlacedObject != null ||
                objectPrefab == null ||
                tablePlacementPoint == null)
            {
                return;
            }

            StartCoroutine(PlaceObjectRoutine(
                objectPrefab,
                tablePlacementPoint,
                Mathf.Max(0.01f, placementDuration)));
        }

        private IEnumerator PlaceObjectRoutine(
            GameObject objectPrefab,
            Transform tablePlacementPoint,
            float placementDuration)
        {
            isPlacingObject = true;

            Transform spawnPoint = itemHoldPoint != null
                ? itemHoldPoint
                : transform;

            GameObject placedObject = Instantiate(
                objectPrefab,
                spawnPoint.position,
                spawnPoint.rotation);

            Vector3 startPosition = placedObject.transform.position;
            Quaternion startRotation = placedObject.transform.rotation;
            Vector3 endPosition = tablePlacementPoint.position;
            Quaternion endRotation = tablePlacementPoint.rotation;

            float elapsedTime = 0f;

            while (elapsedTime < placementDuration)
            {
                if (placedObject == null)
                {
                    isPlacingObject = false;
                    yield break;
                }

                elapsedTime += Time.deltaTime;

                float progress = Mathf.Clamp01(
                    elapsedTime / placementDuration);

                float arcOffset =
                    Mathf.Sin(progress * Mathf.PI) * itemArcHeight;

                placedObject.transform.position =
                    Vector3.Lerp(startPosition, endPosition, progress) +
                    Vector3.up * arcOffset;

                placedObject.transform.rotation =
                    Quaternion.Slerp(startRotation, endRotation, progress);

                yield return null;
            }

            if (placedObject != null)
            {
                placedObject.transform.SetPositionAndRotation(
                    endPosition,
                    endRotation);

                placedObject.transform.SetParent(
                    tablePlacementPoint,
                    true);

                PlacedObject = placedObject;
            }

            isPlacingObject = false;
        }

        public void DepartAndDisappear(
            Vector3 direction,
            float distance,
            float duration)
        {
            StartCoroutine(DepartRoutine(
                direction,
                Mathf.Max(0f, distance),
                Mathf.Max(0.01f, duration)));
        }

        private IEnumerator DepartRoutine(
            Vector3 direction,
            float distance,
            float duration)
        {
            isMoving = false;

            direction.y = 0f;

            if (direction.sqrMagnitude <= 0.001f)
            {
                direction = Vector3.forward;
            }

            direction.Normalize();

            Vector3 startPosition = transform.position;
            Vector3 endPosition = startPosition + direction * distance;
            float elapsedTime = 0f;

            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;

                float progress = Mathf.Clamp01(
                    elapsedTime / duration);

                transform.position =
                    Vector3.Lerp(startPosition, endPosition, progress);

                transform.forward = direction;

                yield return null;
            }

            transform.position = endPosition;
            Destroy(gameObject);
        }
    }
}