### ⚠️ IMPORTANT NOTICE / DISCLAIMER

**Original Author:** SleepingPills
**Original Repository:** HollywoodGraphics
**Original Link:** https://github.com/SleepingPills/HollywoodGraphics
**License:** MIT
**This Port By:** R_F (danyhappy564-cmyk) — unofficial, AI-assisted port. Not affiliated with or endorsed by the original author.

1. **Reflection & Take-Downs:** I deeply reflect on the ECOT incident. As an AI-assisted "vibe coder," I will immediately delete files if the original authors ask.
2. **No Re-Distribution:** These ported builds are unverified, temporary fixes. Please do NOT re-upload or share them anywhere else.
3. **Do Not Pester Original Authors:** Never report bugs or pester original modders regarding issues from my unofficial ports.
4. **Full Credit & Respect:** I will always credit original creators on GitHub and prioritize their decisions above all else.
5. **Support Original Creators:** Instead of using my ports, please visit the original authors' Forge pages to leave kind words or tips.

---

# HollywoodGraphics (SPT 4.1.5)

> **원작자 · 원본 레포**
> **SleepingPills** — https://github.com/SleepingPills/HollywoodGraphics
> SPT Forge에도 게시되어 있습니다.
>
> **라이선스: MIT** (원작 레포의 `LICENSE` 파일, 이 포크에도 그대로 포함)
>
> 이 레포는 위 원작을 **SPT 4.1.5에서 동작하도록 포팅한 포크**입니다.
> 기능을 추가하거나 바꾼 것이 아니라, 4.1의 클라이언트 역난독화로 깨진 참조를
> 되살린 것이 전부입니다. 모드 자체의 설계는 전부 원작자의 것입니다.

---

원작: SleepingPills / 이 포크: SPT 4.1.5 대응 (직전 4.0.10 대응 상태에서 이관)

**원작에는 README가 없습니다.** 그래서 아래 <모드 설명>은 번역이 아니라 코드와 설정
항목에서 제가 정리한 것이고, 원작자가 쓴 글이 아닙니다. 그 아래 <상세 변경점>이
이 포크에서 실제로 바꾼 내용입니다.

---

<모드 설명 — 원작에 README가 없어 코드에서 정리한 것>

게임의 후처리 그래픽을 손보는 클라이언트 플러그인입니다. 서버 모드는 없습니다.

- **앰비언트 오클루전 (HBAO)** — 강도, 반경, 바이어스, 컬러 블리딩, 오프스크린 영향
- **블룸 (Ultimate Bloom)** — 밝기 구간별(어두운/중간/밝은/하이라이트) 강도를 따로
  잡습니다. 렌즈 더스트, 아나모픽 플레어, 스타 플레어
- **모션 블러** — 셔터 앵글과 샘플 수
- **지형 디테일** — 풀·잡초의 표시 거리와 밀도 오버라이드
- **맵별 프리셋** — 라이드 시작 시 맵을 보고 그 맵의 설정을 적용합니다

**Amand's Graphics가 설치되어 있으면 자동으로 비활성화됩니다.** 둘 다 같은 후처리를
건드리기 때문입니다.

F12 설정에서 조절하고, 맵별로 따로 저장됩니다.

**빌드** — .NET SDK와 SPT 4.1 설치본이 필요합니다.

```
dotnet build
dotnet build -p:SptRoot="D:\내 SPT 경로"     # 기본값이 아닐 때
dotnet build -p:AutoInstall=false            # 설치본에 복사하지 않기
```

