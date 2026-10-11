---
title: ADR-018 - Simple-First UX and Safe Switching
status: Accepted
date: 2026-10-11
---

# Context

IPMan 1.0 was initially conceived as a dense, technician-oriented utility with a fixed 280px left profile pane, adapter tabs, a read-only current configuration card next to an editable draft card, and an explicit two-step "apply draft" pattern. User feedback and usability analysis showed that non-technical users (persona P1) struggle with this layout:
- The concept of a "draft" is confusing; users expect clicking a profile to activate it directly.
- The UI surfaces technical jargon everywhere (subnet mask, default gateway, DHCP, MAC, vEthernet).
- The fundamental user question—"Is internet working?"—is not prominently answered.
- Modifying network settings creates anxiety because a bad switch can cut off access with no built-in safety net.
- Virtual adapters clutter the view.

To resolve these friction points without diminishing technician speed (personas P2–P4), a unified "hybrid" (karma) user experience is established.

# Decision

### 1. Profile-Centric Shell Replacing Tabs and Drafts
Replace the left drawer, adapter tabs, and dual "current/draft" cards with a unified profile-centric window:
- A header bar featuring a friendly connection selector dropdown ("Bağlantı: Kablolu (Ethernet) ▾").
- A prominent full-width status hero card reporting the active connection and internet connectivity state, linking to an "Ayrıntılar ve elle ayar" sub-page.
- A central grid or list of saved settings, where "Otomatik" (DHCP) is a built-in, immutable first entry.
- The minimum main window dimensions are reduced from 1100×700 to 900×600 (default ~1000×680).

### 2. Direct Apply Flow and Removal of Draft Model
Clicking "Bu ağa geç" on any card, list row, or tray menu item applies the target configuration immediately. The intermediate draft concept is eliminated. The legacy setting `ApplyProfileOnSelection` is deprecated and ignored; direct execution becomes the universal behavior.

### 3. Conditional Internet Loss Confirmation
Show a confirmation modal prior to switching only when the mutation is likely to sever internet connectivity:
- **Condition:** The active adapter currently reports verified internet connectivity, AND the target configuration is static IPv4 with either no default gateway or a gateway residing outside the target IPv4 subnet.
- Displays an old-to-new address comparison, adapter identifier, an amber advisory note explaining that internet access may drop, folded technical details, and a "Bir daha sorma" (Do not ask again) checkbox.
- The opt-out preference is persisted additively in `AppSettings` (`SkipInternetLossConfirmation`) and can be reset from Settings.

### 4. Mandatory 15-Second Safety Countdown
Following every successful UI-initiated network mutation (profile switch, manual apply, or DHCP reset, whether triggered from the window or the system tray):
- A topmost dialog displays a 15-second radial progress countdown: *"Saha PLC ayarına geçildi — Her şey yolunda mı? Yanıt vermezseniz 15 saniye sonra eski ayara (Ofis) otomatik dönülecek."*
- Actions: "Hayır, geri al" (revert immediately) and "Evet, böyle kalsın" (confirm and dismiss).
- **Timer location:** The countdown timer resides in an App layer view-model coordinator service (`ISafetyCountdownCoordinator` / `SafetyCountdownViewModel`) injected with a deterministic time abstraction (`IMonotonicClock` / `ITimerService`) to ensure fully deterministic testing with fakes. The deadline is measured on a monotonic clock so wall-clock adjustments cannot stall or shorten the countdown.
- **Revert logic:** Timeout or clicking "Hayır, geri al" invokes the pre-mutation snapshot restore workflow via `RecoveryRestoreService` (ADR-016).
- **Mutation lock:** The cross-process mutation lock (ADR-014) is acquired during both the initial apply and the restore operation.
- **App exit during countdown:** Attempting to close the window or exit from tray while the countdown is active prompts the user whether to keep the new settings or revert before exiting.
- **Revert failure:** If automatic or manual revert fails, an inline error banner is displayed with clear, non-panicking guidance and fallback steps.
- The safety countdown is mandatory and non-optional in release 1.0.

