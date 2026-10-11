# IPMan UX İlkeleri ve Kullanıcı Deneyimi Rehberi

Bu belge, IPMan 1.0 "karma" arayüz yenilemesinin (ADR-018) temel tasarım felsefesini, kullanıcı deneyimi ilkelerini, görev akışlarını, terim sözlüğünü ve hata/bildirim yazım kurallarını tanımlar.

---

## 1. Hedef ve Hedef Kullanıcı

IPMan'in birincil hedef kullanıcısı **P1 (Genel Windows Kullanıcısı)** profilidir:
- Bilgisayar ve ağ konularında derin teknik bilgisi olmayan, sahada bir PLC'ye, güvenlik kamerasına veya cihaz yönetim portuna bağlanması gereken veya ofis/ev ağına geri dönmek isteyen çalışanlar.
- Bu kullanıcılar için varsayılan akış yardım almadan, tereddüt etmeden ve sıfır jargonla tek tıkla çalışmalıdır.

İkincil kullanıcılar **P2–P4 (Teknisyenler ve İleri Düzey Kullanıcılar)** profilidir:
- Günde defalarca ağ değiştiren, birden fazla alt ağda çalışan uzmanlar.
- Teknisyenler hızlarını sadeleştirilmiş arayüzden kaybetmez; ihtiyaç duydukları hız **liste görünümü**, **anlık arama**, **yıldızlı favoriler**, **sistem tepsisi (tray) menüsü** ve katlanan **Gelişmiş** bölümleriyle eksiksiz sağlanır.
- Teknik ayrıntılar arayüzden asla silinmez; yalnızca P1'i boğmamak için varsayılan olarak katlanır.

---

## 2. Temel UX İlkeleri

1. **Tek ana eylem (Doğrudan uygulama):**
   Kullanıcı her ekranda ana eylemi tereddütsüz görür. Eski iki adımlı "profili seç → taslağı doldur → uygula" akışı kaldırılmıştır. Karttaki veya satırdaki **"Bu ağa geç"** butonu ayarı doğrudan uygular.

2. **Sade ve anlaşılır dil:**
   Arayüzde ağ mühendisliği jargonu yerine günlük Türkçe kullanılır. Terimler kullanıcının zihnindeki amaca göre adlandırılır (örneğin *IP adresi* yerine *Bilgisayarın adresi*, *DHCP* yerine *Otomatik*).

3. **Teknik ayrıntı silinmez, katlanır:**
   Alt ağ maskesi, DNS sunucuları, donanım adı, MAC adresi ve bağlantı hızı gibi teknik veriler arayüzden atılmaz. Bunlar **"Gelişmiş"** başlığı altında düzgünce katlanır; merak eden veya ihtiyaç duyan uzman tek tıkla açar.

