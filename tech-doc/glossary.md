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
| Bestandsaufnahme | Inventory / stock count | `SessionType.Inventory` | Free-form scan session without a predefined list |
| Einheit | Unit | `ArticleUnit` | A single packaged variant of an article (weight, EAN) |
| Fehlt | Missing | `Fehlt` (Excel column) | Required − actual (`Soll − Ist`); shown only when `Ist < Soll` |
| Gewicht | Weight | `Weight`, `unitWeight` | Weight of one unit in grams |
| Ist | Actual | `count` | Scanned quantity; short form used in UI columns |
| Istbestand | Actual stock | `count` | Same as Ist, used in longer-form labels |
| Karton | Box / carton | EAN box | A full box of units, scanned as a single EAN |
| Konfiguration | Configuration | `/config` route | The setup screen for dispatch sheets and article catalogue |
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
| Bestandsaufnahme | Inventory | `Inventory` |

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
