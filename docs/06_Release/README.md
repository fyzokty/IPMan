# Yayın rehberi

IPMan yalnızca `win-x64`, self-contained klasör olarak dağıtılır; hedef makinede
.NET kurulumu gerekmez. Taşınabilir zip içinde tek üst klasör `IPMan` bulunur.
Kurulum Inno Setup 6.3+ ile hazırlanır. Her iki paket LICENSE içerir.

## Sürüm çıkarma

1. Yayınlanacak commit'in CI sonuçlarını ve temiz makine kontrollerini gözden geçirin.
2. O commit'e `vX.Y.Z` biçiminde etiket koyup GitHub'a gönderin (ör. `v1.0.0`).
   Baştaki sıfırlar, ön-sürüm veya başka ekler etiketli yayınlarda kabul edilmez;
   geçersiz `v*` etiketi iş akışını başarısız kılar.
3. `Release packages` iş akışı Release build ve testlerden sonra iki paket üretir:
   `IPMan-X.Y.Z-win-x64-portable.zip` ve `IPMan-X.Y.Z-win-x64-setup.exe`.
   Paketler taslak GitHub Release'e eklenir; otomatik yayınlanmaz.
4. Taslağın sürüm notlarını düzenleyin, aşağıdaki kontrolleri tamamlayın ve GitHub
   üzerinden elle yayınlayın. Sürüm notuna imzasız dağıtım / SmartScreen uyarısını ekleyin.

`Directory.Build.props` içindeki `Version=1.0.0` geliştirme varsayılanıdır.
Etiketli derleme `-p:Version=X.Y.Z` ile bu değeri geçersiz kılar. SDK,
AssemblyVersion ve FileVersion'ı sayısal sürümden türetir (`X.Y.Z.0`);
InformationalVersion ve uygulamada görülen sürüm, ön-sürüm ekleri dahil Version'dır.
Git commit hash eki eklenmez. Paketleme dosyalarını değiştiren PR'larda ve elle
tetiklemede `0.0.0-ci.<run_number>` sürümüyle yalnızca 14 gün saklanan Actions
artifact'i üretilir; Release oluşturulmaz.

## Yerel paketleme

PowerShell 7, .NET 8 SDK ve kurulum paketi için Inno Setup 6.3+ gerekir.

```powershell
dotnet build IPMan.sln -c Release
dotnet test IPMan.sln -c Release
./tools/release/build-release.ps1 -Version 1.0.0 -OutputDirectory ./artifacts/release
# Inno Setup olmadan yalnızca publish + zip:
./tools/release/build-release.ps1 -Version 1.0.0 -OutputDirectory ./artifacts/release -SkipInstaller
```

Betik `dotnet publish src/IPMan.App -c Release -r win-x64 --self-contained true`
çalıştırır; tek dosya üretmez, PDB eklemez ve uygulamayı başlatmaz. ISCC, PATH veya
Program Files altındaki `Inno Setup 6` klasöründe aranır; bulunamazsa hata verir.
Geçici publish klasörü işlem sonunda temizlenir. `artifacts/` ve `out/` git dışında kalır.
Çevrimdışı doğrulamada `-NuGetConfigFile <dosya>` ile kaynakları boş bir NuGet
yapılandırması verilebilir; self-contained runtime paketleri dahil bütün bağımlılıkların
önbellekte bulunması gerekir. Betik yazılım kurmaz; CI gerekirse Inno Setup'ı kurar.
Hiçbir aşamada `IPMAN_*` değişkenleri ayarlanmaz veya `tools/host-lab` çağrılmaz.

## Temiz Windows doğrulaması (kullanıcı)

Temiz Windows 10 22H2 ve Windows 11 makinelerinde, .NET kurulu değilken:

1. Zip'i açın; `IPMan/IPMan.exe` ve LICENSE bulunduğunu kontrol edin. Uygulamayı
   açınca UAC yönetici izni istemeli; durum çubuğu ve tanılama paneli paket sürümünü göstermeli.
2. Kurulumun Türkçe / İngilizce dil seçimini, lisans ekranını ve varsayılan
   `Program Files\IPMan` konumunu kontrol edin. Başlat menüsü kısayolu oluşmalı;
   masaüstü kısayolu yalnızca görev seçilirse oluşmalı (başlangıçta seçili olmamalı).
3. Kurulum sonundaki başlatma seçeneğini ve kısayollardan açılışta UAC'yi kontrol edin.
   Taşınabilir ve kurulu uygulamada arayüzün açılması için .NET indirme istemi çıkmamalı.
4. Arayüzden bir profil ve ayar kaydedin; varsa mevcut yedek / günlük verilerini kaydedin.
   Sonraki sürümü aynı dizine kurun: tek kaldırma kaydı kalmalı ve veriler korunmalı.
5. Uygulamayı tamamen kapatıp kaldırın. Kurulum dosyaları ve kısayollar kalkmalı;
   `Documents\IPMan` ile `%LocalAppData%\IPMan` altındaki kullanıcı verileri korunmalı.
   Kullanıcı verilerini silen bir kaldırma seçeneği bulunmamalı.

1.0 paketleri kod imzası taşımaz. Windows SmartScreen veya tanınmayan yayıncı uyarısı
görülebilir; paketin doğru GitHub Release'ten geldiğini doğrulayın ve kuruluş politikasına
uyun. Ağ ayarlarını uygulayan işlemler bu dağıtım kontrolünün parçası değildir;
gerçek bağdaştırıcı doğrulaması yalnızca backlog 11'in izole VM sürecinde yapılır.

Backlog 10, temiz makine doğrulaması tamamlanana kadar **devam ediyor** kalır.
“Çıktılar CI'dan üretilebiliyor” kutusunu orkestratör, CI yeşil olduktan sonra işaretler.

Kurulumun başlatma seçeneği Inno Setup'ın
[Run bölümü](https://jrsoftware.org/ishelp/topic_runsection.htm) üzerinden tanımlanır.