### 5. OS-Driven Connectivity Indication
Query the operating system's native connectivity assessment behind an Application layer abstraction (`INetworkConnectivityService`) implemented in Infrastructure via the Windows Network List Manager (NLM / `INetworkListManager`) and NCSI (Network Connectivity Status Indicator) APIs:
- States: "İnternet çalışıyor", "Yerel ağ — internet yok", "Kablo takılı değil", and "Bağlantı yok".
- IPMan sends no custom ping probes, synthetic HTTP requests, or DNS queries for connectivity detection.
- When an adapter is disconnected ("Kablo takılı değil"), the status card transitions to a neutral grey state explaining that setting selections will apply when plugged in, and offers a shortcut to an active Wi-Fi adapter if available. Setting cards remain fully selectable and editable while disconnected.

### 6. Subnet-Aware IP Address Suggestion
On the new/edit setting page, when a static IP mode is chosen with an optional target device IP (e.g. `192.168.0.10`):
- The application generates a candidate computer address within the same `/24` subnet.
- The search skips `.0`, `.255`, the target device address, and the adapter's current gateway.
- Up to 5 candidates (e.g. `.50`, `.51`, ...) are sequentially tested using the existing `IIpv4ConflictProbe` (`PingIpv4ConflictProbe`).
- The UI shows "Boş olduğu kontrol edildi" (Verified free) only if a probe actually completed and received no echo response. If probing could not complete or was inconclusive, it displays "Kontrol edilemedi, yine de kullanabilirsiniz".

### 7. Automatic Subnet Mask Default
When an IPv4 address is entered and the subnet mask field is empty, the mask automatically fills with `255.255.255.0` (tagged as "otomatik dolduruldu"). The mask field is tucked under the folded "Gelişmiş" section but remains editable.

### 8. Virtual Adapters Hidden by Default
The default value of `ShowVirtualAdapters` in `AppSettings` is changed from `true` to `false`. Virtual adapters (e.g. vEthernet, WSL, Hyper-V virtual switches) are hidden by default to keep the interface uncluttered for everyday users. Existing configuration files that already specify a value remain unaffected; because settings files written so far always contain `ShowVirtualAdapters: true`, the new default reaches fresh installs and reset settings only, which is acceptable before the first public release.

### 9. View Mode Threshold and Persisted Preferences
- When the collection of saved settings exceeds 6 items, the main view defaults to the compact "Liste" view.
- Explicit user toggling of "Kart" or "Liste" is remembered via a new additive setting in `AppSettings` (`PreferredViewMode`).

### 10. System Tray Menu Enhancements
The system tray menu is expanded from a bare open/exit menu to a functional switcher:
- Header indicating current adapter and connectivity: "● Şu an: Ofis · İnternet çalışıyor".
- "Ayara geç" section displaying up to 10 entries (starred favorites first, followed by others, with a radio indicator on the active profile), concluding with "Diğer ayarlar…" to open the window if more exist.
- "Son değişikliği geri al" quick action invoking snapshot rollback.
- "IPMan'i aç" and "Çıkış".
- Profile switches initiated from the tray invoke the same conditional confirmation and safety countdown dialogs.

# Consequences

- **Documentation & UI Assets:** The legacy screens in `docs/03_UI_UX/stitch/` and the original `DESIGN.md` layout are superseded. The canonical visual reference is defined by `docs/03_UI_UX/stitch-karma/` (screens 01–10).
- **Settings Schema:** Two additive fields are introduced to `AppSettings`: `PreferredViewMode` (enum: `Auto`, `Card`, `List`) and `SkipInternetLossConfirmation` (`bool`). Schema version remains `CurrentSchemaVersion = 1` because missing fields deserialize gracefully to defaults. `ApplyProfileOnSelection` is marked obsolete.
- **Architectural Layering:** The strict dependency rule (`Domain ← Application ← Infrastructure ← App`) is preserved. NLM and Windows COM interfaces are confined to Infrastructure; UI timing and countdown lifecycle belong to App view-models; snapshot rollback logic remains in Application (`RecoveryRestoreService`).
- **Testing Strategy:** No third-party mock libraries are introduced. All new components are tested with fakes under `tests/IPMan.Tests/Fakes/` (`FakeClock`, `FakeTimerService`, `FakeNetworkConnectivityService`, `FakeIpv4ConflictProbe`).
- **Trade-offs and Mitigations:**
  - *Expert friction:* A 15-second safety dialog after every switch adds a step for power users. This is accepted for release 1.0 to guarantee safety; the dialog can be dismissed instantly with Enter / "Evet, böyle kalsın".
  - *OS connectivity latency:* NLM/NCSI assessments can occasionally lag network state changes by a few seconds. The indicator is treated strictly as informative visual feedback and never blocks profile application.
