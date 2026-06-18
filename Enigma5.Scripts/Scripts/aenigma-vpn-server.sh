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
 
SERVICE_USER="openvpn"
OPENVPN_DIRECTORY="/etc/openvpn"
EASYRSA_DIRECTORY="$OPENVPN_DIRECTORY/easy-rsa"
EASYRSA="$EASYRSA_DIRECTORY/easyrsa"
EASYRSA_PKI="$EASYRSA_DIRECTORY/pki"
export EASYRSA_PKI
 
show_help() {
    echo "Usage: $0 -d DOMAIN -p PORT -a ADDRESS -n NETMASK"
    echo ""
    echo "Options:"
    echo "  -d DOMAIN       The domain name for the server (e.g., example.com)"
    echo "  -p PORT         VPN port (e.g., 1194)"
    echo "  -a ADDRESS      VPN network address (e.g., 10.8.0.0)"
    echo "  -n NETMASK      VPN network netmask (e.g., 255.255.255.0)"
    echo ""
    echo "Example:"
    echo "  sudo $0 -d example.com -p 1194 -a 10.8.0.0 -n 255.255.255.0"
    exit 1
}
 
[[ $EUID -ne 0 ]] && { echo "ERROR: Run as root: sudo bash $0"; exit 1; }
 
if [ "$#" -lt 8 ]; then
    show_help
fi
 
while getopts "d:p:a:n:h" opt; do
    case $opt in
        d) DOMAIN=$OPTARG ;;
        p) PORT=$OPTARG ;;
        a) ADDRESS=$OPTARG ;;
        n) NETMASK=$OPTARG ;;
        h) show_help ;;
        *) show_help ;;
    esac
done
 
if [[ ! -v DOMAIN || ! -v PORT || ! -v ADDRESS || ! -v NETMASK ]]; then
    echo "Error: DOMAIN, PORT, ADDRESS, NETMASK are required."
    show_help
fi
 
LOG_DIRECTORY="/var/log/openvpn/$DOMAIN"
CLIENT_CONFIGS_DIRECTORY="$OPENVPN_DIRECTORY/ccd/$DOMAIN"
SERVER_DIRECTORY="$OPENVPN_DIRECTORY/$DOMAIN"
 
apt-get update -qq
apt-get install -y openvpn easy-rsa
 
# Create unprivileged openvpn user/group if they don't exist
if ! id "$SERVICE_USER" &>/dev/null; then
    echo "Creating user '$SERVICE_USER' for the service..."
    useradd --system --no-create-home --shell /usr/sbin/nologin "$SERVICE_USER"
fi
 
# Create required directories
mkdir -p "$SERVER_DIRECTORY"
mkdir -p "$CLIENT_CONFIGS_DIRECTORY"
mkdir -p "$LOG_DIRECTORY"
 
# First run: initialise PKI, build CA and generate DH params
if [[ ! -f "${EASYRSA_PKI}/ca.crt" ]]; then
    make-cadir "$EASYRSA_DIRECTORY"
    "$EASYRSA" --batch init-pki
    "$EASYRSA" --batch build-ca nopass
    "$EASYRSA" --batch gen-dh
    cp -v "${EASYRSA_PKI}/ca.crt" "$OPENVPN_DIRECTORY/"
    cp -v "${EASYRSA_PKI}/dh.pem" "$OPENVPN_DIRECTORY/"
fi
 
"$EASYRSA" --batch gen-req "$DOMAIN" nopass
"$EASYRSA" --batch sign-req server "$DOMAIN"
 
cp -v "${EASYRSA_PKI}/issued/${DOMAIN}.crt"  "$SERVER_DIRECTORY/"
cp -v "${EASYRSA_PKI}/private/${DOMAIN}.key" "$SERVER_DIRECTORY/"
chown "$SERVICE_USER:$SERVICE_USER" "$SERVER_DIRECTORY/${DOMAIN}.key"
chmod 700 "$SERVER_DIRECTORY/${DOMAIN}.key"
 
openvpn --genkey secret "$SERVER_DIRECTORY/ta.key"
chown "$SERVICE_USER:$SERVICE_USER" "$SERVER_DIRECTORY/ta.key"
chmod 700 "$SERVER_DIRECTORY/ta.key"
 
cat > "$OPENVPN_DIRECTORY/$DOMAIN.conf" << CONF
port $PORT
proto udp
dev tun
tun-mtu 1378
 
ca   $OPENVPN_DIRECTORY/ca.crt
cert $SERVER_DIRECTORY/$DOMAIN.crt
key  $SERVER_DIRECTORY/$DOMAIN.key
dh   $OPENVPN_DIRECTORY/dh.pem
 
topology subnet
 
server $ADDRESS $NETMASK
ifconfig-pool-persist $SERVER_DIRECTORY/ipp.txt
status $LOG_DIRECTORY/openvpn-status.log

client-config-dir $CLIENT_CONFIGS_DIRECTORY
client-to-client
 
keepalive 10 120
 
tls-auth $SERVER_DIRECTORY/ta.key 0
 
user $SERVICE_USER
group $SERVICE_USER
 
persist-key
persist-tun
 
explicit-exit-notify 1
verb 3
CONF
 
systemctl enable "openvpn@${DOMAIN}"
systemctl restart  "openvpn@${DOMAIN}"
 
echo ""
echo "Server setup complete."
echo "  Config file:      $OPENVPN_DIRECTORY/$DOMAIN.conf"
echo "  Cert/key/ta.key:  $SERVER_DIRECTORY/"
echo "  CCD directory:    $CLIENT_CONFIGS_DIRECTORY/"
echo "  Logs/status:      $LOG_DIRECTORY/"
echo "  Service:          openvpn@${DOMAIN}"
