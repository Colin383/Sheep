using UnityEngine;

namespace Game.Common
{
    public class SmokeParticle : ParticleRecycle
    {
        public override void OnSpawn()
        {
            var particle = GetComponent<ParticleSystem>();
            if (particle != null)
            {
                particle.Clear(true);
                particle.Play(true);
            }
        }
    }
}
