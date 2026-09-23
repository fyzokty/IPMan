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

## Sorular

1. “İlk açılış” hangi kalıcı işaretle belirlenecek; ayarlar dosyasının veya yedek klasörünün kullanıcı tarafından silinmesi yeniden ilk açılış sayılacak mı?
2. Yedek alma denemesi başarısız olursa (örneğin disk dolu veya erişim engeli), sonraki her açılışta yeniden denenecek mi?
3. Yedek alınamadığında kullanıcıya hiç bildirim yapılmayacak mı; hata yalnızca günlüğe mi yazılacak, yoksa uygulama içi bir durum bilgisi gösterilecek mi?
4. “Görünür bağdaştırıcı” tanımı hangi bağdaştırıcıları kapsar: yalnızca fiziksel Ethernet ve Wi-Fi bağdaştırıcılarını mı, yoksa etkin VPN, sanal makine, köprü ve loopback bağdaştırıcılarını da mı?
5. Devre dışı, bağlantısı kesik veya o anda IPv4 yapılandırması bulunmayan görünür bağdaştırıcıların yedeği de alınacak mı?
6. Her bağdaştırıcı için hangi yapılandırma alanları kurtarılacak: DHCP durumu, tüm IPv4 adresleri ve alt ağ maskeleri, varsayılan ağ geçitleri, DNS sunucuları, metrikler ve diğer Windows’a özgü ayarlar mı?
7. Aynı bağdaştırıcıda birden fazla IPv4 adresi, ağ geçidi veya DNS sunucusu varsa sıraları korunacak mı?
8. Yedek, bağdaştırıcıyı yeniden bulmak için hangi kimliği saklayacak; kullanıcı tarafından değişebilen adın yanı sıra GUID, MAC adresi veya arabirim indeksi de kullanılacak mı?
9. Uygulamanın açılış sırasında yapılandırmayı okuyamadığı tek bir bağdaştırıcı olursa, erişilebilen bağdaştırıcılarla kısmi yedek oluşturulacak mı, yoksa tüm işlem başarısız mı sayılacak?
10. Hiç uygun bağdaştırıcı bulunamazsa boş bir yedek ve ilk-açılış işareti oluşturulacak mı, yoksa daha sonraki açılışta yeniden denemek için işaret yazılmayacak mı?
11. İlk açılış yedeği mevcut kurtarma yedeklerinden dosya adı, konum ve şema bakımından nasıl ayırt edilecek; ADR-016’daki mevcut saklama/temizleme kurallarından muaf mı olacak?
12. Bu yedek kesinlikle hiç üzerine yazılmayacak mı; kullanıcı uygulamayı kaldırıp yeniden kurduğunda veya ayarları sıfırladığında yenilenmesi isteniyor mu?
13. Kullanıcı bu yedeği hangi akıştan görüntüleyip geri yükleyebilecek; bu iş için ayrıca bir arayüz veya komut planlanıyor mu?
14. Yedek dosyasının kullanıcıya ait ağ adresleri ve DNS bilgileri içermesi nedeniyle dosya izinleri, dışa aktarma veya silme konusunda özel bir gereksinim var mı?
15. İlk açılış yedeğinin normal “uygula öncesi” kurtarma yedeğiyle aynı anda oluşması engellenecek mi; oluşursa geri yükleme sırasında hangisinin öncelikli olduğu nasıl anlaşılacak?
