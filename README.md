# pipeMuzzle

## Türkçe

**pipeMuzzle**, Unity ile geliştirilen küçük bir mobil bulmaca oyunudur.

Oyuncu, boru şeklindeki parçaları 90 derecelik adımlarla döndürerek kaynak ile namlu arasında kesintisiz bir yol oluşturur. Bağlantı tamamlandığında mermi oluşturulan hat boyunca ilerler ve hedefe ulaşır.

## Güncel durum

### Tamamlananlar

- [x] Unity projesi ve klasör yapısı
- [x] `Direction`, `TileShape`, `TileRole`
- [x] Bit maskesi tabanlı `ConnectionMask`
- [x] Extension metotları
- [x] `TileState`
- [x] `BoardState`
- [x] BFS tabanlı `ConnectionChecker`
- [x] 3x3 mantık testi
- [x] Hamle sayacı
- [x] `TileDefinition`

### Doğrulanan test

```text
Dönüşten önce çözüldü mü: False
Parça döndü mü: True
Dönüşten sonra çözüldü mü: True
Hamle sayısı: 1
```

### Sıradaki adım

- [ ] `LevelDefinition` ScriptableObject yapısını oluşturmak
- [ ] `Level_001` bölüm asset'ini hazırlamak
- [ ] Bölüm verisinden `BoardState` üretmek
- [ ] İlk görsel grid'i oluşturmak

## Sürüm planı

### V1
Temel oynanabilir prototip, veri tabanlı bölüm yapısı, restart ve bölüm tamamlama.

### V2
15 elle hazırlanmış bölüm, bölüm seçimi, yıldız sistemi, ilerleme kaydı ve kilitli parçalar.

### V3
Nihai görseller, mermi animasyonu, ses, titreşim, mobil uyumluluk ve Android yayın hazırlığı.

Proje kapsamı V3 ile tamamlanacaktır.

---

# English

**pipeMuzzle** is a small mobile puzzle game built with Unity.

Players rotate pipe-shaped tiles in 90-degree steps to create a continuous route between a source and a muzzle.

## Current status

### Completed

- [x] Unity project and folder structure
- [x] `Direction`, `TileShape`, `TileRole`
- [x] Bit-mask-based `ConnectionMask`
- [x] Extension methods
- [x] `TileState`
- [x] `BoardState`
- [x] BFS-based `ConnectionChecker`
- [x] 3x3 logic test
- [x] Move counter
- [x] `TileDefinition`

### Verified test

```text
Solved before rotation: False
Tile rotated: True
Solved after rotation: True
Move count: 1
```

### Next step

- [ ] Create the `LevelDefinition` ScriptableObject
- [ ] Create the `Level_001` asset
- [ ] Build a `BoardState` from level data
- [ ] Create the first visual grid

## Version plan

### V1
Core playable prototype, data-driven levels, restart and level completion.

### V2
15 handcrafted levels, level selection, star ratings, saved progression and locked tiles.

### V3
Final visuals, projectile animation, audio, haptics, mobile support and Android release preparation.

The planned project scope ends with V3.
