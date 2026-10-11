---
name: IPMan Fluent
colors:
  background: '#F3F3F3'
  surface: '#FFFFFF'
  outline: '#D1D1D1'
  on-surface: '#1B1B1B'
  on-surface-variant: '#5D5D5D'
  primary: '#0F6CBD'
  on-primary: '#FFFFFF'
  focus: '#005FB8'
  hover: '#E8F1FB'
  disabled: '#E5E5E5'
  success: '#107C10'
  success-container: '#DFF6DD'
  warning: '#B8860B'
  warning-container: '#FFF4CE'
  error: '#A80000'
  error-container: '#FDE7E9'
  info: '#0F6CBD'
  info-container: '#EFF6FC'
  connected: '#107C10'
  disconnected: '#6B6968'
  changed: '#D87C01'
  changed-container: '#FFFCF5'
dark-colors:
  background: '#202020'
  surface: '#2B2B2B'
  outline: '#3D3D3D'
  on-surface: '#FFFFFF'
  on-surface-variant: '#C5C5C5'
  primary: '#4CC2FF'
  on-primary: '#000000'
  changed: '#F59E0B'
  changed-container: '#2C2416'
typography:
  title: { fontFamily: Inter, fontSize: 1.375rem, fontWeight: '600' }
  subtitle: { fontFamily: Inter, fontSize: 1.0625rem, fontWeight: '600' }
  body: { fontFamily: Inter, fontSize: 0.9375rem, fontWeight: '400' }
  caption: { fontFamily: Inter, fontSize: 0.75rem, fontWeight: '400' }
  mono: { fontFamily: JetBrains Mono, fontSize: 0.875rem, fontWeight: '500' }
rounded:
  control: 4px
  card: 8px
spacing:
  xs: 4px
  sm: 8px
  md: 12px
  lg: 20px
---

## Brand & Style

IPMan is a native Windows 11 desktop utility (.NET 8, WPF, MVVM) that manages IPv4 network adapter configurations and saved settings. The interface follows Windows 11 Fluent guidelines: calm neutral surfaces, subtle hairline borders (`1px`), clean card elevations, and a single primary blue accent (`#0F6CBD`). There are no marketing banners, web chrome, gradients, or extraneous decorations. All user-facing text is strictly in Turkish (`Strings.resx`).

The primary target audience is the everyday computer user (persona P1) who needs to connect to field devices, cameras, or test networks without understanding networking jargon. Power users and field technicians (personas P2–P4) retain maximum execution speed through a dense list view, instant filtering, starred favorites, system tray switching, and collapsible "Gelişmiş" (advanced) sections. Technical details are never omitted; they are tucked into organized, folded sections so novice users are never overwhelmed.

## Colors

- Window background `#F3F3F3`, cards `#FFFFFF`, hairline borders `#D1D1D1` (light mode).
- Primary text `#1B1B1B`, secondary text `#5D5D5D`, placeholder/outline `#717783`.
- Accent `#0F6CBD` (dark: `#4CC2FF`) reserved for primary action triggers, active tabs/selections, and keyboard focus rings.
- Status states:
  - Connected with internet: `#107C10` (green dot/badge, container `#DFF6DD`).
  - Local network without internet: `#B8860B` (amber dot/badge, container `#FFF4CE`).
  - Disconnected cable / no connection: `#6B6968` (grey dot/badge, container `#F3F4F6`).
- Mutation & validation states:
  - Changed field highlight: amber `#D87C01`, container `#FFFCF5`.
  - Blocking error: `#A80000` (border & caption), container `#FDE7E9`.
  - Non-blocking advisory warning: amber `#B8860B`.
- Dark theme mirrors identical semantic roles (`#202020` background, `#2B2B2B` card, `#4CC2FF` accent, `#F59E0B` changed highlight).

## Typography

Inter serves as the cross-platform stand-in for Segoe UI Variable. Titles are 22px semibold, section headers 17px semibold, card titles 16px semibold, body text 15px regular, and captions 12px regular. All IP addresses, subnet masks, gateways, and DNS numbers use `JetBrains Mono` at 13px/14px medium for tabular alignment.

