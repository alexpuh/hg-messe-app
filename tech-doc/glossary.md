# Glossary — German / English

Domain terms used in the UI, documentation, and codebase.

| German | English | Code identifier | Description |
|---|---|---|---|
| Artikel | Article | `ArticleUnit` | A product SKU with barcode(s) |
| Artikelliste | Article catalogue | — | JSON file uploaded to replace all articles in the database |
| Artikelnummer | Article number | `ArtNr`, `articleNr` | Human-readable product code |
| Beladung | Loading | — | The act of scanning goods onto a truck/dispatch |
| Beladeliste | Dispatch sheet / loading list | `DispatchSheet` | A named list of articles with required quantities for one dispatch |
| Beladelisten | Dispatch sheets | `DispatchSheet[]` | Plural of Beladeliste |
| Bestand | Stock / actual count | `count`, `QuantityUnits` | Number of units actually scanned |
| Bestandsaufnahme | Inventory / stock count | `SessionType.Inventory` | Scan session to count stock; can be at Stand (free-form) or Lager (compared against a Beladeliste) |
| Debug-Scan | Debug scan | `POST /api/Debug/scan` | Development-only endpoint that simulates a barcode scan for E2E testing |
| Einheit | Unit | `ArticleUnit` | A single packaged variant of an article (weight, EAN) |
| Fehlt | Missing | `Fehlt` (Excel column) | Required − actual (`Soll − Ist`); shown only when `Ist < Soll` |
| Gesamt | Total | `total` | Combined count: Stand Ist + Lager Ist; used in combined view |
| Gewicht | Weight | `Weight`, `unitWeight` | Weight of one unit in grams |
| Ist | Actual | `count` | Scanned quantity; short form used in UI columns |
| Istbestand | Actual stock | `count` | Same as Ist, used in longer-form labels |
| Messeabschluss | Trade-show close-out | `CombinedView` | View merging one Stand and one Lager session into a single comparison table. UI route: `/combined-view`. Excel tab: "Messeabschluss". |
| Lager | Mobile warehouse (trailer) | `Ort.Lager` | The trailer used as a mobile warehouse at the exhibition. Sessions tagged with `Ort=Lager` show Soll/Fehlt columns. |
| Karton | Box / carton | EAN box | A full box of units, scanned as a single EAN |
| Konfiguration | Configuration | `/config` route | The setup screen for dispatch sheets and article catalogue |
| Ort | Location / place | `Ort` enum | Where a scan session takes place: `Stand` (booth) or `Lager` (trailer). Controls which columns are shown and whether a Beladeliste is required. |
| Stand | Exhibition stand | `Ort.Stand` | The exhibition booth where goods from the truck are displayed. Stand inventory sessions do not require a Beladeliste. |
| Neue Beladung starten | Start new loading | `startNewScanSession` | UI action that creates a new `ProcessDispatchList` session |
| Scanvorgang | Scan session | `ScanSession` | One active session of scanning barcodes |
| Sollbestand | Required stock / target count | `RequiredCount`, `requiredCount` | Number of units expected to be loaded |
| Soll | Required / target | `requiredCount` (Excel column) | Short form of Sollbestand used in UI and Excel columns |
| Starten | Start | — | Button label to confirm session creation |
| ✓ / ✗ | Confirm / Cancel | — | Inline edit confirm/cancel buttons in the config screen |

## Session types

| German label | English | `SessionType` enum value |
|---|---|---|
| Beladung / Beladeliste | Dispatch / Loading | `ProcessDispatchList` |
| Bestandsaufnahme Stand | Inventory at Stand | `Inventory` + `Ort.Stand` |
| Bestandsaufnahme Lager | Inventory at Lager | `Inventory` + `Ort.Lager` |

## Ort (session location)

| Value | German | When used |
|---|---|---|
| `Stand` (0) | Stand | Exhibition booth; free-form inventory, no Beladeliste |
| `Lager` (1) | Lager | Trailer/warehouse; Beladeliste required for Inventory; always used for ProcessDispatchList |

## Excel column names

| Column (DE) | Column (EN) | Field |
|---|---|---|
| Art.Nr. | Article No. | `articleNr` |
| Artikel | Article name | `articleDisplayName` |
| Gewicht | Weight (g) | `unitWeight` |
| EAN | EAN barcode | `ean` |
| Bestand | Actual count | `count` |
| Soll | Required count | `requiredCount` |
| Fehlt | Missing | `requiredCount − count` (only when negative) |
| Stand Ist | Stand actual | `countStand` (combined export only) |
| Lager Ist | Lager actual | `countAnhaenger` (combined export only) |
| Gesamt | Total | `total` (combined export only) |
