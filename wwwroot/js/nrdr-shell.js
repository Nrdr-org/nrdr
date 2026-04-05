const frameSelector = "[data-shell-game-frame]";
const firebaseSdkVersion = "12.11.0";

let firebaseSdkPromise;
let firebaseBundlePromise;
let firebaseBundleKey;

function findFrame() {
    return document.querySelector(frameSelector);
}

function normalizeAuthError(error) {
    switch (error?.code) {
        case "auth/popup-closed-by-user":
            return "Sign-in was canceled.";
        case "auth/cancelled-popup-request":
            return "A sign-in popup is already open.";
        case "auth/popup-blocked":
            return "The sign-in popup was blocked by the browser.";
        case "auth/email-already-in-use":
            return "An account with this email already exists.";
        case "auth/invalid-email":
            return "The email address is not valid.";
        case "auth/weak-password":
            return "The password is too weak. Use at least 6 characters.";
        case "auth/user-not-found":
        case "auth/wrong-password":
        case "auth/invalid-credential":
            return "Invalid email or password.";
        case "auth/too-many-requests":
            return "Too many attempts. Please try again later.";
        default:
            return error?.message || "Authentication failed.";
    }
}

function normalizeUser(user) {
    if (!user?.uid) {
        return null;
    }

    const provider =
        user.providerData?.find((item) => item?.providerId && item.providerId !== "firebase") ??
        user.providerData?.[0] ??
        null;

    return {
        userId: user.uid,
        displayName: user.displayName ?? "",
        email: user.email ?? "",
        providerId: provider?.providerId ?? "",
        avatarUrl: user.photoURL ?? ""
    };
}

function createProvider(bundle, providerId) {
    switch (providerId) {
        case "google.com": {
            const provider = new bundle.GoogleAuthProvider();
            provider.setCustomParameters({ prompt: "select_account" });
            return provider;
        }
        case "apple.com": {
            const provider = new bundle.OAuthProvider("apple.com");
            provider.addScope("email");
            provider.addScope("name");
            return provider;
        }
        default:
            throw new Error(`Unsupported provider: ${providerId}`);
    }
}

async function loadFirebaseSdk() {
    if (!firebaseSdkPromise) {
        firebaseSdkPromise = Promise.all([
            import(`https://www.gstatic.com/firebasejs/${firebaseSdkVersion}/firebase-app.js`),
            import(`https://www.gstatic.com/firebasejs/${firebaseSdkVersion}/firebase-auth.js`)
        ]).then(([appModule, authModule]) => ({
            initializeApp: appModule.initializeApp,
            getApp: appModule.getApp,
            getApps: appModule.getApps,
            getAuth: authModule.getAuth,
            GoogleAuthProvider: authModule.GoogleAuthProvider,
            OAuthProvider: authModule.OAuthProvider,
            browserLocalPersistence: authModule.browserLocalPersistence,
            onAuthStateChanged: authModule.onAuthStateChanged,
            setPersistence: authModule.setPersistence,
            signInWithPopup: authModule.signInWithPopup,
            signInWithEmailAndPassword: authModule.signInWithEmailAndPassword,
            createUserWithEmailAndPassword: authModule.createUserWithEmailAndPassword,
            sendPasswordResetEmail: authModule.sendPasswordResetEmail,
            signOut: authModule.signOut
        }));
    }

    return firebaseSdkPromise;
}

function getConfigKey(config) {
    return [
        config?.apiKey ?? "",
        config?.authDomain ?? "",
        config?.projectId ?? "",
        config?.appId ?? ""
    ].join("|");
}

async function createFirebaseBundle(config) {
    if (!config?.apiKey || !config.authDomain || !config.projectId || !config.appId) {
        throw new Error("Firebase web authentication is not configured.");
    }

    const sdk = await loadFirebaseSdk();
    const app = sdk.getApps().length > 0
        ? sdk.getApp()
        : sdk.initializeApp(config);
    const auth = sdk.getAuth(app);

    await sdk.setPersistence(auth, sdk.browserLocalPersistence);

    return { ...sdk, auth };
}

async function getFirebaseBundle(config) {
    const nextKey = getConfigKey(config);
    if (!firebaseBundlePromise || firebaseBundleKey !== nextKey) {
        firebaseBundleKey = nextKey;
        firebaseBundlePromise = createFirebaseBundle(config);
    }

    return firebaseBundlePromise;
}

function waitForInitialAuthState(bundle) {
    return new Promise((resolve, reject) => {
        const unsubscribe = bundle.onAuthStateChanged(
            bundle.auth,
            (user) => {
                unsubscribe();
                resolve(user);
            },
            (error) => {
                unsubscribe();
                reject(error);
            });
    });
}

export async function initializeFirebase(config) {
    try {
        const bundle = await getFirebaseBundle(config);
        const user = await waitForInitialAuthState(bundle);
        return normalizeUser(user);
    } catch (error) {
        throw new Error(normalizeAuthError(error));
    }
}

export async function signInWithProvider(config, providerId) {
    try {
        const bundle = await getFirebaseBundle(config);
        const provider = createProvider(bundle, providerId);
        const result = await bundle.signInWithPopup(bundle.auth, provider);
        return normalizeUser(result.user);
    } catch (error) {
        throw new Error(normalizeAuthError(error));
    }
}

export async function signInWithEmail(config, email, password) {
    try {
        const bundle = await getFirebaseBundle(config);
        const result = await bundle.signInWithEmailAndPassword(bundle.auth, email, password);
        return normalizeUser(result.user);
    } catch (error) {
        throw new Error(normalizeAuthError(error));
    }
}

export async function signUpWithEmail(config, email, password) {
    try {
        const bundle = await getFirebaseBundle(config);
        const result = await bundle.createUserWithEmailAndPassword(bundle.auth, email, password);
        return normalizeUser(result.user);
    } catch (error) {
        throw new Error(normalizeAuthError(error));
    }
}

export async function sendPasswordReset(config, email) {
    try {
        const bundle = await getFirebaseBundle(config);
        await bundle.sendPasswordResetEmail(bundle.auth, email);
    } catch (error) {
        throw new Error(normalizeAuthError(error));
    }
}

export async function signOutFromFirebase(config) {
    try {
        const bundle = await getFirebaseBundle(config);
        await bundle.signOut(bundle.auth);
    } catch (error) {
        throw new Error(normalizeAuthError(error));
    }
}

export function postShellAction(gameSlug, actionId, side) {
    const frame = findFrame();
    const payload = {
        type: "nrdr-action",
        actionId,
        side,
        gameSlug: gameSlug ?? frame?.dataset.gameSlug ?? null,
        triggeredAt: new Date().toISOString()
    };

    if (frame?.contentWindow) {
        frame.contentWindow.postMessage(payload, window.location.origin);
    }

    window.dispatchEvent(new CustomEvent("nrdr:action", { detail: payload }));
}
