# 09 — İlk açılışta yapılandırma yedeği

- **Durum:** yapılacak
- **Bağımlılık:** yok
- **Gereksinimler:** PR-016

## Amaç
İlk çalıştırmada, kullanıcıya soru sormadan mevcut ağ yapılandırmasının
kurtarılabilir bir kopyasını almak.

## Kapsam
- Depolama klasörleri yoksa oluşturulur
- Görünür bağdaştırıcıların mevcut yapılandırması kaydedilir
- Kafa karıştırıcı profil adları oluşturulmaz; veri kurtarma yedeği olarak tutulur
- Mevcut kurtarma yedeği mekanizmasıyla uyumlu (ADR-016)

## Bitti sayılır
- [ ] İlk açılışta yedek sessizce oluşuyor
- [ ] Sonraki açılışlarda gereksiz kopya oluşmuyor
