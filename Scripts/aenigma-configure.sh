#!/bin/bash

set -e

CONFIG_FILE_PATH="/usr/local/aenigma/appsettings.Production.json"

# Default values
DbConnectionString="data source=aenigmaDb.sqlite"
HttpUrl="http://localhost:8080"
UseAzureVaultForKeys=false
UseAzureVaultForPassphrase=false
AzureVaultUrl="null"
PrivateKeyPath="/usr/local/etc/aenigma/private-key.pem"
PublicKeyPath="/usr/local/etc/aenigma/public-key.pem"
PassphrasePath="null"
Hostname="http://localhost:8080"
SerilogLevelDefault="Error"
SerilogLevelMicrosoft="Warning"
SerilogLevelDiagnostics="Error"
SerilogLevelLifetime="Warning"
LogFilePath="/var/log/aenigma/aenigma-logs-.txt"
RollingInterval="Day"

# Display help message
function display_help() {
    echo "Usage: $0 [options]"
    echo
    echo "Options:"
    echo "  -dbConnectionString     Database connection string (default: $DbConnectionString)"
    echo "  -httpUrl                HTTP URL for the application (default: $HttpUrl)"
    echo "  -useAzureVaultForKeys   Use Azure Vault for keys (default: $UseAzureVaultForKeys)"
    echo "  -useAzureVaultForPassphrase Use Azure Vault for passphrase (default: $UseAzureVaultForPassphrase)"
    echo "  -azureVaultUrl          Azure Vault URL (default: $AzureVaultUrl)"
    echo "  -privateKeyPath         Path to the private key file (default: $PrivateKeyPath)"
    echo "  -publicKeyPath          Path to the public key file (default: $PublicKeyPath)"
    echo "  -passphrasePath         Path to the passphrase file (default: $PassphrasePath)"
    echo "  -hostname               Hostname of the application (default: $Hostname)"
    echo "  -serilogLevelDefault    Default logging level for Serilog (default: $SerilogLevelDefault)"
    echo "  -serilogLevelMicrosoft  Logging level for Microsoft logs (default: $SerilogLevelMicrosoft)"
    echo "  -serilogLevelDiagnostics Logging level for ASP.NET diagnostics (default: $SerilogLevelDiagnostics)"
    echo "  -serilogLevelLifetime   Logging level for host lifetime logs (default: $SerilogLevelLifetime)"
    echo "  -logFilePath            Path to the log file (default: $LogFilePath)"
    echo "  -rollingInterval        Rolling interval for log files (default: $RollingInterval)"
    echo "  -h, --help              Display this help message"
    echo
    echo "Example:"
    echo "  $0 -dbConnectionString \"customDb.sqlite\" -hostname \"http://127.0.0.1:5000\""
    exit 0
}

# Parse named arguments
while [[ "$#" -gt 0 ]]; do
    case $1 in
        -h|--help) display_help ;;
        -dbConnectionString) DbConnectionString="$2"; shift ;;
        -httpUrl) HttpUrl="$2"; shift ;;
        -useAzureVaultForKeys) UseAzureVaultForKeys="$2"; shift ;;
        -useAzureVaultForPassphrase) UseAzureVaultForPassphrase="$2"; shift ;;
        -azureVaultUrl) AzureVaultUrl="$2"; shift ;;
        -privateKeyPath) PrivateKeyPath="$2"; shift ;;
        -publicKeyPath) PublicKeyPath="$2"; shift ;;
        -passphrasePath) PassphrasePath="$2"; shift ;;
        -hostname) Hostname="$2"; shift ;;
        -serilogLevelDefault) SerilogLevelDefault="$2"; shift ;;
        -serilogLevelMicrosoft) SerilogLevelMicrosoft="$2"; shift ;;
        -serilogLevelDiagnostics) SerilogLevelDiagnostics="$2"; shift ;;
        -serilogLevelLifetime) SerilogLevelLifetime="$2"; shift ;;
        -logFilePath) LogFilePath="$2"; shift ;;
        -rollingInterval) RollingInterval="$2"; shift ;;
        *) echo "Unknown parameter passed: $1"; exit 1 ;;
    esac
    shift
done

# Create the JSON content
cat <<EOF > $CONFIG_FILE_PATH
{
  "ConnectionStrings": {
    "DbConnectionString": "$DbConnectionString"
  },
  "Kestrel": {
    "EndPoints": {
      "Http": {
        "Url": "$HttpUrl"
      }
    }
  },
  "UseAzureVaultForKeys": $UseAzureVaultForKeys,
  "UseAzureVaultForPassphrase": $UseAzureVaultForPassphrase,
  "AzureVaultUrl": "$AzureVaultUrl",
  "PrivateKeyPath": "$PrivateKeyPath",
  "PublicKeyPath": "$PublicKeyPath",
  "PassphrasePath": "$PassphrasePath",
  "Hostname": "$Hostname",
  "Serilog": {
    "MinimumLevel": {
      "Default": "$SerilogLevelDefault",
      "Override": {
        "Microsoft": "$SerilogLevelMicrosoft",
        "Microsoft.AspNetCore.Hosting.Diagnostics": "$SerilogLevelDiagnostics",
        "Microsoft.Hosting.Lifetime": "$SerilogLevelLifetime"
      }
    },
    "WriteTo": [
      {
        "Name": "File",
        "Args": {
          "path": "$LogFilePath",
          "rollingInterval": "$RollingInterval",
          "formatter": "Serilog.Formatting.Compact.CompactJsonFormatter, Serilog.Formatting.Compact"
        }
      }
    ]
  }
}
EOF

echo "JSON configuration file $CONFIG_FILE_PATH created successfully."
