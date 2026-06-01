using csiimnida.CSILib.SoundManager.RunTime;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;

namespace csiimnida.CSILib.SoundManager.Editor
{
    /// <summary>
    /// 에디터 미리듣기를 "실제 AudioSource"로 재생합니다.
    /// Unity 내부 AudioUtil 프리뷰와 달리 볼륨/피치/랜덤/배리에이션/시작 지점/지연 등
    /// SoundSo 설정을 런타임과 동일하게 적용해 들려줍니다.
    /// </summary>
    [InitializeOnLoad]
    internal static class SoundPreviewPlayer
    {
        private static GameObject _go;
        private static AudioSource _src;
        private static AudioListener _tempListener;
        private static AudioClip _currentClip;

        static SoundPreviewPlayer()
        {
            // 플레이 모드 전환 / 도메인 리로드 시 임시 오브젝트 정리
            EditorApplication.playModeStateChanged += _ => Cleanup();
            AssemblyReloadEvents.beforeAssemblyReload += Cleanup;
        }

        private static AudioSource EnsureSource()
        {
            if (_src != null) return _src;

            _go = new GameObject("[SoundPreview]") { hideFlags = HideFlags.HideAndDontSave };
            _src = _go.AddComponent<AudioSource>();
            _src.playOnAwake = false;
            return _src;
        }

        /// <summary>씬에 활성 AudioListener 가 없으면 소리가 안 나므로 임시로 하나 붙여줍니다.</summary>
        private static void EnsureListener()
        {
            if (_tempListener != null) return;

            AudioListener[] listeners = Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None);
            foreach (AudioListener l in listeners)
            {
                if (l != null && l.isActiveAndEnabled)
                    return; // 이미 들을 수 있는 리스너가 있음
            }
            _tempListener = _go.AddComponent<AudioListener>();
        }

        public static void Play(SoundSo so)
        {
            if (so == null) return;

            AudioClip clip = so.PickClip();
            if (clip == null) return;

            AudioSource src = EnsureSource();
            EnsureListener();

            src.Stop();
            src.clip                   = clip;
            src.loop                   = so.loop;
            src.panStereo              = so.stereoPan;
            src.spatialBlend           = 0f;   // 미리듣기는 2D로 고정 (위치 무관하게 들리도록)
            src.ignoreListenerPause    = true; // 미리듣기는 항상 들리게
            src.outputAudioMixerGroup  = null; // 믹서 라우팅 없이 원음 확인

            // 런타임 ApplySettings 와 동일한 규칙
            float pitch = so.RandomPitch ? Random.Range(so.MinPitch, so.MaxPitch) : so.pitch;
            src.pitch = pitch;
            float vol = so.RandomVolume ? Random.Range(so.MinVolume, so.MaxVolume) : so.volume;
            src.volume = Mathf.Clamp01(vol);

            float len = clip.length;
            if (pitch < 0f && len > 0f)
            {
                src.time = len - 0.01f;
            }
            else if (len > 0f)
            {
                float maxStart = Mathf.Max(0f, len - 0.01f);
                float start = Mathf.Clamp(so.startOffset, 0f, maxStart);
                if (so.randomStartPosition)
                    start = Random.Range(start, maxStart);
                src.time = start > 0f ? start : 0f;
            }

            _currentClip = clip;

            if (so.playDelay > 0f)
                src.PlayDelayed(so.playDelay);
            else
                src.Play();
        }

        public static void Stop()   { if (_src != null) _src.Stop(); }
        public static void Pause()  { if (_src != null) _src.Pause(); }
        public static void Resume() { if (_src != null) _src.UnPause(); }

        public static bool  IsPlaying()       => _src != null && _src.isPlaying;
        public static float GetPosition()     => _src != null ? _src.time : 0f;
        public static float CurrentClipLength => _currentClip != null ? _currentClip.length : 0f;

        public static void Cleanup()
        {
            if (_src != null) _src.Stop();
            if (_go != null) Object.DestroyImmediate(_go);
            _go = null;
            _src = null;
            _tempListener = null;
            _currentClip = null;
        }
    }
}
