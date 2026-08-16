# Verify — <görev adı>

Bu dosyayı ana agent yazar, `codex-tester` (`gpt-5.6-luna`) okur.
Kopyala: `.agent/runs/<YYYYMMDD-HHmm>-<slug>/verify.md`

---

## Task
Aşağıdaki kapıyı koş ve sonucu raporla. **Hiçbir kaynak dosyayı değiştirme.**
Kırık varsa düzeltme — teşhis et.

## Gate
<Biri:>
- L1: `dotnet test tests/IPMan.Tests --no-build`
- L2: `dotnet build && dotnet test`

## What changed (bağlam)
<Hangi iş yapıldı, hangi dosyalar değişti — teşhisi hızlandırır.>

## If it fails
Şunları ver:
- Kırılan testlerin tam adı
- Assertion / exception mesajı, kırpmadan
- Şüpheli kaynak konumu `path/file.cs:line` biçiminde
- Hatanın bu değişiklikten mi yoksa önceden mi geldiğine dair gözlem (varsa)

## Constraints
- `AGENTS.md` hard rules geçerli. Ağ değiştiren komut yok, `IPMAN_*` opt-in yok,
  destructive network testi yok.
- `src/` ve `tests/` altında hiçbir dosya değişmemeli; `bin/` ve `obj/` yazması normal.
- Commit atma. Yeni doküman üretme.

## Result contract
`AGENTS.md` içindeki Done / Files / Verification / Blocked formatıyla bitir.
`Verification` bölümünde toplam/başarılı/başarısız test sayısını aynen ver.
