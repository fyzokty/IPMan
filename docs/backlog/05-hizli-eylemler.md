# 05 — Hızlı eylemler

- **Durum:** tamamlandı
- **Bağımlılık:** 01
- **Gereksinimler:** PR-023, PR-024

## Amaç
Seçili bağdaştırıcı için sık kullanılan tek tıklık eylemler.

## Kapsam
- Ağ bilgisini kopyala (kısa metin özeti)
- Ağ geçidine ping
- DNS önbelleğini temizle
- IP yenile (DHCP bağdaştırıcılarında)
- DHCP (mevcut akış kullanılır)
- Eylemler yalnızca seçili bağdaştırıcıya etki eder; sonuç satır içi gösterilir

## Bitti sayılır
- [x] Her eylem çalışıyor ve sonucu Türkçe gösteriliyor
- [x] Uygun olmayan durumda eylem devre dışı (ör. statik IP'de "IP yenile")

## Sorular
1. "Ağ bilgisini kopyala" eyleminin kısa metin özetinde hangi alanlar (bağdaştırıcı adı, IP, alt ağ maskesi, ağ geçidi, DNS, DHCP durumu vb.) ve hangi biçim yer almalıdır?
    * Hepsi olsun
2. Ağ geçidine ping eyleminde birden fazla varsayılan ağ geçidi varsa hangisi hedeflenmelidir; ağ geçidi yoksa kullanıcıya hangi sonuç gösterilmelidir?
    * Bu teknik konuları bilmiyorum, sen karar ver
3. Ping işlemi için zaman aşımı, paket sayısı ve başarılı/başarısız sonucunu belirleyen ölçütler neler olmalıdır?
    * 15 sn, paket sayısına sen karar ver
4. "DNS önbelleğini temizle" eylemi seçili bağdaştırıcıya özgü uygulanamayan sistem genelinde bir işlem olduğundan, eylem seçili bağdaştırıcı bağlamında nasıl sunulmalıdır?
    * Sen karar ver
5. DNS önbelleği temizleme işlemi yönetici yetkisi veya sistem hatası nedeniyle başarısız olursa kullanıcıya hangi ayrıntı düzeyinde mesaj gösterilmelidir?
    * Sen karar ver
6. "IP yenile" yalnızca DHCP bağdaştırıcılarında etkin denmiş; işlem önce IP bırakma ve ardından yenileme mi yapmalıdır, yoksa yalnızca yenileme mi gerçekleştirmelidir?
    * IP bırakma ardından yenileme
7. Hızlı eylemler çalışırken ilgili düğmeler ve diğer bağdaştırıcı sekmeleri nasıl davranmalıdır; eşzamanlı işlem başlatılması engellenecek midir?
    * Sen karar ver
8. "Ağ bilgisini kopyala" eylemi pano erişimi engellenirse veya panoya yazma başarısız olursa hangi geri bildirim gösterilmelidir?
    * Bildirim gösterilsin, program durmasın
9. Kopyalanan ağ bilgisine MAC adresi, bağlantı durumu, DNS sonekleri veya IPv6 bilgileri de dahil edilecek midir; gizlilik nedeniyle hariç tutulması gereken alanlar var mıdır?
    * IPv6 hariç olsun
10. Kopyalama özeti eylem anındaki bağdaştırıcı durumunu mu kullanmalıdır; bilgi alınırken durum değişirse kullanıcıya nasıl bildirilmelidir?
    * Önemi yok, direkt kopyalasın
11. Ping yalnızca IPv4 ağ geçidine mi gönderilecektir; IPv6 varsayılan ağ geçitleri için ayrı bir davranış veya mesaj gerekli midir?
    * Evet IPv4
12. Ağ geçidi adresi geçersiz, erişilemez ya da seçili bağdaştırıcı bağlantısız durumdaysa ping düğmesinin etkinliği ve sonuç mesajı nasıl olmalıdır?
    * Etkinliği pasif olsun, sonuç mesajı bağdaştırıcı artık aktif değil gibi bir bildirim döndürsün
13. Ping sonucunda DNS çözümlemesi, paket kaybı yüzdesi ve gidiş-dönüş süresi gibi ayrıntılar gösterilecek midir; gösterilecekse hangi biçim ve eşikler kullanılmalıdır?
    * Gösterilsin, eşiklere sen karar ver
14. Ping işlemi sırasında kullanıcının eylemi iptal edebilmesi gerekir mi; gerekirse iptal edilen işlem için hangi durum gösterilmelidir?
    * Gerekmez
15. DNS önbelleği temizleme eylemi tüm sistem bağlantılarını etkileyebileceğinden, çalıştırmadan önce kullanıcı onayı veya etki uyarısı gösterilecek midir?
    * Evet
16. DNS önbelleği temizleme sonrasında başarı nasıl doğrulanacaktır; yalnızca işletim sisteminin komut sonucu mu esas alınacaktır?
    * Bilmiyorum sen karar ver
17. DNS önbelleği temizleme için seçili bağdaştırıcının bağlantı durumu, DHCP durumu veya DNS sunucularına sahip olması düğmenin kullanılabilirliğini etkiler mi?
    * Bilmiyorum sen karar ver
18. IP yenileme başlamadan önce mevcut IP yapılandırmasının ya da kurtarma anlık görüntüsünün alınması gerekir mi; başarısızlıkta geri yükleme denenecek midir?
    * Gerekli evet denenecek
19. IP yenileme, DHCP sunucusundan yanıt alınamaması veya zaman aşımı durumunda mevcut adresi korumaya mı çalışmalıdır; kullanıcıya hangi ağ etkisi açıklanmalıdır?
    * Evet korumalı
20. Bir bağdaştırıcı DHCP etkin olduğu hâlde ortam kablosu çıkarılmışsa veya Wi-Fi bağlantısı kesilmişse "IP yenile" eylemi etkin kalacak mıdır?
    * Hayır
21. "DHCP" hızlı eylemi mevcut akışın hangi adımlarını ve onaylarını kullanacaktır; profil uygulama ya da geri alma davranışından farklılaşacak mıdır?
    * Hayır, hangi bağdaştırıcı ise onu DHCP moduna alacak
22. DHCP eylemi statik yapılandırmayı değiştireceğinden, uygulanmadan önce mevcut statik IP, ağ geçidi ve DNS değerleri kullanıcıya gösterilecek veya ayrıca onay alınacak mıdır?
    * Onay penceresinin altında, bu pencereyi bir daha gösterme tiki olacak
23. Eylemler yalnızca seçili bağdaştırıcıya etki eder denirken DNS önbelleği temizleme istisnası kullanıcı arayüzünde ve sonuç metninde nasıl açıkça belirtilecektir?
    * Bilmiyorum, sen karar ver
24. Seçili bağdaştırıcı eylem sürerken kaldırılır, devre dışı bırakılır veya sekme seçimi değişirse işlem hangi bağdaştırıcı için tamamlanacak ve sonuç nerede gösterilecektir?
    * Bir eylem sürerken, tablar tıklanılamaz olsun. Bağdaştırıcı kaldırılırsa veya devre dışı kalırsa, işlem tamamlanmayacak iptal olacak
25. Hızlı eylemlerin uygunluğu sanal, döngü (loopback), tünel veya etkin olmayan bağdaştırıcılarda nasıl belirlenecektir?
    * Eylem için özel kararlar verilsin. Sen karar ver
26. Sonuç satır içi mesajları yeni bir eylem başlatıldığında mı, bağdaştırıcı seçimi değiştiğinde mi, yoksa belirli bir süre sonunda mı temizlenecektir?
    * Temizle butonu olsun
27. Başarı, uyarı ve hata sonuçları için ortak mesaj şablonları ile teknik hata ayrıntılarının kullanıcıya gösterim düzeyi ne olmalıdır?
    * Kullanıcıya hata kodu gösterilsin
28. Yönetici olarak başlatılmamış uygulama veya işletim sistemi tarafından engellenen bir işlemde, kullanıcının eylemi tekrar deneyebilmesi için hangi yönlendirme gösterilmelidir?
    * Programı, programatik olarak yönetici olarak çalıştırabiliyorsak eğer, yönetici olarak çalıştırılsın mı diye bir popup çıksın.
