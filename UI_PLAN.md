# SoundManager UI 변경 계획

---

## 현재 구조 요약

```
SoundListEditor (EditorWindow)
├── Topbar          : 제목 + Language + Create 버튼
├── SettingsPanel   : 데이터 폴더 경로
└── SplitView
    ├── Left  : 사운드 아이템 목록 (ScrollView)
    └── Right : SoundSo 인스펙터 (SoundSOEditor)

SoundSo 인스펙터
├── 이름 레이블
├── 기본 설정     : SoundName / Type / AudioClip / Loop / Priority
├── 오디오 설정   : Volume / Pitch / StereoPan / SpatialBlend
├── 랜덤 피치     : Toggle + Min/Max FloatField
└── 미리듣기      : Play / Pause / Stop + 진행 슬라이더
```

---

## 추가 시스템 & UI 변경 계획

---

### 1. SoundListEditor — 탭 구조로 개편

현재 Topbar 아래에 탭을 추가해 기능별로 분리합니다.

```
┌─────────────────────────────────────────────────────┐
│  SoundManager Editor          [Language] [Sounds ▼] │  ← Topbar
├──────────────────────────────────────────────────────│
│  [ Sounds ]  [ Settings ]                            │  ← Tab Bar (신규)
├──────────────────────────────────────────────────────│
│  (탭 내용)                                           │
└─────────────────────────────────────────────────────┘
```

#### Tab 1 — Sounds (현재와 동일)
```
┌────────────────┬────────────────────────────────────┐
│  [+ Create]    │                                    │
│ ─────────────  │   SoundSo 인스펙터                  │
│  BGM_Main      │   (선택한 아이템 표시)               │
│  SFX_Jump  [x] │                                    │
│  SFX_Hit   [x] │                                    │
└────────────────┴────────────────────────────────────┘
```

#### Tab 2 — Settings (신규)
```
┌─────────────────────────────────────────────────────┐
│  [Pool]                                             │
│    Pool Size        [  32  ]                        │
│    Auto Expand      [ ✓ ]                           │
│                                                     │
│  [Volume Defaults]                                  │
│    Master  ──●──────────  0.8  [Save to PlayerPrefs]│
│    BGM     ────●────────  1.0                       │
│    SFX     ────────●────  1.0                       │
│                                                     │
│  [Data Folder]                                      │
│    Assets/SoundManagerData          [Browse]        │
└─────────────────────────────────────────────────────┘
```

---

### 2. SoundSo 인스펙터 — 섹션 추가

기존 섹션은 유지하고 아래 섹션들을 추가합니다.

```
┌─────────────────────────────────────────────────────┐
│  BGM_Main                              ← 이름 레이블 │
├─────────────────────────────────────────────────────┤
│  [기본 설정]                                         │
│    Sound Name   [BGM_Main         ]                 │
│    SoundType    [ BGM ▼ ]                           │
│    Audio        [ MainTheme       ]                 │
│    IsLoop       [ ✓ ]                               │
│    Priority     [──●──────] 128                     │
├─────────────────────────────────────────────────────┤
│  [오디오 설정]                                       │
│    Volume       [────●──────] 1.0                   │
│    Pitch        [────●──────] 1.0                   │
│    StereoPan    [────●──────] 0.0                   │
│    SpatialBlend [●───────────] 0.0                  │
├─────────────────────────────────────────────────────┤
│  [3D 설정]  ← SpatialBlend > 0 일 때만 표시 (신규)  │
│    Min Distance [  1.0  ]                           │
│    Max Distance [ 15.0  ]                           │
├─────────────────────────────────────────────────────┤
│  [랜덤 피치]                                         │
│    Random Pitch [ ✓ ]                               │
│    Min [ 0.95 ]   Max [ 1.05 ]                      │
├─────────────────────────────────────────────────────┤
│  [페이드] (신규)                                     │
│    Fade In    [  0.5  ] 초                          │
│    Fade Out   [  1.0  ] 초                          │
├─────────────────────────────────────────────────────┤
│  [쿨다운] (신규)                                     │
│    Cooldown   [  0.1  ] 초                          │
│    힌트: 같은 사운드 중복 재생 최소 간격             │
├─────────────────────────────────────────────────────┤
│  [미리듣기]                                          │
│    [  재생  ]  [ 일시정지 ]  [  중지  ]              │
│    ▓▓▓▓▓░░░░░░░░░░░░░░░░░░░░░░                     │
└─────────────────────────────────────────────────────┘
```

#### 3D 설정 표시 조건
- `SpatialBlend > 0` 이면 자동으로 펼쳐짐
- `SpatialBlend == 0` 이면 숨김
- UIToolkit에서 `TrackPropertyValue`로 SpatialBlend 값 감지해서 `display` 토글

---

### 3. 변경이 필요한 파일 목록

| 파일 | 변경 내용 |
|------|-----------|
| `SoundSo.cs` | FadeIn, FadeOut, Cooldown, MinDistance, MaxDistance 필드 추가 |
| `SoundSOEditor.cs` | 페이드/쿨다운/3D 설정 섹션 추가, 3D 조건부 표시 로직 |
| `SoundSO_en_vr.uxml` | 새 섹션 UI 추가 |
| `SoundListEditor.cs` | 탭 시스템 구현, Settings 탭 추가 |
| `SoundListEditor.uxml` | Tab Bar 요소 추가 |
| `SoundManager.cs` | 풀링, 페이드, 쿨다운, 핸들, 3D 재생, 볼륨 관리 구현 |
| `SoundManagerConfig.cs` | 풀 사이즈, 기본 볼륨 설정 저장 추가 |
| `EditorLocalization.cs` | 새 섹션 텍스트 추가 |

---

### 4. 작업 순서 제안

```
1단계 — 데이터 모델 확장
  └─ SoundSo 필드 추가 (Fade, Cooldown, 3D Distance)

2단계 — 인스펙터 UI
  └─ SoundSO_en_vr.uxml + SoundSOEditor 섹션 추가
     └─ 3D 설정 조건부 표시 포함

3단계 — 에디터 탭 구조
  └─ SoundListEditor 탭 전환 + Settings 탭

4단계 — 런타임 시스템
  └─ 풀링 → 페이드 → 쿨다운 → 핸들 → 볼륨 관리 → 3D 재생
```
