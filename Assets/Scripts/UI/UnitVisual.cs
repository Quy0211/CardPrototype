using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

namespace CardGame.UI
{
    /// <summary>Quản lý hiển thị sprite cho đơn vị trong UI Toolkit + animation frame đơn giản.</summary>
    public sealed class UnitVisual
    {
        public VisualElement SpriteEl { get; }
        Sprite[] framesIdle;
        Sprite[] framesSlash;
        Coroutine co;

        public UnitVisual(VisualElement spriteEl, Sprite[] idle, Sprite[] slash)
        {
            SpriteEl = spriteEl;
            framesIdle = idle;
            framesSlash = slash;
        }

        public void PlayIdle(MonoBehaviour host)
        {
            Stop(host);
            co = host.StartCoroutine(Loop(framesIdle, 0.22f));
        }

        public void PlaySlash(MonoBehaviour host, System.Action onComplete = null, float fps = 0.12f)
        {
            Stop(host);
            co = host.StartCoroutine(Once(framesSlash, fps, () =>
            {
                if (onComplete != null) onComplete();
                PlayIdle(host);
            }));
        }

        void Stop(MonoBehaviour host)
        {
            if (co != null && host != null)
            {
                host.StopCoroutine(co);
                co = null;
            }
        }

        IEnumerator Loop(Sprite[] frames, float interval)
        {
            int i = 0;
            while (true)
            {
                if (frames != null && frames.Length > 0)
                {
                    SetSprite(frames[i % frames.Length]);
                    i++;
                }
                yield return new WaitForSeconds(interval);
            }
        }

        IEnumerator Once(Sprite[] frames, float interval, System.Action done)
        {
            if (frames == null || frames.Length == 0)
            {
                done?.Invoke();
                yield break;
            }
            for (int i = 0; i < frames.Length; i++)
            {
                SetSprite(frames[i]);
                yield return new WaitForSeconds(interval);
            }
            done?.Invoke();
        }

        void SetSprite(Sprite s)
        {
            if (SpriteEl == null || s == null) return;
            SpriteEl.style.backgroundImage = new StyleBackground(s);
            SpriteEl.style.unityBackgroundScaleMode = ScaleMode.ScaleToFit;
            SpriteEl.style.width = s.rect.width;
            SpriteEl.style.height = s.rect.height;
        }
    }
}