4. **Her değişiklik güvenle geri alınabilir (Güvenlik ağı):**
   Arayüz üzerinden yapılan her başarılı ağ değişikliğinden sonra (profil geçişi, elle ayar veya Otomatik'e dönüş) ekranda en üstte 15 saniyelik bir geri sayım sayacı açılır. Kullanıcı yanıt vermezse veya "Hayır, geri al" derse sistem otomatik olarak eski çalışan yapılandırmaya döner. Kullanıcı hiçbir zaman "bağlantım koptu ve geri dönemiyorum" endişesi yaşamaz.

5. **Ağ değişmediğinde bunu açıkça söyle (Güven verici hata dili):**
   Bir ayar uygulanamadığında veya Windows yeni adresi reddettiğinde kullanıcı paniğe sevk edilmez. İlk cümle daima durum güvencesi vermelidir: *"Ağınız değişmedi, Ofis ayarı ile devam ediyorsunuz."*

6. **Örnekli ve yol gösterici hata mesajları:**
   Hata bildirimleri kuru ve cezalandırıcı ("Geçersiz IP adresi") olamaz. Hatayı açıklar ve geçerli bir örnek sunar: *"Her sayı 0 ile 255 arasında olmalı. Örnek: 192.168.1.50"*. Kırmızı mesajlar kaydetmeyi engeller; amber (sarı) uyarılar bilgi verir ama işlemi engellemez.

7. **Renk tek başına anlam taşımaz:**
   Erişilebilirlik ve netlik gereği renkler (yeşil, amber, kırmızı, gri) mutlaka açıklayıcı metinler ve simgelerle eşleştirilir (*"Etkin"*, *"Şu an kullanılıyor"*, *"değişecek"*, *"Kablo takılı değil"*).

8. **Akıllı ve güvenli varsayılanlar:**
   Sanal bağdaştırıcılar varsayılan olarak gizlenir. Sabit adres seçildiğinde `/24` alt ağ maskesi (`255.255.255.0`) otomatik doldurulur. Bağlanılacak cihaz yazıldığında boş bir bilgisayar IP adresi otomatik önerilir.

---

## 3. Ana Görevler ve Akışlar

| Görev | Akış Özeti | İlgili Ekranlar |
|---|---|---|
| **Ayara geçiş** | Ana ekranda istenen ayar bulunur (arama/kart/liste) → "Bu ağa geç" tıklanır → Eğer statik ayar interneti kesecekse koşullu onay çıkar → Ayar uygulanır → 15 sn geri alma sayacı çalışır → "Evet, böyle kalsın" ile onaylanır; yanıt verilmezse 15 sn sonra eski ayara dönülür. | `01`, `02`, `03`, `04` |
| **Normale / İnternete dön (Otomatik)** | Ana ekrandaki veya tepsi menüsündeki yerleşik "Otomatik" seçeneğinde "Bu ağa geç" tıklanır → DHCP uygulanır → 15 sn sayacında "Evet, böyle kalsın" ile onaylanır. İnternet geri gelir. | `01`, `02`, `04`, `10` |
| **Yeni bir cihaza bağlanma (Yeni ayar)** | "+ Yeni ayar ekle" tıklanır → İsim verilir (örn. "Saha PLC") → "Sabit adres" seçilir → Cihazın adresi yazılır → Önerilen adres "Bunu kullan" ile seçilir → "Kaydet ve geç" tıklanır. | `05`, `07` |
| **Elle ince ayar ve bağlantı ayrıntıları** | Durum kartındaki "Ayrıntılar ve elle ayar →" linkine tıklanır → Değerler değiştirilir (değişen alanlar amber renkle vurgulanır) → Gelişmiş açılıp DNS ve araçlar kontrol edilir → "Uygula" ile kaydedilir. | `06` |
| **Hatalı geçiş / Başarısızlık** | Ayar uygulanırken Windows hata döndürür → Kırmızı durum bandı çıkar: "Ağınız değişmedi, Ofis ayarı ile devam ediyorsunuz." → "Tekrar dene" veya "Ayrıntılar" seçilebilir. | `08` |
| **Kablo takılı değil durumu** | Kablo çıktığında durum kartı griye döner: "Kablo takılı değil — Kabloyu takın. Seçtiğiniz ayar kablo takılınca da geçerli olur." Varsa "Kablosuz bağlantıya geç" butonu sunulur. Kartlar kullanılabilir kalır. | `09` |
| **Sistem tepsisinden hızlı geçiş** | Sağ alttaki simgeye sağ tıklanır → Mevcut durum ve yıldızlı favori ayarlar listelenir → Ayar tıklanır → Koşullu onay ve 15 sn güvenlik sayacı çalışır. | `10` |

---

## 4. Terim Sözlüğü

Arayüzde teknik terimler yerine kullanıcının zihnindeki modeli yansıtan terimler kullanılır. Geliştiriciler ve metin yazarları bu tabloya uymak zorundadır:

| Teknik Terim | Arayüzdeki Karşılığı | Not ve Kullanım Kuralı |
|---|---|---|
| **IP adresi** | **Bilgisayarın adresi** | Cihaz adresi sorulduğunda: *"Bağlanacağınız cihazın adresi (biliyorsanız)"*. |
| **Alt ağ maskesi** | **Alt ağ maskesi** | Yalnızca *Gelişmiş* bölümünde gösterilir. Boş bırakıldığında `255.255.255.0` olarak otomatik doldurulur ve *"otomatik dolduruldu"* etiketi alır. |
| **Varsayılan ağ geçidi** | **Ağ geçidi (genelde modem adresi)** | Açıklama: *"Bilmiyorsanız boş bırakın"*. Uyarı durumunda kırmızı değil amber renk kullanılır. |
| **DHCP** | **Otomatik** | Kart açıklaması: *"Adresi ağ kendisi verir"*, rozet: *"Çoğu ağ için önerilir"*. |
| **Statik IP** | **Sabit adres** | Radyo kartı açıklaması: *"Bir cihaza doğrudan bağlanmak için"*. |
| **DNS** | **DNS (alan adı sunucuları)** | Yalnızca *Gelişmiş* bölümünde yer alır. Birinci DNS ve İkinci DNS olarak etiketlenir. |
| **Bağdaştırıcı (Adapter)** | **Bağlantı** | Başlıkta: *"Bağlantı: Kablolu (Ethernet) ▾"* veya *"Kablolu bağlantı (Ethernet)"* / *"Kablosuz bağlantı (Wi-Fi)"*. |
| **Profil** | **Ayar** | Menülerde ve butonlarda: *"Yeni ayar ekle"*, *"Ayar olarak kaydet"*, *"Ofis ayarına geçildi"*. |
| **Taslak (Draft)** | *(Kaldırıldı)* | Arayüzden bütünüyle çıkarılmıştır. Değişiklikler elle ayar sayfasında doğrudan alan üzerinde amber vurguyla gösterilir. |
| **Uygula** | **Bu ağa geç** / **Uygula** | Kart ve liste görünümünde: *"Bu ağa geç"*. Elle ayar sayfasındaki alt çubukta: *"Uygula"*. |
| **Geri yükle (Restore)** | **Geri al** / **Eski ayara dön** | Sayaç iletişim kutusunda: *"Hayır, geri al"*. Menüde: *"Son değişikliği geri al"*. |
| **Ping** | **Ulaşılıyor mu?** | Hızlı araç butonu: *"Modeme ulaşılıyor mu?"*. |
| **DNS Flush** | **DNS önbelleğini temizle** | Hızlı araç butonu. |
| **IP Renew** | **Adresi yenile** | Hızlı araç butonu. |
| **MAC adresi** | **MAC adresi** | Salt okunur bilgi olarak yalnızca *Gelişmiş* bölümünde yer alır. |
| **vEthernet / Virtual Adapter** | *(Gizli)* | Sanal bağdaştırıcılar varsayılan olarak gizlenir (`ShowVirtualAdapters = false`). Ayarlardan açılabilir. |

---

## 5. Hata ve Bildirim Yazım Kuralları

Kullanıcı bir hata yaptığında ya da sistem seviyesinde bir aksaklık yaşandığında kullanılan dil şu prensiplere dayanır:

### 5.1. Üç Adımlı Mesaj Formülü
Her hata ve sonuç bildirimi üç parçadan oluşmalıdır:
1. **Ne oldu?** (Kısa, net durum tespiti)
2. **Ağ değişti mi?** (Kullanıcıya güven veren mevcut durum bilgisi)
3. **Ne yapmalı?** (Açık ve eyleme dönük sonraki adım)

*Örnek:*
> **Saha PLC ayarına geçilemedi.** Windows yeni adresi kabul etmedi. Ağınız değişmedi, Ofis ayarı ile devam ediyorsunuz.
> `[Tekrar dene]` `[Ayrıntılar]`

### 5.2. Asla Kullanıcıyı Suçlama
- ❌ *"Hatalı IP adresi girdiniz."* / *"Yanlış işlem yaptınız."*
- ✔️ *"Her sayı 0 ile 255 arasında olmalı. Örnek: 192.168.1.50"*
- ❌ *"Ağ geçidi aralık dışında, düzeltin."*
- ✔️ *"Ağ geçidi bilgisayarın adresiyle aynı ağda görünmüyor. İnternet çalışmayabilir."* (Amber uyarı)

### 5.3. Hata (Kırmızı) ve Uyarı (Amber) Ayrımı
- **Kırmızı (Hata):** Kaydetmeyi veya uygulamayı mutlak olarak engeller. Giriş kutusunun alt kenarı ve metni kırmızı olur. Yalnızca sözdizimi ve kesin geçersizliklerde kullanılır (örn. `256.0.0.1` veya harf girilmesi).
- **Amber / Sarı (Uyarı):** Kaydetmeyi engellemez, yalnızca kullanıcıyı bilgilendirir. Ağ geçidinin farklı alt ağda olması veya internetin kesilebileceği durumlar amber renkle gösterilir.

### 5.4. Somut Örnek Verme Zorunluluğu
Geçersiz giriş bildiren her hata mesajı mutlaka geçerli bir örnek içermelidir:
- *"Adres dört sayıdan oluşmalı ve noktayla ayrılmalıdır. Örnek: 192.168.1.50"*
- *"Ayar adı boş bırakılamaz. Örnek: Saha PLC, Ofis, Ev"*
