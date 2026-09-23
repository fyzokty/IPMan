# 05 — Hızlı eylemler

- **Durum:** yapılacak
- **Bağımlılık:** 01
- **Gereksinimler:** PR-023, PR-024

## Amaç
Seçili bağdaştırıcı için sık kullanılan tek tıklık eylemler.

## Kapsam
- Ağ bilgisini kopyala (kısa metin özeti)
- Ağ geçidine ping
- DNS önbelleğini temizle
- IP yenile (DHCP bağdaştırıcılarında)
- DHCP (mevcut akış kullanılır)
- Eylemler yalnızca seçili bağdaştırıcıya etki eder; sonuç satır içi gösterilir

## Bitti sayılır
- [ ] Her eylem çalışıyor ve sonucu Türkçe gösteriliyor
- [ ] Uygun olmayan durumda eylem devre dışı (ör. statik IP'de "IP yenile")

## Sorular
1. "Ağ bilgisini kopyala" eyleminin kısa metin özetinde hangi alanlar (bağdaştırıcı adı, IP, alt ağ maskesi, ağ geçidi, DNS, DHCP durumu vb.) ve hangi biçim yer almalıdır?
2. Ağ geçidine ping eyleminde birden fazla varsayılan ağ geçidi varsa hangisi hedeflenmelidir; ağ geçidi yoksa kullanıcıya hangi sonuç gösterilmelidir?
3. Ping işlemi için zaman aşımı, paket sayısı ve başarılı/başarısız sonucunu belirleyen ölçütler neler olmalıdır?
4. "DNS önbelleğini temizle" eylemi seçili bağdaştırıcıya özgü uygulanamayan sistem genelinde bir işlem olduğundan, eylem seçili bağdaştırıcı bağlamında nasıl sunulmalıdır?
5. DNS önbelleği temizleme işlemi yönetici yetkisi veya sistem hatası nedeniyle başarısız olursa kullanıcıya hangi ayrıntı düzeyinde mesaj gösterilmelidir?
6. "IP yenile" yalnızca DHCP bağdaştırıcılarında etkin denmiş; işlem önce IP bırakma ve ardından yenileme mi yapmalıdır, yoksa yalnızca yenileme mi gerçekleştirmelidir?
7. Hızlı eylemler çalışırken ilgili düğmeler ve diğer bağdaştırıcı sekmeleri nasıl davranmalıdır; eşzamanlı işlem başlatılması engellenecek midir?
