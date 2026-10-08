# Ruilay — Gameplay Polish Pass

## GAMEPLAY POLISH COMPLETED

Mevcut TileState, HintSelector, GameController, BoardView, EnergyFlowView, GameFeedback ve WorldGameplayTheme akışı genişletildi. Yeni solver, audio manager, dependency veya kalıcı kayıt sistemi eklenmedi. Önceki branding değişiklikleri korundu; 36 level asset'i değiştirilmedi. Commit/push yapılmadı.

## HINT SYSTEM

- Baked SolutionPath üzerindeki ilk uygun, yanlış yönlü normal pipe deterministik seçilir. Source/target, hint kilitli ve bağlantıları zaten doğru olan simetrik orientation'lar atlanır.
- Mevcut animasyon doğru orientation'a döndürür. Yarış durumunu önlemek için mantıksal hint kilidi animasyon başlangıcında alınır; animasyon sonunda görsel orientation doğru konuma ulaşır. Kilitli pipe hafif renk/glow ile görünür ve click feedback vermez.
- GameController Inspector'da Max Hints Per Level varsayılanı 3. WorldGameplayTheme Max Hints Per Level: -1 controller değerini devralır, 0 hint'i kapatır, pozitif değer world override olur.
- HINT kalan/toplam sayacı güncellenir; bütçe veya uygun hedef kalmadığında buton kapanır. Restart bütçeyi ve tile kilitlerini sıfırlar.

## START / END

- Authored level'larda source ve target gerçek connection mask'inde yalnızca baked yolun komşusuna açılır. Role kilidi nedeniyle authored lock flag eksik olsa da döndürülemezler.
- Source küçük idle shimmer, target hafif idle glow ve mevcut su alıcı/arrival pulse görünümünü kullanır.
- Eksik/geçersiz baked endpoint komşusu bulunan legacy/test verileri eski connection mask'ine döner. Mevcut 36 authored level'ın tamamı tek port doğrulamasından geçti.

## PIPE JUICE

Mevcut 0,14 saniyelik eased rotation ve hafif squash korunur. TileView ve controller devam eden dönüş sırasında aynı pipe input'unu reddeder; stale tile view da controller tarafından reddedilir. Mantıksal çözüm görsel animasyondan bağımsız kalır.

## DECORATION

WorldPresentationController grid dışına beş küçük SpriteRenderer yerleştirir: Sakura pembe petal, Bamboo yeşil yaprak, Moon Shrine mavi/gri taş. Runtime watercolor pigment sprite kullanılır. Collider, UI raycast, TileState veya path rolü yoktur. Presenter kapatılırken gizlenir; yok edilirken kendisine ait renderer/sprite/texture temizlenir.

## WATER FLOW

Mevcut ConnectionChecker solved path sırası kullanılır. Input kapanır, source aktifleşir, mevcut su fill'i yolu izler; geçilen tile'larda mevcut completion pulse tetiklenir. Target arrival efekti ardından 0,55 saniye beklenir ve mevcut completion UI akışı devam eder. Restart/disable sırasında flow callback'leri ve transient efektler temizlenir; ikinci completion engellenir. Yeni path hesaplama sistemi eklenmedi.

## AUDIO

WorldGameplayTheme üzerinde Pipe Rotate Clip ve Level Complete Clip isteğe bağlı slotları eklendi. Mevcut GameFeedback/AudioSource kullanılır. Slotlar boşken mevcut procedural fallback sesleri korunur; yeni ses asset'i üretilmedi veya indirilmedi.

## FILES CHANGED

Bu pass'in source dosyaları:

- Assets/Scripts/Board/TileState.cs
- Assets/Scripts/Board/BoardBuilder.cs
- Assets/Scripts/Gameplay/GameController.cs
- Assets/Scripts/UI/GameplayHintUI.cs
- Assets/Scripts/Data/WorldGameplayTheme.cs
- Assets/Scripts/View/TileView.cs
- Assets/Scripts/View/EnergyFlowView.cs
- Assets/Scripts/View/BoardView.cs
- Assets/Scripts/UI/WorldPresentationController.cs

Test dosyaları (Assets/Tests/EditMode/Editor altında):

- EndpointVisualTests.cs
- GameControllerWorldTests.cs
- PipeChannelVisualTests.cs
- GameplayPauseTests.cs
- LevelDifficultyProgressionTests.cs
- StoryCheckpointFlowTests.cs
- LevelDefinitionContentTests.cs

