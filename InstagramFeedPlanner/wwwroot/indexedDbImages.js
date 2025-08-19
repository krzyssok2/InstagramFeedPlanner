const DB_NAME = "ImageStore";
const DB_VERSION = 1;
const STORE_NAME = "images";

let dbPromise = null;

function openDb() {
    if (dbPromise) return dbPromise;

    dbPromise = new Promise((resolve, reject) => {
        const request = indexedDB.open(DB_NAME, DB_VERSION);

        request.onupgradeneeded = (event) => {
            const db = event.target.result;
            if (!db.objectStoreNames.contains(STORE_NAME)) {
                db.createObjectStore(STORE_NAME);
            }
        };

        request.onsuccess = () => resolve(request.result);
        request.onerror = () => reject(request.error);
    });

    return dbPromise;
}

async function hashImage(url) {
    const response = await fetch(url);
    const buffer = await response.arrayBuffer();
    const hashBuffer = await crypto.subtle.digest("SHA-256", buffer);
    const hashArray = Array.from(new Uint8Array(hashBuffer));
    return hashArray.map(b => b.toString(16).padStart(2, "0")).join("");
}

export async function saveImage(url) {
    const hash = await hashImage(url);
    const db = await openDb();

    // Always fetch blob BEFORE transaction
    const response = await fetch(url);
    const blob = await response.blob();

    return new Promise((resolve, reject) => {
        const tx = db.transaction(STORE_NAME, "readwrite");
        const store = tx.objectStore(STORE_NAME);

        const existing = store.get(hash);
        existing.onsuccess = () => {
            if (existing.result) {
                // already exists, no need to write
                resolve(hash);
            } else {
                store.put(blob, hash);
            }
        };

        tx.oncomplete = () => resolve(hash);
        tx.onerror = () => reject(tx.error);
    });
}

export async function getBlobUrl(hash) {
    const db = await openDb();

    return new Promise((resolve, reject) => {
        const tx = db.transaction(STORE_NAME, "readonly");
        const store = tx.objectStore(STORE_NAME);

        const req = store.get(hash);
        req.onsuccess = () => {
            const blob = req.result;
            if (blob) {
                const url = URL.createObjectURL(blob);
                resolve(url);
            } else {
                resolve(null);
            }
        };
        req.onerror = () => reject(req.error);
    });
}

export async function deleteImage(hash) {
    const db = await openDb();

    return new Promise((resolve, reject) => {
        const tx = db.transaction(STORE_NAME, "readwrite");
        const store = tx.objectStore(STORE_NAME);

        const req = store.delete(hash);

        req.onsuccess = () => resolve(true);
        req.onerror = () => reject(req.error);
    });
}

export async function getAllImages() {
    const db = await openDb();

    return new Promise((resolve, reject) => {
        const tx = db.transaction(STORE_NAME, "readonly");
        const store = tx.objectStore(STORE_NAME);

        const req = store.getAllKeys();
        const reqValues = store.getAll();

        let keys, values;

        req.onsuccess = () => {
            keys = req.result;
            if (values) finish();
        };
        reqValues.onsuccess = () => {
            values = reqValues.result;
            if (keys) finish();
        };

        const finish = () => {
            const result = keys.map((k, i) => ({
                key: k,
                url: URL.createObjectURL(values[i])
            }));
            resolve(result);
        };

        req.onerror = reqValues.onerror = () => reject(req.error ?? reqValues.error);
    });
}