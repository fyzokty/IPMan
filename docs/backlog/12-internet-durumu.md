# 12 — İnternet durumu ve bağlantı yok durumu

- **Durum:** tamamlandı
- **Bağımlılık:** yok
- **Gereksinimler:** ADR-018 · Ekran 01, 09

## Amaç
Kullanıcıya o anki ağ bağlantısının internete erişip erişmediğini ve kablo durumunu sade, güvenilir bir dille göstermek; sanal bağdaştırıcı kirliliğini varsayılan olarak önlemek.

## Kapsam
- İşletim sisteminin kendi ağ bağlantı değerlendirmesini (Windows Network List Manager / NCSI) Infrastructure katmanında okuyan ve Application katmanında soyutlayan servis altyapısı (`INetworkConnectivityService`)
- IPMan'in kendi başına hiçbir ping, HTTP isteği veya DNS denetimi yapmaması; yalnızca işletim sistemi sonucunu okuması
- Bağlantı durumları: "İnternet çalışıyor", "Yerel ağ — internet yok", "Kablo takılı değil", "Bağlantı yok"
- Dost bağdaştırıcı adlandırmaları: "Kablolu bağlantı (Ethernet)", "Kablosuz bağlantı (Wi-Fi)" ve arkasındaki Windows aygıt adı
- Sanal bağdaştırıcıların (vEthernet, WSL vb.) varsayılan olarak gizlenmesi (`ShowVirtualAdapters` varsayılanının `false` yapılması, var olan yapılandırma dosyalarının değerlerini koruması)
- Durum kartının görseli 13'te yapılır; bu iş durum bilgisini ve sade metinleri hazırlar ve mevcut pencerede durum metni olarak gösterir
- Kablo takılı olmadığında gri durum kartı için metin ("Kablo takılı değil — Kabloyu takın. Seçtiğiniz ayar kablo takılınca da geçerli olur.")
- Kablo takılı olmadığında bağlı bir kablosuz bağdaştırıcı varsa "Kablosuz bağlantıya geç" kısayol butonu sunulması; bu durumda dahi ayar kartlarının kullanılabilir kalması

## Bitti sayılır
- [x] Windows NLM/NCSI durumu Application soyutlaması arkasında okunabiliyor
- [x] Sahte servis (`FakeNetworkConnectivityService`) ile birim testler internetli, internetsiz ve takılı olmayan durumları doğruluyor
- [x] `ShowVirtualAdapters` varsayılanı `false` olarak ayarlandı ve sanal bağdaştırıcılar varsayılanda gizleniyor
- [x] Kablosuz ve kablolu bağdaştırıcılar sade Türkçe adlarla listeleniyor

Durum metni mevcut sekmelerde ve ayrıntıların Durum satırında gösterilir. Gri durum kartı
ve "Kablosuz bağlantıya geç" kısayolu 13'e taşındı.
