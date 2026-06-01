using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using csiimnida.CSILib.MonoSingleton.RunTime;
using Random = UnityEngine.Random;

namespace csiimnida.CSILib.SoundManager.RunTime
{
    public class SoundManager : MonoSingleton<SoundManager>
    {
        [Header("Data")]
        [SerializeField] private SoundListSo soundListSo;

        [Header("Mixer")]
        [SerializeField] private AudioMixer mixer;
        [SerializeField] private AudioMixerGroup sfxGroup;
        [SerializeField] private AudioMixerGroup bgmGroup;

        [Header("Mixer Exposed Parameters")]
        [SerializeField] private string masterVolumeParam = "MasterVolume";
        [SerializeField] private string bgmVolumeParam = "BGMVolume";
        [SerializeField] private string sfxVolumeParam = "SFXVolume";

        // ── 활성 사운드 엔트리 (풀링 대상, 사용자에게 노출되지 않음) ──
        internal class ActiveSound
        {
            public AudioSource Source;
            public Transform Tr;          // source.transform 캐시 (extern 호출 절감)
            public SoundHandle Handle;
            public Transform Follow;
            public float TargetVolume;
            public bool Started;          // 실제 재생 시작을 확인했는지

            public string SoundName;      // 보이스 제한/스틸 집계용
            public bool Persist;          // 씬 전환에도 유지할지

            public int Id;                // 핸들 세대 검증용 (재사용 시 0 으로 무효화)
            public int Index;             // _active 내 위치 (swap-remove 용)

            public bool FadingIn;
            public float FadeInDuration, FadeInTimer;

            public bool FadingOut;
            public float FadeOutDuration, FadeOutTimer, FadeOutStartVolume;

            public bool HasScheduledFadeOut;
            public float ScheduledFadeOutAt, FadeOutLength;
        }

        // ── 풀 ──────────────────────────────────────────────────────
        private Transform _poolRoot;
        private readonly Queue<AudioSource> _pool = new Queue<AudioSource>();
        private readonly Stack<ActiveSound> _entryPool = new Stack<ActiveSound>();
        private int _poolSize;
        private bool _autoExpand;
        private int _nextId = 1;

        // ── 활성 사운드 목록 ────────────────────────────────────────
        private readonly List<ActiveSound> _active = new List<ActiveSound>();

        // ── 쿨다운 ──────────────────────────────────────────────────
        private readonly Dictionary<string, float> _lastPlayTime = new Dictionary<string, float>();

        // ── BGM ─────────────────────────────────────────────────────
        private SoundHandle _currentBGM;

        // ════════════════════════════════════════════════════════════
        // 초기화
        // ════════════════════════════════════════════════════════════
        protected override void Awake()
        {
            base.Awake();

            if (soundListSo == null)
                Debug.LogWarning("[SoundManager] SoundListSo가 할당되지 않았습니다. Inspector에서 SoundListSO 에셋을 지정하세요.");
            if (mixer == null)
                Debug.LogWarning("[SoundManager] AudioMixer가 할당되지 않았습니다. 믹서 라우팅 없이 동작합니다.");

            _poolSize   = SoundManagerPrefs.GetPoolSize();
            _autoExpand = SoundManagerPrefs.GetAutoExpand();

            _poolRoot = new GameObject("[SoundPool]").transform;
            _poolRoot.SetParent(transform);

            for (int i = 0; i < _poolSize; i++)
                _pool.Enqueue(CreatePooledSource());

            LoadVolumes();

            // 씬 전환에도 유지(D-⑨) 처리를 위한 훅
            SceneManager.activeSceneChanged += OnActiveSceneChanged;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            SceneManager.activeSceneChanged -= OnActiveSceneChanged;
        }

        // 씬이 바뀌면 'persistAcrossScenes' 가 아닌 재생 중인 사운드를 정지합니다.
        // (SoundManager 자체가 DontDestroyOnLoad 로 살아있을 때 의미가 있습니다.)
        private void OnActiveSceneChanged(Scene from, Scene to)
        {
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                ActiveSound e = _active[i];
                if (e.Source == null)
                {
                    RemoveActiveAt(i);
                    continue;
                }
                if (!e.Persist)
                    FinishActive(e);
            }

            if (_currentBGM != null && !_currentBGM.IsValid)
                _currentBGM = null;
        }

        private AudioSource CreatePooledSource()
        {
            var go = new GameObject("PooledAudio");
            go.transform.SetParent(_poolRoot);
            go.SetActive(false);
            var src = go.AddComponent<AudioSource>();
            src.playOnAwake = false;   // 활성화 시 자동 재생 방지
            return src;
        }

        // ════════════════════════════════════════════════════════════
        // 재생 API
        // ════════════════════════════════════════════════════════════
        public SoundHandle PlaySound(string soundName)
            => PlayInternal(soundName, false, Vector3.zero, null);

