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

## Sorular

1. Doğrulama için hangi hipervizör, Windows sürümü ve sanal makine yapılandırması desteklenecek?
2. Sanal makinenin ağ topolojisi nasıl izole edilecek; yalnızca host-only/özel sanal anahtar mı kullanılacak, yoksa internet ya da kurumsal ağa erişim gerekli mi?
3. Test edilecek bağdaştırıcı nasıl kesin olarak seçilecek ve yanlışlıkla yönetim/ağ erişimi sağlayan bağdaştırıcının değiştirilmesi nasıl engellenecek?
4. Yıkıcı testleri açan `IPMAN_*` ortam değişkenlerinin adları, beklenen değerleri ve doğrulanma kuralları nelerdir?
5. Statik yapılandırma senaryosunda kullanılacak IPv4 adresi, alt ağ maskesi, varsayılan ağ geçidi ve DNS değerleri neler olmalı?
6. DHCP senaryosu için sanal ağda bir DHCP sunucusu bulunması zorunlu mu; zorunluysa hangi adres aralığını ve kiralama davranışını sağlamalı?
7. Statik → DHCP ve DHCP → statik geçişlerinin başarı ölçütleri nelerdir; yalnızca ayarların okunarak doğrulanması yeterli mi, yoksa bağlantı/ping doğrulaması da gerekli mi?
8. Geri yükleme senaryosunda hangi başlangıç durumları kapsanacak ve geri yüklemenin özgün IP, ağ geçidi, DNS ile DHCP durumunu aynen koruması bekleniyor mu?
9. Her yıkıcı senaryodan sonra bağdaştırıcının başlangıç durumuna otomatik döndürülmesi zorunlu mu; başarısız geri yükleme için manuel kurtarma adımları nerede tanımlanacak?
10. Birden fazla IPv4 adresi, birden fazla DNS sunucusu veya birden fazla varsayılan ağ geçidi olan yapılandırmalar bu doğrulamanın kapsamında mı?
11. Testlerin çalıştırılması için yönetici yetkisi dışında ön koşul, güvenlik yazılımı istisnası veya özel WMI/Hyper-V izni gerekiyor mu?
12. Başarısız bir senaryoda hangi günlükler, WMI çıktıları ve yapılandırma bilgileri kaydedilecek; hassas ağ verileri nasıl korunacak?
13. `docs/STATE.md`'ye işlenecek sonuçların biçimi, sorumlusu ve başarılı/başarısız çalıştırmaların kayıt süresi nedir?
