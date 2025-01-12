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

set -e

# Function to display usage/help message
show_help() {
    echo "Usage: $0 -d DOMAIN -p APP_PORT"
    echo ""
    echo "Options:"
    echo "  -d DOMAIN     The domain name for the reverse proxy (e.g., example.com)"
    echo "  -p APP_PORT   The port on which the ASP.NET application is running (e.g., 5000)"
    echo ""
    echo "Example:"
    echo "  sudo $0 -d example.com -p 5000"
    exit 1
}

# Check if the script is run with sufficient arguments
if [ "$#" -lt 4 ]; then
    show_help
fi

# Parse command line arguments
while getopts "d:p:h" opt; do
    case $opt in
        d) DOMAIN=$OPTARG ;;
        p) APP_PORT=$OPTARG ;;
        h) show_help ;;
        *) show_help ;;
    esac
done

# Check if DOMAIN and APP_PORT are provided
if [ -z "$DOMAIN" ] || [ -z "$APP_PORT" ]; then
    echo "Error: Both domain and application port are required."
    show_help
fi

# Set Apache configuration path and log directory
APACHE_CONF="/etc/apache2/sites-available/aenigma.conf"
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
    RewriteRule /(.*)           ws://localhost:$APP_PORT/\$1 [P,L]
    RewriteCond %{HTTP:Upgrade} !=websocket [NC]
    RewriteRule /(.*)           http://localhost:$APP_PORT/\$1 [P,L]

    # Proxy for HTTP requests
    ProxyPass "/" "http://localhost:$APP_PORT/"
    ProxyPassReverse "/" "http://localhost:$APP_PORT/"

    # Proxy for WebSocket (SignalR) requests
    ProxyPass "/OnionRouting" "ws://localhost:$APP_PORT/OnionRouting"
    ProxyPassReverse "/OnionRouting" "ws://localhost:$APP_PORT/OnionRouting"

    ErrorLog ${LOG_DIR}/aenigma-error.log
    CustomLog ${LOG_DIR}/aenigma-access.log combined
</VirtualHost>
EOF
)

# Update and install Apache if not already installed
sudo apt update
sudo apt install -y apache2

# Enable Apache modules required for reverse proxying and WebSockets
sudo a2enmod rewrite
sudo a2enmod proxy
sudo a2enmod proxy_http
sudo a2enmod proxy_wstunnel

# Create Apache virtual host configuration
echo "Creating virtual host configuration for $DOMAIN..."
echo "$VIRTUAL_HOST_CONF" | sudo tee $APACHE_CONF

# Enable the new site configuration
sudo a2ensite aenigma.conf

# Disable the default Apache site, if necessary
sudo a2dissite 000-default.conf

# Restart Apache to apply changes
echo "Restarting Apache..."
sudo systemctl restart apache2

# Ensure Apache starts on boot
sudo systemctl enable apache2

# Allow Apache through the firewall (if UFW is installed)
if command -v ufw >/dev/null 2>&1; then
    echo "Configuring UFW to allow Apache traffic..."
    sudo ufw allow 'Apache Full'
fi

echo "Apache reverse proxy setup completed for $DOMAIN with app port $APP_PORT"
