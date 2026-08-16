# Brief — <görev adı>

Bu dosyayı ana agent yazar, `codex-coder` (`gpt-5.6-sol`) okur.
Kopyala: `.agent/runs/<YYYYMMDD-HHmm>-<slug>/brief.md`

---

## Task
<Tek paragraf: ne yapılacak. Nedenini de yaz — Codex "neden"i bilirse doğru kararı verir.>

## Files you may touch
- `src/...`
- `tests/...`

Bu liste kapsayıcıdır. Başka bir dosya gerekiyorsa dur ve bildir.

## Do not touch
- `docs/contracts/**`
- <ilgili diğer yollar>

## Acceptance criteria
- [ ] <gözlemlenebilir sonuç 1>
- [ ] <gözlemlenebilir sonuç 2>

## Your gate (L0)
`dotnet build src/<project>` temiz geçmeden bitti deme.
Test paketinin tamamını koşma — o kapı başka bir agent'ta.

## Constraints
- `AGENTS.md` içindeki hard rules geçerlidir; özellikle ağ değiştiren hiçbir komut yok.
- Yeni doküman üretme. Rapor dosyası, gate kaydı, changelog yazma.
- Commit atma.

## Result contract
`AGENTS.md` içindeki Done / Files / Verification / Blocked formatıyla bitir.
