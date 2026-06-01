using System.Collections.Generic;
using UnityEngine;

namespace csiimnida.CSILib.SoundManager.RunTime
{
    /// <summary>동시 재생이 한도에 도달했을 때의 처리 방식.</summary>
    public enum VoiceStealMode
    {
        /// <summary>한도 초과 시 새 재생을 무시(스킵).</summary>
        Skip = 0,
        /// <summary>가장 오래된 소리를 끊고 새로 재생.</summary>
        Oldest = 1,
        /// <summary>가장 작은 소리를 끊고 새로 재생.</summary>
        Quietest = 2,
    }

    public class SoundSo : ScriptableObject
    {
        public string soundName;
        [Space(5)]
        public SoundType soundType = SoundType.SFX;
    
        [Space(3)]
        public AudioClip clip;
    
        [Space(6)]
        public bool loop;
    
        [Space(2)]
        [Range(0f,256f)] [Tooltip("장면에 공존하는 모든 오디오 소스 중에서 이 오디오 소스의 우선순위를 결정합니다. (우선순위: 0 = 가장 중요함. 256 = 가장 덜 중요함. 기본값 = 128.). 음악 트랙에는 0을 사용하여 가끔씩 바뀌는 것을 방지합니다.")]
        public int Priority = 128;
    
        [Space(2)]
        [Range(0f,1f)] [Tooltip("1월드 단위(1미터) 떨어진 곳에서 소리가 얼마나 큰가오디오 리스너\n.")]
        public float volume = 1.0f;

        [Space(2)]
        [Range(-3f,3f)][Tooltip("오디오 클립 의 감속/가속으로 인한 피치 변화량 . 값 1은 일반 재생 속도입니다.")]
        public float pitch = 1.0f;
    
        [Space(2)]
        [Range(-1f,1f)] [Tooltip("2D 사운드의 스테레오 필드에서 위치를 설정합니다.")]
        public float stereoPan = 0.0f;
    
        [Space(2)]
        [Range(0f,1f)] [Tooltip("3D 엔진이 오디오 소스에 얼마나 많은 영향을 미치는지 설정합니다.")]
        public float SpatialBlend = 0.0f;

        // ── 3D 거리 (SpatialBlend > 0 일 때 사용) ──────────────────
        [Tooltip("이 거리 안에서는 소리가 최대 음량으로 들립니다.")]
        public float minDistance = 1.0f;
        [Tooltip("이 거리 밖에서는 소리가 더 이상 작아지지 않습니다.")]
        public float maxDistance = 500.0f;

        // ── 랜덤 피치 ───────────────────────────────────────────────
        public bool RandomPitch = false;
        public float MinPitch = 0.95f;
        public float MaxPitch = 1.05f;

        // ── 페이드 (초) ─────────────────────────────────────────────
        [Tooltip("재생을 시작할 때 0에서 목표 음량까지 올라오는 시간(초). 0이면 즉시.")]
        public float fadeIn = 0.0f;
        [Tooltip("재생이 끝나거나 정지할 때 목표 음량에서 0까지 내려가는 시간(초). 0이면 즉시.")]
        public float fadeOut = 0.0f;

        // ── 쿨다운 (초) ─────────────────────────────────────────────
        [Tooltip("같은 사운드를 다시 재생하기까지의 최소 간격(초). 너무 빠른 중복 재생을 막습니다. 0이면 제한 없음.")]
        public float cooldown = 0.0f;

        // ── 고급 설정 ───────────────────────────────────────────────

        // [시작 지점] 재생을 시작할 때 클립 앞부분을 건너뛸 시간(초).
        [Tooltip("재생을 시작할 때 클립 앞부분을 건너뛸 시간(초). 예: 0.5면 0.5초 지점부터 재생.")]
        public float startOffset = 0.0f;

        // [A-②] 랜덤 시작 지점: 매번 시작 위치를 무작위로.
        [Tooltip("켜면 재생할 때마다 시작 지점(위)부터 클립 끝 사이의 임의 위치에서 재생합니다.")]
        public bool randomStartPosition = false;

        // [A-①] 클립 배리에이션: 재생 시 아래 목록(+기본 클립) 중 하나를 무작위로 선택.
        [Tooltip("재생할 때마다 이 목록(과 기본 클립) 중 하나를 무작위로 골라 재생합니다. 발소리·타격음 등에 유용합니다.")]
        public List<AudioClip> clipVariations = new List<AudioClip>();

        // [B-③] 동시 재생 최대 개수 (0 = 무제한)
        [Tooltip("같은 사운드가 동시에 재생될 수 있는 최대 개수. 0이면 무제한입니다.")]
        [Min(0)] public int maxVoices = 0;

        // [B-④] 보이스 스틸 규칙
        [Tooltip("동시 재생 한도에 도달했을 때 처리 방식.")]
        public VoiceStealMode voiceSteal = VoiceStealMode.Skip;

        // [C-⑤] 재생 지연(초)
        [Tooltip("재생을 요청한 뒤 실제 소리가 나기까지의 지연 시간(초).")]
        [Min(0f)] public float playDelay = 0.0f;

        // [D-⑧] 일시정지 중에도 재생 (AudioListener.pause 무시)
        [Tooltip("게임이 일시정지(AudioListener.pause)되어도 이 사운드는 계속 재생됩니다. UI/메뉴 사운드에 유용합니다.")]
        public bool ignoreListenerPause = false;

        // [D-⑨] 씬 전환에도 유지
        [Tooltip("씬이 전환되어도 재생 중인 이 사운드를 멈추지 않습니다. (SoundManager가 씬 전환 후에도 살아있어야 합니다.)")]
        public bool persistAcrossScenes = false;

        // [랜덤 음량]
        [Tooltip("재생할 때마다 음량을 무작위로 변화시킵니다.")]
        public bool RandomVolume = false;
        [Tooltip("랜덤 음량의 최소값")]
        public float MinVolume = 0.9f;
        [Tooltip("랜덤 음량의 최대값")]
        public float MaxVolume = 1.0f;

        /// <summary>배리에이션을 고려해 재생할 클립을 하나 선택합니다.</summary>
        public AudioClip PickClip()
        {
            if (clipVariations == null || clipVariations.Count == 0)
                return clip;

            // 기본 클립 + 유효한 배리에이션을 후보로 모음
            int validVariations = 0;
            for (int i = 0; i < clipVariations.Count; i++)
                if (clipVariations[i] != null) validVariations++;

            int candidateCount = validVariations + (clip != null ? 1 : 0);
            if (candidateCount == 0) return clip;

            int pick = Random.Range(0, candidateCount);
            if (clip != null)
            {
                if (pick == 0) return clip;
                pick--;
            }

            for (int i = 0; i < clipVariations.Count; i++)
            {
                if (clipVariations[i] == null) continue;
                if (pick == 0) return clipVariations[i];
                pick--;
            }
            return clip;
        }
    }

}
    
