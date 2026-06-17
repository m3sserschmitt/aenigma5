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
    echo "Usage: $0 -d DOMAIN -i DNS_IP"
    echo ""
    echo "Options:"
    echo "  -d DOMAIN       The domain name of the server (e.g., example.com)"
    echo "  -i DNS_IP       VPN server IP to be pushed as DNS (e.g., 10.8.0.1)"
    echo ""
    echo "Example:"
    echo "  sudo $0 -d example.com -i 10.8.0.1"
    exit 1
}
 
[[ $EUID -ne 0 ]] && { echo "ERROR: Run as root: sudo bash $0"; exit 1; }
 
if [ "$#" -lt 4 ]; then
    show_help
fi
 
while getopts "d:i:h" opt; do
    case $opt in
        d) DOMAIN=$OPTARG ;;
        i) DNS_IP=$OPTARG ;;
        h) show_help ;;
        *) show_help ;;
    esac
done
 
if [[ ! -v DOMAIN || ! -v DNS_IP ]]; then
    echo "Error: DOMAIN and DNS_IP are required."
    show_help
fi
 
apt-get update -qq
apt-get install -y dnsmasq
 
# Point dnsmasq to our hosts file
cat > "/etc/dnsmasq.d/$DOMAIN.conf" << CONF
interface=tun0
bind-interfaces
addn-hosts=$DNSMASQ_HOSTS
CONF
 
# Create empty hosts file if it does not exist
touch "$DNSMASQ_HOSTS"
 
# Add push directive to server config if not already present
SERVER_CONF="$OPENVPN_DIRECTORY/$DOMAIN.conf"
if ! grep -q "dhcp-option DNS" "$SERVER_CONF"; then
    echo "push \"dhcp-option DNS $DNS_IP\"" >> "$SERVER_CONF"
fi
 
systemctl enable dnsmasq
systemctl restart dnsmasq
systemctl restart "openvpn@${DOMAIN}"
 
echo ""
echo "DNS setup complete."
echo "  dnsmasq config: /etc/dnsmasq.d/$DOMAIN.conf"
echo "  Hosts file:     $DNSMASQ_HOSTS"
echo "  Pushed DNS:     $DNS_IP"
