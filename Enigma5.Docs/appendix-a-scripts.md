# Appendix A. Packaging, deployment and VM images

**Abstract.** The folder `Enigma5.Scripts` holds everything needed to turn the source code into
something an operator installs: helper scripts for development, two Debian packages, the
command-line tools those packages install, and the scripts that build ready-made virtual machine
images. This appendix describes what each part does, how an installed node is laid out on disk, how
the service is protected by systemd, and what happens when a virtual machine boots for the first
time. Each tool also has a manual page, which gives its options in detail.

## A.1 Contents of the folder

**Table A.1:** Contents of `Enigma5.Scripts`.

| Path | Content |
|---|---|
| `build-libs.sh` | Builds the native library (Section A.2). |
| `genkeys-dev.sh` | Creates a key pair for a development node (Section A.2). |
| `Packages/aenigma/` | The main Debian package: build script, configuration, systemd unit, tools, manual pages (Sections A.3 to A.5). |
| `Packages/aenigma-image-utils/` | A small package with systemd units used by the virtual machine images (Section A.6). |
| `Images/` | The image build: Packer template, provisioning scripts, Vagrant files, box metadata (Section A.7). |

Build output goes to `Packages/*/Deb/`, `Images/Virtualbox/` and `Images/Qemu/`, which git
ignores.

## A.2 Helper scripts for development

**`build-libs.sh`** runs the build scripts of the submodule `Libaenigma7` for the current machine
and for `arm64`, and copies both copies of `libaenigma.so` into `Enigma5.Crypto/runtimes/`
(Chapter 5).

**`genkeys-dev.sh`** creates an unencrypted 4096-bit RSA key pair for a development node with
`openssl`, in `Enigma5.App/`; this is the same size the node uses when it creates keys itself. It
finds that folder from its own location, so it can be started from any folder. A node creates missing keys by itself (Chapter 6), so the script is only needed to replace
them.

## A.3 The Debian package `aenigma`

### A.3.1 Building

**Listing A.1:** Building the package.

```bash
cd Enigma5.Scripts/Packages/aenigma
./package-aenigma.sh -v 5.1.0 -c debian -a amd64      # or -a arm64
```

The option `-c` names a folder under `Configs/` that holds the `appsettings.json` of the package;
`debian` is the only one at present. The script:

1. publishes `Enigma5.App` as a self-contained program for `linux-<arch>`, so the target machine
   needs no .NET installation;
2. lays out the package tree (Table A.2) and writes the Debian control files, among them the
   scripts that run before and after installation (`preinst`, `postinst`) and after removal
   (`postrm`);
3. builds the package with `dpkg-buildpackage` and checks it with `lintian`.

When the package is built, the native libraries of other processor types are removed from it. The
package depends on `openssl` (version 3.5 or newer), which the node uses to create keys and whose
library the native library uses, on `libkeyutils1`, which the native library needs for the kernel
keyring, and on `jq` and `basez`, which the tools use.

### A.3.2 Files on an installed node

**Table A.2:** Where an installed node keeps its files.

| Path | Content | Owner, mode |
|---|---|---|
| `/usr/lib/aenigma/` | The program, its libraries and `libaenigma.so` | root |
| `/usr/lib/aenigma/appsettings.json` | The default settings of the package; replaced on every upgrade | root |
| `/etc/aenigma/appsettings.json` | The settings of this node that differ from the defaults, passed to the node with `--config`; created by `postinst`, not part of the package | root, 0644 |
| `/usr/bin/aenigma-*` | The tools (Section A.5) | root |
| `/usr/lib/systemd/system/aenigma.service` | The systemd unit (Section A.4) | root |
| `/var/lib/aenigma/` | `private-key.pem`, `public-key.pem` | `aenigma`, 0700; key file 0600 |
| `/var/lib/aenigma/db/` | The database `aenigmaDb.sqlite` and its WAL files | `aenigma`, 0700 |
| `/srv/aenigma/uploads/` | Uploaded files (`WebContentDirectory`) | `aenigma`, 0700 |
| `/var/log/aenigma/` | Log files | `aenigma`, 0750; files 0600 |

The default settings of the package differ from the development configuration in these values:
absolute paths as in Table A.2, and the log level `Warning` (Chapter 18). `OnionService` is empty
until the onion service is set up (Section A.5).

The node reads the two settings files one after the other (Chapter 4): first the defaults, then
the file under `/etc`, whose values win. On a new installation the second file contains only `{}`.
After `aenigma-tor` has run, it contains the onion address and nothing else. An operator should
change settings only in the file under `/etc`, by hand or with `aenigma-config`; the defaults file
is overwritten by the next upgrade.