## Layout & Spacing

- Window size: minimum `900×600` (default `1000×680`), resizable.
- The previous fixed 280px left profiles panel, adapter tabs header, and bottom status bar are completely removed.
- Navigation flows across three views:
  1. **Ana ekran (Main screen):** Header with connection selector dropdown ("Bağlantı: Kablolu (Ethernet) ▾"), full-width active status card, filter and segmented Kart|Liste toggle, and a grid/list of saved settings.
  2. **Yeni / Düzenle ayar sayfası (New / edit setting page):** Full-page sub-view with "← Geri" navigation, friendly name, Otomatik / Sabit radio cards, optional target device helper, auto-suggested IP card, and folded Gelişmiş section.
  3. **Ayrıntılar ve elle ayar sayfası (Details & manual configuration page):** Dedicated sub-view opened from the status card, showing live adapter parameters, inline amber diff indicators ("değişecek"), collapsible Gelişmiş tools, and a sticky bottom action bar.
- Dialogs:
  - **Conditional confirmation dialog:** `480px` wide centered modal overlay with before/after address diff, amber caution box, folded technical details, and a "Bir daha sorma" checkbox.
  - **Safety countdown dialog:** `480px` wide topmost modal overlay with a 15-second radial progress ring, prompt text, and keep/revert buttons.

## Shapes

- Interactive controls, buttons, text fields: `4px` corner radius.
- Cards, modal containers, and segmented groups: `8px` corner radius.
- Badges and status pills: fully rounded (`9999px`).

## Components

- **Status card:** Full-width hero card on the main screen. Displays connection icon, bold headline ("Şu an: Ofis ağındasınız"), secondary status line ("Kablolu bağlantı · İnternet çalışıyor"), and a subtle link button "Ayrıntılar ve elle ayar →". When a cable is unplugged, switches to a neutral grey state with explanatory copy ("Kablo takılı değil — Kabloyu takın. Seçtiğiniz ayar kablo takılınca da geçerli olur.") and an optional button "Kablosuz bağlantıya geç".
- **Setting card:** `138px` tall card in a 3-column responsive grid. Displays profile name, built-in badge (e.g. "Çoğu ağ için önerilir" for Otomatik), favorite star, single-line plain summary ("Sabit adres · 192.168.0.50" or "Adresi ağ kendisi verir"), "⋯" context menu (Düzenle, Kopyala, Yeniden adlandır, Dışa aktar, Sil), and primary action button "Bu ağa geç". When active, gains a `2px` primary border, green "Şu an kullanılıyor" badge, and an inert "Etkin" button.
- **Setting list row:** Compact alternative for high profile counts. Displays favorite star, name, short description, address pill, a small "Geç" button (a green "Kullanılıyor" pill on the active row) and "⋯" menu; rows are grouped under "Favoriler" and "Diğer".
- **Kart | Liste toggle:** Segmented control next to search. By default, automatically switches to Liste view when more than 6 saved settings exist. When the user explicitly picks Kart or Liste, this preference is persisted in application settings.
- **Connection selector:** Compact dropdown button in the window header ("Bağlantı: Kablolu (Ethernet) ▾"). Displays friendly names ("Kablolu bağlantı (Ethernet)", "Kablosuz bağlantı (Wi-Fi)") alongside hardware adapter names, hiding virtual adapters by default.
- **Confirmation dialog:** Displayed when switching to a static profile likely to interrupt internet connectivity (adapter currently reports internet, and target has no gateway or a gateway outside subnet). Shows old → new IP diff, adapter name, amber advisory note ("Bu ayarda internet çalışmayabilir. İşiniz bitince tek tıkla Otomatik ayara dönebilirsiniz."), expandable "Teknik ayrıntılar", "Bir daha sorma" checkbox, and Vazgeç / Geç buttons.
- **Safety countdown dialog:** Topmost modal appearing after every successful mutation. Features an animated 15-second countdown ring, prompt copy ("Saha PLC ayarına geçildi — Her şey yolunda mı? Yanıt vermezseniz 15 saniye sonra eski ayara (Ofis) otomatik dönülecek."), and "Hayır, geri al" / "Evet, böyle kalsın" buttons.
- **Address suggestion card:** Inline helper card on the new/edit setting page. When a target device address is entered, scans candidate IPs in the same `/24` subnet using `IIpv4ConflictProbe`, skipping network boundaries, target IP, and gateway. Displays "Boş olduğu kontrol edildi" if verified unreachable, or "Kontrol edilemedi, yine de kullanabilirsiniz" if inconclusive, with a "Bunu kullan" button.
- **Radio cards:** Side-by-side selection cards for "Otomatik" ("Ağ adresi kendisi verir (önerilir)") and "Sabit adres" ("Bir cihaza doğrudan bağlanmak için").
- **Changed-field highlight:** In manual edit mode, edited inputs feature an amber left border (`#D87C01`), a badge label "değişecek", and a muted baseline readout showing the current live value ("şu an: 192.168.10.25").
- **Sticky action bar:** Docked footer bar on the manual settings page. Displays change summary (e.g. "1 değişiklik"), "Ayar olarak kaydet" shortcut, and Vazgeç / Uygula buttons.
- **Inline banner:** Dismissible full-width alert banner below the status card. On mutation failure, displays an error icon, bold headline, plain explanation ("Windows yeni adresi kabul etmedi. Ağınız değişmedi, Ofis ayarı ile devam ediyorsunuz."), "Tekrar dene", and "Ayrıntılar" buttons.
- **Collapsible Gelişmiş section:** Folded accordion containing subnet mask (auto-filled `255.255.255.0`), primary/secondary DNS, read-only hardware specs (controller name, MAC, link speed), and plain-language maintenance tools.
- **Tray menu:** Windows 11 system tray context menu containing app header with live connectivity dot ("● Şu an: Ofis · İnternet çalışıyor"), "Ayara geç" section with radio check on active profile (top 10 profiles, favorites first, followed by "Diğer ayarlar…"), "Son değişikliği geri al", "IPMan'i aç", and "Çıkış".

