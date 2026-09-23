# 07 — Kritik hata günlüğü ve çökme işareti

- **Durum:** yapılacak
- **Bağımlılık:** yok
- **Gereksinimler:** PR-026, PR-027 · AC-022

## Amaç
Yalnızca kritik teknik hataları kaydetmek ve beklenmedik kapanmayı bir sonraki
açılışta bildirmek.

## Kapsam
- Günlüğe yazılanlar: bozuk/okunamayan JSON, ayar/profil yazma hataları,
  beklenmeyen Windows ağ API hataları, yakalanmamış istisnalar
- Kullanıcı eylem geçmişi tutulmaz
- Günlük dosyasının konumu ve boyut sınırı
- Oturum işareti: temiz kapanışta silinir, sonraki açılışta varsa kısa bilgi gösterilir

## Bitti sayılır
- [ ] Yakalanmamış istisna günlüğe düşüyor
- [ ] Zorla kapatılan uygulama bir sonraki açılışta bunu bildiriyor
