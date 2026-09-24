# 03 — Ayarlar penceresi

- **Durum:** yapılacak
- **Bağımlılık:** 02
- **Gereksinimler:** PR-018, PR-010 · AC-010

## Amaç
Dişli simgesinden açılan küçük bir ayarlar penceresi.

## Kapsam
- Tema seçimi (işlevi 04'te)
- Tepsi davranışı ve kapatma düğmesi davranışı
- Bildirimler açık/kapalı
- "Profil seçilince hemen uygula"
- Bağdaştırıcı kategorilerinin görünürlüğü
- Son pencere durumunu hatırlama
- Değişiklikler `settings.json`'a kaydedilir

## Bitti sayılır
- [ ] Tüm ayarlar kaydediliyor ve yeniden açılışta korunuyor
- [ ] "Hemen uygula" açıkken profil seçimi normal doğrulamalı uygulama akışını başlatıyor

## Sorular
1. Tema seçimi için hangi seçenekler sunulmalı ve ayar 04 numaralı iş tamamlanana kadar arayüzde nasıl davranmalı?
    * Açık, koyu ve sistem olmalı, şimdilik sadece açık mod kalsın, 4. görevden sonra entegre edilir
2. Tepsiye küçültme, pencereyi kapatma ve uygulamadan tamamen çıkma davranışları hangi kullanıcı eylemlerinde uygulanmalı?
    * Alt+F4, kapatma tuşuna basma. Yani kapatma eyleminde tetiklenmeli anlamadım bu soruyu tam olarak
3. Bildirimler kapalıyken hangi bildirim türleri gizlenmeli; hata ve kritik durum bildirimleri bu tercihten etkilenmeli mi?
    * Tüm bildirimler gizlenmeli, hata ve kritik durumlar, kullanıcı önüne popup açmalı
4. "Profil seçilince hemen uygula" ayarı etkin olduğunda doğrulama akışında kullanıcıya hangi onay, uyarı veya iptal imkânları sunulmalı?
    * sadece ayar değiştirildi gibi bir bildirim çıkmalı
5. Bağdaştırıcı kategorilerinin görünürlüğünde hangi kategoriler bulunmalı ve varsayılan görünürlükleri ne olmalı?
    * Sanal ve Gerçek olarak iki farklı kategori, kablosuz ve kablolu olarak da iki alt kategori olmalı, sanalların görünürlüğünü kullanıcı açıp kapatmalı
6. Son pencere durumu olarak hangi bilgiler (boyut, konum, büyütülmüş durum, seçili sekme) kaydedilmeli; geçersiz ekran konumları nasıl ele alınmalı?
    * Bunlar, kullanıcının görmesi gereken ayarlar değil
7. Ayarlar penceresi ana pencereye göre modal mı olmalı; tekrar açma girişiminde mevcut pencere öne mi getirilmeli?
    * Evet modal olmalı
8. Ayarlar değişiklikleri anında mı kaydedilmeli, yoksa Uygula/Tamam/İptal düğmeleriyle mi yönetilmeli; İptal hangi değişiklikleri geri almalı?
    * Kritik bir değişiklik ise onay sormalı, tüm ayar değişiklikleri anında kaydedilmeli
9. Ayarları fabrika varsayılanlarına döndürme işlevi gerekli mi; gerekliyse tüm ayarlar mı yoksa seçili bölümler mi sıfırlanabilmeli ve işlem nasıl onaylanmalı?
    * Tüm ayarlar, sıfırlanabilmeli
10. `settings.json` yazılamadığında veya okunamadığında kullanıcıya hangi durum gösterilmeli; uygulama varsayılanlarla çalışmayı nasıl sürdürmeli?
    * Kullanıcıya ayarlar ve ana ekranda küçük kırmızı bir alan ile setting.json yazılamıyor, ayarlarınız şu an kaydedilemiyor gibi bir uyarı olsun
11. Eksik, bozuk veya eski şemalı ayar değerleri algılandığında hangi varsayılanlar kullanılmalı ve ayarlar dosyası ne zaman düzeltilmeli?
    * sürümün varsayılan ayarları yüklenmeli
12. Ayarlar tüm kullanıcı profilleri ve bağdaştırıcılar için ortak mı olmalı; makineye, kullanıcıya veya seçili bağdaştırıcıya özgü bir ayar gereksinimi var mı?
    * kullanıcıya bağlı olmalı
13. Gizlenen bir bağdaştırıcı kategorisinde etkin ya da seçili bağdaştırıcı varsa arayüz nasıl davranmalı; kullanıcı bu bağdaştırıcıya yeniden nasıl erişebilmeli?
    * Bunu bilmiyorum, ui görmeden cevap veremem, şimdilik kalabilir
14. Yeni, tanınmayan veya birden fazla kategoriye uyan bağdaştırıcılar hangi görünürlük kuralına tabi olmalı?
    * Bilmiyorum, sen karar ver
15. "Profil seçilince hemen uygula" yalnızca fareyle açık seçimde mi, klavye ile gezinmede de mi tetiklenmeli; aynı profilin yeniden seçilmesi nasıl ele alınmalı?
    * Fareyle açık seçimde
16. Hemen uygulama öncesindeki normal doğrulama; IP çakışma denetimi, yönetici yetkisi ve başarısız uygulama geri alma davranışlarını aynen korumalı mı?
    * Evet korumalı
17. Bildirim tercihi uygulama içi durum mesajlarını, Windows bildirimlerini ve tepsi balonlarını nasıl ayrı ayrı etkilemeli?
    * Uygulama içi durum mesajını etkilemez, windows bildirimlerini kapatır, tepsi balonlarını da kapatır
18. Windows bildirim izni kapalıysa veya tepsi simgesi kullanılamıyorsa kullanıcıya hangi alternatif geri bildirim sunulmalı?
    * Bi bildirim sunulmamalı
19. Tema, tepsi ve pencere davranışı değişiklikleri anında etkili olmalı mı; uygulamanın yeniden başlatılmasını gerektiren ayarlar var mı?
    * Anında etkili olmalı
20. Son pencere durumu geri yüklenirken ekran çözünürlüğü, DPI ölçeklemesi veya monitör düzeni değişmişse boyut ve konum hangi güvenli sınırlara çekilmeli?
    * Ana ekrana çekilmeli ve default boyutlara geri döndürülmeli
21. Ayarlar penceresi klavye ile tamamen kullanılabilir olmalı mı; odak sırası, erişilebilir adlar ve yüksek karşıtlık modu için hangi koşullar aranmalı?
    * Çok gerek yok
22. Her ayarın yanında açıklama, varsayılan değer veya etkisini gösteren yardım metni gerekli mi; özellikle geri alınması zor davranışlar nasıl açıklanmalı?
    * Yardım metni olsun fakat bir "i" ikonunun üzerine fare ile gelindiğinde açıklansın. Varsayılan değer de bu açıklamanın altında olsun.