빌드가 끝나면 `BepInEx\plugins\HollywoodGraphics\`로 자동 복사됩니다. 게임이 켜져
있으면 DLL이 잠겨 복사가 실패하는데, 빌드를 실패시키지 않고 경고만 남깁니다.

빌드 후 설치본 복사에는 `RELEASE_README.txt`와 `LICENSE`도 함께 들어갑니다. MIT는
재배포 시 라이선스 전문과 저작권 표기를 동봉하도록 요구하고, 원작자도 크레딧을
파일 옆에 두기를 요청했습니다. 배포용 zip을 만들 때 따로 챙길 필요 없이 플러그인
폴더에 이미 들어 있게 됩니다.


---

<26/08/30 상세 변경점>

- 게임 어셈블리 참조가 원작자 로컬 폴더 구조(`..\..\..\Client_Dev\...` 상대경로)로
  하드코딩되어 있어서 다른 환경에서는 어셈블리를 못 찾던 문제 — `SptRoot` 속성으로
  오버라이드 가능하게 수정

- **매 프레임 NullReferenceException 도배** — `Bloom.cs` 생성자가
  `AddComponent<UltimateBloom>()` 직후 아직 `Start()`가 안 돈 상태의 필드를 바로
  읽다가 죽던 문제. null 체크 3곳 추가:
  - `ResetIntensities`의 배열 체크
  - `Bloom.Update()`의 `_ultimateBloom` 체크
  - `GraphicsController.Update()`의 `_bloom` 체크 — 결정적 수정. 생성자가 어떤
    이유로 실패하든 매 프레임 도배를 막는 최종 안전망
  - `UpdateMapSettings` / `UpdateBloomSettings` / `UpdateLensDust` 등 외부에서도
    호출되는 public 메서드도 같은 이유로 null 안전 처리
  - 생성자 실패 자체는 라이드당 한 번 정도 여전히 뜨지만(무해, 비주얼 차이 없음)
    반복 도배는 사라짐 — 실전 로그로 확인

- `AmbientOcclusion.cs`도 같은 종류의 버그 — `camera.GetComponent<HBAO>()`에 null
  체크 없이 다음 줄에서 필드를 읽던 문제. null 체크 추가

- **나이트비전 착용 시 화면이 비정상적으로 어둡던 문제** — 이미지 이펙트가 컴포넌트
  순서대로 실행되는데 HBAO(AO)가 NightVision보다 먼저 돌아서, AO가 화면을 어둡게 만든
  다음 나이트비전이 그 위에 증폭을 거는 구조였습니다. 컴포넌트 순서를 바꾸는 안전한
  런타임 API가 없어서, 나이트비전이 켜져 있는 동안만 AO를 끄고 꺼지면 복구하는
  방식으로 수정 (모든 맵에서 재현, 필드 테스트로 개선 확인)

---

<26/09/03 상세 변경점>

- **(08/30 수정 정정)** 나이트비전 AO 가드가 "밝아졌다"고 확인했던 건 반쪽짜리
  확인이었습니다. `NightVision.enabled`로 온/오프를 판단했는데, 이 필드는 켜는
  순간엔 반영되지만 **끄는 순간엔 전혀 안 바뀝니다** (N키로 여러 번 껐다 켜도 로그상
  "꺼짐" 전환이 한 번도 안 뜸). 즉 라이드 중 나이트비전을 한 번이라도 쓰면 그 뒤로는
  꺼도 AO가 라이드 끝까지 억제된 채로 남던 버그 — "나이트비전 안 써도 AO가 꺼진 것
  같다"는 필드 리포트로 발견

- 정확한 신호를 몰라서 `NightVision` 컴포넌트의 모든 bool 필드를 리플렉션으로 훑어
  토글 시점에 실제로 값이 바뀌는 걸 로그로 잡는 진단을 임시로 넣었고, 라이드에서 N키를
  여러 번 눌러본 결과 private 필드 `_on`이 정확히 토글에 맞춰 전환됨을 확인
  (`.enabled`는 계속 True 고정)

- `_on`을 읽도록 가드 교체, 진단용 전체 필드 덤프 코드는 제거

---

<26/09/07 상세 변경점 — SPT 4.1.5 대응>

**SPT 4.1은 클라이언트를 역난독화했습니다.** 타입들이 진짜 이름과 네임스페이스를 갖게
됐고, **4.0 클라이언트 모드는 4.1에서 하나도 로드되지 않습니다.**

## 이름 바뀐 것

공식 위키의 5,957줄짜리 매핑 표에 이 모드의 모든 식별자를 대조했습니다.
**바뀐 것은 하나뿐입니다** — 이 모드가 EFT 타입을 적게 건드리는 덕분입니다:

| 4.0 | 4.1 |
| --- | --- |
| `CameraClass` | `EFT.CameraControl.CameraManager` |

3곳(`AmbientOcclusion.cs`, `Bloom.cs`, `MotionBlur.cs`) + `using EFT.CameraControl;`.

나머지(`GameWorld`, `TarkovApplication`, `LampController`, `GPUInstancerDetailManager`,
`FlareLight`, `MaterialEmission`, `RaidSettings`, `BSG.CameraEffects.NightVision`,
`HBAO`, `UltimateBloom`)는 표에 없으므로 이름이 그대로입니다. 4.1.4에서 되돌린
직렬화 필드 이름 표도 확인했는데 해당 없습니다.

## `method_41` → `LocalGameMatching`

라이드 초기화 패치의 대상입니다. 위키 표는 **타입만** 다루고 메서드는 안 다뤄서,
이건 다른 방법으로 찾았습니다.

어셈블리 덤프 두 개(역난독화 **전**과 **후**)가 같은 파일의 전·후라 메서드 순서가
같습니다. 난독화된 메서드만 순서대로 0,1,2… 세면 de4dot의 번호가 재구성됩니다.
`assembly-tool`이 이름을 알려준 메서드 11개를 앵커로 검증한 결과 **오프셋 +2에서
11/11 일치**했습니다 (`method_38 = OnApplicationLoaded`, `method_49 = LocalGameCreate` …).

그 정렬에서:

```
method_40 = OnAbortFinished
method_41 = LocalGameMatching      ← Task LocalGameMatching(TimeAndWeatherSettings, bool)
method_42 = NetworkGameMatching    ← 네트워크 게임용 짝
```

싱글플레이 라이드로 매칭해 들어가는 지점입니다. `_raidSettings`가 채워져 있고 맵이
로드되기 전이라, 맵별 그래픽 오버라이드를 정하는 이 패치의 자리로 의미까지 맞습니다.
라이드 실측으로 확인됐습니다:

```
[HollywoodGraphics] Running raid initialization
[HollywoodGraphics] Graphics overrides map: woods - Woods enabled: True
```

이제 진짜 이름이라 다음에 없어지면 **컴파일 에러**로 잡힙니다. 조용히 엉뚱한 메서드에
붙을 일이 없어졌습니다.

## 나이트비전 AO 가드 — 4.1에서 죽어 있던 것

09/03에 고친 가드가 4.1에서 **무력화**돼 있었습니다. 폴백이 설계대로 안전하게
동작해서 크래시는 없었지만, 폴백이 하필 "수정 전 상태"라 **나이트비전 착용 시 화면이
어두워지는 원래 문제가 돌아와 있었습니다.**

원인은 이름이 아니었습니다. 어셈블리 덤프로 확정:

```
=== BSG.CameraEffects.NightVision
    field    bool _on   [Public]
