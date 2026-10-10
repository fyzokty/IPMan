# 08 — Tanılama paneli

- **Durum:** tamamlandı
- **Bağımlılık:** 07
- **Gereksinimler:** PR-028 · AC-021

## Amaç
Destek için kopyalanabilir sistem bilgisi.

## Kapsam
- Windows sürümü, uygulama sürümü, yönetici durumu
- Bağdaştırıcı sayısı, profil deposu durumu, profil sayısı
- Günlük dosyasının konumu
- "Kopyala" eylemi

## Bitti sayılır
- [x] Panel bilgileri doğru gösteriyor
- [x] Tek tıkla panoya kopyalanıyor

## Sorular

Yanıtlar kullanıcı onayı beklemeden makul varsayılan olarak verildi; değiştirilebilir.
1. Tanılama paneli ana pencerede nerede açılmalı (ayrı pencere, ayarlar içinde bölüm veya başka bir konum) ve hangi kullanıcı eylemiyle erişilmelidir?
    * Ayarlar penceresindeki "Tanılama…" düğmesiyle açılan ayrı, küçük, ayarlar penceresine bağlı modal bir pencere.
2. Windows sürümü kullanıcıya hangi ayrıntı düzeyinde gösterilmelidir (ürün adı, sürüm, derleme numarası ve mimari gibi)?
    * Ürün adı (derleme ≥ 22000 ise Windows 11), görünen sürüm (ör. 23H2), derleme numarası ve işletim sistemi mimarisi.
3. Uygulama sürümü hangi kaynaktan alınmalı ve ön-sürüm/derleme bilgisi de görüntülenmeli midir?
    * Uygulama derlemesinin `AssemblyInformationalVersion` değeri; ön-sürüm eki varsa olduğu gibi gösterilir.
4. Yönetici durumu yalnızca uygulamanın yükseltilmiş çalışıp çalışmadığını mı belirtmeli, yoksa kullanıcının yönetici grubunda olup olmadığını da mı göstermelidir?
    * Yalnızca uygulamanın yükseltilmiş (yönetici) çalışıp çalışmadığı.
5. Bağdaştırıcı sayısına fiziksel, sanal, devre dışı ve bağlantısız bağdaştırıcıların hangileri dahil edilmelidir?
    * Ana pencerenin listelediği bağdaştırıcı sayısı (aynı kaynak ve filtre).
6. "Profil deposu durumu" hangi durumları ve hangi kullanıcı dostu ifadeleri kapsamalıdır (mevcut, boş, erişilemiyor, bozuk veya oluşturulmamış gibi)?
    * Profil klasörü yolu ve durumu: "Mevcut", "Henüz oluşturulmadı", "Erişilemiyor".
7. Profil sayısına geçersiz ya da okunamayan profil dosyaları dahil edilmeli midir; edilmezse bunların varlığı ayrıca belirtilmeli midir?
    * Geçerli profil sayısı ayrı, okunamayan/bozuk profil dosyası sayısı ayrı satırda gösterilir.
8. Günlük dosyası için yalnızca beklenen kayıt konumu mu gösterilmeli, yoksa henüz günlük dosyası yoksa bu durum ayrıca belirtilmeli midir?
    * Beklenen günlük yolu gösterilir; dosya yoksa "(henüz oluşturulmadı)" eklenir.
9. Kopyalanan içerik hangi biçimde olmalıdır (düz metin, satır ad/değer düzeni, Markdown gibi) ve paneldeki tüm bilgiler eksiksiz yer almalı mıdır?
    * Düz metin, her bilgi `Ad: Değer` satırı; paneldeki tüm bilgiler ve oluşturulma zamanı yer alır.
10. Tanılama bilgileri kişisel veya hassas kabul edilebilecek veriler içerdiğinden, kopyalama öncesinde gizleme, uyarı veya kullanıcı onayı gerekli midir?
    * Hayır; veriler kullanıcının kendi bilgisayarında (07 / soru 4 ile tutarlı).
11. "Kopyala" eylemi başarılı olduğunda veya pano erişimi başarısız olduğunda kullanıcıya nasıl geri bildirim verilmelidir?
    * Satır içi durum metni: "Panoya kopyalandı" / "Panoya kopyalanamadı". Bildirim (toast) gösterilmez.
12. Tanılama verileri panel her açıldığında mı toplanmalı, yoksa bir yenileme eylemi bulunmalı mıdır?
    * Pencere açıldığında toplanır; ayrıca "Yenile" düğmesi bulunur.
13. Panel, bilgi kaynaklarından biri okunamadığında kalan bilgileri göstermeye devam etmeli mi; başarısız alan için hangi metin gösterilmelidir?
    * Evet; okunamayan alan "Okunamadı" gösterir, diğerleri gösterilmeye devam eder.
