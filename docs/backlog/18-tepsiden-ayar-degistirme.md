# 18 — Tepsi menüsünden ayar değiştirme

- **Durum:** bitti
- **Bağımlılık:** 14
- **Gereksinimler:** ADR-018 · Ekran 10

## Amaç
Uygulama penceresini açmaya gerek kalmadan, Windows sistem tepsisi (tray) menüsünden tek tıkla ağ durumu görüntüleme, favori ayarlar arasında geçiş yapma ve son değişikliği geri alma olanağı sunmak.

## Kapsam
- Tepsi menüsü başlığı: "IPMan" ve anlık bağlantı/internet durumu ("● Şu an: [Ağ adı] · [İnternet çalışıyor / Yerel ağ / Kablo takılı değil]")
- "Ayara geç" bölümü:
  - Kayıtlı ayarların listesi (yıldızlı favoriler en üstte, ardından diğerleri)
  - Etkin olan ayarın yanında radyo/onay işareti
  - En fazla 10 ayar listelenmesi; 10'dan fazla varsa "Diğer ayarlar…" öğesinin ana pencereyi açması
- "Son değişikliği geri al" menü öğesi: En son yapılan değişikliği `RecoveryRestoreService` üzerinden anında geri yükleme
- "IPMan'i aç" ve "Çıkış" menü öğeleri
- Tepsi üzerinden tetiklenen ağ geçişlerinin de koşullu onay ve 15 saniyelik güvenlik geri alma sayacı diyaloğunu en üstte (topmost) çalıştırması

## Bitti sayılır
- [x] Tepsi menüsü açıldığında güncel ağ ve internet durumu doğru simge ve metinle görünüyor
- [x] Favori ve diğer ayarlar doğru sırada listeleniyor, etkin ayar işaretleniyor
- [x] Menüden bir ayara tıklandığında koşullu onay ve 15 saniyelik güvenlik geri alma sayacı çalışıyor
- [x] "Son değişikliği geri al" tıklandığında önceki yapılandırmaya dönülüyor
- [x] "IPMan'i aç" pencereyi öne getiriyor; "Çıkış" uygulamayı kapatıyor