Rapor: docs/gameplay-polish-pass.md. Çalışma ağacındaki diğer değişiklikler önceki branding çalışmasına aittir. Source/test mevcut .meta dosyalarını kullanır; yeni Unity asset dosyası eklenmedi.

## TESTS

- Unity EditMode: **629 passed, 0 failed, 0 skipped**, 41,35 saniye. Job: bc60d27093df4e028eead19b13192133.
- Python: `python -m unittest discover -s tools -p 'test_*.py'` — **9 passed**, 2,729 saniye.
- C# compile: `dotnet build PipeMuzzle.slnx --no-restore -m:1 -p:UseSharedCompilation=false --verbosity minimal` — **0 warnings, 0 errors**.
- LevelValidationUtility.Analyze: **36 level, 0 errors**.
- Regression kapsamı: endpoint tek port/kilit, configurable hint bütçesi/world override/counter/restart, queued spam, ordered tile callback/cancellation, arrival hold. Mevcut hint/completion/pause/story/ilerleme testleri de suite içinde çalıştı.
- Endpoint'in ek portlarını kapatmak reachable topology metriğini azaltır. LevelDefinitionContentTests eski authored topology bütçesini orijinal tile definition'larından ölçmeye devam eder; runtime reachable sayısının bu bütçeyi aşmadığını ayrıca kontrol eder. Min move/solution/path doğrulaması kaldırılmadı.
- Bağımsız code review: actionable bulgu yok.

## VERIFICATION

Gerçek world/level asset'leri ve mevcut BoardView/TileView/EnergyFlowView kullanan, save/progression/story servislerinden ayrılmış Play Mode QA fixture'ı:

| World | Level | Double tap / sync / endpoint / hint lock / solve / single completion / restart | Target hold |
| --- | --- | --- | --- |
| Sakura Garden | 1 | Hepsi geçti | 0,559 s |
| Sakura Garden | 6 | Hepsi geçti | 0,556 s |
| Sakura Garden | 12 | Hepsi geçti | 0,555 s |
| Bamboo Workshop | 6 | Hepsi geçti | 0,553 s |
| Moon Shrine | 6 | Hepsi geçti | 0,552 s |

Beş completion görüntüsü ve bir flow görüntüsü incelendi. Fixture completion paneli serialized panel kopyasıdır; gerçek GameUI theme/buton lifecycle'ı için uçtan uca doğrulama değildir. Gerçek Next, story checkpoint ve kalıcı ilerleme canlı olarak değiştirilmedi. İlgili mantık EditMode suite içinde test edildi.

Unity console: compile error/exception yok; fixture kurulumu sırasında orijinal BoardCameraFitter'ın görünür board bulamadığı bir uyarı var. Missing Script 0, broken serialized reference 0. QA sonrasında Play Mode durduruldu: Gameplay sahnesi dirty=false, fixture yok, Missing Script 0, broken reference 0, productName=Ruilay.

`git diff --check` yalnızca görev başlangıcında da bulunan Assets/BuildProfiles/WebGL RC1.asset:955 trailing whitespace'ını bildirir. İlgisiz profile formatting değiştirilmedi. Temp/GameplayPolishQA sonuçları/görüntüleri ignored kalır; build/cache commit'e eklenmedi. Son COMPLETE sonrası stop cleanup'ının yazdığı INTERRUPTED satırı bitmiş beş kontrolü geçersiz kılmaz.

## MANUAL ACTIONS

Zorunlu sprite/material/clip ataması yok. Özel ses istenirse ilgili WorldGameplayTheme asset'inin Pipe Rotate Clip alanına kısa yumuşak dönüş sesi, Level Complete Clip alanına kısa cozy başarı sesi atanabilir. Moon Shrine için iki hint istenirse aynı theme asset'inde Max Hints Per Level=2 yapılabilir; şu an tüm dünyalar varsayılan 3'ü devralır.

Gerçek oyun smoke testi: Gameplay sahnesinde Play, bir pipe'a hızlı çift tık, HINT ile kilitleme ve kalan sayı kontrolü, çözüm sırasında input kontrolü, su/target beklemesi, Replay ve Next. Sakura 1/6/12, Bamboo 6, Moon 6 üzerinde denenebilir. Fiziksel mobil touch/audio işitme testi ve WebGL player build bu pass'te çalıştırılmadı.

Otomatik onay incelemesi canlı QA için PlayerPrefs progression/story değerlerini geçici değiştirme işlemini, kullanıcının save/progression/story sınırı nedeniyle reddetti. Bu işlem yürütülmedi; kayıtları değiştirmeyen izole fixture kullanıldı.
