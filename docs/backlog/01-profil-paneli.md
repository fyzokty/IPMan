# 01 — Profil paneli

- **Durum:** devam ediyor
- **Bağımlılık:** yok
- **Gereksinimler:** PR-009..PR-017 · AC-009, AC-011..AC-015

## Amaç
`IProfileCatalog` arka planda profilleri yüklüyor, izliyor ve bozuk dosyaları
ayırıyor; ama pencerede buna bağlı bir arayüz yok. Ana pencereye sabit bir sol
profil paneli eklenir.

## Kapsam
- Profil listesi: favoriler önce, gruplar içinde alfabetik
- Arama: ad, açıklama ve IP değerleri
- Profil seçimi düzenleme alanlarını doldurur, Windows'u değiştirmez
- Mevcut değerlerden farklı alanların vurgulanması (karşılaştırma)
- Çalışma alanından profil kaydetme (ad, açıklama, DHCP/statik)
- Aynı ad girilince numaralı ad üretilir (`PLC (1)`)
- Sağ tık menüsü: yeniden adlandır, çoğalt, favori, dışa aktar, sil (onaylı)
- Tek profil içe/dışa aktarma (JSON)
- Bozuk profil dosyalarının panelde ayrıca gösterilmesi
- Tüm durum/hata sonuçları için Türkçe metinler

## Alınmış kararlar
- **İçe/dışa aktarma `IProfileCatalog` üzerinden yapılır.** Katalog, bellekteki
  asıl görünümdür ve kaydetme/silmeden sonra kendini yeniler; `IProfileRepository`
  kullanılırsa liste, klasör izleyici tetiklenene kadar eski kalır. İkisi de
  depodaki gibi `Stream` alır.
- **Dosya penceresi arayüzü yol değil, stream döndürür.** ViewModel dosya
  sistemine hiç dokunmaz; testlerde `MemoryStream` kullanılır.
- **Kaydetme satır içidir, ayrı pencere açılmaz.** Panelde ad, açıklama ve DHCP
  profili onay kutusu kaydet düğmesinin yanında durur. Yalnızca yeniden
  adlandırma, `IUserTextInputService` üzerinden ad sorar.
- **Çoğaltma ad sormaz.** Kopyayı kaydeder, mevcut `ProfileNameResolver`
  `PLC (1)` adını üretir (AC-011).
- **Fark vurgulama taslak (draft) üzerindedir.** Taslak, bağdaştırıcının mevcut
  değerlerini zaten tuttuğu için alan başına "Windows'tan farklı" bayrağı tek
  karşılaştırmayla hesaplanır.

## Uygulama adımları
- **A — Application ve ViewModel katmanı:** katalog içe/dışa aktarma, panel ve
  liste ViewModel'leri, taslağa profil yükleme ve fark bayrakları, pencere
  arayüzleri, metinler, testler. XAML ve DI bağlantısı yok.
- **B — Görünüm ve bağlantı:** sol panel, liste gruplama, sağ tık menüsü, arama,
  kaydetme satırı, bozuk profil uyarısı, WPF pencere uygulamaları, DI bağlantısı.

## Kapsam dışı
- **Dosya konumunu açma.** `Process.Start` proje kurallarında yasak; bunun için
  `SHOpenFolderAndSelectItems` P/Invoke arayüzü gerekir ve profil sözleşmeleri
  dosya yolunu dışarı vermiyor. Ayrı bir iş olarak ele alınır.
- **Seçilince hemen uygulama (AC-010).** `AppSettings.ApplyProfileOnSelection`
  alanı var ama açılışta ayarları yükleyen bir şey yok; ayarlar penceresiyle
  (03) birlikte gelir. O zamana kadar panel yalnızca alanları doldurur.
- Çoklu profil ZIP dışa aktarma (1.0 dışında).

## Bitti sayılır
- [ ] Yukarıdaki davranışlar arayüzden çalışıyor
- [ ] Seçilen DHCP profili sahte statik değer üretmez; hangi eylemin onu uygulayacağını söyler
- [ ] Tüm `ProfileSaveStatus`, `ProfileDeleteStatus`, `ProfileImportStatus`,
      `ProfileExportStatus` ve `ProfileLoadFailureKind` üyeleri Türkçe metne eşleniyor
- [ ] `Strings.resx` dışında kullanıcıya görünen metin yok
- [ ] ViewModel katmanı birim testleriyle kapsanıyor
- [ ] `IProfileCatalog` artık bir ViewModel tarafından kullanılıyor
