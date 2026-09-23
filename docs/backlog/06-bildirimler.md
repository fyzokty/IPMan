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
2. Proje Windows 10'u da desteklediğinden, Windows 10'da bildirim davranışı ve görünümü için kabul edilen beklenti nedir?
3. Bildirim kapsamına yalnızca Uygula, DHCP ve geri yükleme işlemleri mi girer; profil kaydetme, silme, içe/dışa aktarma ve diğer işlemler kapsam dışı mıdır?
4. Her işlem için başarı, hata, uyarı ve kısmi başarı durumlarının hangilerinde bildirim gösterilmelidir?
5. Bildirim metninde hangi bilgiler yer almalıdır: adaptör adı, profil adı, işlem türü, hata özeti ve/veya uygulama içi durum metninin tamamı?
6. Birden fazla adaptör veya ardışık işlem sonucunda her sonuç için ayrı bildirim mi, yoksa özet tek bildirim mi gösterilmelidir?
7. Bildirime tıklanınca uygulama öne getirilmeli ve ilgili adaptör sekmesi seçilmeli midir?
8. Bildirimlerde "Uygulamayı aç", "Ayrıntıları göster", "Tekrar dene" gibi eylem düğmeleri gerekli midir; gerekiyorsa hangileri desteklenmelidir?
9. Ayar varsayılan olarak açık mı kapalı mı olmalı; kullanıcı tercihi mevcut ayarlar dosyasında kalıcı olarak saklanmalı mıdır?
10. Bildirim ayarı tüm uygulama için tek bir genel tercih mi olmalı, yoksa işlem türüne veya adaptöre göre ayrı seçenekler gerekli midir?
11. Uygulama penceresi ön plandayken de bildirim gösterilmeli mi, yoksa yalnızca küçültülmüş veya sistem tepsisindeyken mi gösterilmelidir?
12. Uygulama sistem tepsine kapatıldığında/indirildiğinde bildirim beklentisi nedir; uygulama tamamen kapalıyken işlem sonucu oluşamayacağı varsayımı doğru mudur?
13. Windows'un Odak yardımı/Rahatsız etmeyin ayarı bildirimi engellerse uygulama içi durum dışında kullanıcıya ek bir geri bildirim gerekir mi?
14. Windows bildirim izni kapalıysa veya toast bildirimi gönderilemezse bu durum kullanıcıya nasıl ve nerede bildirilmelidir?
15. Bildirimlerin otomatik kaybolma süresi, öncelik seviyesi ve Windows Bildirim Merkezi geçmişinde kalma davranışı için bir beklenti var mıdır?
16. Bildirim metinleri yalnızca Türkçe mi olmalıdır; gelecekte dil desteği için yerelleştirme gereksinimi var mıdır?
