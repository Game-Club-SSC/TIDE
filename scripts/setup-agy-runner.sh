#!/usr/bin/env bash
set -euo pipefail

REPO="Game-Club-SSC/TIDE"
RUNNER_NAME="tide-agy-mac"
RUNNER_LABEL="tide-agy"
RUNNER_DIR="${HOME}/.github-actions/tide-agy-runner"
RUNNER_URL="https://github.com/${REPO}"

need() {
  command -v "$1" >/dev/null 2>&1 || { echo "Missing required command: $1" >&2; exit 1; }
}

need gh
need curl
need tar

if ! gh auth status >/dev/null 2>&1; then
  echo "GitHub CLI is not authenticated. Run: gh auth login" >&2
  exit 1
fi

if [[ ! -x "${HOME}/.local/bin/agy" ]]; then
  echo "AGY CLI not found at ${HOME}/.local/bin/agy" >&2
  exit 1
fi

mkdir -p "$RUNNER_DIR"
cd "$RUNNER_DIR"

if [[ ! -x ./config.sh ]]; then
  arch="$(uname -m)"
  case "$arch" in
    arm64) asset_suffix="osx-arm64" ;;
    x86_64) asset_suffix="osx-x64" ;;
    *) echo "Unsupported Mac architecture: $arch" >&2; exit 1 ;;
  esac

  asset_url="$(gh api repos/actions/runner/releases/latest | jq -r --arg suffix "$asset_suffix" '.assets[] | select(.name | contains($suffix)) | select(.name | endswith(".tar.gz")) | .browser_download_url' | head -n 1)"
  if [[ -z "$asset_url" ]]; then
    echo "Could not find the latest GitHub Actions runner for $asset_suffix" >&2
    exit 1
  fi

  echo "Downloading GitHub Actions runner..."
  curl -fL "$asset_url" -o actions-runner.tar.gz
  tar xzf actions-runner.tar.gz
  rm -f actions-runner.tar.gz
fi

if [[ ! -f .runner ]]; then
  token="$(gh api --method POST "repos/${REPO}/actions/runners/registration-token" --jq .token)"
  ./config.sh \
    --unattended \
    --url "$RUNNER_URL" \
    --token "$token" \
    --name "$RUNNER_NAME" \
    --labels "$RUNNER_LABEL" \
    --work _work \
    --replace
else
  echo "Runner is already configured."
fi

# Install as a per-user LaunchAgent so it survives logout/restart without sudo.
if ./svc.sh status 2>/dev/null | grep -qiE 'started|running'; then
  echo "Runner service is already running."
else
  ./svc.sh install || true
  ./svc.sh start
fi

echo
echo "Runner installed and started: $RUNNER_NAME"
echo "Directory: $RUNNER_DIR"
echo "To completely undo this later, run from the TIDE repo:"
echo "  bash scripts/remove-agy-runner.sh"
