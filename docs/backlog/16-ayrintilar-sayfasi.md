# 16 — Ayrıntılar ve elle ayar sayfası

- **Durum:** bitti
- **Bağımlılık:** 13, 14, 15
- **Gereksinimler:** ADR-018 · Ekran 06, 08

## Amaç
Eski "Mevcut yapılandırma / Yapılandırma taslağı" ikili kart düzenini kaldırarak, aktif bağdaştırıcının anlık durumunu, yapılan değişikliklerin farkını (diff) ve ağ tanılama araçlarını tek bir akıcı sayfada toplamak.

## Kapsam
- Durum kartındaki "Ayrıntılar ve elle ayar →" bağlantısından açılan tam sayfa görünümü ve "← Geri" butonu
- Üstte bağlantı adı ("Kablolu bağlantı (Ethernet)") ve durum rozeti ("Bağlı · İnternet çalışıyor")
- Parçalı adres modu seçici: [Otomatik] [Sabit]
- Temel üç alan: Bilgisayarın adresi, Alt ağ maskesi, Ağ geçidi (modem adresi)
- Değişiklik vurgusu: Değiştirilen alanların soluna amber kenarlık (`#D87C01`), yanına "değişecek" rozeti ve mevcut değerin ("şu an: 192.168.10.25") yazılması
- Katlanabilir "Gelişmiş ayarlar ve bağdaştırıcı bilgisi" bölümü:
  - DNS (alan adı sunucuları): Birinci DNS, İkinci DNS
  - Bağlantı bilgileri (salt okunur): Donanım adı, MAC adresi, Bağlantı hızı
  - Sade dilli araç butonları: "Bilgileri kopyala", "Modeme ulaşılıyor mu?", "DNS önbelleğini temizle", "Adresi yenile", "Son değişikliği geri al"
- Yapışkan alt işlem çubuğu (Sticky footer):
  - Sol taraf: Değişiklik sayısı ("1 değişiklik"), "Ayar olarak kaydet" bağlantısı
  - Sağ taraf: "Vazgeç" ve "Uygula" butonları
- Başarısızlık durumunda (Ekran 08): Sayfa üstünde kırmızı inline hata bandı gösterilmesi ("Saha PLC ayarına geçilemedi. Windows yeni adresi kabul etmedi. Ağınız değişmedi, Ofis ayarı ile devam ediyorsunuz.") ile "Tekrar dene" ve "Ayrıntılar" seçenekleri

## Bitti sayılır
- [x] Ayrıntılar sayfası açılıyor ve mevcut yapılandırmayı doğru yüklüyor
- [x] Değiştirilen alanlar amber renkle "değişecek" ve eski değer bilgisiyle vurgulanıyor
- [x] Gelişmiş araçlar (kopyala, modem pingi, DNS temizleme, IP yenileme) sorunsuz çalışıyor
- [x] Yapışkan alt çubuk değişiklik sayısını dinamik gösteriyor; "Ayar olarak kaydet" yeni profil açıyor; "Uygula" onay ve 15 sn sayacını tetikliyor
- [x] Başarısız uygulama durumunda kullanıcıyı rahatlatan kırmızı hata bandı doğru metinle beliriyor
