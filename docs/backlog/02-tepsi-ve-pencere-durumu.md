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
    * İlk kapatmada kullanıcıya sorulsun, eğer hayır işaretlenirse direkt kapansın.
2. Tepsi simgesi uygulama açıkken her zaman mı görünmeli, yoksa yalnızca pencere tepsiye küçültüldüğünde mi gösterilmeli?
    * Pencere tepsiye küçültüldüğünde
3. Kaydedilen pencere konumunun ekran dışında sayılması ve güvenli konuma alınması için çoklu monitör, çözünürlük ve DPI değişimlerinde hangi davranış bekleniyor?
    * En son hangi monitördeyse, o monitörde açılmaya çalışılsın, eğer o monitör artık yoksa, ana monitörde açılsın.
4. Kapatma ve küçültme davranışları ayrı ayrı mı yapılandırılmalı, yoksa tek bir tercih her iki işlemi de mi belirlemeli?
    * Tek tercih olsun
5. Kullanıcı ilk tercihini yaptıktan sonra bu davranışı uygulama içinden değiştirebileceği bir ayar bulunmalı mı; bulunacaksa hangi ekranda yer almalı?
    * Evet kullanıcı ayarlar penceresinde de olmalı. Eğer ayarlar penceresi şu an yoksa bunu o zamana bırakalım.
6. Kapatma düğmesi tepsiye gönderme davranışına ayarlıysa, uygulamadan gerçekten çıkmak için tepsi menüsündeki “Çıkış” dışında bir yol sunulmalı mı?
    * Evet "Çıkış" seçeneği olmalı
7. Tepsi menüsündeki “Aç” komutu pencere zaten görünür ve etkin durumdaysa pencereyi öne getirip etkinleştirmeli mi, yoksa başka bir davranış mı bekleniyor?
    * Yok sadece öne getirip odak o pencerede olsun
8. Tepsi simgesine tek tıklama, çift tıklama ve sağ tıklama için hangi etkileşimler bekleniyor?
    * Çift ve tek tıklama "Aç" komutu ile aynı davranış, sağ tıklama bağlam menüsü
9. Tepsi simgesinin bağlam menüsünde yalnızca “Aç” ve “Çıkış” mı bulunmalı, yoksa seçili bağdaştırıcı ya da profil işlemleri gibi ek kısayollar da gerekli mi?
    * Aç ve Çıkış, uygulama adı ve versiyonu olsun. Diğer özellikleri sonra düşünürüz
10. Uygulama tepsiye küçültüldüğünde kullanıcının durumu anlaması için bildirim balonu, ilk kullanım ipucu veya başka bir geri bildirim gösterilmeli mi?
    * Küçük bir bildirim görüntülenebilir.
11. Windows bildirimlerinin kapalı olduğu ya da tepsi simgesinin taşma alanında bulunduğu durumlarda kullanıcıya gösterilecek geri bildirim nasıl ele alınmalı?
    * Bunu sen seç, işi karmaşıklaştırmadan
12. Uygulama tepsiye küçültülmüşken ağ yapılandırması uygulama, doğrulama veya geri alma işlemi sürüyorsa pencerenin otomatik açılması ya da kullanıcıya bildirim gönderilmesi gerekir mi?
    * Pencere hata durumunda ekrana gelsin, başarılıysa bildirim göndermeye çalışsın
13. Çıkış komutu devam eden ağ işlemi, bekleyen doğrulama veya kaydedilmemiş ayar varken nasıl davranmalı; onay istenmeli mi?
    * Onay istesin
14. Pencere kapatılırken veya tepsiye gönderilirken ayarlar her seferinde mi kaydedilmeli, yoksa yalnızca boyut, konum veya seçili bağdaştırıcı değiştiğinde mi yazılmalı?
    * Hangisi daha az işlem maliyetliyse öyle yapalım
15. Pencerenin normal, büyütülmüş ve küçültülmüş durumlarından hangisi kalıcı olarak saklanmalı; uygulama yeniden açıldığında büyütülmüş durum geri yüklenmeli mi?
    * En son hangi haldeyse öyle geri yüklenmeli
