#!/usr/bin/env bash
set -euo pipefail

REPO="Game-Club-SSC/TIDE"
RUNNER_DIR="${HOME}/.github-actions/tide-agy-runner"

if [[ ! -d "$RUNNER_DIR" ]]; then
  echo "AGY self-hosted runner directory does not exist; nothing to remove."
  exit 0
fi

cd "$RUNNER_DIR"

pid_file="$RUNNER_DIR/.agy-runner.pid"
if [[ -f "$pid_file" ]]; then
  runner_pid="$(cat "$pid_file")"
  if kill -0 "$runner_pid" 2>/dev/null; then
    kill "$runner_pid" 2>/dev/null || true
    sleep 1
  fi
  rm -f "$pid_file"
fi

# Clean up an older service-based install if one exists.
if [[ -x ./svc.sh ]]; then
  ./svc.sh stop 2>/dev/null || true
  ./svc.sh uninstall 2>/dev/null || true
fi

if [[ -f .runner && -x ./config.sh ]]; then
  if command -v gh >/dev/null 2>&1 && gh auth status >/dev/null 2>&1; then
    token="$(gh api --method POST "repos/${REPO}/actions/runners/remove-token" --jq .token)"
    ./config.sh remove --unattended --token "$token" || true
  else
    echo "GitHub CLI is unavailable or unauthenticated, so the remote runner registration could not be removed automatically." >&2
    echo "Remove the runner from GitHub Settings > Actions > Runners if it remains listed." >&2
  fi
fi

cd "$HOME"
rm -rf "$RUNNER_DIR"

echo "AGY self-hosted runner removed from this Mac."
