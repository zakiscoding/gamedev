using UnityEngine;

namespace Cmis3150.Fps.Weapons
{
    /// <summary>Grenade explosion visual: an expanding sphere and a fading point light, then self-destroy.</summary>
    public class ExplosionFx : MonoBehaviour
    {
        float _size, _life, _t;
        Light _light;

        public void Init(float size, float life)
        {
            _size = size;
            _life = life;
            _light = GetComponent<Light>();
            transform.localScale = Vector3.one * 0.2f;
        }

        void Update()
        {
            _t += Time.deltaTime;
            float k = Mathf.Clamp01(_t / _life);
            transform.localScale = Vector3.one * Mathf.Lerp(0.2f, _size, Mathf.Sqrt(k));
            if (_light != null) _light.intensity = Mathf.Lerp(6f, 0f, k);
            if (_t >= _life) Destroy(gameObject);
        }
    }
}
