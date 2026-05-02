using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Audio;
using Random = UnityEngine.Random;

namespace csiimnida.CSILib.SoundManager.RunTime
{
    public class SoundManager : MonoSingleton<SoundManager>
    {
    
        [SerializeField] private SoundListSo soundListSo;
        [SerializeField] private AudioMixer mixer;
        [SerializeField] private AudioMixerGroup sfxGroup;
        [SerializeField] private AudioMixerGroup bgmGroup;
        
        private void Awake()
        {
            if (soundListSo == null)
            {
                Debug.Assert(soundListSo != null,$"SoundListSo asset is null");
            }
            if (mixer == null)
            {
                Debug.LogError("AudioMixer가 할당되지 않았습니다. SoundManager를 사용하기 전에 할당해주세요.");
            }
        }
        public GameObject PlaySound(string soundName)
        {
            GameObject obj = new GameObject();
            obj.name = soundName + " Sound";
            AudioSource source = obj.AddComponent<AudioSource>();
            SoundSo so;
            soundListSo.SoundsDictionary.TryGetValue(soundName, out so);
            if (so == null)
            {
                Debug.LogError($"{soundName}이 없습니다.");
                Destroy(obj);
                return null;
            }
            if (mixer == null)
            {
                Debug.LogWarning("Mixer가 할당되지 않았습니다. SoundManager를 사용하기 전에 할당해주세요.");
                SetAudio(source,so);
                return obj;
            }
            source.outputAudioMixerGroup = so.soundType switch
            {
                SoundType.SFX => sfxGroup,
                SoundType.BGM =>  bgmGroup,
                _ => mixer.FindMatchingGroups("Master")[0], //todo
            };
            
                /*Debug.LogWarning("Type이 없습니다");
                source.outputAudioMixerGroup = mixer.FindMatchingGroups("Master")[0];*/

            SetAudio(source,so);
            return obj;
        }

        private void SetAudio(AudioSource source,SoundSo sounds)
        {
            source.clip = sounds.clip;
            source.loop = sounds.loop;
            source.priority = sounds.Priority;
            source.volume = sounds.volume;
            source.pitch = sounds.pitch;
            source.panStereo = sounds.stereoPan;
            source.spatialBlend = sounds.SpatialBlend;
            if (sounds.RandomPitch)
            {
                sounds.pitch = Random.Range(sounds.MinPitch, sounds.MaxPitch);
            }
            if (sounds.pitch < 0)
            {
                source.time = 1;
            }
            source.Play();
            if (!sounds.loop) 
            {
                DestroyCo(source.clip.length,source.gameObject);
            }

        }

        private async void DestroyCo(float endTime,GameObject obj)
        {
            try
            {
                await Awaitable.WaitForSecondsAsync(endTime + 0.2f);
                Destroy(obj);
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                throw; // TODO 예외 처리
            }
        }
    }


}