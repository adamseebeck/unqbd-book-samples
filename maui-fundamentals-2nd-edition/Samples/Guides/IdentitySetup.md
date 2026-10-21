# Identity integration worksheet

This guide completes the source setup for the optional Entra public-client adapter. The default app refuses the all-zero identifiers. No provider sign-in, tenant mutation, broker enrollment or protected API call is needed for the controlled renewal exercise.

## Choose one flow

The included adapter uses MSAL.NET 4.91.0 and the system browser, with no WithBroker call. It asks MSAL for a silent token first and falls back to interaction when MSAL requires it. MSAL owns PKCE, its cache and refresh protocol. User.Read is a demonstration Graph scope, not permission to a custom inventory API. For a custom API, obtain its intended audience and delegated scopes from its owner and keep server-side authorization separate.

Record the client ID, tenant, supported account type, public-client setting, exact redirect URI and intended scopes before editing the public constants. Register only an application you own and have permission to change. A native app never receives a confidential-client secret. Keep a callback-to-registration comparison in the target test record.

## Android system-browser callback

Platforms/Android/MsalActivity.cs derives from MSAL's BrowserTabActivity and declares VIEW, DEFAULT and BROWSABLE with host auth and the msal{ClientId} scheme. Its attribute reads the same constant as the adapter. MainActivity forwards OnActivityResult to AuthenticationContinuationHelper. The manifest includes custom-tab discovery. Inspect the merged manifest after the Android build: a source attribute alone does not establish the final intent filter.

Replace the public client ID only after configuring the matching mobile/desktop public-client redirect in your app registration. Verify the actual MSAL.NET system-browser mode; do not paste an Android Java broker redirect or signature-hash recipe into this nonbroker example. If your organization requires a broker, follow the current MSAL broker instructions and change registration, package/signature redirect, package visibility and continuation together. That is a separate integration, not an extra WithBroker line added to this sample.

## Apple callback and keychain

The supplied Apple Info.plist URL scheme is the matching all-zero msal placeholder. Replace it with the actual msal{ClientId} scheme at the same time as the public constant and registration. AppDelegate.OpenUrl forwards a nonbroker callback to MSAL. SceneDelegate.OpenUrlContexts forwards URLs when the .NET 11 scene lifecycle delivers them there. The scene's registered type and __MAUI_DEFAULT_SCENE_CONFIGURATION__ name must remain consistent with the plist.

MSAL's native token cache also needs the correct keychain access policy for the actual signed app. In an authorized signing rehearsal, configure the documented WithIosKeychainSecurityGroup value and matching keychain-access-groups entitlement with the real application identifier prefix. A normal local source build does not supply a signing team, profile or entitlement grant. Do not enable broker cache sharing or add a different application's group as a workaround for a keychain error. Mac Catalyst support must be checked against the exact MSAL target assets and Apple runtime, rather than inferred from an iOS API name.

The provided Mac Catalyst entitlements cover its sandbox, network, location and camera exercise. They do not claim a configured MSAL sharing group. A keychain entry template is:

```xml
<!-- Inert signing-time example; replace only for the authorized app/team. -->
<key>keychain-access-groups</key>
<array><string>$(AppIdentifierPrefix)YOUR_APPROVED_CACHE_GROUP</string></array>
```

The selected group must match the builder configuration and the signed entitlement. Treat that as an explicit iOS identity-integration prerequisite, not as a completed device setup in this companion code. The pinned package has no Mac Catalyst native adapter here: its real-MSAL path explicitly throws PlatformNotSupportedException, and its delegates do not call the iOS-only continuation type. Use the controlled renewal page on Mac Catalyst or design a separately supported provider integration.

## Windows system browser

The nonbroker adapter uses http://localhost. MSAL listens on a local loopback port for its own callback; this is a documented loopback redirect, not a general permission to send credentials over remote HTTP. Configure the matching mobile/desktop redirect and verify browser return, cancellation and local firewall behavior. A WAM/broker integration has a different setup and is not enabled here.

## Verify the actual boundary

On each configured target, test first consent, silent reacquisition, a canceled browser, a wrong registration/redirect, logout, process restart and unavailable secure storage. Inspect only safe error categories and expiry; never copy token text into a screenshot/log. SessionService intentionally duplicates an access token only to teach a vault boundary. A production MSAL client should use its supported cache rather than treating this duplicate as a refresh-token store.

The separate RenewalPracticePage uses a fixed local value and an adjustable exercise clock. It shows expiry, delays renewal for cancellation testing and simulates failure/cooldown. It does not call MSAL or prove an OAuth callback. BearerSessionHandler is a separate restricted-origin teaching handler; an actual MSAL integration supplies a silent acquisition delegate and a transport with redirects disabled. Never blindly replay a write after a 401.

Primary references: [MSAL and MAUI](https://learn.microsoft.com/entra/msal/dotnet/acquiring-tokens/desktop-mobile/mobile-applications), [MAUI authentication](https://learn.microsoft.com/dotnet/maui/data-cloud/authentication), [MSAL browsers](https://learn.microsoft.com/entra/msal/dotnet/acquiring-tokens/using-web-browsers), [native OAuth](https://www.rfc-editor.org/rfc/rfc8252), [PKCE](https://www.rfc-editor.org/rfc/rfc7636).

## WebAuthenticator is a different boundary

WebAuthenticator.Default.AuthenticateAsync starts a supported browser/callback operation and returns properties. It does not turn an arbitrary callback into a validated token. A provider-specific implementation still needs per-attempt correlation/state, PKCE where applicable, exact redirect validation, trusted issuer/audience/nonce rules and a token exchange over TLS. If a provider needs a confidential secret, that part belongs to an appropriately protected backend. Prefer a maintained provider library for the complete protocol.

A WebAuthenticator application also needs its own Android WebAuthenticatorCallbackActivity/intent filter or Apple callback scheme/continuation. Do not substitute it for the supplied MSAL BrowserTabActivity. Check current Windows support before selecting it; a portable-looking API is not proof of every target implementation. The book's runnable default is the controlled exercise, while the optional real Entra adapter has the explicit target prerequisites above.
