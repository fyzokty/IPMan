# 03 — Ayarlar penceresi

- **Durum:** yapılacak
- **Bağımlılık:** 02
- **Gereksinimler:** PR-018, PR-010 · AC-010

## Amaç
Dişli simgesinden açılan küçük bir ayarlar penceresi.

## Kapsam
- Tema seçimi (işlevi 04'te)
- Tepsi davranışı ve kapatma düğmesi davranışı
- Bildirimler açık/kapalı
- "Profil seçilince hemen uygula"
- Bağdaştırıcı kategorilerinin görünürlüğü
- Son pencere durumunu hatırlama
- Değişiklikler `settings.json`'a kaydedilir

## Bitti sayılır
- [ ] Tüm ayarlar kaydediliyor ve yeniden açılışta korunuyor
- [ ] "Hemen uygula" açıkken profil seçimi normal doğrulamalı uygulama akışını başlatıyor

## Sorular
1. Tema seçimi için hangi seçenekler sunulmalı ve ayar 04 numaralı iş tamamlanana kadar arayüzde nasıl davranmalı?
2. Tepsiye küçültme, pencereyi kapatma ve uygulamadan tamamen çıkma davranışları hangi kullanıcı eylemlerinde uygulanmalı?
3. Bildirimler kapalıyken hangi bildirim türleri gizlenmeli; hata ve kritik durum bildirimleri bu tercihten etkilenmeli mi?
4. "Profil seçilince hemen uygula" ayarı etkin olduğunda doğrulama akışında kullanıcıya hangi onay, uyarı veya iptal imkânları sunulmalı?
5. Bağdaştırıcı kategorilerinin görünürlüğünde hangi kategoriler bulunmalı ve varsayılan görünürlükleri ne olmalı?
6. Son pencere durumu olarak hangi bilgiler (boyut, konum, büyütülmüş durum, seçili sekme) kaydedilmeli; geçersiz ekran konumları nasıl ele alınmalı?
