#!/bin/bash

# Aenigma - Federated messaging system
# Copyright © 2024-2026 Romulus-Emanuel Ruja <romulus-emanuel.ruja@tutanota.com>

# This file is part of Aenigma project.

# Aenigma is free software: you can redistribute it and/or modify
# it under the terms of the GNU General Public License as published by
# the Free Software Foundation, either version 3 of the License, or
# (at your option) any later version.

# Aenigma is distributed in the hope that it will be useful,
# but WITHOUT ANY WARRANTY; without even the implied warranty of
# MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
# GNU General Public License for more details.

# You should have received a copy of the GNU General Public License
# along with Aenigma.  If not, see <https://www.gnu.org/licenses/>.
# Displayed on every login (console + SSH).
# Shows aenigma service status, Tor status, and configured onion services.

set -Eeuo pipefail

# ── ANSI colors ───────────────────────────────────────────────────────────
RESET="\e[0m"
BOLD="\e[1m"
GREEN="\e[32m"
RED="\e[31m"
YELLOW="\e[33m"
CYAN="\e[36m"
WHITE="\e[97m"
DIM="\e[2m"

TOR_DIR="/var/lib/tor"
TORRC_FILE="/etc/tor/torrc"

service_status() {
    local name="$1"
    if systemctl is-active --quiet "$name" 2>/dev/null; then
        echo -e "${GREEN}● running${RESET}"
    else
        echo -e "${RED}● stopped${RESET}"
    fi
}

# Given a HiddenServiceDir path, find the matching HiddenServicePort
# line(s) in /etc/tor/torrc. A torrc block looks like:
#
#   HiddenServiceDir /var/lib/tor/myservice
#   HiddenServicePort 80 127.0.0.1:8080
#
# We scan the file, track which HiddenServiceDir block we're in, and
# collect all HiddenServicePort values for the matching block.
get_ports_for_service() {
    local service_dir="$1"
    local in_block=0
    local ports=()

    while IFS= read -r line; do
        # Strip leading whitespace
        line="${line#"${line%%[![:space:]]*}"}"

        if [[ "$line" =~ ^HiddenServiceDir[[:space:]]+(.+)$ ]]; then
            local dir="${BASH_REMATCH[1]}"
            # Normalise trailing slash for comparison
            dir="${dir%/}"
            local target="${service_dir%/}"
            if [[ "$dir" == "$target" ]]; then
                in_block=1
            else
                in_block=0
            fi
        elif [[ $in_block -eq 1 && "$line" =~ ^HiddenServicePort[[:space:]]+([^[:space:]]+) ]]; then
            ports+=("${BASH_REMATCH[1]}")
        elif [[ $in_block -eq 1 && "$line" =~ ^HiddenServiceDir ]]; then
            # Hit the next service block — stop
            break
        fi
    done < "$TORRC_FILE"

    if [[ ${#ports[@]} -gt 0 ]]; then
        # Return space-separated list of ports
        echo "${ports[*]}"
    else
        echo "unknown port"
    fi
}

get_tor_services_info() {
    found_any=0

    if [[ -d "$TOR_DIR" ]]; then
        for service_path in "$TOR_DIR"/*/; do
            # Skip if glob didn't match anything
            [[ -d "$service_path" ]] || continue

            hostname_file="${service_path}hostname"
            service_name="$(basename "$service_path")"

            # Each subdirectory of /var/lib/tor is a hidden service —
            # but skip any that don't have a hostname file yet (service
            # not yet started/configured by Tor)
            if [[ ! -f "$hostname_file" ]]; then
                continue
            fi

            onion_address="$(cat "$hostname_file" 2>/dev/null | tr -d '[:space:]')"

            if [[ -z "$onion_address" ]]; then
                echo -e "${YELLOW}service: ${service_name}${RESET}"
                echo -e "${DIM}hostname file is empty${RESET}"
                echo -e ""
                found_any=1
                continue
            fi

            # Get port(s) from torrc
            ports="$(get_ports_for_service "${service_path}")"

            echo -e "${BOLD}${WHITE}service: ${service_name}${RESET}"
            echo -e "${CYAN}address: ${onion_address}${RESET}"

            # There may be multiple HiddenServicePort entries for one service
            for port in $ports; do
                echo -e "${DIM}port: ${port}${RESET}"
            done

            echo -e ""
            found_any=1
        done
    fi

    if [[ $found_any -eq 0 ]]; then
        echo -e "${DIM}No onion services configured yet${RESET}"
        echo -e ""
    fi
}

[[ $EUID -ne 0 ]] && { echo "ERROR: Run as root: sudo bash $0"; exit 1; }

# ── Header ────────────────────────────────────────────────────────────────
echo -e ""
echo -e "${BOLD}${CYAN}╔══════════════════════════════════════════════╗${RESET}"
echo -e "${BOLD}${CYAN}║           aenigma system status              ║${RESET}"
echo -e "${BOLD}${CYAN}╚══════════════════════════════════════════════╝${RESET}"
echo -e ""

# ── Service status ────────────────────────────────────────────────────────
echo -e "${BOLD}${WHITE}aenigma${RESET}   $(service_status aenigma)"
echo -e "${BOLD}${WHITE}tor${RESET}       $(service_status tor)"
echo -e ""

# ── Onion services ────────────────────────────────────────────────────────
echo -e "${BOLD}${CYAN}Onion Services${RESET}"
echo -e "${DIM}──────────────────────────────────────────────${RESET}"
get_tor_services_info

echo -e "${DIM}──────────────────────────────────────────────${RESET}"
echo -e ""

echo -e "Use ${RED}aenigma-tor-get-auth${RESET} command to get access key for listed services (if any authentication configured)."
