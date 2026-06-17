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

set -euo pipefail
 
OPENVPN_DIRECTORY="/etc/openvpn"
DNSMASQ_HOSTS="/etc/openvpn/dnsmasq-hosts"
 
show_help() {
    echo "Usage: $0 -c CLIENT_NAME -d DOMAIN -i IP -n NETMASK"
    echo ""
    echo "Options:"
    echo "  -c CLIENT_NAME  Name of the client (e.g., john)"
    echo "  -d DOMAIN       The domain name of the server (e.g., example.com)"
    echo "  -i IP           Static VPN IP to assign to the client (e.g., 10.8.0.10)"
    echo "  -n NETMASK      VPN network netmask (e.g., 255.255.255.0)"
    echo ""
    echo "Example:"
    echo "  sudo $0 -c john -d example.com -i 10.8.0.10 -n 255.255.255.0"
    exit 1
}
 
[[ $EUID -ne 0 ]] && { echo "ERROR: Run as root: sudo bash $0"; exit 1; }
 
if [ "$#" -lt 8 ]; then
    show_help
fi
 
while getopts "c:d:i:n:h" opt; do
    case $opt in
        c) CLIENT_NAME=$OPTARG ;;
        d) DOMAIN=$OPTARG ;;
        i) IP=$OPTARG ;;
        n) NETMASK=$OPTARG ;;
        h) show_help ;;
        *) show_help ;;
    esac
done
 
if [[ ! -v CLIENT_NAME || ! -v DOMAIN || ! -v IP || ! -v NETMASK ]]; then
    echo "Error: CLIENT_NAME, DOMAIN, IP and NETMASK are required."
    show_help
fi
 
CLIENT_CONFIGS_DIRECTORY="$OPENVPN_DIRECTORY/ccd/$DOMAIN"
HOSTNAME="$CLIENT_NAME.$DOMAIN"
 
[[ ! -d "$CLIENT_CONFIGS_DIRECTORY" ]] && { echo "ERROR: $CLIENT_CONFIGS_DIRECTORY not found. Make sure to setup vpn server first."; exit 1; }
 
# Update or create ccd file with static IP, preserving other directives
touch "$CLIENT_CONFIGS_DIRECTORY/$CLIENT_NAME"
sed -i '/^ifconfig-push/d' "$CLIENT_CONFIGS_DIRECTORY/$CLIENT_NAME"
echo "ifconfig-push $IP $NETMASK" >> "$CLIENT_CONFIGS_DIRECTORY/$CLIENT_NAME"
 
# Add hostname entry to dnsmasq hosts file if dnsmasq is configured
if [[ -f "$DNSMASQ_HOSTS" ]]; then
    if ! grep -q "$HOSTNAME" "$DNSMASQ_HOSTS"; then
        echo "$IP  $HOSTNAME" >> "$DNSMASQ_HOSTS"
    fi
    systemctl restart dnsmasq
else
    echo "Note: $DNSMASQ_HOSTS not found, skipping DNS hostname registration."
fi
 
echo ""
echo "Client host configured."
echo "  CCD file:    $CLIENT_CONFIGS_DIRECTORY/$CLIENT_NAME"
echo "  Static IP:   $IP"
echo "  Hostname:    $HOSTNAME"
echo "  Hosts file:  $DNSMASQ_HOSTS"
