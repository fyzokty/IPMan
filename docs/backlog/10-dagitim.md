# 10 — Dağıtım: taşınabilir paket ve kurulum

- **Durum:** devam ediyor
- **Bağımlılık:** 01–09
- **Gereksinimler:** PR-031, PR-032

## Amaç
IPMan'i hem taşınabilir paket hem kurulum programı olarak yayınlamak.

## Kapsam
- Sürüm numarası yönetimi
- Taşınabilir yayın (tek klasör veya tek dosya)
- Kurulum programı (araç seçimi yapılacak)
- Yönetici yetkisi isteği (manifest) korunur
- Otomatik güncelleme yok
- CI'da yayın çıktısı üretimi

## Bitti sayılır
- [ ] Temiz bir Windows 10/11 makinesinde iki biçim de kurulup çalışıyor
- [x] Çıktılar CI'dan üretilebiliyor

## Sorular

"Kullanıcı kararı" işaretli olmayan yanıtlar makul varsayılan olarak verildi; değiştirilebilir.

1. Sürüm numarasının tek kaynağı hangi dosya veya süreç olacak ve sürüm numarası CI sırasında mı, yoksa sürüm hazırlığında mı güncellenecek?
    * Kullanıcı yanıtına bağlı varsayılan: `Directory.Build.props` içindeki `<Version>` geliştirme varsayılanıdır; `v*` etiketli derlemede sürüm etiketten (`vX.Y.Z` → `X.Y.Z`) alınır. Sürüm hazırlığı etiket atmaktır.
2. Taşınabilir yayın tek klasör mü, tek dosya mı, yoksa iki seçenek olarak mı dağıtılacak?
    * Klasör (zip). (Kullanıcı kararı)
3. Taşınabilir paketin hedef mimarisi yalnızca x64 mü olacak; self-contained mi, yoksa kullanıcıda .NET 8 Desktop Runtime bulunması mı beklenecek?
    * Yalnızca x64, self-contained; kullanıcıda .NET gerekmez. (Kullanıcı kararı)
4. Taşınabilir pakette profil, ayar ve yedek verilerinin uygulama klasöründen bağımsız mevcut kullanıcı dizinlerinde tutulması isteniyor mu?
    * Evet; profiller ve ayarlar `Documents\IPMan`, yedekler ve günlükler `%LocalAppData%\IPMan` altında kalır (ADR-015). Taşınabilir paket uygulama klasörüne veri yazmaz.
5. Kurulum programı için tercih edilen araç nedir (örneğin WiX Toolset, Inno Setup veya MSIX) ve bu tercihin lisans ya da kurumsal dağıtım kısıtı var mı?
    * Inno Setup 6. (Kullanıcı kararı) Ücretsiz lisans; kurumsal MSI gereksinimi yok.
6. Kurulum tüm kullanıcılar için mi, yalnızca mevcut kullanıcı için mi yapılacak; varsayılan kurulum dizini ne olacak?
    * Tüm kullanıcılar için (uygulama zaten yönetici yetkisi istiyor); varsayılan dizin `{autopf}\IPMan`.
7. Kurulumda Başlat menüsü, masaüstü kısayolu, kaldırma kaydı ve uygulamayı başlatma seçeneklerinden hangileri yer alacak?
    * Başlat menüsü kısayolu ve kaldırma kaydı her zaman; masaüstü kısayolu isteğe bağlı (varsayılan kapalı); kurulum sonunda "IPMan'i başlat" seçeneği.
8. Yeni bir sürüm kurulduğunda önceki sürümün üzerine yükseltme desteklenecek mi; desteklenecekse profil, ayar ve yedek verilerinin korunması nasıl doğrulanacak?
    * Evet; sabit `AppId` ile yerinde yükseltme. Kullanıcı verileri kurulum dizini dışında olduğundan korunur.
9. Kaldırma işleminde kullanıcı profilleri, ayarlar ve yedekler korunacak mı, yoksa isteğe bağlı olarak silinebilecek mi?
    * Korunur; kaldırma yalnızca kurulum dizinini siler.
10. Paket ve kurulum dosyaları kod imzalama sertifikasıyla imzalanacak mı; imzalanacaksa sertifika ve zaman damgası CI'a nasıl güvenli biçimde sağlanacak?
    * 1.0'da imza yok; SmartScreen uyarısı sürüm notunda belirtilir. Sertifika sonra eklenebilir.
11. Yayın paketlerinde lisans metni, sürüm notları, kullanım dokümantasyonu veya üçüncü taraf bildirimleri bulunacak mı?
    * LICENSE paketlere ve kuruluma eklenir; sürüm notları GitHub Release'te yazılır.
12. CI yayın çıktısı hangi olayda üretilecek (her ana dal derlemesi, etiketli sürüm veya elle tetikleme) ve çıktılar hangi adlandırmayla ne kadar süre saklanacak?
    * `v*` etiketinde paketler üretilir ve Release'e eklenir; paketleme dosyalarını değiştiren PR'larda ve elle tetiklemede yalnızca 14 gün saklanan Actions artifact'i üretilir. Ad: `IPMan-<sürüm>-win-x64-portable.zip`, `IPMan-<sürüm>-win-x64-setup.exe`.
13. CI'ın ürettiği paketler yalnızca artifact olarak mı indirilecek, yoksa GitHub Release gibi bir yayın noktasına da yüklenecek mi?
    * Taslak GitHub Release'e yüklenir; yayınlamayı kullanıcı elle yapar. (Kullanıcı kararı)
14. Temiz Windows 10/11 doğrulaması hangi Windows sürümleri ve hangi .NET çalışma zamanı kurulum durumları için yapılacak?
    * Temiz Windows 10 22H2 ve Windows 11 makinede, .NET kurulu değilken elle doğrulanır (kullanıcı tarafından).
15. Yönetici yetkisi gereksinimi nedeniyle uygulama her açılışta mı yükseltilecek; kurulum sırasında ayrıca yönetici ayrıcalıkları için özel bir kullanıcı deneyimi beklentisi var mı?
    * Uygulama her açılışta UAC ile yükseltilir (manifest); kurulum yönetici yetkisiyle çalışır, ek deneyim yok.
