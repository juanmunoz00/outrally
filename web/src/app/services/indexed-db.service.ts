import { Injectable } from '@angular/core';
import { RosterEntry } from '../models/medical-profile.model';

export interface CachedRoster {
  activityId: number;
  activityTitle: string;
  roster: RosterEntry[];
  savedAt: string; // ISO string
}

@Injectable({ providedIn: 'root' })
export class IndexedDbService {
  private readonly DB_NAME = 'outrally-offline';
  private readonly DB_VERSION = 1;
  private readonly STORE = 'emergency-rosters';

  private openDb(): Promise<IDBDatabase> {
    return new Promise((resolve, reject) => {
      const req = indexedDB.open(this.DB_NAME, this.DB_VERSION);
      req.onupgradeneeded = () => {
        req.result.createObjectStore(this.STORE, { keyPath: 'activityId' });
      };
      req.onsuccess = () => resolve(req.result);
      req.onerror = () => reject(req.error);
    });
  }

  async saveRoster(activityId: number, activityTitle: string, roster: RosterEntry[]): Promise<void> {
    const db = await this.openDb();
    return new Promise((resolve, reject) => {
      const tx = db.transaction(this.STORE, 'readwrite');
      const record: CachedRoster = { activityId, activityTitle, roster, savedAt: new Date().toISOString() };
      tx.objectStore(this.STORE).put(record);
      tx.oncomplete = () => { db.close(); resolve(); };
      tx.onerror = () => { db.close(); reject(tx.error); };
    });
  }

  async getRoster(activityId: number): Promise<CachedRoster | null> {
    const db = await this.openDb();
    return new Promise((resolve, reject) => {
      const req = db.transaction(this.STORE, 'readonly').objectStore(this.STORE).get(activityId);
      req.onsuccess = () => { db.close(); resolve(req.result ?? null); };
      req.onerror = () => { db.close(); reject(req.error); };
    });
  }

  async listRosters(): Promise<Omit<CachedRoster, 'roster'>[]> {
    const db = await this.openDb();
    return new Promise((resolve, reject) => {
      const req = db.transaction(this.STORE, 'readonly').objectStore(this.STORE).getAll();
      req.onsuccess = () => {
        db.close();
        resolve(req.result.map(({ activityId, activityTitle, savedAt }) => ({ activityId, activityTitle, savedAt })));
      };
      req.onerror = () => { db.close(); reject(req.error); };
    });
  }

  async deleteRoster(activityId: number): Promise<void> {
    const db = await this.openDb();
    return new Promise((resolve, reject) => {
      const tx = db.transaction(this.STORE, 'readwrite');
      tx.objectStore(this.STORE).delete(activityId);
      tx.oncomplete = () => { db.close(); resolve(); };
      tx.onerror = () => { db.close(); reject(tx.error); };
    });
  }
}
