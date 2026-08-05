# pipeMuzzle

## Türkçe

**pipeMuzzle**, Unity ile geliştirilen küçük bir mobil bulmaca oyunudur.

Oyuncu, boru şeklindeki parçaları döndürerek kaynak ile namlu arasında kesintisiz bir yol oluşturur. Bağlantı tamamlandığında mermi oluşturulan hat boyunca ilerler ve hedefe ulaşır.

Proje şu anda erken geliştirme aşamasındadır. Hem oynanabilir bir mobil oyun hem de Unity ve C# öğrenme sürecini destekleyen bir proje olarak geliştirilmektedir.

---

## Oyun Fikri

Oyun alanı, farklı boru parçaları içeren bir grid yapısından oluşur.

Oyuncunun amacı:

- Parçaları 90 derecelik adımlarla döndürmek
- Kaynağı namluya bağlamak
- Rotayı mümkün olduğunca az hamlede tamamlamak
- Giderek zorlaşan bölümleri çözmek

Bağlantı doğru şekilde kurulduğunda bir mermi tamamlanan yol boyunca hareket eder.

---

## Planlanan Özellikler

### V1: Temel Prototip

- Grid tabanlı puzzle alanı
- Döndürülebilen boru parçaları
- Düz, köşe, üçlü ve dört yönlü parçalar
- Kaynak ve hedef parçaları
- Bağlantı doğrulama sistemi
- Bölüm tamamlama kontrolü
- Yeniden başlatma sistemi
- Temel mobil dokunmatik kontrol

### V2: Bölüm Sistemi

- Elle hazırlanmış birden fazla bölüm
- Bölüm seçim ekranı
- Kilitli ve açık bölümler
- Hamle sayacı
- Yıldız değerlendirme sistemi
- Kayıt ve ilerleme sistemi
- Kilitli boru parçaları
- Bölüm doğrulama araçları

### V3: Sunum ve Mobil Yayın

- Mermi ilerleme animasyonu
- Geliştirilmiş boru ve namlu görselleri
- Ses efektleri ve müzik
- Dokunsal geri bildirim
- Arayüz animasyonları
- Mobil ekran ve Safe Area desteği
- Android build
- Google Play test süreci

---

## Teknik Yapı

Proje; bölüm verisi, çalışma anındaki oyun durumu, oyun mantığı ve görsel sunumu birbirinden ayıracak şekilde tasarlanmaktadır.

```text
Assets/
├── Art/
├── Audio/
├── Prefabs/
├── Scenes/
├── Scripts/
│   ├── Board/
│   ├── Data/
│   ├── Gameplay/
│   └── UI/
├── ScriptableObjects/
└── Tests/
```

Projede planlanan temel sistemler:

- `LevelDefinition`: Değişmeyen bölüm verilerini saklar
- `TileState`: Bir parçanın oyun sırasındaki durumunu saklar
- `BoardState`: Tüm oyun alanının durumunu saklar
- `ConnectionChecker`: Rotanın bağlı olup olmadığını kontrol eder
- `BoardController`: Oyuncu hareketlerini ve oyun akışını yönetir
- `BoardView`: Görsel grid yapısını oluşturur ve günceller
- `TileView`: Parça görsellerini ve etkileşimini yönetir
- `ProgressService`: Tamamlanan bölümleri ve oyuncu ilerlemesini saklar

---

## Güncel Geliştirme Durumu

Tamamlananlar:

- Proje klasör yapısı
- Yön tanımları
- Parça şekli tanımları
- Parça rolü tanımları
- Bağlantı maskesi tasarımı

Şu anda geliştirilenler:

- Bağlantı maskesi yardımcı metotları
- Parça döndürme mantığı
- Çalışma anındaki parça durumu
- Oyun alanı durumu
- İlk oynanabilir 3x3 test bölümü

---

## Temel Tasarım Hedefleri

- Basit, tek dokunuşlu mobil kontroller
- Kısa ve anlaşılır puzzle bölümleri
- Oyun mantığı ile görsellerin temiz biçimde ayrılması
- Veri odaklı bölüm üretimi
- Sürdürülebilir ve test edilebilir C# kodu
- Gereksiz büyümeyen, küçük ama tamamlanmış bir proje

---

## Kullanılan Teknolojiler

- Unity
- C#
- ScriptableObject tabanlı bölüm verisi
- Git ve GitHub

---

## Ekran Görüntüleri

Ekran görüntüleri ve oynanış GIF'leri geliştirme ilerledikçe eklenecektir.

```text
docs/
├── screenshots/
└── gifs/
```

Eklenmesi planlanan içerikler:

- Boru döndürme GIF'i
- Mermi ilerleme GIF'i
- Bölüm tamamlama ekranı
- Bölüm seçim ekranı

---

## Geliştirme Yol Haritası

