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
2. Tepsiye küçültme, pencereyi kapatma ve uygulamadan tamamen çıkma davranışları hangi kullanıcı eylemlerinde uygulanmalı?
3. Bildirimler kapalıyken hangi bildirim türleri gizlenmeli; hata ve kritik durum bildirimleri bu tercihten etkilenmeli mi?
4. "Profil seçilince hemen uygula" ayarı etkin olduğunda doğrulama akışında kullanıcıya hangi onay, uyarı veya iptal imkânları sunulmalı?
5. Bağdaştırıcı kategorilerinin görünürlüğünde hangi kategoriler bulunmalı ve varsayılan görünürlükleri ne olmalı?
6. Son pencere durumu olarak hangi bilgiler (boyut, konum, büyütülmüş durum, seçili sekme) kaydedilmeli; geçersiz ekran konumları nasıl ele alınmalı?
7. Ayarlar penceresi ana pencereye göre modal mı olmalı; tekrar açma girişiminde mevcut pencere öne mi getirilmeli?
8. Ayarlar değişiklikleri anında mı kaydedilmeli, yoksa Uygula/Tamam/İptal düğmeleriyle mi yönetilmeli; İptal hangi değişiklikleri geri almalı?
9. Ayarları fabrika varsayılanlarına döndürme işlevi gerekli mi; gerekliyse tüm ayarlar mı yoksa seçili bölümler mi sıfırlanabilmeli ve işlem nasıl onaylanmalı?
10. `settings.json` yazılamadığında veya okunamadığında kullanıcıya hangi durum gösterilmeli; uygulama varsayılanlarla çalışmayı nasıl sürdürmeli?
11. Eksik, bozuk veya eski şemalı ayar değerleri algılandığında hangi varsayılanlar kullanılmalı ve ayarlar dosyası ne zaman düzeltilmeli?
12. Ayarlar tüm kullanıcı profilleri ve bağdaştırıcılar için ortak mı olmalı; makineye, kullanıcıya veya seçili bağdaştırıcıya özgü bir ayar gereksinimi var mı?
13. Gizlenen bir bağdaştırıcı kategorisinde etkin ya da seçili bağdaştırıcı varsa arayüz nasıl davranmalı; kullanıcı bu bağdaştırıcıya yeniden nasıl erişebilmeli?
14. Yeni, tanınmayan veya birden fazla kategoriye uyan bağdaştırıcılar hangi görünürlük kuralına tabi olmalı?
15. "Profil seçilince hemen uygula" yalnızca fareyle açık seçimde mi, klavye ile gezinmede de mi tetiklenmeli; aynı profilin yeniden seçilmesi nasıl ele alınmalı?
16. Hemen uygulama öncesindeki normal doğrulama; IP çakışma denetimi, yönetici yetkisi ve başarısız uygulama geri alma davranışlarını aynen korumalı mı?
17. Bildirim tercihi uygulama içi durum mesajlarını, Windows bildirimlerini ve tepsi balonlarını nasıl ayrı ayrı etkilemeli?
18. Windows bildirim izni kapalıysa veya tepsi simgesi kullanılamıyorsa kullanıcıya hangi alternatif geri bildirim sunulmalı?
19. Tema, tepsi ve pencere davranışı değişiklikleri anında etkili olmalı mı; uygulamanın yeniden başlatılmasını gerektiren ayarlar var mı?
20. Son pencere durumu geri yüklenirken ekran çözünürlüğü, DPI ölçeklemesi veya monitör düzeni değişmişse boyut ve konum hangi güvenli sınırlara çekilmeli?
21. Ayarlar penceresi klavye ile tamamen kullanılabilir olmalı mı; odak sırası, erişilebilir adlar ve yüksek karşıtlık modu için hangi koşullar aranmalı?
22. Her ayarın yanında açıklama, varsayılan değer veya etkisini gösteren yardım metni gerekli mi; özellikle geri alınması zor davranışlar nasıl açıklanmalı?