        /// <summary>지정한 월드 위치에서 3D로 재생합니다.</summary>
        public SoundHandle PlaySound(string soundName, Vector3 position)
            => PlayInternal(soundName, true, position, null);

        /// <summary>지정한 Transform 을 따라다니며 3D로 재생합니다.</summary>
        public SoundHandle PlaySound(string soundName, Transform follow)
            => PlayInternal(soundName, true, follow != null ? follow.position : Vector3.zero, follow);

        private SoundHandle PlayInternal(string soundName, bool useWorldPos, Vector3 position, Transform follow)
        {
            if (soundListSo == null || soundListSo.SoundsDictionary == null)
            {
                Debug.LogWarning("[SoundManager] SoundListSo 가 준비되지 않았습니다.");
                return null;
            }

            if (!soundListSo.SoundsDictionary.TryGetValue(soundName, out SoundSo so))
            {
                Debug.LogError($"[SoundManager] '{soundName}'을 찾을 수 없습니다.");
                return null;
            }

            // 배리에이션을 고려해 실제 재생할 클립 선택
            AudioClip chosenClip = so.PickClip();
            if (chosenClip == null)
            {
                Debug.LogError($"[SoundManager] '{soundName}'의 AudioClip이 null입니다.");
                return null;
            }

            float now = Time.unscaledTime;

            // 쿨다운 검사
            if (so.cooldown > 0f)
            {
                if (_lastPlayTime.TryGetValue(soundName, out float last) && now - last < so.cooldown)
                    return null;
                _lastPlayTime[soundName] = now;
            }

            // 동시 재생 제한 / 보이스 스틸
            if (so.maxVoices > 0 && !TryMakeRoomForVoice(soundName, so))
                return null;

            AudioSource source = GetSource();
            if (source == null) return null;

            bool is3D = useWorldPos || follow != null;
            Transform tr = source.transform;

            // 믹서 라우팅
            if (mixer != null)
            {
                source.outputAudioMixerGroup = so.soundType switch
                {
                    SoundType.SFX => sfxGroup,
                    SoundType.BGM => bgmGroup,
                    _ => null,
                };
            }

            if (is3D)
                tr.position = position;

            float targetVolume = ApplySettings(source, so, is3D, chosenClip);

            // 엔트리/핸들 구성
            int id = _nextId++;
            var handle = new SoundHandle { Owner = this };
            ActiveSound entry = RentEntry();

            entry.Source       = source;
            entry.Tr           = tr;
            entry.Handle       = handle;
            entry.Follow       = follow;
            entry.TargetVolume = targetVolume;
            entry.Started      = false;
            entry.SoundName    = soundName;
            entry.Persist      = so.persistAcrossScenes;
            entry.Id           = id;
            entry.FadingIn = false;  entry.FadeInTimer = 0f;
            entry.FadingOut = false; entry.FadeOutTimer = 0f;
            entry.HasScheduledFadeOut = false;

            handle.Entry = entry;
            handle.Id    = id;

            // 페이드 인
            if (so.fadeIn > 0f)
            {
                entry.FadingIn = true;
                entry.FadeInDuration = so.fadeIn;
                source.volume = 0f;
            }

            // 루프가 아니면 자연 종료에 맞춰 페이드아웃 예약
            // (시작 지점/재생 지연을 고려해 남은 재생 시간 계산)
            if (!so.loop && so.fadeOut > 0f)
            {
                float remaining = Mathf.Max(0f, source.clip.length - source.time);
                float duration = remaining / Mathf.Max(0.01f, Mathf.Abs(source.pitch));
                entry.HasScheduledFadeOut = true;
                entry.FadeOutLength = Mathf.Min(so.fadeOut, duration);
                entry.ScheduledFadeOutAt = now + so.playDelay + Mathf.Max(0f, duration - entry.FadeOutLength);
            }

            // 재생 지연(C-⑤) 적용
            if (so.playDelay > 0f)
                source.PlayDelayed(so.playDelay);
            else
                source.Play();

            entry.Index = _active.Count;
            _active.Add(entry);

            // BGM 단일 재생 처리
            if (so.soundType == SoundType.BGM)
            {
                if (_currentBGM != null && _currentBGM.IsValid && _currentBGM.Entry != entry)
                    StopHandle(_currentBGM, so.fadeIn > 0f ? so.fadeIn : 0.5f);
                _currentBGM = handle;
            }

            return handle;
        }

        /// <summary>현재 재생 중인 BGM을 페이드아웃하며 정지합니다.</summary>
        public void StopBGM(float fadeOut = 0.5f)
        {
            if (_currentBGM != null && _currentBGM.IsValid)
                StopHandle(_currentBGM, fadeOut);
            _currentBGM = null;
        }

