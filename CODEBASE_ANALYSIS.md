# SoundManager 코드베이스 분석 문서

> 리메이킹 준비용 — 현재 구조의 전체 파악 및 문제점 정리

---

## 1. 전체 구조 개요

```
Assets/csiimnida/CSILib/SoundManager/
├── RunTime/                        (Assembly: SoundManager.RunTime)
│   ├── SoundType.cs                enum 정의
│   ├── SoundSo.cs                  개별 사운드 데이터 (ScriptableObject)
│   ├── SoundListSo.cs              사운드 목록 컨테이너 (ScriptableObject)
│   ├── SoundManager.cs             런타임 사운드 재생 매니저 (MonoSingleton)
│   ├── MonoSingleton.cs            제네릭 싱글톤 베이스
│   ├── TempSoundPlayer.cs          씬에서 사운드 트리거용 컴포넌트
│   └── DestroyTempAudio.cs         임시 오디오 오브젝트 자동 제거
└── Editor/                         (Assembly: SoundManagerr.editor)
    ├── SoundListEditor.cs          에디터 윈도우 (Window/CSILib/SoundManager)
    ├── SoundSOEditor.cs            SoundSo 커스텀 인스펙터
    ├── SoundItemUI.cs              목록 아이템 UI 래퍼
    ├── SplitView.cs                TwoPaneSplitView UXML 확장
    └── UIS/
        ├── SoundSO.uxml            인스펙터 UI (한국어)
        ├── SoundSO_en_vr.uxml      인스펙터 UI (영어)
        └── SoundListUIs/
            ├── SoundListEditor.uxml
            └── SoundItemUI.uxml
```

---

## 2. 각 클래스 상세 분석

### 2-1. `SoundType` (enum)
```
namespace: csiimnida.CSILib.SoundManager.RunTime
값: BGM, SFX
```
- AudioMixer 라우팅 분기에만 사용됨.

---

### 2-2. `SoundSo` (ScriptableObject)
개별 사운드 하나의 설정값을 담는 데이터 에셋.

| 필드 | 타입 | 기본값 | 설명 |
|------|------|--------|------|
| soundName | string | - | 딕셔너리 키로 사용되는 식별자 |
| soundType | SoundType | SFX | BGM/SFX 믹서 그룹 분기 |
| clip | AudioClip | null | 실제 오디오 클립 |
| loop | bool | false | 루프 여부 |
| Priority | int | 128 | AudioSource 우선순위 (0=최고, 256=최저) |
| volume | float | 1.0 | 볼륨 (0~1) |
| pitch | float | 1.0 | 피치 (-3~3) |
| stereoPan | float | 0.0 | 스테레오 패닝 (-1~1) |
| SpatialBlend | float | 0.0 | 2D/3D 블렌드 (0~1) |
| RandomPitch | bool | false | 랜덤 피치 활성화 |
| MinPitch | float | 0.95 | 랜덤 피치 최솟값 |
| MaxPitch | float | 1.05 | 랜덤 피치 최댓값 |