Lists need care. A list in the file under `/etc` is combined with the default list by position: its
items replace the default items at the same positions, and default items at higher positions still
apply. An operator who writes an own `HttpBlacklists` list must therefore check that the default
rules, such as the one for `/_blazor`, are still in effect (Chapter 14).

### A.3.3 Installing and removing

The script that runs after installation (`postinst`) creates the system user `aenigma` if it does
not exist, and creates the folders of Table A.2 with their owners and modes. The user has no home
folder (`/nonexistent`) and no login shell. The script also creates an empty `private-key.pem` with
mode 0600. The node treats an empty key file as missing and writes its new key into it (Chapter 6),
so the key file has the right owner and mode from the start. Last, it creates
`/etc/aenigma/appsettings.json` with the content `{}` if the file does not exist.

Debian's helper tools then enable and start the systemd unit. A new installation therefore runs a
node at once, with a new, unencrypted key; encrypting it is the operator's task (Section A.5).

The script that runs after removal (`postrm`) works in two steps:

- **remove** deletes the program folder `/usr/lib/aenigma` and keeps all data;
- **purge** also deletes the configuration, the keys, the database, the uploads, the logs and the
  user `aenigma`.

### A.3.4 Upgrading

An upgrade keeps all data and the file `/etc/aenigma/appsettings.json`. It replaces the program and
the defaults file, and systemd restarts the node. Because the package itself contains no file under
`/etc`, the package manager never has to ask which version of a configuration file to keep, and an
upgrade also completes when no terminal is attached, as in the daily update of the machine images
(Section A.6).

Versions before 5.1.0 were different: they shipped `/etc/aenigma/appsettings.json` as a full copy
of the defaults, marked as a configuration file of the package (a *conffile*). Once an operator or
a tool had changed that file, every upgrade that brought new defaults stopped with a question, and
failed when nobody could answer it. The upgrade from such a version to 5.1.0 or later therefore
hands the file over, in three steps:

1. Before the new files are unpacked, `preinst` keeps a copy of the old defaults, and Debian's
   helper (`dpkg-maintscript-helper rm_conffile`, set up through `debian/aenigma.maintscript`)
   moves the old file out of the way.
2. `postinst` compares the old file with the old defaults and writes only the settings that differ
   to the new `/etc/aenigma/appsettings.json`. Lists are compared as a whole.
3. The full old file is kept as `/etc/aenigma/appsettings.json.dpkg-bak` until the package is
   purged.

**Table A.2a:** The content of `/etc/aenigma/appsettings.json` after installing 5.1.0 or later.

| Situation before | Content afterwards |
|---|---|
| No earlier version | `{}` |
| Earlier version, file never changed | `{}` |
| Earlier version, file changed (for example by `aenigma-tor`) | Only the changed settings |
| Earlier version, file is not valid JSON, or the old defaults are missing | The old file, unchanged; `postinst` prints a warning |

A node of a version before 5.1.0 whose upgrade stopped at that question, and was left half
installed, is repaired with `dpkg --configure -a --force-confdef --force-confold`.

The version number in `preinst` and in the `rm_conffile` line (5.1.0) names the first release with
this behavior and must not be changed later. A file under `/etc/aenigma` must not be added to the
package again, because that would bring the question back.

## A.4 The systemd unit

The unit `aenigma.service` starts the node through `/usr/bin/aenigma-launcher`, which runs
`/usr/lib/aenigma/Enigma5.App --config /etc/aenigma/appsettings.json` in the foreground. systemd
restarts the node ten seconds after it stops for any reason.

**Table A.3:** Protective settings of `aenigma.service`.

