# 07 — Kritik hata günlüğü ve çökme işareti

- **Durum:** tamamlandı
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
- [x] Yakalanmamış istisna günlüğe düşüyor
- [x] Zorla kapatılan uygulama bir sonraki açılışta bunu bildiriyor

## Sorular

1. Günlük dosyası hangi dizinde tutulmalı; kullanıcı profili altında mı, uygulama dizininde mi?
    * kullanıcı profili altında
2. Günlük için azami boyut, saklama süresi ve eski kayıtların silinmesi ya da döndürülmesi nasıl belirlenmeli?
    * Kullanıcı silmediği sürece kalsın.
3. Günlük kayıt formatı ne olmalı ve her kayıtta zaman damgası, hata kodu, istisna ayrıntısı, yığın izi ve uygulama sürümü gibi hangi alanlar bulunmalı?
    * Bulunmalı
4. Günlükte IP adresi, ağ geçidi, DNS, profil adı veya dosya yolu gibi kişisel ya da hassas olabilecek veriler maskelenmeli mi?
    * Hayır, zaten kullanıcının bilgisayarında
5. “Beklenmeyen Windows ağ API hatası” kapsamına hangi hata türleri ve dönüş kodları girer; beklenen kullanıcı/ortam hataları nasıl ayrıştırılmalı?
    * Bilmiyorum sen karar ver
6. Günlüğe yazma işlemi başarısız olursa uygulama bunu kullanıcıya bildirmeli mi; bildirecekse hangi kanaldan?
    * Toast bildirim olsun
7. Yakalanmamış istisnada günlük kaydından sonra uygulama kapanmalı mı, güvenli bir durumda açık kalmayı denemeli mi?
    * Bilmiyorum sen karar ver
8. Oturum işareti uygulama başlar başlamaz mı oluşturulmalı; başlatma aşamasındaki hangi noktadan itibaren çökme olarak kabul edilmeli?
    * Yok, ilk değişiklikten sonra başlamalı
9. Temiz kapanış sayılan durumlar nelerdir: normal pencere kapatma, Windows oturum kapatma, sistem kapanışı ve uygulama güncellemesi ayrı ele alınmalı mı?
    * Yok kullanıcının kapatmadığı tüm kapatma türleri temiz kapanış sayılmamalı
10. Bir önceki oturumun beklenmedik kapandığı bilgisi kullanıcıya nerede, ne zaman ve ne kadar süreyle gösterilmeli; kullanıcı bu bildirimi kapatabilmeli mi?
    * ilk açılışta, kısa süreli ve kapatılabilir toast
11. Çökme bildirimi gösterildikten sonra oturum işareti hemen silinmeli mi; bildirim gösterilemezse veya uygulama yeniden çökerse nasıl davranılmalı?
    * Bilmiyorum sen karar ver
12. Aynı anda birden fazla uygulama örneği çalışabiliyor mu; çalışabiliyorsa oturum işareti ve günlük dosyasının çakışması nasıl önlenmeli?
    * Hayır
13. Başlangıç sırasında oturum işaretine veya günlüğe erişilememesi durumunda uygulama açılmaya devam etmeli mi?
    * Evet
14. Günlük ve çökme işareti kullanıcının silebileceği, dışa aktarabileceği veya destek talebine ekleyebileceği bir arayüz gerektiriyor mu?
    * Hayır
