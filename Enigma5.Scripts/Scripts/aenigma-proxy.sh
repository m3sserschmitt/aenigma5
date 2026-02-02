#!/bin/bash

# Aenigma - Federal messaging system
# Copyright © 2024-2025 Romulus-Emanuel Ruja <romulus-emanuel.ruja@tutanota.com>

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

set -Eeuo pipefail

SERVICE_NAME="aenigma"

# Function to display usage/help message
show_help() {
    echo "Usage: $0 -d DOMAIN -l LOCAL_ADDRESS"
    echo ""
    echo "Options:"
    echo "  -d DOMAIN           The domain name for the reverse proxy (e.g., example.com)"
    echo "  -l LOCAL_ADDRESS    Local address on which the ASP.NET application is listening (e.g., 127.0.0.1:8080)"
    echo ""
    echo "Example:"
    echo "  sudo $0 -d example.com -l 127.0.0.1:8080"
    exit 1
}

if [[ $EUID -ne 0 ]]; then
    echo "Error: Please run the script as root."
    exit 1
fi

# Check if the script is run with sufficient arguments
if [ "$#" -lt 4 ]; then
    show_help
fi

# Parse command line arguments
while getopts "d:l:h" opt; do
    case $opt in
        d) DOMAIN=$OPTARG ;;
        l) LOCAL_ADDRESS=$OPTARG ;;
        h) show_help ;;
        *) show_help ;;
    esac
done

# Check if DOMAIN and APP_PORT are provided
if [[ ! -v DOMAIN || ! -v LOCAL_ADDRESS ]]; then
    echo "Error: Both domain and local application address are required."
    show_help
    exit 1
fi

# Set Apache configuration path and log directory
APACHE_CONF="/etc/apache2/sites-available/$SERVICE_NAME.conf"
LOG_DIR="/var/log/apache2"

# Define the virtual host configuration
VIRTUAL_HOST_CONF=$(cat <<EOF
<VirtualHost *:80>
    RewriteEngine On
    ProxyPreserveHost On
    ProxyRequests Off
    ServerName $DOMAIN

    # allow for upgrading to websockets
    RewriteEngine On
    RewriteCond %{HTTP:Upgrade} =websocket [NC]
    RewriteRule /(.*)           ws://$LOCAL_ADDRESS/\$1 [P,L]
    RewriteCond %{HTTP:Upgrade} !=websocket [NC]
    RewriteRule /(.*)           http://$LOCAL_ADDRESS/\$1 [P,L]

    # Proxy for HTTP requests
    ProxyPass "/" "http://$LOCAL_ADDRESS/"
    ProxyPassReverse "/" "http://$LOCAL_ADDRESS/"

    # Proxy for WebSocket (SignalR) requests
    ProxyPass "/OnionRouting" "ws://$LOCAL_ADDRESS/OnionRouting"
    ProxyPassReverse "/OnionRouting" "ws://$LOCAL_ADDRESS/OnionRouting"

    ErrorLog ${LOG_DIR}/$SERVICE_NAME-error.log
    CustomLog ${LOG_DIR}/$SERVICE_NAME-access.log combined
</VirtualHost>
EOF
)

# Update and install Apache if not already installed
apt update
apt install -y apache2

# Enable Apache modules required for reverse proxying and WebSockets
a2enmod rewrite
a2enmod proxy
a2enmod proxy_http
a2enmod proxy_wstunnel

# Create Apache virtual host configuration
echo "Creating virtual host configuration for $DOMAIN..."
echo "$VIRTUAL_HOST_CONF" | sudo tee $APACHE_CONF

# Enable the new site configuration
a2ensite $SERVICE_NAME.conf

# Disable the default Apache site, if necessary
a2dissite 000-default.conf

# Restart Apache to apply changes
echo "Restarting Apache..."
systemctl restart apache2

# Ensure Apache starts on boot
systemctl enable apache2

# Allow Apache through the firewall (if UFW is installed)
if command -v ufw >/dev/null 2>&1; then
    echo "Configuring UFW to allow Apache traffic..."
    ufw allow 'Apache Full'
fi

echo "Apache reverse proxy setup completed for $DOMAIN with local address $LOCAL_ADDRESS"

aenigma-config -p Hostname -v "http://$DOMAIN"
exit 0
