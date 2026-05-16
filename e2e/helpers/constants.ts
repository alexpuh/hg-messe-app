/**
 * EAN codes and identifiers for articles used in E2E test fixtures.
 * Sourced from e2e/fixtures/articles.json.
 *
 * Note: EanBox is not set by the current import pipeline (ArticlesService sets it to null).
 * Both TEST_EAN_UNIT and TEST_EAN_UNIT_SECONDARY are EanUnit values from distinct weight variants.
 */

/** EAN for "Anis ganz 50g" (unit ID 1, artNr 1100) — primary scan target */
export const TEST_EAN_UNIT = '4260011990035';

/** EAN for "Anis ganz 1000g" (unit ID 2, artNr 1100) — secondary scan target */
export const TEST_EAN_UNIT_SECONDARY = '4260011990042';

/** Article number for "Anis ganz" */
export const TEST_ARTICLE_NR = '1100';

/** Unit ID for "Anis ganz 50g" — used for Sollbestand setup */
export const TEST_UNIT_ID = 1;

/** Unit ID for "Anis ganz 1000g" */
export const TEST_UNIT_ID_SECONDARY = 2;

/** Required count set during dispatch sheet tests */
export const TEST_REQUIRED_COUNT = 5;

/** Base URL of the backend API */
export const API_BASE = 'http://localhost:5227';

/** Base URL of the Angular dev server */
export const APP_BASE = 'http://localhost:4200';
