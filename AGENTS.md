# Repository instructions for setup agents

Read `docs/AI-AGENT-SETUP.md` before installing or launching anything. The source checkout is not the runnable distribution; use the pinned GitHub Release ZIP and verify its exact SHA-256.

- Preserve the base client; install into a separate directory through the provided installer.
- Do not read, copy, modify, or commit `.zcode` content.
- Do not bypass version, hash, executable path, or runtime fingerprint checks.
- Treat UAC, download-page verification, and the lobby Enter step as explicit user handoffs. Do not auto-drive or start a race.
- Distinguish package checks, owned helper tests, server dry-runs, and actual native gameplay observations.
- Do not commit game installations, original client archives, profiles, logs, credentials, or large release assets. The distribution assets belong in Releases.
- `package/` mirrors the scripts shipped in Release v1.0.0; absent binary payloads are intentional. Do not tell users to launch that incomplete source tree.
