# Prepare and inspect a target release

The Release templates are inert examples, not activated workflows or signing configuration. The companion code performs no signing, publish, store upload, MDM enrollment or schedule change.

## Establish identity and version inputs

Record the stable application ID, display version, increasing build number, target TFM/RID, source revision and SDK/workload/native-tool identities. update-version.ps1 deliberately accepts only three numeric components without leading zeroes, each 0 through 65535. That is a constrained demonstration contract, not full SemVer and not proof that every store accepts the value. Keep logical prerelease/build labels in source/release records when a target requires a different numeric display string. Test the helper on a disposable copy before changing release inputs.

## Source validation first

source-validation.yml.example shows a manually dispatched, read-only-permission workflow that installs the Windows MAUI workload, runs portable tests and builds an ordinary Windows Release target. Replace the action placeholders with reviewed full commit SHAs before activating it. Pin the supported runner/native toolchain and retain TRX/logs from the actual source revision. Add target jobs only on hosts that supply the required native tools. A queued or unstarted runner is no build evidence.

For version preparation, copy the intended project into an owned temporary directory, run the helper there and inspect the two XML values before applying the reviewed change to the source revision. Do not copy a versioned .csproj alone and then claim that its unresolved ProjectReference paths make a complete build. The full source tree belongs to the build input. Never echo an environment or a credential store into the job log.

## Android signing and tracks

An authorized owner creates and protects the upload/signing key material using the current Android tooling. Record the alias/public certificate fingerprint and recovery owner, with private keystore/password files kept outside source. AndroidSigning.props.example maps explicit protected paths to AndroidSigningKeyStore and file-based password inputs. Check the selected Android toolchain's supported property/password-file behavior before use. An AAB publish command belongs to a separate reviewed operation after source/native setup; the template does not execute it.

Inspect the resulting artifact with the official Android tools: verify application ID, version name/code, supported architectures, requested permissions and signing certificate. Install a suitable generated test artifact on an intended device and exercise data upgrade, offline/cancel paths and sign-in. An AAB file itself is not an installed app. Play internal testing is a distribution track with its own testers and approval; a store production rollout is another action. Use the current Play Console requirements for data safety, content rating, screenshots, test eligibility and signing ownership instead of assuming a static checklist remains current.

## Apple identities and channels

A real signing Mac must use the supported Xcode, team, application ID, certificate and provisioning profile. AppleSigning.props.example names those supplied inputs and an explicit ios-arm64 runtime. Verify the certificate/private-key availability in the authorized keychain and that profile entitlements, app identity, team and intended channel agree. Ordinary compile success does not verify those grants.

Inspect an authorized archive/export using Xcode Organizer and the native codesign/security tools. Check bundle ID, short/build versions, architectures, signed entitlements, embedded profile and expiration. A simulator build is not a device distribution package. Ad hoc distribution targets a profile's registered devices; TestFlight/App Store distribution uses Apple's separate service and review path; eligible enterprise/MDM distribution has organizational and platform restrictions. MDM is a delivery/management channel, not a way to bypass signing or store eligibility. This book creates none of these accounts, identities or packages.

Before submission, an authorized operator prepares the current App Store Connect metadata, privacy disclosures, export-compliance answers, screenshots and intended testers/release choice. Upload, processing, review approval and public availability have distinct receipts. Recheck current official requirements when that operation actually happens.

## Windows package preparation

Inventory.App sets WindowsPackageType=None for development. To prepare MSIX, use a separate reviewed packaging configuration with a certificate/publisher identity that matches the intended deployment channel. Verify the generated manifest, package identity, architectures, declared capabilities, display/build versions and signature. Test installation/update/uninstall on the intended Windows environment. An unpackaged executable or a compiled DLL is not a signed MSIX or a Microsoft Store submission.

## Keep a concrete artifact record

For every authorized package, record full path, byte size, SHA-256, source revision, exact producing command/log, identity/versions, architecture/permission readback, installed-device test and reviewer. Hash the artifact actually inspected, rather than a similarly named previous output. Track public certificate/profile identifiers and expiration with expiry-record.csv.example; it is an offline worksheet, not a created reminder or calendar. Never put key passwords or private keys in it.

Primary references: [Android CLI distribution](https://learn.microsoft.com/dotnet/maui/android/deployment/publish-cli), [Apple CLI distribution](https://learn.microsoft.com/dotnet/maui/ios/deployment/publish-cli), [Windows CLI distribution](https://learn.microsoft.com/dotnet/maui/windows/deployment/publish-cli), [Apple distribution](https://developer.apple.com/help/account/), [Play app publishing](https://support.google.com/googleplay/android-developer/).
