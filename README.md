## Getting Started

These instructions will get you a copy of the project up and running on your local machine.

### Prerequisites

This project is intended for Linux operating systems. This document describes the
installation process for Debian/Ubuntu distro. You need `OpenSSL` library and `.NET10`
to be installed on your machine. Checkout [OpenSSL](https://www.openssl.org/) for
details about installation process or use the version provided by the package manager.
You can follow the instructions
[here](https://learn.microsoft.com/en-us/dotnet/core/install/linux-scripted-manual#scripted-install)
for .NET installation.

__Important note:__ *Relative paths are relative to the root of your local repository.*

### Building the source code

You need a copy of the source code. Clone the repository using git:

```bash
git clone https://github.com/m3sserschmitt/aenigma5.git --recursive
cd aenigma5
```

(__*Optional step*__)
This project make use of prebuilt native libraries required to facilitate cryptographic
operations. These are located into `./Enigma5.Crypto/runtimes` directory. Build
workflow for these libraries is automated by `build-libs.sh` script located into
`./Enigma5.Scripts`. So, if you want to rebuild the libraries by yourself:

```bash
cd ./Enigma5.Scripts

sudo apt-get update
sudo apt-get install openssl cmake g++ libssl-dev libkeyutils-dev

./build-libs.sh
cd ..
```

Now the application can be compiled and launched from `./Enigma5.App` directory.

```bash
cd ./Enigma5.App
dotnet build
dotnet run --no-build
```

Open you browser and try to access [http://localhost:8080/info](http://localhost:8080/info)
and [http://localhost:8081/dashboard](http://localhost:8081/dashboard). You should expect
`200 OK` for both of them.

## Configuration

### Dev encryption keys

(__*Optional step*__) This app requires a public/private key pair in order to run.
When the app starts for the first time, it will generate `public-key.pem`
and `private-key.pem` into `./Enigma5.App` directory. You can regenerate the keys
using `genkeys-dev.sh` script located into `./Enigma5.Scripts` directory:

```bash
cd ./Enigma5.Scripts
./genkeys-dev.sh
```

### App settings

Depending on your environment, you might want to change the default configuration
values within `./Enigma5.App/appsettings.json`. Let's walk through the file and
explain all sections.

```json
"ConnectionStrings": {
    "DbConnectionString": "data source=aenigmaDb.sqlite"
}
```

This section indicates the location of the database. By default the database will be
located into `./Enigma5.App` directory.

---


```json
"DbProvider": "Sqlite"
```

This value controls what db provider will be used to access the database. For now
only `Sqlite` is accepted.

---


```json
"Kestrel": {
    "EndPoints": {
      "Http": {
        "Url": "http://127.0.0.1:8080"
      },
      "HttpControl": {
        "Url": "http://127.0.0.1:8081"
      }
    }
  }
```

These two endpoints will be exposed by default. Here we can define other endpoints
and with the help of `HttpBlacklists` and `HubBlacklists` sections below we can
configure granular access to the [API](./API.md).

---

```json
"HttpBlacklists": [
    {
      "Endpoint": "http://127.0.0.1:8080",
      "Items": [
        {
          "Path": "/Dashboard",
          "Methods": [
            "GET"
          ]
        }
      ]
    }
  ]
```

As the name suggests, this section can be used to effectively block access to a specific
API path on a given endpoint. In this default configuration `GET /Dashboard` http
requests will be blocked on `http://127.0.0.1:8080`. Similarly we can define other rules
and control who can access what.

---

```json
"HubBlacklists": [
    {
      "Endpoint": "http://127.0.0.1:8080",
      "Items": [
        {
          "Methods": [
            "TriggerBroadcast"
          ]
        }
      ]
    }
  ]
```

Same blacklist behavior as explained in `HttpBlacklists` but this time applied to the
[SignalR API](./SIGNALR_API.md) hub methods.

---

```json
"OnionService": null
```

Can be used to make local onion service address visible to others accessing the API.
This property is useful when setting up federation as others might want to connect to your
local instance through TOR. For a single app instance running in isolation it is of no
use and can be null. When onion services are available this property should hold values like
`http://brave4u7jddbv7cyviptqjc7jusxh72uik7zt6adtckl5f4nwy2v72qd.onion`. `http` scheme is
required because it controls how other peers will connect to your local instance.

---

```json
"Socks5Proxy": "socks5://127.0.0.1:9050"
```

This property is used to configure local address for listening socks5 TOR proxy. This
property is mainly used in scenarios when configuring federation over TOR. The proxy will
be used to connect to other app instances through TOR network. When running a single
instance in isolation this property is of no use and can be null.

---

```json
"KeySource": "File"
```

This property controls the location from which the keys will be retrieved. Valid values
are at this point are `File` (local filesystem) and `Azure`
(to read the keys from Azure Key Vault).

---

```json
"PassphraseSource": "Dashboard"
```

This property controls the location from which the private key protection
passphrase (for passphrase protected private keys) will be retrieved. Valid
values are `Dashboard` ([http://localhost:8081/dashboard](http://localhost:8081/dashboard)
by default), `Azure` (Azure Key Vault), `Keyboard` (prompted in terminal).

---

```json
"AzureVaultUrl": null
```
Url of the Azure Key Vault containing the keys and/or protection passphrase. You have
to use Key Vault Secrets to store the data. Can be null when not using Azure Key
Vault to store secrets.

---

```json
"PrivateKeyPath": "private-key.pem"
```

Private key path on local filesystem (if using `"KeySource": "File"`) or Azure Key Vault
secret name (if using `"KeySource": "Azure"`). By default the private key will be retrieved
from `./Enigma5.App` directory.

```json
"PublicKeyPath": "public-key.pem"
```

Same as `PrivateKeyPath`.

---

```json
"PassphrasePath": null
```

When using Azure Key Vault to store secrets, this property can be used to specify the name
of Key Vault secret holding the private key protection passphrase. Otherwise it is of no
use and can be null.

```json
"PassphrasePersistence": "Persistent"
```

This property controls for how long the private key protection passphrase will persist in
memory. Valid values are `Ephemeral` (private key protection required at every restart) or
`Persistent` (passphrase will survive across restarts). When using `Persistent`, Kernel
Key Retention System will be used to cache the private key protection passphrase.

---

```json
"WebContentDirectory": "./"
```

This property controls where user uploaded web content via the [API](./API.md) will be saved. By default
all the files will be saved into `./Enigma5.App` directory.

---

```json
"MessageRetentionPeriod": "14.00:00:00"
```

Controls for how long the undelivered messages will be retained in database until
permanently deleted. Default is 14 days.

---

```json
"SentMessageRetentionPeriod": "00:00:00"
```

Controls for how long delivered messages will be retained in database until permanently
deleted. By default all delivered messages are scheduled for immediate removal.

---

```json
"SharedDataRetentionPeriod": "14.00:00:00"
```

Controls for how long shared data will be retained in database until permanently deleted.
Default is 14 days.

---

```json
"FilesRetentionPeriod": "03.00:00:00"
```

Controls for how long uploaded files will be retained until permanently deleted. Default is
3 days.

---

```json
"SharedDataMaxSize": 16384
```

Controls the maximum size in bytes that will be accepted for uploaded shared data.
default is 16KB.

---

```json
"SharedFileMaxSize": 67108864
```

Controls the maximum size in bytes that will be accepted for uploaded files. Default is
64 MB.

---

```json
"Hostname": null
```

Can be used to allow others to access the local [API](./API.md) through your custom domain.
This property is useful when setting up federation as others might want to connect to your
local instance. For a single app instance running in isolation it is of no
use and can be null. When using a custom domain and you want to make the local instance
available over internet, this property should hold values like
`https://your-custom-domain/`. `https` or `http` scheme is required in order to make clear
to others how they should connect to your local instance.

---

```json
"VertexLifetime": "00:30:00"
```

Controls for how long a node will be retained in local graph after its last update. When
setting up federation, every instance keeps a local ledger containing information about
every other node in the network. Every node in the network should also share their own
information with their neighbors. When a specific node stops sending information into
the network we should erase its info from the local ledger after a certain period
of inactivity. When running an isolated instance, this property is of no use. Default
is 30 minutes.

---

```json
"Network": {
    "DelayBetweenConnectionRetries": 3000
}
```

Controls the delay between peer connection retries.

---

```json
"Serilog": {
    "MinimumLevel": {
      "Default": "Debug",
      "Override": {
        "Microsoft": "Warning",
        "Microsoft.AspNetCore.Hosting.Diagnostics": "Error",
        "Microsoft.Hosting.Lifetime": "Warning"
      }
    },
    "WriteTo": [
      {
        "Name": "File",
        "Args": {
          "path": "logs/logs-.txt",
          "rollingInterval": "Day",
          "formatter": "Serilog.Formatting.Compact.CompactJsonFormatter,
          Serilog.Formatting.Compact"
        }
      }
    ]
  }
```

This application uses [Serilog](https://serilog.net/) for structured logging. By default,
application logs are captured at the `Debug` level, while noisy framework logs from ASP.NET
Core and other Microsoft libraries are limited to `Warning` (or `Error` for hosting
diagnostics) to keep output focused on what matters. Logs are written to disk in the
`./Enigma5.App/logs/` folder, with a new file created each day (e.g. `logs-20260902.txt`)
and formatted as compact JSON — making them easy to parse with log aggregation tools like
Seq, Elasticsearch, or Datadog.

---

**Important Note**: The Azure setup was tested only for Azure Virtual Machines with Managed Identities.

## License

[![License: GPL v3](https://img.shields.io/badge/License-GPLv3-blue.svg)](https://www.gnu.org/licenses/gpl-3.0)

This project is licensed under the GNU General Public License v3.0. See the [LICENSE](./LICENSE) file for details.

## Contact

You can report errors or suggest improvements at [contact@aenigma.ro](mailto:contact@aenigma.ro)