```

**`_on`은 이름이 바뀐 게 아니라 `public`이 된 것**이었습니다. 09/03에 넣은 조회가
`BindingFlags.Instance | NonPublic`이라 public이 된 필드를 못 봤고, 그 결과가
"필드가 없다"와 로그상 완전히 똑같았습니다.

public이면 찾을 이유가 없으므로 **리플렉션을 걷어내고 직접 읽습니다**:

```csharp
var nvOn = _nightVision != null && _nightVision._on;
```

짧아서가 아닙니다. 리플렉션은 런타임에 조용히 실패하고 폴백으로 넘어가는데 그 폴백이
수정 전 상태라, 버그가 돌아와도 아무도 모릅니다. 실제로 그렇게 됐었고요. 직접 접근은
BSG가 다시 private으로 돌리면 **빌드 머신에서 컴파일 에러**가 납니다.

`On` 프로퍼티도 있지만 `_on`을 씁니다 — 09/03에 N키 양방향 추적을 실제로 검증한 게
`_on`이고, `On`은 자체 게터라 같은 값이라는 보장이 없습니다.

컴포넌트 자체를 못 찾는 경우는 원인도 대응도 다르므로 별도 경고를 남깁니다.

## 커스텀 맵에서 모드 전체가 죽던 문제 (2026-09-15)

쇄빙선(Icebreaker)에 들어가면 **HollywoodGraphics가 통째로 안 먹었습니다.** 블룸만이
아니라 AO·모션블러·맵별 설정까지 전부입니다. 로그가 딱 거기서 끊깁니다:

```
[Info :Janky-HollywoodGraphics] Resetting Star Bloom intensities
[Warning] FIRST Exception: NullReferenceException
  HollywoodGraphics.Components.Bloom..ctor ()
  HollywoodGraphics.GraphicsController.Start ()
