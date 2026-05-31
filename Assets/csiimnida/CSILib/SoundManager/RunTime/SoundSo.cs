using UnityEngine;

namespace csiimnida.CSILib.SoundManager.RunTime
{
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
    }

}
    
