#!/usr/bin/env bash
set -euo pipefail
printf '%s\n' >&2 'PhoneBox Lite: automatic upstream installer disabled on this branch.'
printf '%s\n' >&2 'No downloaded binaries, automatic Homebrew installs, quarantine overrides or startup agents.'
printf '%s\n' >&2 'Review docs/LITE-SECURITY.md and build locally from source after security checks.'
exit 1
