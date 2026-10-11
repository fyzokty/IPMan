# 14 — Güvenli geçiş: koşullu onay ve geri alma sayacı

- **Durum:** bitti
- **Bağımlılık:** 13
- **Gereksinimler:** ADR-018 · Ekran 03, 04

## Amaç
Kullanıcının internet erişimini kesebilecek geçişlerde önceden uyarılmasını sağlamak ve yapılan her başarılı ağ değişikliğinin ardından otomatik 15 saniyelik güvenlik ağı sunarak hatalı yapılandırmalarda mahsur kalmayı önlemek.

## Kapsam
- **Koşullu onay iletişim kutusu (Ekran 03):** Yalnızca geçişin interneti kesme olasılığı yüksek olduğunda gösterilir (bağdaştırıcıda internet çalışıyor VE hedef ayar sabit IP'li olup ağ geçidi yok veya hedef alt ağın dışında).
- Onay içeriği: Eski → yeni bilgisayar adresi karşılaştırması, bağlantı adı, amber renkli bilgi kutusu ("Bu ayarda internet çalışmayabilir. İşiniz bitince tek tıkla Otomatik ayara dönebilirsiniz."), katlanmış "Teknik ayrıntılar", kalıcı saklanan "Bir daha sorma" seçeneği (`AppSettings.SkipInternetLossConfirmation`), "Vazgeç" ve "Geç" butonları
- **15 saniye güvenlik geri alma sayacı (Ekran 04):** Arayüz üzerinden başlatılan (pencereden veya tepsiden, profil geçişi, elle ayar veya Otomatik) HER başarılı değişikliğin ardından açılan en üstte (topmost) kalıcı modal kutu
- Sayaç görseli: 15 saniyeden geriye sayan dairesel (radial) animasyonlu halka, başlık: "[Ayar adı] ayarına geçildi", gövde: "Her şey yolunda mı? Yanıt vermezseniz 15 saniye sonra eski ayara ([Eski ayar]) otomatik dönülecek."
- Butonlar: "Hayır, geri al" (derhal eski ayara döner) ve "Evet, böyle kalsın" (diyaloğu kapatır ve değişikliği onaylar)
- Geri alma mantığı: Süre dolduğunda veya "Hayır, geri al" seçildiğinde ADR-016 pre-mutation anlık görüntüsünün `RecoveryRestoreService` üzerinden geri yüklenmesi; mutasyon kilidinin (ADR-014) korunması
- Sayaç açıkken uygulama kapatılmak istendiğinde kullanıcıya ayarın saklanması mı yoksa geri mi alınması gerektiğinin sorulması
- Geri alma başarısız olursa paniğe sevk etmeyen, sonraki adımı gösteren hata bandı sunulması
- Zamanlayıcı mimarisi: App katmanında zamanlayıcı soyutlaması (`IMonotonicClock` / `ITimerService`) kullanılarak birim testlerde sahte nesnelerle (`FakeClock`, `FakeTimer`) deterministik test edilebilmesi; 1.0 sürümünde sayacın zorunlu olması

## Bitti sayılır
- [x] İnternet kesilme kuralı sağlandığında onay diyaloğu açılıyor; "Bir daha sorma" seçildiğinde ayar saklanıyor ve tekrar sormuyor
- [x] Her başarılı geçişten sonra 15 saniyelik geri sayım diyaloğu en üstte açılıyor
- [x] "Hayır, geri al" tıklandığında veya 15 saniye dolduğunda `RecoveryRestoreService` ile eski ayara başarıyla dönülüyor
- [x] "Evet, böyle kalsın" tıklandığında sayaç iptal edilip diyalog kapanıyor
- [x] Sayacın çalışması ve geri alma mantığı `tests/IPMan.Tests/Fakes/` içindeki sahte zamanlayıcılarla test edildi
