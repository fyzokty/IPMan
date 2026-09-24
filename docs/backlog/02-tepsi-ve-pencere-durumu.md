# 02 — Sistem tepsisi ve pencere durumu

- **Durum:** yapılacak
- **Bağımlılık:** 01
- **Gereksinimler:** PR-019, PR-020 · AC-018

## Amaç
Uygulama tepsiye küçülebilir ve pencere düzenini hatırlar.

## Kapsam
- Tepsi simgesi ve menüsü (aç, çıkış)
- İlk kapatma/küçültmede davranış seçimi sorulur ve kaydedilir
- Pencere boyutu, konumu ve son seçili bağdaştırıcı hatırlanır
- Tepsiden açınca önceki boyut/konum geri gelir
- Makul bir en küçük pencere boyutu
- Yeni alanlar `AppSettings`'e eklemeli olarak girer (şema sürümü değişmez)

## Bitti sayılır
- [ ] Yeniden başlatmada pencere aynı yerde ve boyutta açılıyor
- [ ] Seçilen kapatma davranışı kalıcı
- [ ] Ekran dışında kalan konum güvenli şekilde düzeltiliyor

## Sorular
1. İlk kapatma veya küçültme işleminde kullanıcıya sunulacak seçenekler tam olarak neler olmalı (örneğin tepsiye küçült, uygulamadan çık) ve varsayılan seçim hangisi olmalı?
2. Tepsi simgesi uygulama açıkken her zaman mı görünmeli, yoksa yalnızca pencere tepsiye küçültüldüğünde mi gösterilmeli?
3. Kaydedilen pencere konumunun ekran dışında sayılması ve güvenli konuma alınması için çoklu monitör, çözünürlük ve DPI değişimlerinde hangi davranış bekleniyor?
4. Kapatma ve küçültme davranışları ayrı ayrı mı yapılandırılmalı, yoksa tek bir tercih her iki işlemi de mi belirlemeli?
5. Kullanıcı ilk tercihini yaptıktan sonra bu davranışı uygulama içinden değiştirebileceği bir ayar bulunmalı mı; bulunacaksa hangi ekranda yer almalı?
6. Kapatma düğmesi tepsiye gönderme davranışına ayarlıysa, uygulamadan gerçekten çıkmak için tepsi menüsündeki “Çıkış” dışında bir yol sunulmalı mı?
7. Tepsi menüsündeki “Aç” komutu pencere zaten görünür ve etkin durumdaysa pencereyi öne getirip etkinleştirmeli mi, yoksa başka bir davranış mı bekleniyor?
8. Tepsi simgesine tek tıklama, çift tıklama ve sağ tıklama için hangi etkileşimler bekleniyor?
9. Tepsi simgesinin bağlam menüsünde yalnızca “Aç” ve “Çıkış” mı bulunmalı, yoksa seçili bağdaştırıcı ya da profil işlemleri gibi ek kısayollar da gerekli mi?
10. Uygulama tepsiye küçültüldüğünde kullanıcının durumu anlaması için bildirim balonu, ilk kullanım ipucu veya başka bir geri bildirim gösterilmeli mi?
11. Windows bildirimlerinin kapalı olduğu ya da tepsi simgesinin taşma alanında bulunduğu durumlarda kullanıcıya gösterilecek geri bildirim nasıl ele alınmalı?
12. Uygulama tepsiye küçültülmüşken ağ yapılandırması uygulama, doğrulama veya geri alma işlemi sürüyorsa pencerenin otomatik açılması ya da kullanıcıya bildirim gönderilmesi gerekir mi?
13. Çıkış komutu devam eden ağ işlemi, bekleyen doğrulama veya kaydedilmemiş ayar varken nasıl davranmalı; onay istenmeli mi?
14. Pencere kapatılırken veya tepsiye gönderilirken ayarlar her seferinde mi kaydedilmeli, yoksa yalnızca boyut, konum veya seçili bağdaştırıcı değiştiğinde mi yazılmalı?
15. Pencerenin normal, büyütülmüş ve küçültülmüş durumlarından hangisi kalıcı olarak saklanmalı; uygulama yeniden açıldığında büyütülmüş durum geri yüklenmeli mi?
16. Kaydedilen boyut en küçük pencere boyutunun altında, sıfır ya da geçersizse hangi varsayılan boyut ve konum kullanılmalı?
17. Pencerenin yalnızca başlık çubuğu görünür kaldığı ya da kısmen ekran dışında olduğu konumlar güvenli kabul edilmeli mi; görünürlük için asgari alan ne olmalı?
18. Kaydedilen pencere konumu artık bağlı olmayan bir monitöre aitse, pencere birincil ekrana mı yoksa imlecin bulunduğu ekrana mı taşınmalı?
19. DPI ölçeği değiştiğinde kaydedilen boyut fiziksel piksel, WPF aygıttan bağımsız birim veya göreli ekran oranı olarak mı yorumlanmalı?
20. Pencere boyutu ve konumu farklı kullanıcı oturumları ya da Windows kullanıcıları arasında paylaşılmalı mı, yoksa mevcut kullanıcıya özgü mü kalmalı?
21. Son seçili bağdaştırıcı artık yoksa, devre dışıysa veya adı değişmişse başlangıçta hangi bağdaştırıcı seçilmeli ve kullanıcıya bilgi verilmeli mi?
22. Son seçili bağdaştırıcıyı eşleştirmek için görünen ad yeterli mi, yoksa GUID gibi daha kararlı bir kimlik saklanmalı mı?
23. Başlangıçta hiç etkin bağdaştırıcı yoksa pencere ve tepsi menüsü hangi durumda açılmalı?
24. Ayarlar dosyasında yeni pencere veya tepsi alanları bulunmadığında hangi geriye dönük varsayılanlar kullanılmalı?
25. Ayarlar dosyasındaki yeni alanlar bozuk veya birbiriyle çelişkili olduğunda yalnızca ilgili alanlar mı sıfırlanmalı, yoksa tüm pencere durumu mu varsayılanlara dönmeli?
26. Uygulama Windows oturum kapatma, yeniden başlatma veya sistem kapatma sırasında tepsiye gönderilmek yerine doğrudan kapanmalı mı ve ayarların kaydedilmesi için özel bir davranış gerekli mi?
27. Uygulama birden fazla kez başlatılmaya çalışılırsa tek örnek davranışı bu kapsamda ele alınmalı mı; ele alınacaksa ikinci başlatma mevcut pencereyi nasıl görünür yapmalı?
28. Tepsi simgesi için uygulama simgesi yeterli mi, yoksa ağ işlemi sürüyor, hata oluştu veya yapılandırma değişti gibi durumları yansıtan ayrı görseller gerekli mi?
29. Klavye ile erişilebilirlik açısından tepsi menüsü, pencereyi geri getirme ve gerçek çıkış işlemleri için ek kısayol veya ekran okuyucu metni gerekli mi?
30. Tepsi davranışı ve pencere düzeni tercihlerinin sıfırlanması için bir “varsayılanlara dön” seçeneği gerekli mi?
