/*
    Aenigma - Federal messaging system
    Copyright © 2024-2025 Romulus-Emanuel Ruja <romulus-emanuel.ruja@tutanota.com>

    This file is part of Aenigma project.

    Aenigma is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    Aenigma is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with Aenigma.  If not, see <https://www.gnu.org/licenses/>.
*/

using System.Reflection;
using System.Runtime.InteropServices;

namespace Enigma5.App.Common.Utils;

public static class RuntimeHelpers
{
    public static string? GetRuntimeIdentifier()
    {
        string? osPart = null;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) osPart = "win";
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) osPart = "linux";
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) osPart = "osx";

        if(string.IsNullOrWhiteSpace(osPart))
        {
            return null;
        }

        string? archPart = RuntimeInformation.ProcessArchitecture switch
        {
            Architecture.X86 => "x86",
            Architecture.X64 => "x64",
            Architecture.Arm => "arm",
            Architecture.Arm64 => "arm64",
            _ => null
        };

        if(string.IsNullOrWhiteSpace(archPart))
        {
            return null;
        }

        return $"{osPart}-{archPart}";
    }

    public static string? ResolveNativeLibraryPath(string libraryName)
    {
        var runtimeIdentifier = GetRuntimeIdentifier();
        if(string.IsNullOrWhiteSpace(runtimeIdentifier))
        {
            return runtimeIdentifier;
        }

        var assemblyPath = Assembly.GetEntryAssembly()?.Location;
        if(string.IsNullOrWhiteSpace(assemblyPath))
        {
            return null;
        }

        var assemblyDirectory = Path.GetDirectoryName(assemblyPath);
        if(string.IsNullOrWhiteSpace(assemblyDirectory))
        {
            return null;
        }

        return Path.Combine(assemblyDirectory, string.Format(Constants.NativeLibsRelativePathTemplate, runtimeIdentifier, libraryName));
    }
}
