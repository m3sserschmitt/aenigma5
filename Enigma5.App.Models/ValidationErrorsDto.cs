/*
    Aenigma - Federated messaging system
    Copyright © 2024-2026 Romulus-Emanuel Ruja <romulus-emanuel.ruja@tutanota.com>

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

namespace Enigma5.App.Models;

public static class ValidationErrorsDto
{
    public const string NULL_REQUIRED_PROPERTIES = "One or more required properties not provided.";

    public const string TOO_MANY_PAYLOADS = "Too many payloads for one request.";

    public const string PROPERTIES_NOT_IN_CORRECT_FORMAT = "One or more properties not in correct format.";

    public const string PROPERTIES_FORMAT_COULD_NOT_BE_VERIFIED = "One ore more properties format could not be verified due to insufficient/malformed information.";

    public const string INVALID_VALUE_FOR_PROPERTY = "One or more properties have invalid values.";
}
