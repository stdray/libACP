#!/usr/bin/env bash
set -euo pipefail

cd "$(dirname "${BASH_SOURCE[0]}")"

TARGET="Default"
CONFIGURATION="Release"
EXTRA_ARGS=()

for ARG in "$@"; do
	case "$ARG" in
		--target=*) TARGET="${ARG#*=}" ;;
		--configuration=*) CONFIGURATION="${ARG#*=}" ;;
		*) EXTRA_ARGS+=("$ARG") ;;
	esac
done

dotnet tool restore >/dev/null

dotnet run build.cs --target="$TARGET" --configuration="$CONFIGURATION" ${EXTRA_ARGS[@]+"${EXTRA_ARGS[@]}"}