        private float ApplySettings(AudioSource source, SoundSo so, bool is3D, AudioClip clip)
        {
            source.clip         = clip;
            source.loop         = so.loop;
            source.priority     = so.Priority;
            source.panStereo    = so.stereoPan;
            source.spatialBlend = is3D ? Mathf.Max(so.SpatialBlend, 0.0001f) : so.SpatialBlend;
            source.minDistance  = so.minDistance;
            source.maxDistance  = so.maxDistance;

            // 고급 설정: 일시정지 중에도 재생(D-⑧)
            source.ignoreListenerPause = so.ignoreListenerPause;

            // SO 데이터를 직접 수정하지 않고 로컬 변수로 피치 계산
            float pitch = so.RandomPitch ? Random.Range(so.MinPitch, so.MaxPitch) : so.pitch;
            source.pitch = pitch;

            float baseVolume = so.RandomVolume ? Random.Range(so.MinVolume, so.MaxVolume) : so.volume;
            float targetVolume = Mathf.Clamp01(baseVolume);
            source.volume = targetVolume;

            float len = source.clip != null ? source.clip.length : 0f;
            if (pitch < 0f && len > 0f)
            {
                source.time = len - 0.01f;
            }
            else if (len > 0f)
            {
                float maxStart = Mathf.Max(0f, len - 0.01f);
                float start = Mathf.Clamp(so.startOffset, 0f, maxStart);
                // 랜덤 시작 지점(A-②): 시작 지점 ~ 클립 끝 사이 임의 위치
                if (so.randomStartPosition)
                    start = Random.Range(start, maxStart);
                if (start > 0f)
                    source.time = start;
            }

            return targetVolume;
        }

        // ════════════════════════════════════════════════════════════
        // 보이스 제한 / 스틸
        // ════════════════════════════════════════════════════════════
        private int CountVoices(string soundName)
        {
            int count = 0;
            for (int i = 0; i < _active.Count; i++)
            {
                ActiveSound e = _active[i];
                if (e.Source != null && !e.FadingOut && e.SoundName == soundName)
                    count++;
            }
            return count;
        }

        /// <summary>maxVoices 한도 내에서 재생 슬롯을 확보합니다. 필요 시 규칙에 따라 기존 보이스를 끊습니다.</summary>
        private bool TryMakeRoomForVoice(string soundName, SoundSo so)
        {
            if (CountVoices(soundName) < so.maxVoices) return true;
            if (so.voiceSteal == VoiceStealMode.Skip) return false;

            ActiveSound victim = null;
            for (int i = 0; i < _active.Count; i++)
            {
                ActiveSound e = _active[i];
                if (e.Source == null || e.FadingOut || e.SoundName != soundName) continue;

                if (victim == null)
                {
                    victim = e;
                    continue;
                }

                bool better = so.voiceSteal == VoiceStealMode.Oldest
                    ? e.Id < victim.Id                       // 더 작은 Id = 더 오래됨
                    : e.Source.volume < victim.Source.volume; // 더 조용함
                if (better) victim = e;
            }

            if (victim == null) return false;
            FinishActive(victim);
            return true;
        }

        // ════════════════════════════════════════════════════════════
        // 풀 관리
        // ════════════════════════════════════════════════════════════
        private AudioSource GetSource()
        {
            AudioSource source;
            if (_pool.Count > 0)
                source = _pool.Dequeue();
            else if (_autoExpand)
                source = CreatePooledSource();
            else
            {
                Debug.LogWarning("[SoundManager] 풀이 가득 찼고 자동 확장이 꺼져 있어 재생을 건너뜁니다.");
                return null;
            }

            source.gameObject.SetActive(true);
            return source;
        }

        private void ReturnToPool(AudioSource source)
        {
            if (source == null) return;
            source.Stop();
            source.clip = null;
            source.outputAudioMixerGroup = null;
            Transform tr = source.transform;
            tr.SetParent(_poolRoot);
            tr.localPosition = Vector3.zero;
            source.gameObject.SetActive(false);
            _pool.Enqueue(source);
        }

        private ActiveSound RentEntry()
            => _entryPool.Count > 0 ? _entryPool.Pop() : new ActiveSound();

        // ════════════════════════════════════════════════════════════
        // 핸들 제어 (SoundHandle 에서 호출) — 모두 O(1)
        // ════════════════════════════════════════════════════════════
        internal void StopHandle(SoundHandle handle, float fadeOut)
        {
            if (handle == null || !handle.IsValid) return;
            ActiveSound e = handle.Entry;

            if (fadeOut <= 0f)
            {
                FinishActive(e);
            }
            else
            {
                e.FadingOut = true;
                e.FadingIn = false;
                e.FadeOutDuration = fadeOut;
                e.FadeOutTimer = 0f;
                e.FadeOutStartVolume = e.Source.volume;
            }
        }

