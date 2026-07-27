"""lowpoly_lib — 로우폴리 AI 에셋 파이프라인 공유 bpy 라이브러리 (kit).

이 패키지는 프로젝트 중립이다. 게임별 값(출력 경로·팔레트 색)은 `ArtPipeline/pipeline.json`과
그것이 가리키는 팔레트 레지스트리에서 온다.

- 이 프로젝트의 사용 규약: ArtPipeline/CONVENTIONS.md
- 다른 프로젝트로 이식: ArtPipeline/KIT.md
"""
from . import config, paths, palette, builders, export, preview, rigging, animation  # noqa: F401