| Setting | Effect |
|---|---|
| `User=aenigma`, `Group=aenigma` | The node runs without root rights. |
| `UMask=0077` | Files the node creates are readable only by the user `aenigma`. |
| `KeyringMode=private` | The service gets its own session keyring, separate from other services (Chapter 6). |
| `NoNewPrivileges=yes` | The node and its child processes (such as `openssl`) cannot gain rights. |
| `CapabilityBoundingSet=` and `AmbientCapabilities=` (empty) | No Linux capabilities at all. |
| `ProtectProc=invisible` | Processes of other users are hidden from the node. |
| `ProtectSystem=strict`, `ReadWritePaths=/var/lib/aenigma /var/log/aenigma /srv/aenigma` | The whole file system is read-only for the node, except its three data folders. It cannot change its own program or settings. |
| `ProtectHome=yes`, `PrivateTmp=yes`, `PrivateDevices=yes` | No access to home folders; an own `/tmp`; no physical devices. |
| `ProtectKernelTunables`, `ProtectKernelModules`, `ProtectKernelLogs`, `ProtectControlGroups`, `ProtectClock`, `ProtectHostname` (all `yes`) | The node cannot change kernel settings, the clock or the host name. |
| `RestrictAddressFamilies=AF_INET AF_INET6 AF_UNIX AF_NETLINK` | Only the socket types the node needs. |
| `RestrictNamespaces`, `RestrictRealtime`, `RestrictSUIDSGID`, `LockPersonality`, `RemoveIPC` (all `yes`) | Further kernel features the node does not need are closed. |
| `SystemCallArchitectures=native`, `SystemCallFilter=@system-service`, `SystemCallFilter=~@privileged` | Only the system calls of an ordinary service; a blocked call ends the node. |
| `LimitCORE=0` | No core dumps, which could contain the key or the passphrase. |
| `DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1` | .NET runs without the ICU library for language-specific data, on which the package does not depend. |

With these settings `systemd-analyze security aenigma` rates the unit at 1.4 ("OK"); without the
settings added in 5.1.0 it was 6.3 ("medium").

Two settings that such a list usually contains must stay out, because the .NET runtime does not
work with them: `SystemCallFilter=~@resources` (the runtime calls `sched_setaffinity`, and the node
is ended at start), and `MemoryDenyWriteExecute=yes` (the just-in-time compiler writes and then
runs code in memory). Any change to the unit should be followed by a start of the node and a look
at the kernel log for blocked calls (`journalctl -k | grep type=1326`).

The working folder is `/usr/lib/aenigma`. The node reads the defaults file from it; all paths in
the settings are absolute, so nothing else depends on it.

## A.5 Tools

The package installs the tools of Table A.4 to `/usr/bin`. Each has a manual page
(`man aenigma-tor` and so on). All of them, except `aenigma-launcher`, must be run as root.

**Table A.4:** Tools of the package.

| Tool | Purpose |
|---|---|
| **Service** | |
| `aenigma-launcher` | Starts the node with the package configuration; used by the systemd unit. |
| `aenigma-start` | Enables the unit for boot and restarts the node. |
| `aenigma-status` | Shows whether the node and Tor run, and every configured onion service with its address and ports. |
| `aenigma-config` | Sets one value in `/etc/aenigma/appsettings.json` with `jq`, for example `aenigma-config -p Hostname -v https://node.example.com`. The value `null` clears a setting. The file is created if it is missing. |
| `aenigma-update` | Installs the newest version of the package with `apt-get`. |
| **Keys** | |
| `aenigma-keys` | Creates a new 4096-bit key pair in `/var/lib/aenigma`, with the private key encrypted; asks for the passphrase. |
| `aenigma-lock-key` | Encrypts the existing private key with a passphrase. |
| `aenigma-unlock-key` | Removes the encryption from the private key. |
| **Tor** | |
| `aenigma-tor` | Installs Tor, adds an onion service for a local address to `/etc/tor/torrc`, waits for its address, and can write the address to the `OnionService` setting. |
| `aenigma-tor-auth` | Creates a client key for an onion service, so that only the holder of that key can reach it. |
| `aenigma-tor-get-auth` | Prints a client key created earlier, to be given to the user. |
| `aenigma-standard-setup` | One-time setup of a standard node (see below). |
| **Reverse proxy** | |
| `aenigma-proxy` | Installs Apache as a reverse proxy for a domain in front of the node, including the WebSocket connections of the hub. |
| **VPN** | |
| `aenigma-vpn-server` | Sets up an OpenVPN server instance with its own certificate authority. |
| `aenigma-vpn-server-client` | Issues a client certificate on the server. |
| `aenigma-vpn-server-host` | Gives a VPN client a fixed address and a host name. |
| `aenigma-vpn-dns` | Sets up `dnsmasq` to answer DNS queries inside the VPN. |
| `aenigma-vpn-client` | Configures and starts the VPN on a client machine. |

The three key tools change the key file in place, through a temporary file, so that a failed or
interrupted run leaves the old key intact. They ask for passphrases interactively and are not
meant for unattended use. A node that is running does not notice their changes until it needs its
key again; a node whose key was locked from outside closes its connections to peers at the next
run of the network bridge (Chapter 11).

`aenigma-standard-setup` prepares the setup a standard node uses:

