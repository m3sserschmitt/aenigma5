## Getting Started

These instructions will get you a copy of the project up and running on your local machine.

### Prerequisites

This project is intended for Linux operating systems. This document describes the installation process for Ubuntu distro. You need `OpenSSL` library and `.NET8` to be installed on your machine. Checkout [OpenSSL](https://www.openssl.org/) for details about installation process or use the version provided by the package manager. You can follow the instructions [here](https://learn.microsoft.com/en-us/dotnet/core/install/linux-scripted-manual#scripted-install) for .NET installation. You will also need `openssl-dev`, `libkeyutils-dev`, `cmake` and `g++` packages installed in order to compile the source code. Use this command to install them:

`sudo apt-get install cmake g++ openssl libssl-dev libkeyutils-dev`

### Building the source code

You need a copy of the source code. Clone the repository using git:

`git clone https://github.com/m3sserschmitt/aenigma5.git --recursive`

Change working directory to newly downloaded source code directory and then into the `./Libaenigma` library directory. The following commands will build the library:

`cmake -B./build`.

`cd build`

`make all`

Now you can leave `./Libaenigma/build` directory and go back to the root source code directory. Type:

`dotnet build`

then

`dotnet test`

and make sure that all test passed.

In order to generate the dev keys pair you can run the `./aenigma-genkeys-dev.sh` located into `/Scripts` directory. The keys will be generated into `./Enigma5.App` directory. To run the application, change directory to `Enigma5.App` directory and type: 

`./run-dev.sh`

This way you will have a running local dev instance of the app.

## Configuration

Depending on your environment, you might want to change the default configuration values within `./Enigma5.App/appsettings.*.json`. Inside `appsettings` files you will find the following properties:

- `ConnectionStrings`: this section indicates the location of the database.
- `Kestrel`: this section controls properties related to Kestrel.
- `UseAzureVaultForKeys`: true if the keys (i.e, public and private keys) will be retrieved from Azure Key Vault secrets. If false, the keys will be retrieved from local filesystem.
- `UseAzureVaultForPassphrase`: true if the private key protection passphrase shall be retrieved from Azure Key Vault secrets. If false, you will be prompted to enter the passphrase from keyboard.
- `AzureVaultUrl`: url of the Azure Key Vault containing the keys and/or protection passphrase. You have to use Key Vault Secrets to store the data. This property can be null if `UseAzureVaultForPassphrase` and `UseAzureVaultForKeys` are both false.
- `PrivateKeyPath`: if `UseAzureVaultForKeys` is true, this represents the name of the key vault secret containing the key. Otherwise, it represents the path on your local system where your private key is stored.
- `PublicKeyPath`: same as `PrivateKeyPath`
- `PassphrasePath`: name of the Azure Key Vault Secret containing the private key protection passphrase, when `UseAzureVaultForPassphrase` is true. Otherwise, it can be null.
- `Hostname`: The hostname to be published into the public ledger once you connect to the other instances of Aenigma. This will control how other nodes will connect to your instance. If null, nobody will be able to connect to you.
- `Serilog`: this section controls how Serilog will collect and store app logs. Please consult the official documentation [here](https://serilog.net/) for more details.

**Important Note**: The Azure setup was tested only for Azure Virtual Machines with Managed Identities.

## Debian package

The debian package available into this repository will install the app and and the `.NET Runtime` into `/usr/local/aenigma/`. Use `sudo dpkg -i aenigma_<version>.deb` to install the package. Make sure to replace `<version>` with the the actual version you want to install.

## Authors

* **Romulus-Emanuel Ruja** <<romulus-emanuel.ruja@tutanota.com>>

## License

This project is licensed under the GNU General Public License v3.0. See the [LICENSE](./LICENSE) file for details.
