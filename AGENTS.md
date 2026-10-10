# AGENTS.md

IPMan: ağ bağdaştırıcılarının IPv4 ayarlarını ve profillerini yöneten Windows aracı (.NET 8, WPF, MVVM). Kararlar `docs/ADR/`'dedir; mimari değişiklik yeni ADR gerektirir.

## Doğrulama
- `dotnet build IPMan.sln -c Release` (her analizör uyarısı hatadır) ve `dotnet test IPMan.sln -c Release`.
- Tek test: `dotnet test tests/IPMan.Tests --filter "FullyQualifiedName~<ad>"`.

## Ağ güvenliği (gevşetilmez)
- Uygulama, ağı değiştiren komutlar ve `tools/host-lab` çalıştırılmaz; `IPMAN_*` ortam değişkenleri ayarlanmaz.
- Birim testler gerçek bağdaştırıcıya, WMI'ya veya kullanıcı klasörlerine dokunmaz.

## Kod kuralları
- Katman yönü Domain ← Application ← Infrastructure ← App; Windows/WMI ve dosya erişimi yalnızca Infrastructure'da.
- Kullanıcı metni yalnızca `Strings.resx` üzerinden; paketler yalnızca `Directory.Packages.props` üzerinden eklenir.
- Testlerde mock kütüphanesi yok; `tests/IPMan.Tests/Fakes/` kullanılır.