        private void FinishActive(ActiveSound e)
        {
            AudioSource src = e.Source;

            e.Id = 0;          // 핸들 무효화 (세대 불일치)
            e.Source = null;
            e.Handle = null;
            e.Follow = null;
            e.Tr = null;

            RemoveActiveAt(e.Index);
            ReturnToPool(src);
            _entryPool.Push(e);
        }

        /// <summary>swap-remove: 마지막 원소를 빈 자리로 옮겨 O(1) 제거.</summary>
        private void RemoveActiveAt(int index)
        {
            int last = _active.Count - 1;
            if (index != last)
            {
                ActiveSound moved = _active[last];
                _active[index] = moved;
                moved.Index = index;
            }
            _active.RemoveAt(last);
        }

        // ════════════════════════════════════════════════════════════
        // 업데이트 루프 (페이드 / 추적 / 자동 반환)
        // ════════════════════════════════════════════════════════════
        private void Update()
        {
            if (_active.Count == 0) return;

            float dt  = Time.unscaledDeltaTime;
            float now = Time.unscaledTime;

            // 위에서 아래로 순회 → swap-remove 시 이미 처리한 원소만 앞으로 이동하므로 안전
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                ActiveSound e = _active[i];

                if (e.Source == null)
                {
                    RemoveActiveAt(i);
                    continue;
                }

                if (e.Follow != null)
                    e.Tr.position = e.Follow.position;

                if (!e.Started && (e.Source.isPlaying || e.Source.time > 0f))
                    e.Started = true;

                // 페이드 인
                if (e.FadingIn)
                {
                    e.FadeInTimer += dt;
                    float t = Mathf.Clamp01(e.FadeInTimer / e.FadeInDuration);
                    e.Source.volume = e.TargetVolume * t;
                    if (t >= 1f) e.FadingIn = false;
                }

                // 예약된 자동 페이드아웃 시작
                if (e.HasScheduledFadeOut && !e.FadingOut && now >= e.ScheduledFadeOutAt)
                {
                    e.HasScheduledFadeOut = false;
                    e.FadingOut = true;
                    e.FadeOutDuration = e.FadeOutLength;
                    e.FadeOutTimer = 0f;
                    e.FadeOutStartVolume = e.Source.volume;
                }

                // 페이드 아웃
                if (e.FadingOut)
                {
                    e.FadeOutTimer += dt;
                    float t = Mathf.Clamp01(e.FadeOutTimer / e.FadeOutDuration);
                    e.Source.volume = e.FadeOutStartVolume * (1f - t);
                    if (t >= 1f)
                    {
                        FinishActive(e);
                        continue;
                    }
                }

                // 자연 종료: 재생이 시작됐고, 루프가 아니며, 더 이상 재생 중이 아니고,
                // time 이 0 으로 리셋됨(일시정지는 time 이 유지되므로 제외).
                if (e.Started && !e.FadingOut && !e.Source.loop
                    && !e.Source.isPlaying && e.Source.time <= 0f)
                {
                    FinishActive(e);
                }
            }
        }

        // ════════════════════════════════════════════════════════════
        // 볼륨 관리 (AudioMixer Exposed Parameter)
        // ════════════════════════════════════════════════════════════
        public void SetMasterVolume(float linear) => SetMixerVolume(masterVolumeParam, linear, SoundManagerPrefs.MasterVolume);
        public void SetBGMVolume(float linear)    => SetMixerVolume(bgmVolumeParam, linear, SoundManagerPrefs.BGMVolume);
        public void SetSFXVolume(float linear)    => SetMixerVolume(sfxVolumeParam, linear, SoundManagerPrefs.SFXVolume);

        private void SetMixerVolume(string param, float linear, string prefsKey)
        {
            linear = Mathf.Clamp01(linear);
            PlayerPrefs.SetFloat(prefsKey, linear);

            if (mixer == null || string.IsNullOrEmpty(param)) return;

            // 0~1 선형 → 데시벨. 0 일 때는 -80dB(사실상 음소거).
            float dB = linear <= 0.0001f ? -80f : Mathf.Log10(linear) * 20f;
            if (!mixer.SetFloat(param, dB))
                Debug.LogWarning($"[SoundManager] 믹서에 노출된 파라미터 '{param}' 를 찾을 수 없습니다.");
        }

        /// <summary>저장된 볼륨 값을 믹서에 적용합니다.</summary>
        public void LoadVolumes()
        {
            SetMasterVolume(SoundManagerPrefs.GetMasterVolume());
            SetBGMVolume(SoundManagerPrefs.GetBGMVolume());
            SetSFXVolume(SoundManagerPrefs.GetSFXVolume());
        }

        public void SaveVolumes() => PlayerPrefs.Save();
    }
}
