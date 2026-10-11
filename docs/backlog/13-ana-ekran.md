# 13 — Profil merkezli ana ekran

- **Durum:** bitti
- **Bağımlılık:** 12
- **Gereksinimler:** ADR-018 · Ekran 01, 02, 09

## Amaç
Eski sekme ve taslak temelli arayüzü kaldırarak, P1 kullanıcısının tek tıkla ağ değiştirebileceği, P2–P4 teknisyenlerinin ise hızlı filtreleyip listeleyebileceği profil merkezli ana ekranı oluşturmak.

## Kapsam
- Üst başlık alanı: "Ağ ayarları" ve bağdaştırıcı sekmeleri yerine açılır bağlantı seçici ("Bağlantı: Kablolu (Ethernet) ▾")
- Tam genişlikte durum kartı: "Şu an: [Ağ adı] ağındasınız · [Bağlantı tipi] · [İnternet durumu]" ve "Ayrıntılar ve elle ayar →" bağlantısı
- "Hangi ağa geçmek istiyorsunuz?" başlığı, anlık filtreleme yapan arama kutusu ve parçalı Kart | Liste görünüm seçicisi
- Görünüm eşiği: Kayıtlı ayar sayısı 6'dan fazlaysa varsayılan olarak Liste görünümünün seçilmesi; kullanıcının Kart veya Liste seçiminin `AppSettings` içinde (`PreferredViewMode`) kalıcı saklanması
- Yerleşik ve silinemez ilk öğe olarak "Otomatik" (DHCP) kartı ("Adresi ağ kendisi verir", "Çoğu ağ için önerilir")
- Ayar kartları: Ad, tek satırlık sade açıklama ("Sabit adres · 192.168.0.50"), "Bu ağa geç" butonu, etkin ayarda "Şu an kullanılıyor" rozeti ve "Etkin" durumu, yıldızlı favori butonu ve "⋯" bağlam menüsü (Düzenle, Kopyala, Yeniden adlandır, Dışa aktar, Sil)
- Kablo takılı değilken gri durum kartı ("Kablo takılı değil — Kabloyu takın. Seçtiğiniz ayar kablo takılınca da geçerli olur.") ve bağlı bir kablosuz bağlantı varsa "Kablosuz bağlantıya geç" kısayolu (Ekran 09; durum bilgisi 12'de hazır)
- "+ Yeni ayar ekle" kartı / butonu
- Doğrudan uygulama akışı: "Bu ağa geç" butonunun ayarı doğrudan uygulaması; eski iki adımlı taslak doldurma modelinin ve `ApplyProfileOnSelection` ayarının yürürlükten kalkması
- Geçiş dönemi: 16 bitene kadar mevcut yapılandırma/taslak görünümü "Ayrıntılar ve elle ayar →" bağlantısının arkasında geçici olarak kalır; elle düzenleme hiçbir ara sürümde kaybolmaz
- Ana pencere asgari boyutunun 1100×700'den 900×600'e (varsayılan ~1000×680) düşürülmesi; eski sol profil çekmecesinin ve alt durum çubuğunun kaldırılması

## Bitti sayılır
- [x] Ana ekranda bağlantı seçici, durum kartı ve ayar kartları doğru verilerle render ediliyor
- [x] "Bu ağa geç" tıklandığında ara taslak olmadan doğrudan uygulama tetikleniyor
- [x] 6'dan fazla ayarda otomatik Liste görünümüne geçiliyor; elle yapılan Kart/Liste tercihi kalıcı saklanıyor
- [x] Otomatik kartı her zaman ilk sırada ve kullanılabilir durumda yer alıyor
- [x] Pencere boyutu asgari 900×600 sınırına uyuyor ve duyarlı yerleşim sağlanıyor
- [x] Kablo takılı değilken kartlar tıklanabilir kalıyor ve kablosuz kısayolu çalışıyor
