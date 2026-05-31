using UnityEngine;

namespace csiimnida.CSILib.SoundManager.RunTime
{
    /// <summary>
    /// 재생 중인 개별 사운드를 제어하기 위한 핸들.
    /// PlaySound 가 반환하며, 정지/일시정지/음량 조절/페이드아웃에 사용합니다.
    ///
    /// 내부적으로 활성 엔트리를 직접 참조하고 세대 Id 로 유효성을 검사하므로
    /// 모든 연산이 O(1) 입니다. 사운드가 끝나 엔트리가 재사용되면 Id 가 달라져
    /// IsValid 가 false 가 됩니다.
    /// </summary>
    public class SoundHandle
    {
        internal SoundManager Owner;
        internal SoundManager.ActiveSound Entry;
        internal int Id;

        public bool IsValid =>
            Owner != null && Entry != null && Entry.Id == Id && Entry.Source != null;

        public bool IsPlaying => IsValid && Entry.Source.isPlaying;

        public float Volume
        {
            get => IsValid ? Entry.Source.volume : 0f;
            set { if (IsValid) Entry.Source.volume = Mathf.Clamp01(value); }
        }

        public void Pause()  { if (IsValid) Entry.Source.Pause(); }
        public void Resume() { if (IsValid) Entry.Source.UnPause(); }

        /// <summary>즉시 정지하고 풀로 반환합니다.</summary>
        public void Stop() => Owner?.StopHandle(this, 0f);

        /// <summary>지정한 시간(초) 동안 페이드아웃 후 정지합니다.</summary>
        public void FadeOut(float duration) => Owner?.StopHandle(this, Mathf.Max(0f, duration));
    }
}