```

같은 날 등대 로그에는 정상 경로가 그대로 찍혀 있어서 비교가 됩니다 —
`Bloom: Ultimate Bloom effect applied to camera FPS Camera` → `Bloom initialized` →
`Ambient Occlusion initialized` → `Updated all settings`. 쇄빙선 로그에는 이 네 줄이
전부 없습니다.

### 원인

`Bloom` 생성자는 카메라에 `UltimateBloom` 이 없으면 `AddComponent` 로 붙입니다.
그런데 **`AddComponent` 는 `Awake` 까지만 동기로 돌리고 `Start` 는 다음 프레임**이며,
`UltimateBloom` 이 자기 배열들(`m_BloomIntensities`, `m_BloomUsages` 등)을 채우는 건
`Start` 입니다. 그래서 갓 붙인 컴포넌트는 그 프레임에 배열이 전부 `null` 입니다.

리테일 맵은 카메라 프리팹에 `UltimateBloom` 이 이미 직렬화돼 있어서 이 경로를 안
탑니다. **자기 카메라를 직접 만드는 커스텀 맵**에서만 터집니다.

`ResetIntensities` 에는 이미 `null` 가드가 있었습니다. 문제는 **그 바로 다음 줄**이고,
거기엔 가드가 없었습니다:

```csharp
_ultimateBloom.m_BloomUsages[0] = _ultimateBloom.m_BloomUsages[1] = false;  // ← NRE
```

로그에 `Resetting ... intensities` 세 줄만 찍히고 그 아래 `Intensity: 1` 이 하나도
안 찍힌 게 증거입니다 — 세 번 다 가드에 걸려서 바로 리턴했다는 뜻이고, 그럼 같은
이유로 bool 배열도 `null` 입니다.

### 고친 방식

배열을 건드리는 설정을 생성자에서 **`TryConfigure()` 로 분리**하고, 아직 준비가 안 됐으면
예외 대신 `false` 를 돌려주게 했습니다. `GraphicsController.Update()` 가 준비될 때까지
다음 프레임에 다시 부릅니다(최대 120프레임, 보통 1~2프레임이면 끝납니다).

- 리테일 맵: 생성자에서 첫 호출에 성공 → 재시도 코드는 한 번도 안 돎
- 커스텀 맵: `UltimateBloom.Start()` 가 돈 다음 프레임에 성공하고 로그를 남김
- 끝내 실패: **블룸만** 기본값으로 남고 AO·모션블러는 정상 동작, 에러 한 줄

같이 손본 것:

- `GraphicsController.Start()` 의 블룸·AO 초기화를 **각각 try/catch** 로 감쌌습니다.
  이번 사고의 피해가 컸던 이유가 순차 실행이었기 때문입니다. 한 단계가 실패해도
  나머지는 돌아야 합니다
- `Bloom.UpdateSettings()` / `UpdateLensDust()` 에 `_ultimateBloom == null` 가드 추가.
  `AmbientOcclusion.UpdateSettings()` 에는 원래 있던 가드고, 블룸 쪽에만 없었습니다.
  카메라를 못 찾으면 생성자가 일찍 리턴하는데, `GraphicsController` 의 `_bloom?.` 는
  **Bloom 객체**의 null만 막지 그 안의 `UltimateBloom` null은 못 막습니다

맵 모드 쪽은 건드리지 않았습니다. 이 수정은 쇄빙선뿐 아니라 자기 카메라를 쓰는
어떤 커스텀 맵에도 그대로 적용됩니다.

## 빌드 경로

`SptRoot` 기본값을 SPT 4.1 설치본으로 바꾸고, 게임과 BepInEx 위치를 추측하지 않고
탐색합니다 (루트 → `SPT_Runtime\` → `SPT\`, 각각 따로). 참조를 못 찾으면 어느 폴더에
뭐가 없는지 한 줄로 말하고 멈추며, 빌드할 때 어느 어셈블리를 골랐는지 찍습니다.

설치본 복사를 `copy /Y` 대신 MSBuild `Copy`로 교체 — 게임이 켜져 DLL이 잠겼을 때
빌드를 실패시키지 않고 경고로 끝냅니다.

## 검증

라이드 실측: 패치 4개 전부 적용, 실패 0, 경고 0. 라이드 초기화와 `woods` 맵
오버라이드, 지형 디테일 오버라이드까지 확인.

## 참고 — 어셈블리 이름 확인 도구

`tools/AssemblyDump`가 **HollywoodFX 레포**에 있습니다. 게임 어셈블리에서 실제
타입·멤버 이름과 시그니처를 뽑는 도구로, 이 포팅의 개명은 전부 그걸로 확정했습니다.

**역난독화는 설치가 아니라 게임을 한 번 메인 메뉴까지 띄웠을 때 일어납니다.**
안 띄운 상태로 빌드하면 BSG 원본 이름으로 컴파일되고, SPT가 바꾼 이름은 전부
"찾을 수 없음"이 되며 마치 매핑 표가 틀린 것처럼 보입니다.
