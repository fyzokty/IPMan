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