## Screen references

The canonical reference screens reside in `docs/03_UI_UX/stitch-karma/`:
- `01-ana-ekran-kart.png` (and `.html`): Main screen in card view showing the connection selector, status card, and setting cards with direct "Bu ağa geç" actions.
- `02-ana-ekran-liste.png` (and `.html`): Main screen in list view optimized for technicians managing many profiles with compact rows.
- `03-gecis-onayi.png` (and `.html`): Conditional confirmation modal shown before switching to an internet-isolated static profile.
- `04-geri-alma-sayaci.png` (and `.html`): 15-second radial safety countdown modal providing instant one-click revert.
- `05-yeni-ayar.png` (and `.html`): Full-page setting creation flow with radio cards, target device helper, and candidate IP suggestion.
- `06-ayrintilar-elle-ayar.png` (and `.html`): Manual configuration view with amber diff indicators, folded advanced tools, and sticky bar.
- `07-hatali-giris.png` (and `.html`): Form validation state showing red blocking errors and amber advisory warnings with plain examples. *(Note: gateway field warning is amber; English labels are mockup artifacts).*
- `08-gecis-basarisiz.png` (and `.html`): Reassuring failure banner confirming that the active network configuration remained unchanged.
- `09-kablo-takili-degil.png` (and `.html`): Disconnected adapter view with a helpful grey status card and Wi-Fi switch shortcut.
- `10-tepsi-menusu.png` (and `.html`): System tray context menu featuring live status, favorite profile radio items, rollback, and launch shortcuts.

> [!NOTE]
> The directory `docs/03_UI_UX/stitch/` contains the legacy 1.0 technician layout (left profile panel, adapter tabs, read-only current vs editable draft cards). It is completely superseded by `stitch-karma/` and ADR-018.
