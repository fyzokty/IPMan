# 10 — Dağıtım: taşınabilir paket ve kurulum

- **Durum:** yapılacak
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
- [ ] Çıktılar CI'dan üretilebiliyor

## Sorular

1. Sürüm numarasının tek kaynağı hangi dosya veya süreç olacak ve sürüm numarası CI sırasında mı, yoksa sürüm hazırlığında mı güncellenecek?
2. Taşınabilir yayın tek klasör mü, tek dosya mı, yoksa iki seçenek olarak mı dağıtılacak?
3. Taşınabilir paketin hedef mimarisi yalnızca x64 mü olacak; self-contained mi, yoksa kullanıcıda .NET 8 Desktop Runtime bulunması mı beklenecek?
4. Taşınabilir pakette profil, ayar ve yedek verilerinin uygulama klasöründen bağımsız mevcut kullanıcı dizinlerinde tutulması isteniyor mu?
5. Kurulum programı için tercih edilen araç nedir (örneğin WiX Toolset, Inno Setup veya MSIX) ve bu tercihin lisans ya da kurumsal dağıtım kısıtı var mı?
6. Kurulum tüm kullanıcılar için mi, yalnızca mevcut kullanıcı için mi yapılacak; varsayılan kurulum dizini ne olacak?
7. Kurulumda Başlat menüsü, masaüstü kısayolu, kaldırma kaydı ve uygulamayı başlatma seçeneklerinden hangileri yer alacak?
8. Yeni bir sürüm kurulduğunda önceki sürümün üzerine yükseltme desteklenecek mi; desteklenecekse profil, ayar ve yedek verilerinin korunması nasıl doğrulanacak?
9. Kaldırma işleminde kullanıcı profilleri, ayarlar ve yedekler korunacak mı, yoksa isteğe bağlı olarak silinebilecek mi?
10. Paket ve kurulum dosyaları kod imzalama sertifikasıyla imzalanacak mı; imzalanacaksa sertifika ve zaman damgası CI'a nasıl güvenli biçimde sağlanacak?
11. Yayın paketlerinde lisans metni, sürüm notları, kullanım dokümantasyonu veya üçüncü taraf bildirimleri bulunacak mı?
12. CI yayın çıktısı hangi olayda üretilecek (her ana dal derlemesi, etiketli sürüm veya elle tetikleme) ve çıktılar hangi adlandırmayla ne kadar süre saklanacak?
13. CI'ın ürettiği paketler yalnızca artifact olarak mı indirilecek, yoksa GitHub Release gibi bir yayın noktasına da yüklenecek mi?
14. Temiz Windows 10/11 doğrulaması hangi Windows sürümleri ve hangi .NET çalışma zamanı kurulum durumları için yapılacak?
15. Yönetici yetkisi gereksinimi nedeniyle uygulama her açılışta mı yükseltilecek; kurulum sırasında ayrıca yönetici ayrıcalıkları için özel bir kullanıcı deneyimi beklentisi var mı?
