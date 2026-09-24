# 06 — Bildirimler

- **Durum:** yapılacak
- **Bağımlılık:** 03
- **Gereksinimler:** PR-025

## Amaç
Önemli işlemlerin başarı/başarısızlığı için Windows 11 tarzı bildirim.

## Kapsam
- Uygula, DHCP ve geri yükleme sonuçları için bildirim
- Ayarlardan kapatılabilir
- Uygulama içi durum metni her zaman korunur; bildirim tek kaynak değildir
- Özellikle pencere tepsideyken anlamlı

## Bitti sayılır
- [ ] Bildirimler çıkıyor ve ayarla kapatılabiliyor
- [ ] Bildirim kapalıyken de sonuç uygulama içinde görülebiliyor

## Sorular

1. "Windows 11 tarzı bildirim" ile Windows yerel toast bildirimi mi, yoksa uygulama içinde görsel olarak Windows 11 stilinde bir bildirim mi kastediliyor?
    * Hayır OS bildirimlerinden bahsediliyor
2. Proje Windows 10'u da desteklediğinden, Windows 10'da bildirim davranışı ve görünümü için kabul edilen beklenti nedir?
    * Windows 10 bildirim tarzı
3. Bildirim kapsamına yalnızca Uygula, DHCP ve geri yükleme işlemleri mi girer; profil kaydetme, silme, içe/dışa aktarma ve diğer işlemler kapsam dışı mıdır?
    * Sadece önemli işlemler, Bağdaştırıcıyı değiştiren işlemler
4. Her işlem için başarı, hata, uyarı ve kısmi başarı durumlarının hangilerinde bildirim gösterilmelidir?
    * Hayır bağdaştırıcı ayarlarını değiştiren veya bağdaştırıcı ayarları değiştirilirken alınan hatalar
5. Bildirim metninde hangi bilgiler yer almalıdır: adaptör adı, profil adı, işlem türü, hata özeti ve/veya uygulama içi durum metninin tamamı?
    * İşlem türü ve adaptör adı
6. Birden fazla adaptör veya ardışık işlem sonucunda her sonuç için ayrı bildirim mi, yoksa özet tek bildirim mi gösterilmelidir?
    * Özet tek
7. Bildirime tıklanınca uygulama öne getirilmeli ve ilgili adaptör sekmesi seçilmeli midir?
    * Evet ve uygulamaya bir bildirim geçmişi gibi ufak bir ekran veya modal hazırlanmalı
8. Bildirimlerde "Uygulamayı aç", "Ayrıntıları göster", "Tekrar dene" gibi eylem düğmeleri gerekli midir; gerekiyorsa hangileri desteklenmelidir?
    * Yok
9. Ayar varsayılan olarak açık mı kapalı mı olmalı; kullanıcı tercihi mevcut ayarlar dosyasında kalıcı olarak saklanmalı mıdır?
    * Varsayılan olarak kapalı, ama ilk açılışta bildirim gösterilmek isteniyor gibi bir modal gösterilmeli
10. Bildirim ayarı tüm uygulama için tek bir genel tercih mi olmalı, yoksa işlem türüne veya adaptöre göre ayrı seçenekler gerekli midir?
    * Tüm uygulama için genel tercih olmalı, kullanıcı isterse ayarlardan ne zaman gösterileceğini seçebilmeli
11. Uygulama penceresi ön plandayken de bildirim gösterilmeli mi, yoksa yalnızca küçültülmüş veya sistem tepsisindeyken mi gösterilmelidir?
    * Yalnızca pencerede odak varken gösterilmemeli, eğer pencere odaklı değilse, simge durumundaysa veya tepsideyse bildirim gösterilmeli
12. Uygulama sistem tepsine kapatıldığında/indirildiğinde bildirim beklentisi nedir; uygulama tamamen kapalıyken işlem sonucu oluşamayacağı varsayımı doğru mudur?
    * Bilmiyorum
13. Windows'un Odak yardımı/Rahatsız etmeyin ayarı bildirimi engellerse uygulama içi durum dışında kullanıcıya ek bir geri bildirim gerekir mi?
    * Yok
14. Windows bildirim izni kapalıysa veya toast bildirimi gönderilemezse bu durum kullanıcıya nasıl ve nerede bildirilmelidir?
    * Toast bildirim zaten uygulama için bildirim değil mi, eğer değilse bildirim paneli gibi bir ekran yapacağız orda görüntülensin
15. Bildirimlerin otomatik kaybolma süresi, öncelik seviyesi ve Windows Bildirim Merkezi geçmişinde kalma davranışı için bir beklenti var mıdır?
    * Yok genel geçer kurallar ne ise o kullanılsın
16. Bildirim metinleri yalnızca Türkçe mi olmalıdır; gelecekte dil desteği için yerelleştirme gereksinimi var mıdır?
    * Projede yerelleştirme desteği varsa yerelleştirme desteği eklensin yani projede dil ne durumdaysa o durumda yapılsın
