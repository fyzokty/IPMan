# 04 — Tema desteği

- **Durum:** yapılacak
- **Bağımlılık:** 03
- **Gereksinimler:** PR-018

## Amaç
Windows'u takip et / Açık / Koyu tema seçenekleri.

## Kapsam
- Renklerin kaynak sözlüklerine taşınması
- Açık ve koyu tema kaynakları
- "Windows'u takip et" için sistem temasının okunması ve değişince güncellenmesi
- Ayar değişince yeniden başlatmadan uygulanması

## Bitti sayılır
- [ ] Üç seçenek de doğru görünüyor, tüm denetimler okunabilir
- [ ] Windows teması değişince uygulama uyum sağlıyor

## Sorular

1. Tema tercihi kalıcı olarak hangi ayarda saklanmalı ve uygulama ilk açıldığında varsayılan seçenek "Windows'u takip et" mi olmalı?
    * Tema kalıcı ve default olarak sistemi takip et
2. Açık/koyu tema yalnızca uygulama içi renkleri mi kapsamalı, yoksa başlık çubuğu ve Windows denetimlerinin görünümü de temaya uyarlanmalı mı?
    * Hepsi uyarlanmalı
3. "Windows'u takip et" seçeneğinde sistem temasındaki değişiklik hangi mekanizmayla algılanmalı ve güncellemenin gerçekleşmesi için kabul edilebilir gecikme nedir?
    * Bilmiyorum bunu sen bul
4. Sistem teması belirlenemediğinde, desteklenmeyen bir Windows sürümünde çalışıldığında veya tema okuması hata verdiğinde hangi tema ve kullanıcıya gösterilecek hangi durum mesajı kullanılmalı?
    * Bunu da sen ayarla, açık veya koyu fark etmez
5. Kullanıcının seçtiği Açık veya Koyu tema, Windows teması değişse bile mutlak olarak korunmalı mı; "Windows'u takip et" seçeneğine dönüldüğünde değişiklik anında mı uygulanmalı?
    * Mutlak olarak korunmalı, değişiklik anında uygulanmalı
6. Tema tercihi bozuk, bilinmeyen veya önceki sürümden farklı bir değer olarak yüklendiğinde nasıl iyileştirilmeli ve ayar dosyasındaki değer otomatik düzeltilmeli mi?
    * otomatik düzeltilmeli
7. Temaya taşınacak renklerin kapsamına pencere zemini, kartlar, sekmeler, düğmeler, metin kutuları, açılır listeler, onay kutuları, kaydırma çubukları, araç ipuçları ve doğrulama/hata durumları da dahil mi?
    * dahil
8. Durum, başarı, uyarı ve hata renkleri temaya göre nasıl değişmeli; bu renklerin anlamı iki temada da tutarlı ve erişilebilir olacak mı?
    * Tutarlı olmalı sen seç
9. Açık ve koyu tema için metin, simge, kenarlık ve odak göstergesi kontrastı hangi erişilebilirlik standardına ve hangi minimum oranlara göre doğrulanmalı?
    * Bilmiyorum hiç önemli değil, kafana göre yap
10. Klavye odağı, seçili, devre dışı ve üzerine gelinmiş denetim durumları her tema için ayrı kaynaklarda açıkça tanımlanmalı mı?
    * Hayır
11. Uygulamadaki mevcut özel stiller, üçüncü taraf veya işletim sistemi denetimleri ve olası dinamik olarak oluşturulan görünümler tema değişikliğine nasıl dahil edilecek?
    * Bilmiyorum, çok da önemi yok
12. Tema değişimi açık pencere ve sekmelerdeki tüm denetimlere anında uygulanırken odak, seçili öğe, girilmiş form verisi ve devam eden işlem durumu korunmalı mı?
    * korunmalı
13. Yeniden başlatmadan tema değişiminde kaynak sözlüklerinin yüklenme sırası, eski sözlüklerin kaldırılması ve olası görsel titreşimin önlenmesi için hangi yaklaşım kullanılmalı?
    * Bilmiyorum, sen belirle
14. Uygulama başlatılırken tema ne zaman uygulanmalı; ilk pencere çizilmeden önce uygulanması zorunlu mu ve yanlış temanın kısa süre görünmesi kabul edilebilir mi?
    * yanlış temanın kısa süre görünmesi kabul edilemez
15. Windows yüksek karşıtlık modu etkin olduğunda uygulamanın kendi Açık/Koyu kaynakları mı, yoksa işletim sisteminin yüksek karşıtlık renkleri mi öncelik almalı?
    * İşletim sistemi renkeleri öncelikli
16. Sistem tema izleme yalnızca renk modunu mu takip etmeli, yoksa yüksek karşıtlık, vurgu rengi, saydamlık efektleri ve metin ölçeklendirmesi gibi ilgili görünüm tercihlerini de kapsamalı mı?
    * Hepsini takip etsin
17. Başlık çubuğu tema kapsamındaysa pencere etkin/etkin değilken kullanılan renkler, başlık metni ve sistem düğmelerinin görünürlüğü nasıl sağlanmalı?
    * Normal bir uygulamada nasılsa öyle yapılmalı. Genel geçer kuralları uygulayalım
18. Temayı etkileyen tüm renk, fırça ve stil kaynakları hangi sözlük dosyalarında ve hangi adlandırma düzeniyle tutulmalı; ortak ve temaya özgü kaynaklar nasıl ayrılmalı?
    * Bu konuya da sen karar ver
19. Kullanıcı tema tercihini değiştirirken ayarın diske yazılması başarısız olursa görünüm geçici olarak değişmiş kalmalı mı, eski tercihe geri mi dönülmeli ve hata nasıl bildirilmeli?
    * Kullanıcıya hata bildirilmeli, başka bir işleme gerek yok
20. Kullanıcı farklı uygulama örnekleri açabildiğinde tema tercihleri ve Windows tema değişiklikleri örnekler arasında nasıl tutarlı tutulmalı?
    * Kullanıcı farklı uygulama örneği açamaz
21. Koyu ve açık temaların hangi Windows 10/11 sürümleri, ekran ölçeklendirme oranları ve desteklenen erişilebilirlik ayarlarında görsel olarak kabul edilmesi gerekiyor?
    * Bilmiyorum, sen karar ver
