# Firebase Auth TODO

This repo now has the first auth slice wired in:

- Firebase is the external sign-in adapter
- the session now lives fully in the browser via Firebase web auth persistence
- the header button opens a dialog
- Google and Apple are the two supported providers in code

The remaining work is mostly Firebase and Apple console setup.

## What Is Already In Place

- Client-side auth interfaces and services:
  - `Auth/AuthenticationContracts.cs`
  - `Auth/AuthenticationServices.cs`
  - `Auth/AuthenticationStartup.cs`
- Header/account dialog UI is in:
  - `Pages/Home.razor`
- Browser sign-in flow is in:
  - `wwwroot/js/nrdr-shell.js`
- Firebase config placeholders are in:
  - `wwwroot/appsettings.json`
  - `wwwroot/appsettings.Development.json`

This means the published output stays static-hostable. There is no Firebase Admin SDK or ASP.NET cookie session in the active runtime path anymore.

## 0. Install The Local WebAssembly Tooling

For local full builds and publishes of `nrdr`, install the .NET WebAssembly workload once:

```bash
dotnet workload install wasm-tools
```

The GitHub workflows now do this explicitly before the publish step as well.

## 1. Create Or Choose A Firebase Project

In Firebase Console:

1. Create a Firebase project or use an existing one.
2. Register a Web app for `nrdr`.
3. Copy the Firebase web config values.

You will need these values:

- `ApiKey`
- `AuthDomain`
- `ProjectId`
- `AppId`
- `MessagingSenderId`
- optional: `StorageBucket`
- optional: `MeasurementId`

## 2. Fill The Web App Config In Nrdr

Populate these settings for local development:

- `wwwroot/appsettings.Development.json`

Section:

```json
"Authentication": {
  "Firebase": {
    "WebApp": {
      "ApiKey": "",
      "AuthDomain": "",
      "ProjectId": "",
      "AppId": "",
      "MessagingSenderId": "",
      "StorageBucket": "",
      "MeasurementId": ""
    }
  }
}
```

Notes:

- The Firebase web config is not a secret in the same way as server credentials.
- Still keep environment-specific values out of committed production config where possible.

## 3. Enable Google Sign-In In Firebase

In Firebase Console:

1. Open `Authentication`.
2. Open `Sign-in method`.
3. Enable `Google`.
4. Save.

That is the simpler provider and should be tested first.

## 4. Configure Apple Sign-In

Apple is more involved.

Requirements:

- Apple Developer Program membership
- Sign in with Apple configured in Apple Developer
- Apple enabled as a provider in Firebase Authentication

At a high level:

1. In Apple Developer, configure Sign in with Apple for web.
2. In Firebase Console, enable `Apple`.
3. Enter the Apple-side values Firebase asks for.
4. Make sure the Firebase auth domain / redirect expectations match the Apple setup.

Important Apple-specific testing notes:

- test users need an Apple ID with two-factor auth enabled
- practical testing usually needs a user signed into iCloud on an Apple device/browser context

Do Google first, then Apple.

## 5. Check Authorized Domains

Firebase Authentication must allow the domain you serve `nrdr` from.

Check:

- `Authentication > Settings > Authorized domains`

For local development, verify `localhost` is present if you are serving from localhost.

Note:

- newer Firebase projects do not automatically include `localhost`

If you later serve from a custom domain, add that domain too.

## 6. Validate The Current Flow

Once Firebase config and provider setup are in place:

1. Run `nrdr`.
2. Click the login button in the header.
3. Confirm the dialog opens.
4. Try Google sign-in.
5. Confirm the header switches to the logged-in user state.
6. Reload and confirm Firebase restores the user session automatically.
7. Confirm sign-out clears the client-side session and returns to logged-out state.
8. Then test Apple.

## 7. Production Hardening Still To Do

These are not required for the first working version, but should be next:

- move Firebase config to the right environment-specific static configuration strategy
- decide whether to keep using the Firebase CDN in `nrdr-shell.js` or vendor/package the JS another way
- decide whether to mirror Firebase auth state into a backend later if the shell needs protected APIs
- add a real app user/profile store once the shell needs application data
- add account-linking behavior if the same email uses multiple providers
- decide whether to keep the default Firebase local persistence or switch to session-only persistence
- add return-url handling if login should bounce users back to a specific screen later

## 8. Interface Boundary To Preserve

If Firebase is swapped out later, these are the boundaries to keep stable:

- `IExternalAuthProviderCatalog`
- `IAuthSessionService`
- `IAuthUiConfigurationService`

That keeps the rest of `nrdr` from depending directly on Firebase.

## Suggested Order

1. Firebase project + web app
2. local web config
3. enable Google
4. verify Google end-to-end
5. configure Apple
6. verify Apple end-to-end
7. harden config + session handling
