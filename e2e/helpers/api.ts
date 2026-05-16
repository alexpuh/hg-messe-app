import { APIRequestContext } from '@playwright/test';
import { API_BASE } from './constants';
import * as fs from 'fs';
import * as path from 'path';

/**
 * Uploads the articles fixture to the backend via multipart form upload.
 * Clears any previously imported articles and replaces them with the fixture set.
 */
export async function uploadArticles(request: APIRequestContext): Promise<void> {
  const fixturePath = path.resolve(__dirname, '..', 'fixtures', 'articles.json');
  const buffer = fs.readFileSync(fixturePath);

  const response = await request.post(`${API_BASE}/api/Articles/upload-articles`, {
    multipart: {
      fileName: 'articles.json',
      file: {
        name: 'articles.json',
        mimeType: 'application/json',
        buffer,
      },
    },
  });

  if (!response.ok()) {
    throw new Error(`uploadArticles failed: ${response.status()} ${await response.text()}`);
  }
}

/**
 * Creates a new dispatch sheet (Beladeliste) and returns its ID.
 */
export async function createDispatchSheet(request: APIRequestContext, name: string): Promise<number> {
  const response = await request.post(`${API_BASE}/api/DispatchSheets`, {
    data: { id: null, name },
  });

  if (!response.ok()) {
    throw new Error(`createDispatchSheet failed: ${response.status()} ${await response.text()}`);
  }

  const body = await response.json();
  return body.id as number;
}

/**
 * Sets the required count for a specific article unit on a dispatch sheet.
 */
export async function setRequiredUnits(
  request: APIRequestContext,
  dispatchSheetId: number,
  unitId: number,
  count: number,
): Promise<void> {
  const response = await request.post(
    `${API_BASE}/api/DispatchSheets/${dispatchSheetId}/required-units`,
    { data: { unitId, count } },
  );

  if (!response.ok()) {
    throw new Error(`setRequiredUnits failed: ${response.status()} ${await response.text()}`);
  }
}

/**
 * Creates a new scan session and returns its ID.
 * sessionType: 'ProcessDispatchList' | 'Inventory'
 * ort: 'Stand' | 'Lager'
 */
export async function createSession(
  request: APIRequestContext,
  sessionType: 'ProcessDispatchList' | 'Inventory',
  ort: 'Stand' | 'Lager',
  dispatchSheetId?: number,
): Promise<number> {
  const params = new URLSearchParams({ sessionType, ort });
  if (dispatchSheetId !== undefined) {
    params.append('dispatchSheetId', dispatchSheetId.toString());
  }

  const response = await request.post(`${API_BASE}/api/ScanSessions?${params.toString()}`);

  if (!response.ok()) {
    throw new Error(`createSession failed: ${response.status()} ${await response.text()}`);
  }

  const body = await response.json();
  return body.id as number;
}

/**
 * Simulates a barcode scan via the Development-only debug endpoint.
 * The scan targets the most recently updated session (same as the physical scanner).
 */
export async function simulateScan(request: APIRequestContext, ean: string): Promise<void> {
  const response = await request.post(
    `${API_BASE}/api/Debug/scan?ean=${encodeURIComponent(ean)}`,
  );

  if (!response.ok()) {
    throw new Error(`simulateScan failed: ${response.status()} ${await response.text()}`);
  }
}
