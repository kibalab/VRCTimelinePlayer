
# VRChat VPM Package Template for KIBALAB (GitHub Actions 자동 릴리스)

이 레포는 **VRChat Creator Companion(VCC) / VRChat Package Manager(VPM)** 용 패키지를 배포하기 위한 템플릿입니다.  
패키지 레포에서 **태그(릴리스)만 푸시하면**, GitHub Actions가 자동으로 빌드/릴리스를 만들고, 연결된 **VPM Listing 레포**(예: `kibalab/vpm-listing`)에 `repository_dispatch`로 갱신을 요청합니다.

---

## 무엇이 자동화되나요?

- 태그 푸시 → Release 생성 (zip + unitypackage + package.json 첨부)
- Release 완료 → VPM Listing 레포에 “패키지 릴리스됨” 이벤트 전송
- 패키지 미디어(썸네일 등)도 `Packages/<package_id>/package-media`에 넣어두면 Listing 배포 산출물에 포함되도록 설계 가능

---

## 요구 사항

### 1) 패키지 구조(UPM/VPM 표준)
패키지는 반드시 다음 경로에 있어야 함.

```

Packages/<PACKAGE_ID>/package.json

```

예:
```

Packages/com.kibalab.package-name/package.json

````

### 2) Repository Variables
GitHub 레포 Settings → **Secrets and variables** → **Actions** → **Variables**에 아래 변수를 설정.

- `PACKAGE_NAME`  
  - 예: `com.kibalab.package-name`  
  - 이 값으로 `Packages/<PACKAGE_NAME>` 경로를 찾아 빌드함.

### 3) Repository Secrets
패키지 릴리스가 끝난 뒤 Listing 레포를 트리거하려면 토큰이 필요함.

- `VPM_LISTING_DISPATCH_TOKEN`  
  - **fine-grained PAT**
  - 권한: `kibalab/vpm-listing` (대상 레포만 선택) + Contents(write)
  - 이 토큰으로 Listing 레포에 `repository_dispatch`를 보냄.

> 패키지 레포가 private이고, Listing 레포가 빌드 중 패키지 레포를 clone해야 한다면  
> Listing 레포 쪽에 별도의 read token(`VPM_PACKAGES_READ_TOKEN`)이 필요.

---

## 빠른 시작

### 1) 템플릿에서 새 레포 만들기
- `Use this template`로 새 레포 생성

### 2) 패키지 ID 정하기
- `Packages/<PACKAGE_ID>/package.json`의 `"name"`이 패키지 ID입니다.
- 예: `"name": "com.kibalab.package-name"`

### 3) Repository Variable 설정
- `PACKAGE_NAME = com.kibalab.package-name`

### 4) 태그 푸시로 릴리스 만들기
릴리스는 보통 아래 순서로 합니다.

1) `package.json`의 `"version"`을 올림 (ex: `0.1.0` → `0.1.1`)
2) 커밋/푸시
3) 같은 버전 태그를 찍고 푸시

```bash
git add Packages/com.kibalab.package-name/package.json
git commit -m "Bump version to 0.1.1"
git push

git tag 0.1.1
git push origin 0.1.1
````

> 워크플로우에서 “태그 버전 == package.json 버전” 검증을 켜두었다면
> 둘이 다르면 릴리스가 실패처리 됩니다.

---

## 패키지 미디어(썸네일 등)

### 권장 경로

패키지 레포 안에 다음 폴더를 만들고 파일을 넣습니다.

```
Packages/<PACKAGE_ID>/package-media/
```

예:

```
Packages/com.kibalab.package-name/package-media/thumbnail.png
Packages/com.kibalab.package-name/package-media/banner.png
```

Listing 레포 빌드에서 이 폴더들을 수집/머지하도록 구성되어 있다면, 배포 결과로 아래처럼 서빙될 수 있습니다.

* `https://.../package-media/<package_id>/thumbnail.png`

> 머지 정책에 따라 Listing 레포에 같은 파일이 이미 있으면 덮어쓰지 않을 수 있습니다.
> (예: `rsync --ignore-existing` 사용 시 Listing 쪽 파일이 우선)

---

## GitHub Actions 워크플로우

### `release.yml` (패키지 레포)

* 태그 푸시를 트리거로 실행
* `Packages/<PACKAGE_NAME>` 폴더를 zip으로 만들고 `.unitypackage`도 생성
* GitHub Release에 업로드
* 마지막에 Listing 레포로 `repository_dispatch` 이벤트 전송

---

```
