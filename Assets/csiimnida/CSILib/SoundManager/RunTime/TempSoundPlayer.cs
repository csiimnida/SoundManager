using UnityEngine;

namespace csiimnida.CSILib.SoundManager.RunTime
{
    /// <summary>
    /// 테스트용: Start 시 한 번 재생합니다. SoundManager와 SoundListSo가 씬에 준비된 경우에만 동작합니다.
    /// </summary>
    public class TempSoundPlayer : MonoBehaviour
    {
        [SerializeField] private string soundName;

        private void Start()
        {
            if (string.IsNullOrEmpty(soundName)) return;

            if (!SoundManager.TryGetInstance(out SoundManager manager))
            {
                Debug.LogWarning("[TempSoundPlayer] 씬에 SoundManager가 없습니다.");
                return;
            }

            manager.PlaySound(soundName);
        }
    }
}
