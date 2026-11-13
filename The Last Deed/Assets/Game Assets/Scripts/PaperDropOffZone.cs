using System.Collections.Generic;
using UnityEngine;

namespace GameAssets.Scripts
{
    [RequireComponent(typeof(BoxCollider))]
    public class PaperDropOffZone : MonoBehaviour
    {
        [Header("Boundary Settings")]
        [SerializeField] private float boundaryPadding = 0.02f;
        [SerializeField] private float edgePushDistance = 0.1f;

        private readonly HashSet<Transform> papers =
            new HashSet<Transform>();

        private BoxCollider zoneCollider;

        private void Awake()
        {
            zoneCollider = GetComponent<BoxCollider>();
            zoneCollider.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            RegisterPaper(other);
        }

        private void OnTriggerStay(Collider other)
        {
            RegisterPaper(other);
        }

        private void FixedUpdate()
        {
            Transform[] trackedPapers =
                new Transform[papers.Count];

            papers.CopyTo(trackedPapers);

            foreach (Transform paper in trackedPapers)
            {
                if (paper == null)
                {
                    papers.Remove(paper);
                    continue;
                }

                // Stop controlling the paper once the NPC picks it up.
                if (paper.GetComponentInParent<NpcBehavior>() != null)
                {
                    papers.Remove(paper);
                    continue;
                }

                KeepPaperInsideZone(paper);
            }
        }

        private void RegisterPaper(Collider other)
        {
            Transform paper = FindPaperRoot(other);

            if (paper == null ||
                paper.GetComponentInParent<NpcBehavior>() != null)
            {
                return;
            }

            Rigidbody paperRigidbody =
                paper.GetComponentInChildren<Rigidbody>();

            if (paperRigidbody != null)
            {
                paperRigidbody.isKinematic = true;
                paperRigidbody.useGravity = false;
                paperRigidbody.linearVelocity = Vector3.zero;
                paperRigidbody.angularVelocity = Vector3.zero;
            }

            papers.Add(paper);
        }

        private Transform FindPaperRoot(Collider other)
        {
            Transform current = other.transform;

            while (current != null)
            {
                if (current.CompareTag("paper"))
                {
                    return current;
                }

                current = current.parent;
            }

            return null;
        }

        private void KeepPaperInsideZone(Transform paper)
        {
            Bounds zoneBounds = zoneCollider.bounds;
            Collider paperCollider =
                paper.GetComponentInChildren<Collider>();

            Vector3 paperExtents = paperCollider != null
                ? paperCollider.bounds.extents
                : Vector3.zero;

            float safetyMargin =
                boundaryPadding + edgePushDistance;

            float minimumX =
                zoneBounds.min.x +
                paperExtents.x +
                safetyMargin;

            float maximumX =
                zoneBounds.max.x -
                paperExtents.x -
                safetyMargin;

            float minimumZ =
                zoneBounds.min.z +
                paperExtents.z +
                safetyMargin;

            float maximumZ =
                zoneBounds.max.z -
                paperExtents.z -
                safetyMargin;

            Vector3 currentPosition = paper.position;

            Vector3 correctedPosition = currentPosition;

            correctedPosition.x = ClampInsideBounds(
                currentPosition.x,
                minimumX,
                maximumX);

            correctedPosition.z = ClampInsideBounds(
                currentPosition.z,
                minimumZ,
                maximumZ);

            if (Vector3.Distance(
                    currentPosition,
                    correctedPosition) <= 0.001f)
            {
                return;
            }

            Rigidbody paperRigidbody =
                paper.GetComponentInChildren<Rigidbody>();

            if (paperRigidbody != null &&
                !paperRigidbody.isKinematic)
            {
                paperRigidbody.position = correctedPosition;
                paperRigidbody.linearVelocity = Vector3.zero;
                paperRigidbody.angularVelocity = Vector3.zero;
            }

            paper.position = correctedPosition;
        }

        private float ClampInsideBounds(
            float value,
            float minimum,
            float maximum)
        {
            if (minimum > maximum)
            {
                return (minimum + maximum) * 0.5f;
            }

            return Mathf.Clamp(value, minimum, maximum);
        }

        private void OnDrawGizmosSelected()
        {
            BoxCollider box = GetComponent<BoxCollider>();

            if (box == null)
            {
                return;
            }

            Matrix4x4 previousMatrix = Gizmos.matrix;

            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = new Color(0f, 1f, 1f, 0.2f);
            Gizmos.DrawCube(box.center, box.size);

            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(box.center, box.size);

            Gizmos.matrix = previousMatrix;
        }
    }
}