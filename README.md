# CU_Trollers

Casualties: Unknown 모바일 포팅에 추가된 장난성 아이템인 **BlueOK**와 **쇠파이프(Metal Pipe)** 관련 소스를 모아 둔 저장소야.

## BlueOK

BlueOK는 던져서 바닥에 강하게 충돌시키면 폭발하는 투척 아이템이야. 일정 속도 이상으로 던졌을 때 무장되고, 충돌 시 폭발 파티클·사운드·햅틱을 재생한 뒤 주변 플레이어와 일부 오브젝트를 강하게 날려 보내.

멀티플레이에서는 폭발 효과와 넉백 요청을 별도로 동기화하도록 연결돼 있어.

## Metal Pipe

쇠파이프는 근접 공격용 아이템이야. 공격 시 레이캐스트로 플레이어/더미/월드 충돌을 확인하고, 대상에게 피해와 강한 넉백을 적용해. 바닥에 떨어질 때도 쇠파이프 효과음을 재생해.

멀티플레이에서는 원격 플레이어를 맞혔을 때 별도의 넉백 요청을 보내도록 되어 있어.

## 포함된 코드

- `BlueOK/BlueOKBootstrap.cs`
- `BlueOK/BlueOKItem.cs`
- `BlueOK/TemporaryNoclipFlight.cs`
- `MetalPipe/MetalPipeBootstrap.cs`
- `MetalPipe/MetalPipeItem.cs`

원본 ZIP에는 `Sound/blueok_boom.ogg`, `Sound/metalpipe.ogg`, `Texture/blueok.png`, `Texture/metalpipe.png` 리소스도 함께 들어 있어.

일부 멀티플레이 헬퍼(`HGMultiplayerExtensions` 등)는 제공된 소스 ZIP에서 정의 파일이 빠져 있어서 이 저장소의 코드만으로 완전한 독립 빌드가 되지는 않아.
