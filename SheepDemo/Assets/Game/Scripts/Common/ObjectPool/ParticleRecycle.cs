using System.Collections;
using System.Reflection;
using UnityEngine;

namespace Game.Common
{
    /// <summary>
    /// 可池化的粒子效果包装器。
    /// 播放结束后自动回收到 ObjectPoolManager。
    /// </summary>
    [RequireComponent(typeof(ParticleSystem))]
    public class ParticleRecycle : MonoBehaviour, IRecycle
    {
        private ParticleSystem _particle;
        private Coroutine _autoRecycleCoroutine;

        private void Awake()
        {
            if (_particle == null)
                _particle = GetComponent<ParticleSystem>();
        }

        public virtual void OnSpawn()
        {
            if (_particle == null)
                _particle = GetComponent<ParticleSystem>();

            if (_particle != null)
            {
                _particle.Clear(true);
                _particle.Play(true);

                float duration = _particle.main.duration;
                var startLifetime = _particle.main.startLifetime;
                float maxLife = startLifetime.constantMax > 0 ? startLifetime.constantMax : startLifetime.constant;
                float total = duration + maxLife;

                if (_autoRecycleCoroutine != null)
                    StopCoroutine(_autoRecycleCoroutine);
                _autoRecycleCoroutine = StartCoroutine(AutoRecycle(total));
            }
        }

        public void OnRecycle()
        {
            if (_autoRecycleCoroutine != null)
            {
                StopCoroutine(_autoRecycleCoroutine);
                _autoRecycleCoroutine = null;
            }

            if (_particle != null)
            {
                _particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }

        /// <summary>
        /// 将自身回收到对应的 ObjectPoolManager 池中（自动识别具体子类型）。
        /// </summary>
        public virtual void ReturnToPool()
        {
            if (this == null || gameObject == null)
                return;

            var recycleMethod = typeof(ObjectPoolManager).GetMethod("Recycle");
            if (recycleMethod != null)
            {
                var genericMethod = recycleMethod.MakeGenericMethod(GetType());
                genericMethod.Invoke(ObjectPoolManager.Instance, new object[] { this });
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private IEnumerator AutoRecycle(float delay)
        {
            yield return new WaitForSeconds(delay);
            ReturnToPool();
        }
    }
}
