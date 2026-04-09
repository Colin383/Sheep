using System.Collections.Generic;
using UnityEngine;

namespace Bear.UI
{
    /// <summary>
    /// UI 视图基类
    /// </summary>
    public abstract class BaseUIView : MonoBehaviour, IUIView
    {
        protected UILayer _layer = UILayer.Normal;
        protected bool _isOpen = false;
        protected bool _isVisible = false;

        private List<IUIAnimation> _animations;

        /// <summary>
        /// UI 层级
        /// </summary>
        public UILayer Layer => _layer;

        /// <summary>
        /// 是否已打开
        /// </summary>
        public bool IsOpen => _isOpen;

        /// <summary>
        /// 是否可见
        /// </summary>
        public bool IsVisible => _isVisible;

        protected virtual void Awake()
        {
            // Unity 的 GetComponents<T>() 要求 T : Component，接口类型无法直接获取。
            // 这里通过遍历 MonoBehaviour 来收集所有实现 IUIAnimation 的组件。
            MonoBehaviour[] behaviours = GetComponents<MonoBehaviour>();
            _animations = new List<IUIAnimation>(behaviours.Length);
            for (int i = 0; i < behaviours.Length; i++)
            {
                MonoBehaviour behaviour = behaviours[i];
                if (behaviour is IUIAnimation animation)
                {
                    _animations.Add(animation);
                }
            }
        }

        /// <summary>
        /// UI 创建时调用
        /// </summary>
        public virtual void OnCreate()
        {
        }

        /// <summary>
        /// UI 打开时调用
        /// </summary>
        public virtual void OnOpen()
        {
            _isOpen = true;
        }

        /// <summary>
        /// UI 显示时调用
        /// </summary>
        public virtual void OnShow()
        {
            _isVisible = true;
            gameObject.SetActive(true);

            if (_animations != null && _animations.Count > 0)
            {
                for (int i = 0; i < _animations.Count; i++)
                {   
                    _animations[i]?.ResetAnim();
                    _animations[i]?.PlayOpenAnimation();
                }
            }
        }

        /// <summary>
        /// UI 隐藏时调用
        /// </summary>
        public virtual void OnHide()
        {
            _isVisible = false;

            if (_animations != null && _animations.Count > 0)
            {
                // 第一次循环：统计需要等待的动画数量（非 null）
                int remaining = 0;
                for (int i = 0; i < _animations.Count; i++)
                {
                    if (_animations[i] != null)
                    {
                        remaining++;
                    }
                }

                if (remaining <= 0)
                {
                    gameObject.SetActive(false);
                    return;
                }

                // 第二次循环：播放关闭动画并注册完成回调
                // 避免某些动画（如 Scale 选择 OnHide 不播放）同步回调导致 remaining 在循环内被错误减到 0 而立即关闭
                for (int i = 0; i < _animations.Count; i++)
                {
                    IUIAnimation anim = _animations[i];
                    if (anim == null)
                    {
                        continue;
                    }

                    anim.PlayCloseAnimation(() =>
                    {
                        remaining--;
                        if (remaining <= 0)
                        {
                            gameObject.SetActive(false);
                        }
                    });
                }
            }
            else
            {
                gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// UI 关闭时调用
        /// </summary>
        public virtual void OnClose()
        {
            _isOpen = false;
            _isVisible = false;
        }

        /// <summary>
        /// UI 销毁时调用
        /// </summary>
        public virtual void OnDestroyView()
        {
            OnClose();
        }

        private void OnDestroy()
        {
            // OnDestroyView();
        }

        /// <summary>
        /// 设置 UI 层级
        /// </summary>
        /// <param name="layer">层级</param>
        public void SetLayer(UILayer layer)
        {
            _layer = layer;
        }
    }
}

