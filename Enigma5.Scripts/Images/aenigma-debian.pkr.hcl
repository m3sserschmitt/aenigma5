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

packer {
  required_plugins {
    vagrant = {
      source  = "github.com/hashicorp/vagrant"
      version = "~> 1"
    }
  }
}

# Packer boots this existing box, runs our provisioners on top of it,
# then packages the result as a new .box file ready to ship to users.
source "vagrant" "aenigma-debian-virtualbox" {
  source_path = "bento/debian-13"
  provider    = "virtualbox"
  add_force   = true
  communicator = "ssh"
  output_dir = "Virtualbox"
}

# Build Aenigma image
build {
  name    = "aenigma-debian"
  sources = [ "source.vagrant.aenigma-debian-virtualbox" ]

  # Confirm base box is ready before provisioning
  provisioner "shell" {
    inline = ["echo 'Base box ready - starting aenigma provisioning'"]
  }

  provisioner "shell" {
    environment_vars = ["DEBIAN_FRONTEND=noninteractive"]
    execute_command  = "echo 'vagrant' | {{.Vars}} sudo -S -E bash '{{.Path}}'"
    scripts = [
      "Scripts/install.sh",
      "Scripts/cleanup.sh"
    ]
  }

  post-processor "shell-local" {
    inline = [
      "mv Virtualbox/package.box Virtualbox/aenigma-virtualbox.box"
    ]
  }
}
