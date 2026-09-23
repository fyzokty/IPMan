# 11 — Gerçek bağdaştırıcıda doğrulama

- **Durum:** yapılacak
- **Bağımlılık:** yok (01–10 ile paralel yürüyebilir)
- **Gereksinimler:** PR-006..PR-008 · AC-007, AC-008

## Amaç
DHCP ve geri yükleme yolları şimdiye kadar yalnızca sahte WMI ile test edildi.
Bunların gerçek bir bağdaştırıcıda çalıştığını kanıtlamak.

## Kapsam
- İzole sanal makine ortamının hazırlanması
- `IPMAN_*` onayıyla yıkıcı testlerin çalıştırılması
- Statik → DHCP, DHCP → statik ve geri yükleme senaryoları
- Bulunan hataların düzeltilmesi ve sonuçların kaydı

## Kısıtlar
- Ağı değiştiren komutlar geliştirme makinesinde asla çalıştırılmaz
- CI'a `IPMAN_*` değişkenleri eklenmez

## Bitti sayılır
- [ ] Tüm senaryolar sanal makinede başarılı
- [ ] Sonuçlar `docs/STATE.md`'ye işlendi
