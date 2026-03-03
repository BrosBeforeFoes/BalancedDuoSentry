#!/usr/bin/env bash
set -euo pipefail

CSPROJ="BalancedDuoSentry/BalancedDuoSentry.csproj"

# NuGet flat container endpoints (in priority order)
SOURCES=(
  "https://api.nuget.org/v3-flatcontainer"
  "https://nuget.bepinex.dev/v3/flatcontainer"
  "https://nuget.samboy.dev/v3/flatcontainer"
)

while IFS= read -r line; do
  pkg=$(echo "$line" | grep -oP '(?<=Include=")[^"]+')
  ver=$(echo "$line" | grep -oP '(?<=Version=")[^"]+')

  if [[ "$ver" == *"*"* ]]; then
    echo "Skipping  $pkg (wildcard version)"
    continue
  fi

  pkg_lower=$(echo "$pkg" | tr '[:upper:]' '[:lower:]')
  latest=""

  for src in "${SOURCES[@]}"; do
    result=$(curl -fsSL "${src}/${pkg_lower}/index.json" 2>/dev/null \
      | jq -r '[.versions[] | select(test("^[0-9]+\\.[0-9]+"))] | last // empty' 2>/dev/null || true)
    if [[ -n "$result" ]]; then
      latest="$result"
      break
    fi
  done

  if [[ -z "$latest" ]]; then
    echo "Not found  $pkg (no source returned a version)"
    continue
  fi

  if [[ "$latest" == "$ver" ]]; then
    echo "Up to date $pkg ($ver)"
  else
    echo "Updating   $pkg: $ver -> $latest"
    sed -i "s|Include=\"${pkg}\" Version=\"${ver}\"|Include=\"${pkg}\" Version=\"${latest}\"|" "$CSPROJ"
  fi
done < <(grep 'PackageReference Include=' "$CSPROJ")
