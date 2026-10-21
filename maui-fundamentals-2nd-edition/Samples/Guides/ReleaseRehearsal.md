# Prepare and inspect a target release

The Release templates are inert examples, not activated workflows or signing configuration. The companion code performs no signing, publish, store upload, MDM enrollment or schedule change.

## Establish identity and version inputs

Record the stable application ID, display version, increasing build number, target TFM/RID, source revision and SDK/workload/native-tool identities. update-version.ps1 deliberately accepts only three numeric components without leading zeroes, each 0 through 65535. That is a constrained demonstration contract, not full SemVer and not a guarantee that every store accepts the value. Keep logical prerelease/build labels in source/release records when a target requires a different numeric display string. Test the helper on a disposable copy before changing release inputs.

## Source validation first

source-validation.yml.example shows a manually dispatched, read-only-permission workflow that installs the Windows MAUI workload, runs portable tests and builds an ordinary Windows Release target. Replace the action placeholders with reviewed full commit SHAs before activating it. Pin the supported runner and native toolchain, and keep the TRX files and logs from the actual source revision. Add target jobs only on hosts that supply the required native tools. A queued or unstarted runner hasn't built anything.

For version preparation, copy the intended project into your own temporary directory, run the helper there and inspect the two XML values before applying the reviewed change to the source revision. Don't copy a versioned .csproj alone and expect its unresolved ProjectReference paths to build. The full source tree belongs to the build input. Never echo an environment or a credential store into the job log.

## Android signing and tracks

The app's owner creates and protects the upload and signing keys with the current Android tooling. Record the alias, public certificate fingerprint and recovery owner, and keep private keystore and password files outside source. AndroidSigning.props.example maps explicit protected paths to AndroidSigningKeyStore and file-based password inputs. Check the selected Android toolchain's supported property/password-file behavior before use. Run the AAB publish command as a separate, reviewed step after source and native setup; the template doesn't run it.

Inspect the resulting artifact with the official Android tools: verify application ID, version name/code, supported architectures, requested permissions and signing certificate. Install a suitable generated test artifact on an intended device and exercise data upgrade, offline/cancel paths and sign-in. An AAB file itself is not an installed app. Play internal testing is a distribution track with its own testers and approval; a store production rollout is another action. Use the current Play Console requirements for data safety, content rating, screenshots, test eligibility and signing ownership instead of assuming a static checklist remains current.

## Apple identities and channels

A real signing Mac must use the supported Xcode, team, application ID, certificate and provisioning profile. AppleSigning.props.example names those supplied inputs and an explicit ios-arm64 runtime. Verify the certificate/private-key availability in the signing keychain and that profile entitlements, app identity, team and intended channel agree. Ordinary compile success does not verify those grants.

Inspect the signed archive or export with Xcode Organizer and the native codesign/security tools. Check bundle ID, short/build versions, architectures, signed entitlements, embedded profile and expiration. A simulator build is not a device distribution package. Ad hoc distribution targets a profile's registered devices; TestFlight/App Store distribution uses Apple's separate service and review path; eligible enterprise/MDM distribution has organizational and platform restrictions. MDM is a delivery/management channel, not a way to bypass signing or store eligibility. This book creates none of these accounts, identities or packages.

Before submission, prepare the current App Store Connect metadata, privacy disclosures, export-compliance answers, screenshots and your testers or release choice. Upload, processing, review approval and public availability are separate steps, each with its own confirmation. Recheck the official requirements when you actually submit.

## Windows package preparation

Inventory.App sets WindowsPackageType=None for development. To prepare MSIX, use a separate reviewed packaging configuration with a certificate/publisher identity that matches the intended deployment channel. Verify the generated manifest, package identity, architectures, declared capabilities, display/build versions and signature. Test installation/update/uninstall on the intended Windows environment. An unpackaged executable or a compiled DLL is not a signed MSIX or a Microsoft Store submission.

## Keep a concrete artifact record

For every release package, record its full path, byte size, SHA-256, source revision, the exact command and log that produced it, identity and versions, architectures and permissions, the device test and the reviewer. Hash the artifact you actually inspected, not a similarly named earlier output. Track public certificate and profile identifiers and their expiration with expiry-record.csv.example; it's an offline worksheet, not a reminder or calendar. Never put key passwords or private keys in it.

Primary references: [Android CLI distribution](https://learn.microsoft.com/dotnet/maui/android/deployment/publish-cli), [Apple CLI distribution](https://learn.microsoft.com/dotnet/maui/ios/deployment/publish-cli), [Windows CLI distribution](https://learn.microsoft.com/dotnet/maui/windows/deployment/publish-cli), [Apple distribution](https://developer.apple.com/help/account/), [Play app publishing](https://support.google.com/googleplay/android-developer/).