1. creates the user `aenigma`, if it is missing;
2. creates an onion service `aenigma` for the public endpoint (`127.0.0.1:8080`) and writes its
   address to `OnionService`;
3. creates an onion service `aenigma-dashboard` for the control endpoint (`127.0.0.1:8081`), with
   client authorization for a user `aenigma-dashboard`, so that only the holder of that client key
   can open the dashboard;
4. creates the folders of Table A.2 and an empty key file, as `postinst` does;
5. restarts the node if anything changed.

Each step is skipped if it was done before, so the script can run at every boot. The client key for
the dashboard is printed with `aenigma-tor-get-auth -s aenigma-dashboard -u aenigma-dashboard`.

## A.6 The package `aenigma-image-utils`

This package holds systemd units that only the virtual machine images need. It depends on
`aenigma` 5.0.0 or newer, and its units are enabled when it is installed.

**Table A.5:** Units of `aenigma-image-utils`.

| Unit | When | What it does |
|---|---|---|
| `aenigma-standard-setup.service` | At every boot | Runs `aenigma-standard-setup` (Section A.5). |
| `regenerate-ssh-host-keys.service` | At boot, only if the SSH host keys are missing | Creates new SSH host keys, so that no two machines made from the same image share them. |
| `aenigma-auto-update.timer` and `.service` | Daily at 03:00, with a random delay of up to 30 minutes; a missed run is made up | Runs `aenigma-update`. |

## A.7 Virtual machine images

### A.7.1 Building

**Listing A.2:** Building the images.

```bash
cd Enigma5.Scripts/Images
./package-aenigma-virtualbox.sh      # Packer: Virtualbox/aenigma-virtualbox.box
./package-aenigma-qemu.sh            # converts it: Qemu/aenigma-qemu.box
./generate-metadata.sh 1.0.0         # updates metadata.json for the release
```

The Packer template `aenigma-debian.pkr.hcl` starts from the Vagrant box `bento/debian-13`, runs
two provisioning scripts in it, and saves the result as a new VirtualBox box.

- **`Scripts/install.sh`** adds the package repository `packages.aenigma.ro` with its signing key,
  installs `aenigma` and `aenigma-image-utils`, and adds `aenigma-status` to the login message,
  so that every login shows the state of the node and its onion addresses.
- **`Scripts/cleanup.sh`** removes everything that must be unique to each machine or that comes
  from the build: Tor and its onion service keys, the SSH host keys, the node's key pair, its
  database and logs, the settings `OnionService` and `Hostname`, and the machine ID. It then fills
  the free disk space with zeros, so that the image compresses well.

`package-aenigma-qemu.sh` converts the disk of the VirtualBox box to the `qcow2` format and packs
it as a box for the libvirt provider. `generate-metadata.sh` computes the checksums of both boxes
and adds a version entry to `metadata.json`, which points to the release files on GitHub; Vagrant
reads this file to find and check the box `m3sserschmitt/aenigma5`. Each provider has a sample
`Vagrantfile` under `Vagrantfiles/`.

### A.7.2 The first boot of a machine

Because the build removed every machine-specific item, each machine sets itself up when it first
starts:

1. `regenerate-ssh-host-keys.service` creates new SSH host keys.
2. The node starts and, finding no key, creates a new unencrypted key pair (Chapter 4).
3. `aenigma-standard-setup.service` installs Tor again, creates the two onion services with new
   addresses, and writes the public onion address to `OnionService`.
4. The node is restarted with that address, and from then on publishes it.

Every machine made from the image therefore has its own node key, its own onion addresses and its
own SSH host keys. The operator then reads the dashboard client key with `aenigma-tor-get-auth`,
encrypts the node key with `aenigma-lock-key` if wanted, and adds peers on the dashboard.

## A.8 Summary

`Enigma5.Scripts` builds a self-contained Debian package for `amd64` and `arm64`, which installs
the node as a systemd service running as an unprivileged user with no capabilities, its own
keyring and a file system that is read-only outside its data folders. Its data lives under
`/var/lib/aenigma`, `/srv/aenigma` and `/var/log/aenigma`, and `purge` removes all of it. The
package ships its default settings under `/usr/lib/aenigma`; the file under `/etc/aenigma` holds
only what the operator changed, so upgrades never ask about it. The package brings tools for the service, the keys, Tor onion services,
a reverse proxy and a VPN. A second package and a Packer build produce virtual machine images
which, on their first boot, create their own node key, SSH host keys and onion services, the
dashboard's protected by a client key, and update the node daily.