16. Kaydedilen boyut en küçük pencere boyutunun altında, sıfır ya da geçersizse hangi varsayılan boyut ve konum kullanılmalı?
    * Uygulamanın ilk açılış pencere boyutu kullanılmalı.
17. Pencerenin yalnızca başlık çubuğu görünür kaldığı ya da kısmen ekran dışında olduğu konumlar güvenli kabul edilmeli mi; görünürlük için asgari alan ne olmalı?
    * Pencere boyutunun minimum sınırları olsun mesela 100*80 gibi, ekran dışında herhangi bir bölgesi kalıyorsa güvenli sayılmasın ve kaydetmesin
18. Kaydedilen pencere konumu artık bağlı olmayan bir monitöre aitse, pencere birincil ekrana mı yoksa imlecin bulunduğu ekrana mı taşınmalı?
    * Birincil ekrana
19. DPI ölçeği değiştiğinde kaydedilen boyut fiziksel piksel, WPF aygıttan bağımsız birim veya göreli ekran oranı olarak mı yorumlanmalı?
    * Bu konuyu da bilmiyorum, daha az hata çıkarabilecek, hafif bir entegrasyon olsun
20. Pencere boyutu ve konumu farklı kullanıcı oturumları ya da Windows kullanıcıları arasında paylaşılmalı mı, yoksa mevcut kullanıcıya özgü mü kalmalı?
    * Mevcut kullanıcı
21. Son seçili bağdaştırıcı artık yoksa, devre dışıysa veya adı değişmişse başlangıçta hangi bağdaştırıcı seçilmeli ve kullanıcıya bilgi verilmeli mi?
    * O bağdaştırıcı gri renkte, değiştirilemez ve silinebilir olmalı
22. Son seçili bağdaştırıcıyı eşleştirmek için görünen ad yeterli mi, yoksa GUID gibi daha kararlı bir kimlik saklanmalı mı?
    * GUID ile eşleştirilmeli
23. Başlangıçta hiç etkin bağdaştırıcı yoksa pencere ve tepsi menüsü hangi durumda açılmalı?
    * Pencere
24. Ayarlar dosyasında yeni pencere veya tepsi alanları bulunmadığında hangi geriye dönük varsayılanlar kullanılmalı?
    * Sıfırdan açılıyormuş gibi default ayarlar olmalı
25. Ayarlar dosyasındaki yeni alanlar bozuk veya birbiriyle çelişkili olduğunda yalnızca ilgili alanlar mı sıfırlanmalı, yoksa tüm pencere durumu mu varsayılanlara dönmeli?
    * Yalnızca çelişkili alanlar
26. Uygulama Windows oturum kapatma, yeniden başlatma veya sistem kapatma sırasında tepsiye gönderilmek yerine doğrudan kapanmalı mı ve ayarların kaydedilmesi için özel bir davranış gerekli mi?
    * Kaydedip kapatmalı
27. Uygulama birden fazla kez başlatılmaya çalışılırsa tek örnek davranışı bu kapsamda ele alınmalı mı; ele alınacaksa ikinci başlatma mevcut pencereyi nasıl görünür yapmalı?
    * tek örnek davranışı olmalı, sistem tepsisinden açma işlemi ile aynı olmalı.
28. Tepsi simgesi için uygulama simgesi yeterli mi, yoksa ağ işlemi sürüyor, hata oluştu veya yapılandırma değişti gibi durumları yansıtan ayrı görseller gerekli mi?
    * Uygulama simgesi yeterli
29. Klavye ile erişilebilirlik açısından tepsi menüsü, pencereyi geri getirme ve gerçek çıkış işlemleri için ek kısayol veya ekran okuyucu metni gerekli mi?
    * Klavye ile erişim konusu çok önemli değil sadece basic şeyler çalışsa yeterli
30. Tepsi davranışı ve pencere düzeni tercihlerinin sıfırlanması için bir “varsayılanlara dön” seçeneği gerekli mi?
    * Evet gerekli