- [x] Unity projesini oluştur
- [x] Parça yönlerini tanımla
- [x] Parça şekillerini ve rollerini tanımla
- [x] Bağlantı maskesi sistemini tasarla
- [ ] Bağlantı maskesi yardımcılarını uygula
- [ ] Parça döndürme mantığını uygula
- [ ] `TileState` yapısını oluştur
- [ ] `BoardState` yapısını oluştur
- [ ] Rota doğrulama sistemini uygula
- [ ] İlk oynanabilir bölümü oluştur
- [ ] ScriptableObject ile bölüm verisini ekle
- [ ] İlerleme ve kayıt sistemini ekle
- [ ] Elle hazırlanmış 15 bölüm oluştur
- [ ] Mermi animasyonlarını ekle
- [ ] Android build hazırla

---

## Öğrenme Hedefleri

Bu proje aynı zamanda şu konularda pratik kazanmak amacıyla geliştirilmektedir:

- C# temelleri
- Nesne yönelimli tasarım
- Enum ve bit maskeleri
- Veri yapıları
- Breadth-first search
- Unity mimarisi
- ScriptableObject kullanımı
- Event tabanlı sistemler
- Oyun mantığının test edilmesi
- Mobil oyun geliştirme

---

## Lisans

Henüz bir lisans seçilmemiştir.

Aksi açıkça belirtilmediği sürece proje varlıkları ve kaynak kodu özel kabul edilmelidir.

---

# English

**pipeMuzzle** is a small mobile puzzle game built with Unity.

Players rotate pipe-shaped tiles to create a continuous route between a source and a muzzle. Once the path is completed, a projectile travels through the connected system and reaches its target.

The project is currently in early development and is being built both as a playable mobile game and as a learning-focused Unity project.

---

## Game Concept

The board consists of a grid containing different pipe pieces.

The player must:

- Rotate tiles in 90-degree steps
- Connect the source to the muzzle
- Complete the route with as few moves as possible
- Solve increasingly complex levels

When the connection is valid, a projectile moves through the completed path.

---

## Planned Features

### Version 1: Core Prototype

- Grid-based puzzle board
- Rotatable pipe tiles
- Straight, corner, three-way and cross-shaped pieces
- Source and target tiles
- Connection validation
- Level completion detection
- Restart system
- Basic mobile input

### Version 2: Level System

- Multiple handcrafted levels
- Level selection screen
- Locked and unlocked levels
- Move counter
- Star rating system
- Save and progression system
- Locked pipe pieces
- Level validation tools

### Version 3: Presentation and Mobile Release

- Projectile travel animation
- Improved pipe and muzzle visuals
- Sound effects and music
- Haptic feedback
- UI animations
- Mobile screen and safe-area support
- Android build
- Google Play testing

---

## Technical Structure

The project separates level data, runtime state, gameplay logic and visual presentation.

```text
Assets/
├── Art/
├── Audio/
├── Prefabs/
├── Scenes/
├── Scripts/
│   ├── Board/
│   ├── Data/
│   ├── Gameplay/
│   └── UI/
├── ScriptableObjects/
└── Tests/
```

Main systems planned for the project:

- `LevelDefinition`: Stores immutable level data
- `TileState`: Stores the runtime state of a tile
- `BoardState`: Stores the state of the entire board
- `ConnectionChecker`: Checks whether the route is connected
- `BoardController`: Handles player actions and gameplay flow
- `BoardView`: Creates and updates the visual grid
- `TileView`: Handles tile visuals and interaction
- `ProgressService`: Stores completed levels and player progress

---

## Current Development Status

Currently implemented:

- Project structure
- Direction definitions
- Tile shape definitions
- Tile role definitions
- Connection mask design

Currently being developed:

- Connection-mask helpers
- Tile rotation logic
- Runtime tile state
- Board state
- First playable 3x3 test level

---

## Core Design Goals

- Simple one-touch mobile controls
- Short and understandable puzzle levels
- Clean separation between logic and visuals
- Data-driven level creation
- Maintainable and testable C# code
- A small but complete project instead of an oversized unfinished prototype

---

## Built With

- Unity
- C#
- ScriptableObject-based level data
- Git and GitHub

---

## Screenshots

Screenshots and gameplay GIFs will be added as development progresses.

```text
docs/
├── screenshots/
└── gifs/
```

Suggested future media:

- Tile rotation GIF
- Projectile travel GIF
- Level completion screen
- Level selection screen

---

## Development Roadmap

- [x] Create the Unity project
- [x] Define tile directions
- [x] Define tile shapes and roles
- [x] Design the connection-mask system
- [ ] Implement connection-mask helpers
- [ ] Implement tile rotation
- [ ] Implement `TileState`
- [ ] Implement `BoardState`
- [ ] Implement route validation
- [ ] Build the first playable level
- [ ] Add level data with ScriptableObjects
- [ ] Add progression and save data
- [ ] Create 15 handcrafted levels
- [ ] Add projectile animations
- [ ] Prepare an Android build

---

## Learning Goals

This project is also being developed to improve practical skills in:

- C# fundamentals
- Object-oriented design
- Enums and bit masks
- Data structures
- Breadth-first search
- Unity architecture
- ScriptableObjects
- Event-driven systems
- Testing gameplay logic
- Mobile game development

---

## License

A license has not been selected yet.

All project assets and source code should be considered private unless explicitly stated otherwise.
