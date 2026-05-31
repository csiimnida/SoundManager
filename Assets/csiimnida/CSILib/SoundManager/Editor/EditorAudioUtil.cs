using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace csiimnida.CSILib.SoundManager.Editor
{
    internal static class EditorAudioUtil
    {
        private static readonly Type _type;
        private static readonly MethodInfo _play;
        private static readonly MethodInfo _stop;
        private static readonly MethodInfo _pause;
        private static readonly MethodInfo _resume;
        private static readonly MethodInfo _isPlaying;
        private static readonly MethodInfo _getPosition;
        private static readonly MethodInfo _stopAll;

        static EditorAudioUtil()
        {
            _type = typeof(AudioImporter).Assembly.GetType("UnityEditor.AudioUtil");
            if (_type == null)
            {
                Debug.LogError("[EditorAudioUtil] UnityEditor.AudioUtil을 찾을 수 없습니다. Unity 버전을 확인하세요.");
                return;
            }

            const BindingFlags flags = BindingFlags.Static | BindingFlags.Public;

            // Unity 2020.2+ 시그니처: PlayPreviewClip(AudioClip, int startSample, bool loop)
            _play = _type.GetMethod("PlayPreviewClip", flags, null,
                new[] { typeof(AudioClip), typeof(int), typeof(bool) }, null);

            _stop      = _type.GetMethod("StopPreviewClip",     flags);
            _pause     = _type.GetMethod("PausePreviewClip",    flags);
            _resume    = _type.GetMethod("ResumePreviewClip",   flags);
            _isPlaying = _type.GetMethod("IsPreviewClipPlaying",flags);
            _getPosition = _type.GetMethod("GetPreviewClipPosition", flags);
            _stopAll   = _type.GetMethod("StopAllPreviewClips", flags);
        }

        public static void Play(AudioClip clip, bool loop = false)
            => _play?.Invoke(null, new object[] { clip, 0, loop });

        public static void Stop()
            => _stop?.Invoke(null, null);

        public static void Pause()
            => _pause?.Invoke(null, null);

        public static void Resume()
            => _resume?.Invoke(null, null);

        public static bool IsPlaying()
            => (bool)(_isPlaying?.Invoke(null, null) ?? false);

        // 현재 재생 위치 (초 단위)
        public static float GetPosition()
            => (float)(_getPosition?.Invoke(null, null) ?? 0f);

        public static void StopAll()
            => _stopAll?.Invoke(null, null);
    }
}
