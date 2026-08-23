# UI Context

## Theme

Distill follows the **Windows 11 Fluent Design System** with a dark technical workspace aesthetic:
- **Materials**: Mica Alt backdrop on the main window with layered Acrylic and solid surface cards.
- **Dark Mode Primary**: Deep slate and charcoal tones (`#0F1117`, `#181B24`, `#222634`) with subtle 1px border lines to provide depth and clarity without visual noise.
- **Accent Identity**: A vibrant electric indigo/violet accent (`#7C3AED` to `#6366F1`) paired with subtle warm gradient hints inspired by Instagram's creative palette.

---

## Colors & Design Tokens

### Color Palette

| Token Name | Hex Value | Usage / Role |
| --- | --- | --- |
| `BgBase` | `#0F1117` | Root window background (behind Mica/Acrylic) |
| `BgSurface` | `#181B24` | Primary container surfaces, sidebar, cards |
| `BgSurfaceElevated` | `#222634` | Hovered cards, flyouts, dialogs, inputs |
| `BorderDefault` | `#2D3345` | Standard structural borders & dividers (1px) |
| `BorderSubtle` | `#1F2432` | Subtle section separation |
| `TextPrimary` | `#F8FAFC` | Main headings, primary labels, note titles |
| `TextSecondary` | `#94A3B8` | Subtitles, stage descriptions, metadata |
| `TextMuted` | `#64748B` | Timestamps, placeholders, inactive states |
| `AccentPrimary` | `#6366F1` | Primary action button, active nav item, focus ring |
| `AccentHover` | `#4F46E5` | Hovered primary buttons |
| `AccentGradient` | `linear-gradient(135deg, #EC4899, #8B5CF6, #6366F1)` | Hero banner accent / branding glow |
| `StateSuccess` | `#10B981` | Completed jobs, verified dependencies, saved notes |
| `StateWarning` | `#F59E0B` | Incomplete dependencies, fallback modes |
| `StateError` | `#EF4444` | Download errors, missing Ollama connection |
| `StateProcessing` | `#38BDF8` | Active stage progress, spinning indicators |

---

## Typography

| Role | Font Family | Size | Weight |
| --- | --- | --- | --- |
| **App Title / Hero** | `Segoe UI Variable Display` | 24px | SemiBold |
| **Section Header** | `Segoe UI Variable Display` | 18px | SemiBold |
| **Card Header** | `Segoe UI Variable Text` | 15px | Medium |
| **Body UI** | `Segoe UI Variable Text` | 13px | Regular / Medium |
| **Caption / Meta** | `Segoe UI Variable Text` | 11px | Regular |
| **Code & Markdown** | `Cascadia Code` / `Consolas` | 12px | Regular |

---

## Border Radius & Elevation

| Context | Corner Radius | Elevation / Material |
| --- | --- | --- |
| **Standard Buttons / Inputs** | `4px` - `6px` | Flat with 1px border |
| **Cards & Stepper Containers** | `8px` | Layered surface + subtle shadow |
| **Modals / Flyouts** | `10px` - `12px` | Acrylic backdrop + drop shadow |
| **Pills & Status Badges** | `12px` (Full pill) | Solid background with contrasting text |

---

## Component Library & Dependencies

- **UI Framework**: WinUI 3 (Windows App SDK 1.5+).
- **Control Toolkit**: `CommunityToolkit.WinUI.Controls` (Segmented control, SettingsCard, TokenizingTextBox).
- **Icons**: `Segoe Fluent Icons` (`FontIcon` glyphs) and stroke-based SVG icons.
- **Markdown Rendering**: Native WinUI RichEditBox or `CommunityToolkit.WinUI.UI.Controls.MarkdownTextBlock` for in-app note preview.

---

## Layout Patterns

### 1. Main Navigation View (Left Sidebar + Content Canvas)
- **Left Sidebar**: Compact NavigationRail / NavigationView with:
  - **Quick Ingest** (Hero input, active clipboard monitoring)
  - **Job Queue** (Active jobs, stage steppers, historical distillations)
  - **Vault & Templates** (Obsidian vault selector, template manager)
  - **Engine Status** (Dependency checker for yt-dlp, ffmpeg, whisper.cpp, Ollama)
- **Top Header**: Minimal titlebar with Mica backdrop, active job badge count, and settings cog.

### 2. Hero Quick Ingest Bar
- Large URL input box supporting drag-and-drop and clipboard auto-paste.
- Dynamic Badge: Detects URL type (`📸 Instagram Post` vs `🎬 Instagram Reel`).
- Action Button: Prominent `⚡ Distill Note` button triggering background queue worker.

### 3. Pipeline Stepper Card (Live Job Progress)
Visual 5-stage progress indicator:
1. `⬇️ Downloading Media` (yt-dlp progress bar)
2. `✂️ Demuxing & Sampling` (ffmpeg audio/frames)
3. `🔍 Extracting Text` (Windows.Media.Ocr & whisper.cpp)
4. `🧠 AI Distillation` (Ollama streaming status)
5. `📝 Saving to Vault` (Markdown file written)

### 4. Distilled Note Preview Panel
- Side-by-side or modal preview displaying:
  - Formatted YAML frontmatter card.
  - Rendered Markdown body.
  - "Open in Obsidian" quick action button (invoking `obsidian://` protocol).
  - "Copy Markdown" to clipboard.

---

## Accessibility & Responsiveness

- Minimum window dimensions: `800x600px` (optimized for `1100x750px`).
- High-contrast mode compatibility using standard system brush fallbacks.
- Full keyboard navigation: `Ctrl+V` to paste & ingest, `Ctrl+Enter` to trigger distillation, `Tab` order on all interactive fields.
- Informative tooltips and error flyouts for missing system dependencies.
