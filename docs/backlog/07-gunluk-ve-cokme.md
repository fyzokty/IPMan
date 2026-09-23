# 07 — Kritik hata günlüğü ve çökme işareti

- **Durum:** yapılacak
- **Bağımlılık:** yok
- **Gereksinimler:** PR-026, PR-027 · AC-022

## Amaç
Yalnızca kritik teknik hataları kaydetmek ve beklenmedik kapanmayı bir sonraki
açılışta bildirmek.

## Kapsam
- Günlüğe yazılanlar: bozuk/okunamayan JSON, ayar/profil yazma hataları,
  beklenmeyen Windows ağ API hataları, yakalanmamış istisnalar
- Kullanıcı eylem geçmişi tutulmaz
- Günlük dosyasının konumu ve boyut sınırı
- Oturum işareti: temiz kapanışta silinir, sonraki açılışta varsa kısa bilgi gösterilir

## Bitti sayılır
- [ ] Yakalanmamış istisna günlüğe düşüyor
- [ ] Zorla kapatılan uygulama bir sonraki açılışta bunu bildiriyor

## Sorular

1. Günlük dosyası hangi dizinde tutulmalı; kullanıcı profili altında mı, uygulama dizininde mi?
2. Günlük için azami boyut, saklama süresi ve eski kayıtların silinmesi ya da döndürülmesi nasıl belirlenmeli?
3. Günlük kayıt formatı ne olmalı ve her kayıtta zaman damgası, hata kodu, istisna ayrıntısı, yığın izi ve uygulama sürümü gibi hangi alanlar bulunmalı?
4. Günlükte IP adresi, ağ geçidi, DNS, profil adı veya dosya yolu gibi kişisel ya da hassas olabilecek veriler maskelenmeli mi?
5. “Beklenmeyen Windows ağ API hatası” kapsamına hangi hata türleri ve dönüş kodları girer; beklenen kullanıcı/ortam hataları nasıl ayrıştırılmalı?
6. Günlüğe yazma işlemi başarısız olursa uygulama bunu kullanıcıya bildirmeli mi; bildirecekse hangi kanaldan?
7. Yakalanmamış istisnada günlük kaydından sonra uygulama kapanmalı mı, güvenli bir durumda açık kalmayı denemeli mi?
8. Oturum işareti uygulama başlar başlamaz mı oluşturulmalı; başlatma aşamasındaki hangi noktadan itibaren çökme olarak kabul edilmeli?
9. Temiz kapanış sayılan durumlar nelerdir: normal pencere kapatma, Windows oturum kapatma, sistem kapanışı ve uygulama güncellemesi ayrı ele alınmalı mı?
10. Bir önceki oturumun beklenmedik kapandığı bilgisi kullanıcıya nerede, ne zaman ve ne kadar süreyle gösterilmeli; kullanıcı bu bildirimi kapatabilmeli mi?
11. Çökme bildirimi gösterildikten sonra oturum işareti hemen silinmeli mi; bildirim gösterilemezse veya uygulama yeniden çökerse nasıl davranılmalı?
12. Aynı anda birden fazla uygulama örneği çalışabiliyor mu; çalışabiliyorsa oturum işareti ve günlük dosyasının çakışması nasıl önlenmeli?
13. Başlangıç sırasında oturum işaretine veya günlüğe erişilememesi durumunda uygulama açılmaya devam etmeli mi?
14. Günlük ve çökme işareti kullanıcının silebileceği, dışa aktarabileceği veya destek talebine ekleyebileceği bir arayüz gerektiriyor mu?
