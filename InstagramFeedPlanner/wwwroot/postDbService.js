let db = null;

export async function initDb(dbName, version) {
    return new Promise((resolve, reject) => {
        const request = indexedDB.open(dbName, version);

        request.onupgradeneeded = (event) => {
            db = event.target.result;

            if (!db.objectStoreNames.contains("feeds")) {
                db.createObjectStore("feeds", { keyPath: "id" });
            }

            if (!db.objectStoreNames.contains("posts")) {
                const postStore = db.createObjectStore("posts", { keyPath: "id" });
                postStore.createIndex("feedId", "feedId", { unique: false });
            }
        };

        request.onsuccess = (event) => {
            db = event.target.result;
            resolve();
        };

        request.onerror = (event) => {
            reject(event.target.error);
        };
    });
}

/* -------------------
   FEED FUNCTIONS
------------------- */
export async function addFeed(feed) {
    return new Promise((resolve, reject) => {
        const tx = db.transaction("feeds", "readwrite");
        const store = tx.objectStore("feeds");
        const request = store.add(feed);

        request.onsuccess = () => resolve();
        request.onerror = (e) => reject(e.target.error);
    });
}

export async function updateFeed(feed) {
    return new Promise((resolve, reject) => {
        const tx = db.transaction("feeds", "readwrite");
        const store = tx.objectStore("feeds");
        const request = store.put(feed);

        request.onsuccess = () => resolve();
        request.onerror = (e) => reject(e.target.error);
    });
}

export async function deleteFeed(id) {
    return new Promise((resolve, reject) => {
        const tx = db.transaction(["feeds", "posts"], "readwrite");

        const feedStore = tx.objectStore("feeds");
        const postStore = tx.objectStore("posts");
        const index = postStore.index("feedId");

        feedStore.delete(id);

        const cursorRequest = index.openCursor(IDBKeyRange.only(id));
        cursorRequest.onsuccess = (e) => {
            const cursor = e.target.result;
            if (cursor) {
                postStore.delete(cursor.primaryKey);
                cursor.continue();
            }
        };

        tx.oncomplete = () => resolve();
        tx.onerror = (e) => reject(e.target.error);
    });
}

export async function getFeed(id) {
    return new Promise((resolve, reject) => {
        const tx = db.transaction("feeds", "readonly");
        const store = tx.objectStore("feeds");
        const request = store.get(id);

        request.onsuccess = () => resolve(request.result || null);
        request.onerror = (e) => reject(e.target.error);
    });
}

export async function getAllFeeds() {
    return new Promise((resolve, reject) => {
        const tx = db.transaction("feeds", "readonly");
        const store = tx.objectStore("feeds");
        const request = store.getAll();

        request.onsuccess = () => resolve(request.result || []);
        request.onerror = (e) => reject(e.target.error);
    });
}

/* -------------------
   POST FUNCTIONS
------------------- */
export async function addPost(post) {
    return new Promise((resolve, reject) => {
        const tx = db.transaction("posts", "readwrite");
        const store = tx.objectStore("posts");
        const request = store.add(post);

        request.onsuccess = () => resolve();
        request.onerror = (e) => reject(e.target.error);
    });
}

export async function deletePost(id) {
    return new Promise((resolve, reject) => {
        const tx = db.transaction("posts", "readwrite");
        const store = tx.objectStore("posts");
        const request = store.delete(id);

        request.onsuccess = () => resolve();
        request.onerror = (e) => reject(e.target.error);
    });
}

export async function updateBatchPosts(posts) {
    return new Promise((resolve, reject) => {
        const tx = db.transaction("posts", "readwrite");
        const store = tx.objectStore("posts");

        posts.forEach(post => store.put(post));

        tx.oncomplete = () => resolve();
        tx.onerror = (e) => reject(e.target.error);
    });
}

export async function getPost(id) {
    return new Promise((resolve, reject) => {
        const tx = db.transaction("posts", "readonly");
        const store = tx.objectStore("posts");
        const request = store.get(id);

        request.onsuccess = () => resolve(request.result || null);
        request.onerror = (e) => reject(e.target.error);
    });
}

export async function getAllPosts() {
    return new Promise((resolve, reject) => {
        const tx = db.transaction("posts", "readonly");
        const store = tx.objectStore("posts");
        const request = store.getAll();

        request.onsuccess = () => resolve(request.result || []);
        request.onerror = (e) => reject(e.target.error);
    });
}

/* -------------------
   QUERY POSTS BY FEED
------------------- */
export async function getPostsByFeed(feedId) {
    return new Promise((resolve, reject) => {
        const tx = db.transaction("posts", "readonly");
        const store = tx.objectStore("posts");
        const index = store.index("feedId");

        const request = index.getAll(feedId);

        request.onsuccess = () => resolve(request.result || []);
        request.onerror = (e) => reject(e.target.error);
    });
}