# PipeMuzzle

> Unity ile geliştirilen, veri odaklı bir 2D mobil boru bulmaca oyunu prototipi.

[![Unity](https://img.shields.io/badge/Unity-6000.3.9f1-black?logo=unity)](https://unity.com/)
[![C%23](https://img.shields.io/badge/C%23-Game%20Logic-512BD4?logo=csharp)](https://learn.microsoft.com/dotnet/csharp/)
[![Status](https://img.shields.io/badge/status-pre--alpha-orange)](https://github.com/muhammedcanarica/PipeMuzzle)

## Türkçe

### Oyun fikri

Oyuncu, boru parçalarını 90 derecelik adımlarla döndürerek kaynak ile namlu arasında kesintisiz bir bağlantı kurar. Doğru rota tamamlandığında bölüm çözülür; mermi animasyonu ve ek geri bildirimler sonraki geliştirme adımları arasındadır.

Proje şu anda **pre-alpha / oynanabilir temel prototip** aşamasındadır. Üç veri odaklı bölüm; görsel tahta üretimi, tıklayarak karo döndürme, otomatik kamera uyumu, yeniden başlatma ve sonraki bölüme geçiş akışlarıyla oynanabilir durumdadır.

### Öne çıkan teknik özellikler

- Bit maskeleriyle temsil edilen dört yönlü boru bağlantıları
- Karo şekline ve dönüşüne göre dinamik bağlantı hesabı
- Kilitli karoları destekleyen 90° saat yönü dönüş sistemi
- Başarılı dönüşlerde event üzerinden güncellenen görünür hamle sayacı
- Kaynaktan hedefe ulaşılabilirliği kontrol eden BFS tabanlı bağlantı algoritması
- `ScriptableObject` tabanlı, tekrar kullanılabilir bölüm tanımları
- Bölüm verisini çalışma zamanı durumuna çeviren `BoardBuilder`
- Bölümdeki karoları prefab üzerinden üreten ve grid'i dünya merkezine yerleştiren `BoardView`
- Oluşturulan renderer sınırlarını, ekran oranını ve padding değerini kullanarak kamerayı otomatik ayarlayan `BoardCameraFitter`
- Karo şekline uygun pipe sprite'ını ve dönüşünü görsele uygulayan `TileView`
- `OnMouseDown` ve event zinciri üzerinden çalışan tıklama → döndürme → çözüm kontrolü akışı
- `BoxCollider2D` destekli tile etkileşimi ve kilitli Source/Target kontrolü
- Bölüm çözüldükten sonra yeni tile inputlarını engelleyen tamamlama kilidi
- Yeniden başlatma, bölüm bilgisi, hamle sayacı ve tamamlama panelini yöneten sade oyun UI'ı

### Mimari

| Katman | Sorumluluk | Başlıca sınıflar |
| --- | --- | --- |
| `Data` | Yön, bağlantı, karo ve bölüm tanımları | `Direction`, `ConnectionMask`, `TileDefinition`, `LevelDefinition` |
| `Board` | Çalışma zamanı tahta durumu ve oyun kuralları | `TileState`, `BoardState`, `BoardBuilder`, `ConnectionChecker` |
| `View` | Karo prefablarının oluşturulması, merkezlenmesi, döndürülmesi ve kameranın board sınırlarına uydurulması | `BoardView`, `TileView`, `BoardCameraFitter`, `TilePrefab` |
| `Gameplay` | Bölüm yükleme, yeniden başlatma, hamle ve ilerleme akışının yönetilmesi | `GameController`, `BoardLogicTester` |
| `UI` | Bölüm, hamle ve tamamlama durumlarının ekranda gösterilmesi | `GameUI`, TextMesh Pro, Unity UI |

Bu ayrım sayesinde bölüm verisi, oyun mantığı ve Unity görselleştirmesi birbirinden bağımsız geliştirilebilir.

### Kullanılan teknolojiler

- Unity `6000.3.9f1`
- C#
- Universal Render Pipeline (2D Renderer)
- Unity Input System `1.18.0`
- Unity Test Framework `1.6.0` *(test altyapısı mevcut, otomatik proje testleri henüz eklenmedi)*

### Projeyi çalıştırma

1. Depoyu klonlayın:

   ```bash
   git clone https://github.com/muhammedcanarica/PipeMuzzle.git
   ```

2. Unity Hub üzerinden proje klasörünü ekleyin.
3. Projeyi Unity `6000.3.9f1` veya uyumlu bir Unity 6 sürümüyle açın.
4. Geliştirme sahnesi olarak `Assets/Scenes/Gameplay.unity` dosyasını açın.
5. Play Mode'u başlatın ve döndürülebilir boru karolarına tıklayın.
6. Üst çubuktaki `RESTART`, `LEVEL` ve `HAMLE` bilgilerini; bölüm çözülünce açılan `NEXT LEVEL` akışını kontrol edin.

> **Not:** `OnMouseDown` etkileşimi için `TilePrefab` üzerinde `BoxCollider2D` bulunur ve Active Input Handling ayarı `Both` olarak yapılandırılmıştır.

### Güncel durum

- [x] Temel veri modelleri ve bağlantı maskeleri
- [x] Karo dönüşü, kilit kontrolü ve hamle sayacı
- [x] BFS tabanlı kaynak-hedef bağlantı kontrolü
- [x] `LevelDefinition` ve `TileDefinition` veri yapıları
- [x] Bölüm verisinden `BoardState` oluşturma
- [x] Temel `BoardView` ve `TileView` bileşenleri
- [x] Karo şekline göre değişen pipe sprite'larıyla `TilePrefab`
- [x] Üç oynanabilir bölümün içerik ve sahne bağlantıları
- [x] Tıklama ile karo döndürme ve yeniden çözüm kontrolü
- [x] Tek ve çift boyutlu board'ların geometrik merkezlenmesi
- [x] Renderer bounds ve aspect ratio tabanlı otomatik kamera fit sistemi
- [x] Bölüm tamamlama algılama ve çözüm sonrası input kilidi
- [x] Yeniden başlatma, bölüm tamamlama ve sonraki bölüme geçiş UI akışı
- [x] Başarılı dönüşleri gösteren ve bölüm yüklenince sıfırlanan hamle sayacı UI'ı
- [ ] Mermi animasyonu, ses ve titreşim geri bildirimi
- [ ] Edit Mode / Play Mode otomatik testleri
- [ ] Mobil cihaz doğrulaması ve Android build hazırlığı

### Hızlı doğrulama

`Level_001`, yatay bir Source → Normal → Target hattı kullanır. Source ve Target kilitlidir. Ortadaki Normal karo başlangıçta dikeydir; ilk tıklamada saat yönünde 90° dönerek hattı tamamlar.

Board dünya merkezine otomatik yerleşir ve kamera görünür tile sınırlarını padding bırakarak ekrana sığdırır. Farklı level boyutları ve ekran oranları için `Orthographic Size` değerini elle değiştirmek gerekmez.

Beklenen UI akışı:

```text
RESTART        LEVEL 1 / 3        HAMLE: 0
Başarılı karo dönüşü              HAMLE: 1
RESTART                             HAMLE: 0
Bölüm çözülünce                  LEVEL COMPLETE!
NEXT LEVEL ile yeni bölüm          HAMLE: 0
Son bölüm çözülünce            ALL LEVELS COMPLETE!
```

### Yol haritası

#### V1 — Oynanabilir prototip

Üç veri odaklı bölüm, karo etkileşimi, çözüm kontrolü, hamle sayacı, yeniden başlatma ve bölüm tamamlama akışı.

#### V2 — İçerik ve ilerleme

15 elle hazırlanmış bölüm, bölüm seçimi, yıldız sistemi, kayıtlı ilerleme ve kilitli karolar.

#### V3 — Sunum ve mobil yayın

Nihai görseller, mermi animasyonu, ses, titreşim, mobil optimizasyon ve Android yayın hazırlığı.

---

## English

### Game concept

PipeMuzzle is a data-driven 2D mobile puzzle game prototype built with Unity. Players rotate pipe tiles in 90-degree steps to form a continuous connection between a source and a muzzle. Completing the route solves the level; projectile animation and additional feedback remain planned work.

The project is currently in **pre-alpha / playable core prototype** development. Three data-driven levels are playable with visual board generation, click-to-rotate interaction, automatic camera fitting, restart, and next-level progression.

### Technical highlights

- Four-direction pipe connections represented with bit masks
- Rotation-aware connection calculation for each tile shape
- Clockwise 90° rotation with locked-tile support
- A visible move counter updated through an event after successful rotations
- BFS-based source-to-target connectivity validation
- Reusable, `ScriptableObject`-based level definitions
- A `BoardBuilder` pipeline that creates runtime state from level data
- A `BoardView` that instantiates prefab-based tiles and centers the grid around the world origin
- A `BoardCameraFitter` that uses renderer bounds, screen aspect ratio, and padding to fit the camera automatically
- A `TileView` that selects the matching pipe sprite and applies its rotation
- A click → rotate → solution-check flow built with `OnMouseDown` and C# events
- `BoxCollider2D`-based tile interaction with locked Source/Target handling
- A completion lock that prevents additional tile input after the puzzle is solved
- A compact game UI for restart, level progress, move count, and completion states

### Architecture

| Layer | Responsibility | Main types |
| --- | --- | --- |
| `Data` | Direction, connection, tile, and level definitions | `Direction`, `ConnectionMask`, `TileDefinition`, `LevelDefinition` |
| `Board` | Runtime board state and game rules | `TileState`, `BoardState`, `BoardBuilder`, `ConnectionChecker` |
| `View` | Instantiating, centering, and rotating tile prefabs, plus fitting the camera to board bounds | `BoardView`, `TileView`, `BoardCameraFitter`, `TilePrefab` |
| `Gameplay` | Managing level loading, restart, moves, and progression | `GameController`, `BoardLogicTester` |
| `UI` | Presenting level, move, and completion states | `GameUI`, TextMesh Pro, Unity UI |

### Built with

- Unity `6000.3.9f1`
- C#
- Universal Render Pipeline with the 2D Renderer
- Unity Input System `1.18.0`
- Unity Test Framework `1.6.0` *(available in the project; automated project tests are not implemented yet)*

### Getting started

1. Clone the repository:

   ```bash
   git clone https://github.com/muhammedcanarica/PipeMuzzle.git
   ```

2. Add the project folder through Unity Hub.
3. Open it with Unity `6000.3.9f1` or a compatible Unity 6 release.
4. Open `Assets/Scenes/Gameplay.unity` as the development scene.
5. Enter Play Mode and click the rotatable pipe tiles.
6. Check the `RESTART`, `LEVEL`, and `HAMLE` values in the top bar, then use `NEXT LEVEL` after solving a level.

> **Note:** `TilePrefab` includes a `BoxCollider2D` for `OnMouseDown`, and Active Input Handling is configured as `Both`.

### Development status

- [x] Core data models and connection masks
- [x] Tile rotation, lock handling, and move counting
- [x] BFS-based source-to-target connectivity check
- [x] `LevelDefinition` and `TileDefinition` data structures
- [x] Runtime `BoardState` creation from level data
- [x] Basic `BoardView` and `TileView` components
- [x] Shape-specific pipe sprites configured on `TilePrefab`
- [x] Three playable levels with content and scene wiring
- [x] Click-to-rotate interaction and repeated solution checks
- [x] Geometric board centering for odd and even dimensions
- [x] Renderer-bounds and aspect-ratio-aware automatic camera fitting
- [x] Completion detection and post-solve input lock
- [x] Restart, level-completion, and next-level UI flow
- [x] Move counter UI that updates after valid rotations and resets when a level loads
- [ ] Projectile animation, audio, and haptic feedback
- [ ] Edit Mode / Play Mode automated tests
- [ ] Mobile device validation and Android build preparation

### Quick verification

`Level_001` uses a horizontal Source → Normal → Target route. The Source and Target tiles are locked. The middle Normal tile starts vertically and rotates 90° clockwise on the first click to complete the route.

The board is centered automatically, and the camera fits the visible tile bounds with configurable padding. Manual `Orthographic Size` changes are not required for different level sizes or aspect ratios.

Expected UI flow:

```text
RESTART        LEVEL 1 / 3        HAMLE: 0
Successful tile rotation          HAMLE: 1
RESTART                             HAMLE: 0
Level solved                    LEVEL COMPLETE!
NEXT LEVEL loads a new level       HAMLE: 0
Final level solved          ALL LEVELS COMPLETE!
```

### Roadmap

#### V1 — Playable prototype

Three data-driven levels, tile interaction, solution checks, move counting, restart, and level-completion flow.

#### V2 — Content and progression

15 handcrafted levels, level selection, star ratings, saved progression, and locked tiles.

#### V3 — Presentation and mobile release

Final visuals, projectile animation, audio, haptics, mobile optimization, and Android release preparation.

## Repository

[github.com/muhammedcanarica/PipeMuzzle](https://github.com/muhammedcanarica/PipeMuzzle)