**문제점:**
- `soundName`과 실제 에셋 파일명을 별도로 관리 → 두 개가 불일치할 수 있음
- `Priority` 필드명이 대문자 시작 (C# 컨벤션 위반, 프로퍼티처럼 보임)
- `SpatialBlend` 필드명도 대문자 시작 (일관성 없음)

---

### 2-3. `SoundListSo` (ScriptableObject)
`SoundSo` 목록을 관리하고 딕셔너리로 빠른 조회를 지원.

```csharp
// 내부 구조
List<SoundSo> Sounds          // 직렬화되는 실제 목록
Dictionary<string, SoundSo> SoundsDictionary  // 런타임 조회용 (soundName → SoundSo)
```

**문제점:**
- `SoundsDictionary`가 `OnEnable()`에서만 빌드됨 → 런타임 중 `AddSound()` 호출 시 딕셔너리가 갱신되지 않음
- `SoundsDictionary`가 `public`으로 노출되어 외부에서 직접 수정 가능
- null 방어가 `OnEnable`에만 있음, `AddSound`/`RemoveSound`에는 없음

---

### 2-4. `SoundManager` (MonoSingleton\<SoundManager\>)
씬에 배치하는 런타임 매니저. 사운드 재생의 진입점.

```csharp
// 의존성 (Inspector 할당 필요)
SoundListSo soundListSo
AudioMixer mixer
AudioMixerGroup sfxGroup
AudioMixerGroup bgmGroup
```

**`PlaySound(string soundName)` 흐름:**
1. 빈 `GameObject` 생성 + `AudioSource` 컴포넌트 추가
2. `soundListSo.SoundsDictionary`에서 `SoundSo` 조회
3. `soundType`에 따라 믹서 그룹 할당
4. `SetAudio()`로 AudioSource 파라미터 세팅 후 `Play()`
5. loop가 아닌 경우 `DestroyCo()`로 clip 길이 + 0.2초 후 오브젝트 자동 제거

**문제점:**
- 재생할 때마다 `new GameObject()`를 생성 → 오브젝트 풀링 없음
- 반환값이 `GameObject`지만 호출부에서 대부분 무시됨 (HowToUse.cs 참고)
- `SetAudio()` 안에서 `sounds.pitch`를 직접 수정함 (원본 데이터 오염)
  ```csharp
  // 버그: SO 데이터를 직접 변경
  sounds.pitch = Random.Range(sounds.MinPitch, sounds.MaxPitch);
  ```
- `DestroyCo()`가 `async void`로, 예외 발생 시 `throw`로 재던지지만 async void에서 예외는 크래시 유발
- pitch < 0일 때 `source.time = 1` 하드코딩 (역재생 구현이 미완성)
- `Awake()`에서 `Debug.Assert`와 `Debug.LogError` 혼용 (일관성 없음)
- mixer가 null이어도 재생은 되지만 믹서 라우팅 없이 동작 (의도된 fallback인지 불명확)

---

### 2-5. `MonoSingleton<T>`
씬 내 단일 인스턴스를 보장하는 제네릭 베이스 클래스.

```csharp
// Instance 접근 흐름
IsDestroyed → _instance null 처리
→ FindAnyObjectByType<T>() 탐색
→ 없으면 LogError (자동 생성 없음)
```

**문제점:**
- `DontDestroyOnLoad` 없음 → 씬 전환 시 파괴됨
- `FindAnyObjectByType<T>()`는 매 접근마다 씬 탐색 (캐시 미스 시 비용 큼)
- `IsDestroyed`가 `static`이라 T별로 공유되지 않음 — 실제로는 제네릭이라 각각 별도 static이므로 문제없지만 가독성이 나쁨
- 자동 생성 로직 없음, 씬에 없으면 에러만 출력하고 null 반환

---

### 2-6. `TempSoundPlayer`
씬 오브젝트에 붙여서 Start 시점에 사운드를 트리거하는 단순 컴포넌트.

**문제점:**
- soundName이 인스펙터에서만 설정 가능, 오타 시 런타임 에러
- `Start()`에서 바로 호출 → 씬 로드 타이밍에 SoundManager 초기화 보장 없음

---

### 2-7. `DestroyTempAudio`
`Start()`에서 즉시 `Destroy(gameObject)` 호출. 에디터 미리듣기용 임시 AudioSource 정리에 사용.

**문제점:**
- 역할이 불분명. 실제로는 `SoundSOEditor`에서 에디터 전용 AudioSource GameObject에 붙이는 용도인데, 컴포넌트 이름만 봐서는 알 수 없음
- `Start()`에서 즉시 Destroy → AudioSource가 재생되기도 전에 파괴될 수 있음 (AudioSource.Play()는 비동기적으로 시작됨)

---

### 2-8. `SoundListEditor` (EditorWindow)
`Window > CSILib > SoundManager` 메뉴로 여는 커스텀 에디터 윈도우.

**기능:**
- `SoundListSo` 에셋 자동 로드/생성
- 좌측: 사운드 아이템 목록 (ScrollView + SoundItemUI)
- 우측: 선택한 SoundSo의 커스텀 인스펙터 (SoundSOEditor)
- 새 SoundSo 생성 → GUID로 이름 지정 → `Sounds/` 폴더에 에셋 저장
- 삭제: DisplayDialog 확인 후 에셋 파일 삭제
- 언어 전환 버튼 (한국어/영어, PlayerPrefs `SoundManagerLan` 키)

**문제점:**
- `InitializeWindow(en_vr)` 파라미터가 있지만 `CreateGUI()`에서 항상 `false`로 호출
- en_vr 분기 코드가 동일한 uxml을 로드 (버그)
- 새 사운드의 이름이 GUID → 즉시 알아볼 수 없는 이름
- `_cachedEditor`를 직접 관리, 메모리 누수 가능성
- `OnBecameInvisible`, `OnDestroy`가 비어있음

---

### 2-9. `SoundSOEditor` (CustomEditor)
`SoundSo` 에셋의 커스텀 인스펙터. UXML 기반 UI.

**기능:**
- 에디터 내 오디오 미리듣기 (재생/일시정지/정지)
- AudioClip이 null이면 재생 불가 경고 다이얼로그
- 사운드 이름 변경 → 에셋 파일명도 같이 변경 (AssetDatabase.RenameAsset)
- 랜덤 피치 토글 시 min/max FloatField 활성화/비활성화
- 재생 진행 슬라이더 (EditorApplication.update 훅)
- 언어 분기: PlayerPrefs `SoundManagerLan` 값으로 한/영 uxml 전환

**문제점:**
- `EditorApplication.update += Update` 등록 후 해제를 `OnDestroy`와 `ResetSound`에서만 함 → 에디터 윈도우가 닫힐 때 `OnDestroy`가 호출 안 되는 케이스에서 누수
- `SoundSOEditor`가 자체적으로 `SoundListSo`를 로드하는 로직 포함 — 인스펙터가 리스트 에디터에 종속적
- TempSource GameObject가 씬에 남는 경우가 있을 수 있음 (PlayMode 진입 시)
- 역재생(pitch < 0) 처리가 `time = 1` 하드코딩으로 미완성

---

### 2-10. `SoundItemUI`
목록의 개별 아이템을 래핑하는 클래스. UXML 인스턴스와 `SoundSo`를 연결.

- `OnSelectEvent`, `OnDeleteEvent` 이벤트로 외부에 위임
- `IsActive` : CSS 클래스 `active` 토글로 선택 상태 표시

---

### 2-11. `SplitView`
`TwoPaneSplitView`를 `[UxmlElement]`로 등록한 래퍼. UXML에서 직접 사용 가능하게 하기 위한 용도.

---

## 3. 데이터 흐름 요약

```
[에디터]
SoundListEditor (EditorWindow)
  └─ SoundListSo (SO 에셋)
       └─ List<SoundSo>  ←  각각 별도 .asset 파일로 저장
            └─ SoundSo (SO 에셋)

[런타임]
SoundManager (씬에 배치된 MonoBehaviour)
  ├─ SoundListSo (Inspector 할당)
  │    └─ Dictionary<string, SoundSo>  ← OnEnable에서 빌드
  ├─ AudioMixer (Inspector 할당)
  ├─ sfxGroup (Inspector 할당)
  └─ bgmGroup (Inspector 할당)

PlaySound("name")
  → Dictionary 조회
  → new GameObject + AudioSource
  → 믹서 그룹 라우팅
  → AudioSource 파라미터 세팅
  → Play() + async Destroy (loop 아닌 경우)
```

---

## 4. 현재 버전의 주요 문제점 목록

| 심각도 | 위치 | 문제 |
|--------|------|------|
| 높음 | SoundManager.SetAudio | `sounds.pitch` 원본 SO 데이터를 직접 수정 (랜덤 피치 적용 시) |
| 높음 | SoundManager.DestroyCo | `async void`에서 `throw` → 크래시 유발 가능 |
| 높음 | SoundListSo | `AddSound()` 후 딕셔너리 미갱신 → 런타임 중 추가 사운드 조회 불가 |
| 중간 | SoundManager.PlaySound | 매 호출마다 GameObject 생성 → 풀링 없음 |
| 중간 | MonoSingleton | DontDestroyOnLoad 없음 → 씬 전환 시 소멸 |
| 중간 | SoundSOEditor | EditorApplication.update 누수 가능성 |
| 낮음 | SoundSo | `Priority`, `SpatialBlend` 필드 네이밍 컨벤션 불일치 |
| 낮음 | SoundManager.SetAudio | pitch < 0 역재생 처리 미완성 (time = 1 하드코딩) |
| 낮음 | DestroyTempAudio | Start에서 즉시 Destroy → 역할 불명확 |
| 낮음 | SoundListEditor | 새 사운드 이름이 GUID |

---

## 5. 리메이킹 시 고려할 것들

### 구조 개선
- **오브젝트 풀링**: `PlaySound` 마다 `new GameObject`는 GC 부담 → AudioSource 풀 도입
- **딕셔너리 동기화**: `AddSound`/`RemoveSound` 시 딕셔너리도 함께 갱신
- **SO 데이터 불변**: `SetAudio`에서 SO 값을 복사해서 쓰고 원본은 수정 금지
- **씬 전환 지원**: `DontDestroyOnLoad` + 씬 언로드 시 재생 중인 사운드 처리

### API 개선
- `PlaySound` 반환값 설계 (핸들/토큰 방식으로 개별 사운드 제어 가능하게)
- 볼륨/피치 런타임 오버라이드 파라미터 지원
- 재생 완료 콜백 지원

### 에디터 개선
- 언어 전환을 uxml 교체가 아닌 `LocalizationTable` 방식으로
- GUID 기본 이름 대신 `New Sound 1`, `New Sound 2` 방식
- SoundSo 이름 중복 검증

### 오류 처리
- `async void` → `UniTask` 또는 코루틴으로 교체
- `Debug.Assert` / `Debug.LogError` 통일
- SoundManager 없을 때 자동 생성 옵션 (`[DefaultExecutionOrder]` + Resources 로드)
