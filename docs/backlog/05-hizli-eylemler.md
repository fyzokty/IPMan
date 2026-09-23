# 05 — Hızlı eylemler

- **Durum:** yapılacak
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
- [ ] Her eylem çalışıyor ve sonucu Türkçe gösteriliyor
- [ ] Uygun olmayan durumda eylem devre dışı (ör. statik IP'de "IP yenile")
