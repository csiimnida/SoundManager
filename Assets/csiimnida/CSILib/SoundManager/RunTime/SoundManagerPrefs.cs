using UnityEngine;

namespace csiimnida.CSILib.SoundManager.RunTime
{
    /// <summary>
    /// 런타임과 에디터가 함께 공유하는 PlayerPrefs 키와 기본값.
    /// (에디터 Settings 탭이 값을 쓰고, 런타임 SoundManager가 읽습니다.)
    /// </summary>
    public static class SoundManagerPrefs
    {
        // 언어 (0 = EN, 1 = KR)
        public const string Language = "SoundManagerLan";

        // 볼륨 (0~1 선형)
        public const string MasterVolume = "SoundManager_MasterVolume";
        public const string BGMVolume    = "SoundManager_BGMVolume";
        public const string SFXVolume    = "SoundManager_SFXVolume";

        // 오디오 소스 풀
        public const string PoolSize     = "SoundManager_PoolSize";
        public const string PoolAutoExpand = "SoundManager_PoolAutoExpand";

        // 기본값
        public const float DefaultVolume = 1f;
        public const int   DefaultPoolSize = 16;
        public const bool  DefaultAutoExpand = true;

        public static float GetMasterVolume() => PlayerPrefs.GetFloat(MasterVolume, DefaultVolume);
        public static float GetBGMVolume()    => PlayerPrefs.GetFloat(BGMVolume, DefaultVolume);
        public static float GetSFXVolume()    => PlayerPrefs.GetFloat(SFXVolume, DefaultVolume);
        public static int   GetPoolSize()     => PlayerPrefs.GetInt(PoolSize, DefaultPoolSize);
        public static bool  GetAutoExpand()   => PlayerPrefs.GetInt(PoolAutoExpand, DefaultAutoExpand ? 1 : 0) == 1;
    }
}
