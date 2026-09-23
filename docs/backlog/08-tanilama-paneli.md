# 08 — Tanılama paneli

- **Durum:** yapılacak
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
- [ ] Panel bilgileri doğru gösteriyor
- [ ] Tek tıkla panoya kopyalanıyor

## Sorular
1. Tanılama paneli ana pencerede nerede açılmalı (ayrı pencere, ayarlar içinde bölüm veya başka bir konum) ve hangi kullanıcı eylemiyle erişilmelidir?
2. Windows sürümü kullanıcıya hangi ayrıntı düzeyinde gösterilmelidir (ürün adı, sürüm, derleme numarası ve mimari gibi)?
3. Uygulama sürümü hangi kaynaktan alınmalı ve ön-sürüm/derleme bilgisi de görüntülenmeli midir?
4. Yönetici durumu yalnızca uygulamanın yükseltilmiş çalışıp çalışmadığını mı belirtmeli, yoksa kullanıcının yönetici grubunda olup olmadığını da mı göstermelidir?
5. Bağdaştırıcı sayısına fiziksel, sanal, devre dışı ve bağlantısız bağdaştırıcıların hangileri dahil edilmelidir?
6. "Profil deposu durumu" hangi durumları ve hangi kullanıcı dostu ifadeleri kapsamalıdır (mevcut, boş, erişilemiyor, bozuk veya oluşturulmamış gibi)?
7. Profil sayısına geçersiz ya da okunamayan profil dosyaları dahil edilmeli midir; edilmezse bunların varlığı ayrıca belirtilmeli midir?
8. Günlük dosyası için yalnızca beklenen kayıt konumu mu gösterilmeli, yoksa henüz günlük dosyası yoksa bu durum ayrıca belirtilmeli midir?
9. Kopyalanan içerik hangi biçimde olmalıdır (düz metin, satır ad/değer düzeni, Markdown gibi) ve paneldeki tüm bilgiler eksiksiz yer almalı mıdır?
10. Tanılama bilgileri kişisel veya hassas kabul edilebilecek veriler içerdiğinden, kopyalama öncesinde gizleme, uyarı veya kullanıcı onayı gerekli midir?
11. "Kopyala" eylemi başarılı olduğunda veya pano erişimi başarısız olduğunda kullanıcıya nasıl geri bildirim verilmelidir?
12. Tanılama verileri panel her açıldığında mı toplanmalı, yoksa bir yenileme eylemi bulunmalı mıdır?
13. Panel, bilgi kaynaklarından biri okunamadığında kalan bilgileri göstermeye devam etmeli mi; başarısız alan için hangi metin gösterilmelidir?
