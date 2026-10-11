# 17 — Sade metinler ve terim sözlüğü uyumu

- **Durum:** bitti
- **Bağımlılık:** 13, 14, 15, 16
- **Gereksinimler:** ADR-018 · UX-ilkeleri.md

## Amaç
Uygulama genelindeki tüm kullanıcı arayüzü metinlerini, hata mesajlarını ve bildirimleri `docs/03_UI_UX/UX-ilkeleri.md` belgesindeki terim sözlüğü ve dil prensipleriyle tam uyumlu hale getirmek; teknik jargonu temizlemek.

## Kapsam
- `src/IPMan.App/Resources/Strings.resx` dosyasındaki tüm anahtarların gözden geçirilmesi ve güncellenmesi
- Terim dönüşümleri:
  - "IPv4 adresi" → "Bilgisayarın adresi"
  - "Varsayılan ağ geçidi" → "Ağ geçidi (genelde modem adresi)"
  - "DHCP" → "Otomatik" ("Adresi ağ kendisi verir")
  - "Statik" → "Sabit adres" ("Bir cihaza doğrudan bağlanmak için")
  - "Profil" → "Ayar"
  - "Taslak" → Bütünüyle kaldırıldı
  - "Bağdaştırıcı" → "Bağlantı"
  - "Ping" → "Ulaşılıyor mu?" ("Modeme ulaşılıyor mu?")
  - "DNS Flush" → "DNS önbelleğini temizle"
  - "IP Renew" → "Adresi yenile"
- Hata ve uyarı mesajlarının formüle uyarlanması ([Ne oldu?] + [Ağ değişti mi?] + [Ne yapmalı?])
- Suçlayıcı ifadelerin ("Hatalı girdiniz" vb.) kaldırılması, yerine örnek içeren yol gösterici metinlerin yerleştirilmesi ("Her sayı 0 ile 255 arasında olmalı. Örnek: 192.168.1.50")
- Uygulama içinde hiçbir İngilizce metin kırıntısının kalmaması; tek dilin Türkçe olması

## Bitti sayılır
- [x] `Strings.resx` dosyasındaki tüm terimler `UX-ilkeleri.md` sözlüğüne eksiksiz uyarlandı
- [x] Taslak ve eski teknik ifadelere ait kullanılmayan kaynak dizgileri temizlendi
- [x] Tüm giriş doğrulama mesajları somut örnekler içeriyor
- [x] Arayüzde hiçbir İngilizce veya jargona dayalı metin kalmadı
