// Low Poly City - runtime flicker for torch, brazier and
// firepit lights. Attached automatically to the lit prefabs by
// Tools > Low Poly City > Build Prefabs; you can also add it to any
// Light of your own.
//
// Perlin noise, NOT Random.value: a random value per frame jitters at the
// frame rate and reads as a broken light, where Perlin over time breathes
// like a flame. Two octaves so it is not obviously periodic.
using UnityEngine;

namespace LowPolyCity
{
    [AddComponentMenu("Low Poly City/Flicker Light")]
    [RequireComponent(typeof(Light))]
    public class LPC_Flicker : MonoBehaviour
    {
        [Tooltip("Lowest fraction of the light's own intensity.")]
        [Range(0f, 1f)] public float min = 0.72f;
        [Tooltip("Highest fraction of the light's own intensity.")]
        [Range(0f, 2f)] public float max = 1.12f;
        [Tooltip("Flicker speed. 1 is a calm hearth, 4 a guttering torch.")]
        public float speed = 2.2f;
        [Tooltip("How far the light drifts from its rest position, in metres.")]
        public float sway = 0.012f;

        Light _light;
        float _base, _seed;
        Vector3 _rest;

        void Awake()
        {
            _light = GetComponent<Light>();
            _base = _light.intensity;
            _rest = transform.localPosition;
            // Per-instance, or every torch in the room flickers in unison,
            // which reads as the whole level pulsing rather than as fire.
            _seed = Random.value * 100f;
        }

        void OnEnable() { if (_light != null) _light.intensity = _base; }

        void Update()
        {
            float t = Time.time * speed + _seed;
            float n = Mathf.PerlinNoise(t, 0f) * 0.75f
                    + Mathf.PerlinNoise(t * 2.7f, 5f) * 0.25f;
            _light.intensity = _base * Mathf.Lerp(min, max, n);
            if (sway > 0f)
                transform.localPosition = _rest + new Vector3(
                    (Mathf.PerlinNoise(t, 11f) - 0.5f) * sway, 0f,
                    (Mathf.PerlinNoise(t, 23f) - 0.5f) * sway);
        }
    }
}
