using System.Collections.Generic;
using UnityEngine;

namespace csiimnida.CSILib.SoundManager.RunTime
{
    public class SoundListSo : ScriptableObject
    {
        [SerializeField] private List<SoundSo> Sounds = new List<SoundSo>();

        public Dictionary<string, SoundSo> SoundsDictionary { get; private set; }

        private void OnEnable()
        {
            RebuildDictionary();
        }

        private void RebuildDictionary()
        {
            SoundsDictionary = new Dictionary<string, SoundSo>();
            if (Sounds == null) return;
            foreach (SoundSo soundSo in Sounds)
            {
                if (soundSo == null) continue;
                SoundsDictionary[soundSo.soundName] = soundSo;
            }
        }

        public void AddSound(SoundSo soundSo)
        {
            if (soundSo == null) return;
            Sounds.Add(soundSo);
            SoundsDictionary ??= new Dictionary<string, SoundSo>();
            SoundsDictionary[soundSo.soundName] = soundSo;
        }

        public List<SoundSo> GetSoundList() => Sounds;

        public void RemoveSound(SoundSo so)
        {
            if (so == null) return;
            Sounds.Remove(so);
            SoundsDictionary?.Remove(so.soundName);
        }
    }
}
