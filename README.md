# PipeMuzzle

> Unity ile geliştirilen, veri odaklı bir 2D mobil boru bulmaca oyunu prototipi.

[![Unity](https://img.shields.io/badge/Unity-6000.3.9f1-black?logo=unity)](https://unity.com/)
[![C%23](https://img.shields.io/badge/C%23-Game%20Logic-512BD4?logo=csharp)](https://learn.microsoft.com/dotnet/csharp/)
[![Status](https://img.shields.io/badge/status-pre--alpha-orange)](https://github.com/muhammedcanarica/PipeMuzzle)

## Türkçe

### Oyun fikri

Oyuncu, boru parçalarını 90 derecelik adımlarla döndürerek kaynak ile namlu arasında kesintisiz bir bağlantı kurar. Doğru rota tamamlandığında mermi bu hat boyunca ilerleyerek hedefe ulaşır.

Proje şu anda **pre-alpha / temel prototip** aşamasındadır. Oyun mantığı, veri modeli ve görsel tahta üretim hattı oluşturulmuş; ilk bölüm içeriği, sahne bağlantıları ve oyuncu etkileşimi geliştirilmektedir.

### Öne çıkan teknik özellikler

- Bit maskeleriyle temsil edilen dört yönlü boru bağlantıları
- Karo şekline ve dönüşüne göre dinamik bağlantı hesabı
- Kilitli karoları destekleyen 90° saat yönü dönüş sistemi
- Başarılı dönüşlerde güncellenen hamle sayacı
- Kaynaktan hedefe ulaşılabilirliği kontrol eden BFS tabanlı bağlantı algoritması
- `ScriptableObject` tabanlı, tekrar kullanılabilir bölüm tanımları
- Bölüm verisini çalışma zamanı durumuna çeviren `BoardBuilder`
- Bölümdeki karoları prefab üzerinden koordinatlarına yerleştiren `BoardView`
- Karo dönüşünü görsele uygulayan ve kaynak/hedef rollerini renkle ayıran `TileView`

### Mimari

| Katman | Sorumluluk | Başlıca sınıflar |
| --- | --- | --- |
| `Data` | Yön, bağlantı, karo ve bölüm tanımları | `Direction`, `ConnectionMask`, `TileDefinition`, `LevelDefinition` |
| `Board` | Çalışma zamanı tahta durumu ve oyun kuralları | `TileState`, `BoardState`, `BoardBuilder`, `ConnectionChecker` |
| `View` | Karo prefablarının oluşturulması, konumlandırılması, döndürülmesi ve rol renkleri | `BoardView`, `TileView`, `TilePrefab` |
| `Gameplay` | Prototip akışının sahne üzerinden çalıştırılması | `BoardLogicTester` |

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

> **Not:** Oynanabilir döngü henüz tamamlanmadı. `Level_001` şu anda boş; `Gameplay` sahnesindeki `BoardView` ve Inspector bağlantıları tamamlanmadan Play Mode akışı çalışmayacaktır.

### Güncel durum

- [x] Temel veri modelleri ve bağlantı maskeleri
- [x] Karo dönüşü, kilit kontrolü ve hamle sayacı
- [x] BFS tabanlı kaynak-hedef bağlantı kontrolü
- [x] `LevelDefinition` ve `TileDefinition` veri yapıları
- [x] Bölüm verisinden `BoardState` oluşturma
- [x] Temel `BoardView` ve `TileView` bileşenleri
- [x] Sprite tabanlı `TilePrefab` ve kaynak/hedef rol renkleri
- [ ] İlk oynanabilir bölümün içerik ve sahne bağlantıları
- [ ] Dokunma/tıklama ile karo döndürme
- [ ] Bölüm tamamlama, yeniden başlatma ve UI akışı
- [ ] Mermi animasyonu, ses ve titreşim geri bildirimi
- [ ] Edit Mode / Play Mode otomatik testleri
- [ ] Mobil cihaz doğrulaması ve Android build hazırlığı

### Yol haritası

#### V1 — Oynanabilir prototip

Veri odaklı ilk bölüm, karo etkileşimi, çözüm kontrolü, yeniden başlatma ve bölüm tamamlama akışı.

#### V2 — İçerik ve ilerleme

15 elle hazırlanmış bölüm, bölüm seçimi, yıldız sistemi, kayıtlı ilerleme ve kilitli karolar.

#### V3 — Sunum ve mobil yayın

Nihai görseller, mermi animasyonu, ses, titreşim, mobil optimizasyon ve Android yayın hazırlığı.

---

## English

### Game concept

PipeMuzzle is a data-driven 2D mobile puzzle game prototype built with Unity. Players rotate pipe tiles in 90-degree steps to form a continuous connection between a source and a muzzle. Once the route is complete, a projectile travels through the connected path and reaches the target.

The project is currently in **pre-alpha / core prototype** development. The domain model, connection logic, and visual board pipeline are implemented, while the first level content, scene wiring, and player interaction are still in progress.

### Technical highlights

- Four-direction pipe connections represented with bit masks
- Rotation-aware connection calculation for each tile shape
- Clockwise 90° rotation with locked-tile support
- Move counting for successful rotations
- BFS-based source-to-target connectivity validation
- Reusable, `ScriptableObject`-based level definitions
- A `BoardBuilder` pipeline that creates runtime state from level data
- A `BoardView` that instantiates and positions prefab-based tiles
- A `TileView` that applies rotation and color-codes source/target roles

### Architecture

| Layer | Responsibility | Main types |
| --- | --- | --- |
| `Data` | Direction, connection, tile, and level definitions | `Direction`, `ConnectionMask`, `TileDefinition`, `LevelDefinition` |
| `Board` | Runtime board state and game rules | `TileState`, `BoardState`, `BoardBuilder`, `ConnectionChecker` |
| `View` | Instantiating, positioning, rotating, and color-coding tile prefabs | `BoardView`, `TileView`, `TilePrefab` |
| `Gameplay` | Running the prototype flow from the scene | `BoardLogicTester` |

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

> **Note:** The complete playable loop is not available yet. `Level_001` is currently empty, and the `BoardView` and Inspector references in the `Gameplay` scene must be completed before the Play Mode flow can run.

### Development status

- [x] Core data models and connection masks
- [x] Tile rotation, lock handling, and move counting
- [x] BFS-based source-to-target connectivity check
- [x] `LevelDefinition` and `TileDefinition` data structures
- [x] Runtime `BoardState` creation from level data
- [x] Basic `BoardView` and `TileView` components
- [x] Sprite-based `TilePrefab` with source/target role colors
- [ ] First playable level content and scene wiring
- [ ] Touch/click tile rotation
- [ ] Level completion, restart, and UI flow
- [ ] Projectile animation, audio, and haptic feedback
- [ ] Edit Mode / Play Mode automated tests
- [ ] Mobile device validation and Android build preparation

### Roadmap

#### V1 — Playable prototype

A data-driven first level, tile interaction, solution checks, restart, and level-completion flow.

#### V2 — Content and progression

15 handcrafted levels, level selection, star ratings, saved progression, and locked tiles.

#### V3 — Presentation and mobile release

Final visuals, projectile animation, audio, haptics, mobile optimization, and Android release preparation.

## Repository

[github.com/muhammedcanarica/PipeMuzzle](https://github.com/muhammedcanarica/PipeMuzzle)
