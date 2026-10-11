# 15 — Yeni ve düzenle ayar sayfası

- **Durum:** bitti
- **Bağımlılık:** 13
- **Gereksinimler:** ADR-018 · Ekran 05, 07

## Amaç
Teknik terimler ve karmaşık ağ parametreleri içinde kaybolmadan, kullanıcıların güvenle yeni bir ağ ayarı oluşturabilmesini veya mevcut bir ayarı düzenleyebilmesini sağlamak.

## Kapsam
- "← Geri" butonlu tam sayfa düzenleme görünümü
- Ayarın adı alanı ve açıklama ipucu
- "Adres nasıl belirlensin?" radyo kartları:
  - "Otomatik" ("Ağ adresi kendisi verir (önerilir)")
  - "Sabit adres" ("Bir cihaza doğrudan bağlanmak için")
- Sabit adres seçildiğinde:
  - İsteğe bağlı "Bağlanacağınız cihazın adresi (biliyorsanız)" giriş kutusu ("Cihazın etiketinde veya kılavuzunda yazar")
  - Adres önerisi: Cihaz adresi girildiğinde aynı `/24` alt ağında (`.0`, `.255`, cihaz adresi ve geçerli ağ geçidi hariç) aday adreslerin (en fazla 5 sorgu, örn. `.50`, `.51`...) mevcut `IIpv4ConflictProbe` (`PingIpv4ConflictProbe`) ile kontrol edilmesi
  - Yalnızca tarama gerçekten çalışıp yanıt alamadıysa "Boş olduğu kontrol edildi" rozeti gösterilmesi; aksi takdirde dürüstçe "Kontrol edilemedi, yine de kullanabilirsiniz" uyarısının yer alması; "Bunu kullan" butonu ile "Bilgisayarın adresi" alanının doldurulması
  - "Bilgisayarın adresi" alanı
  - Otomatik alt ağ maskesi tamamlama: Geçerli bir adres girildiğinde boş olan maskenin `255.255.255.0` olarak doldurulması ("otomatik dolduruldu" etiketi ile Gelişmiş altında saklanması)
  - İsteğe bağlı "Ağ geçidi (genelde modem adresi)" alanı ("Bilmiyorsanız boş bırakın")
- Katlanabilir "Gelişmiş" bölümü: Alt ağ maskesi ve DNS sunucuları (Birinci DNS, İkinci DNS)
- Doğrulama ve hata dili (Ekran 07): Hata mesajlarının örnek içermesi ("Her sayı 0 ile 255 arasında olmalı. Örnek: 192.168.1.50"); kırmızı hataların kaydetmeyi engellemesi; amber (sarı) ağ geçidi uyarılarının ise kaydetmeyi engellememesi
- İşlem butonları: "Sadece kaydet" ve "Kaydet ve geç"

## Bitti sayılır
- [x] Yeni/düzenle formu açılıyor, Otomatik ve Sabit radyo kartları arasında sorunsuz geçiş yapılıyor
- [x] Cihaz IP'si yazıldığında aynı alt ağdan boş IP taranıp öneriliyor; sorgu sonucuna göre dürüst onay metni gösteriliyor
- [x] Boş alt ağ maskesi otomatik olarak `255.255.255.0` değerini alıyor
- [x] Doğrulama hataları kırmızı ve örnekli gösterilip kaydetmeyi engelliyor; uyarılar amber renkle kaydedilebilir kalıyor
- [x] "Sadece kaydet" ayarı listeye ekliyor; "Kaydet ve geç" ayarı kaydedip doğrudan uygulama ve sayaç akışını tetikliyor

## Tamamlanan bağlama
- Ana pencere `Home / Details / ProfileEditor` durumlarıyla tam sayfa formu açar; 16 mevcut `Details` görünümünü değiştirebilir.
- Yeni/düzenle, sadece kaydet, güvenli geçiş üzerinden kaydet ve geç (sabit/DHCP), meşgul bağdaştırıcı kilidi ve kaydedilmemiş değişiklik onayı bağlandı.
- Adres önerisi gerçek servis/probe ile kuruldu; form açılırken mevcut ağ geçidi dışlanır.
- Sahtelerle gezinme ve kayıt/geçiş testleri; ana pencere içinde açık/koyu form render testleri eklendi.
- Eski ayrıntılar sayfasındaki taslağı ayar olarak kaydetme 16'ya kadar korunur. Gerçek ağ doğrulaması 11 kapsamındadır.
