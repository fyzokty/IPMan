# 09 — İlk açılışta yapılandırma yedeği

- **Durum:** devam ediyor
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

## Sorular

Yanıtlar kullanıcı onayı beklemeden makul varsayılan olarak verildi; değiştirilebilir.

1. “İlk açılış” hangi kalıcı işaretle belirlenecek; ayarlar dosyasının veya yedek klasörünün kullanıcı tarafından silinmesi yeniden ilk açılış sayılacak mı?
    * `%LocalAppData%\IPMan\FirstRun\first-run.json` dosyasının varlığı. Dosya silinirse sonraki açılış yeniden ilk açılış sayılır; ayarlar dosyası bu karara katılmaz.
2. Yedek alma denemesi başarısız olursa (örneğin disk dolu veya erişim engeli), sonraki her açılışta yeniden denenecek mi?
    * Evet; başarılı yedek yazılana kadar her açılışta yeniden denenir.
3. Yedek alınamadığında kullanıcıya hiç bildirim yapılmayacak mı; hata yalnızca günlüğe mi yazılacak, yoksa uygulama içi bir durum bilgisi gösterilecek mi?
    * Kullanıcıya bildirim yok; hata yalnızca kritik günlüğe yazılır.
4. “Görünür bağdaştırıcı” tanımı hangi bağdaştırıcıları kapsar: yalnızca fiziksel Ethernet ve Wi-Fi bağdaştırıcılarını mı, yoksa etkin VPN, sanal makine, köprü ve loopback bağdaştırıcılarını da mı?
    * Ana pencerenin listelediği bağdaştırıcıların aynısı (aynı kaynak ve filtre).
5. Devre dışı, bağlantısı kesik veya o anda IPv4 yapılandırması bulunmayan görünür bağdaştırıcıların yedeği de alınacak mı?
    * Listede görünen her bağdaştırıcı yedeklenir (bağlantısı kesik olanlar dahil); listede olmayanlar yedeklenmez.
6. Her bağdaştırıcı için hangi yapılandırma alanları kurtarılacak: DHCP durumu, tüm IPv4 adresleri ve alt ağ maskeleri, varsayılan ağ geçitleri, DNS sunucuları, metrikler ve diğer Windows’a özgü ayarlar mı?
    * Mevcut kurtarma yedeğinin (ADR-016) kaydettiği alanların aynısı; yeni alan eklenmez.
7. Aynı bağdaştırıcıda birden fazla IPv4 adresi, ağ geçidi veya DNS sunucusu varsa sıraları korunacak mı?
    * Evet; mevcut kurtarma yedeğindeki sıra korunur.
8. Yedek, bağdaştırıcıyı yeniden bulmak için hangi kimliği saklayacak; kullanıcı tarafından değişebilen adın yanı sıra GUID, MAC adresi veya arabirim indeksi de kullanılacak mı?
    * Mevcut kurtarma yedeğiyle aynı kimlik (SettingID/GUID ve ad).
9. Uygulamanın açılış sırasında yapılandırmayı okuyamadığı tek bir bağdaştırıcı olursa, erişilebilen bağdaştırıcılarla kısmi yedek oluşturulacak mı, yoksa tüm işlem başarısız mı sayılacak?
    * Okunabilenlerle kısmi yedek yazılır, okunamayanlar günlüğe yazılır; yedek tamamlanmış sayılır.
10. Hiç uygun bağdaştırıcı bulunamazsa boş bir yedek ve ilk-açılış işareti oluşturulacak mı, yoksa daha sonraki açılışta yeniden denemek için işaret yazılmayacak mı?
    * Hiç bağdaştırıcı yoksa dosya yazılmaz; sonraki açılışta yeniden denenir.
11. İlk açılış yedeği mevcut kurtarma yedeklerinden dosya adı, konum ve şema bakımından nasıl ayırt edilecek; ADR-016’daki mevcut saklama/temizleme kurallarından muaf mı olacak?
    * Ayrı klasör ve dosya (`FirstRun\first-run.json`), içinde bağdaştırıcı başına mevcut kurtarma yedeği biçimi; ADR-016 budamasından muaftır. Kararlar ADR-017'ye yazılır.
12. Bu yedek kesinlikle hiç üzerine yazılmayacak mı; kullanıcı uygulamayı kaldırıp yeniden kurduğunda veya ayarları sıfırladığında yenilenmesi isteniyor mu?
    * Dosya varken asla üzerine yazılmaz.
13. Kullanıcı bu yedeği hangi akıştan görüntüleyip geri yükleyebilecek; bu iş için ayrıca bir arayüz veya komut planlanıyor mu?
    * 1.0'da arayüz yok; dosya veri kurtarma kopyasıdır ve elle başvurulur. "Son Yapılandırmayı Geri Yükle" bu dosyayı kullanmaz.
14. Yedek dosyasının kullanıcıya ait ağ adresleri ve DNS bilgileri içermesi nedeniyle dosya izinleri, dışa aktarma veya silme konusunda özel bir gereksinim var mı?
    * Özel gereksinim yok; dosya kullanıcının yerel uygulama verisi klasöründedir.
15. İlk açılış yedeğinin normal “uygula öncesi” kurtarma yedeğiyle aynı anda oluşması engellenecek mi; oluşursa geri yükleme sırasında hangisinin öncelikli olduğu nasıl anlaşılacak?
    * Birbirinden bağımsızdır; geri yükleme yalnızca ADR-016 yedeklerini kullanır, ilk açılış yedeği hiçbir zaman öncelik yarışına girmez.
