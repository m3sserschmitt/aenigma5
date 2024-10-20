/*
    Aenigma - Federal messaging system
    Copyright (C) 2024  Romulus-Emanuel Ruja <romulus-emanuel.ruja@tutanota.com>

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

namespace Enigma5.App.Hubs;

public class InvocationErrors
{
    public static readonly string NONCE_GENERATION_ERROR = "Failed to generate authentication Nonce due to internal errors.";

    public static readonly string INVALID_NONCE_SIGNATURE = "Authentications Nonce signature could not be verified.";

    public static readonly string NONCE_SIGNATURE_FAILED = "Nonce signature failed due to internal errors.";

    public static readonly string BROADCAST_HANDLING_ERROR = "Failed to handle vertex broadcast; it will not be forwarded to other nodes.";

    public static readonly string BROADCAST_FORWARDING_ERROR = "Vertex broadcast could not be received by some of the neighbors.";

    public static readonly string BROADCAST_TRIGGERING_FAILED = "Failed to trigger broadcast or some neighbors were not able to receive.";

    public static readonly string BROADCAST_TRIGGERING_WARNING = "Broadcast will not be triggered because validation failures or no changes required to local neighborhood.";

    public static readonly string ONION_ROUTING_FAILED = "Failed to route or store message.";

    public static readonly string NOT_AUTHORIZED = "Could not authorize.";

    public static readonly string AUTHENTICATION_REQUIRED = "Authentication required";

    public static readonly string INVALID_INVOCATION_DATA = "Invalid data provided for method invocation.";

    public static readonly string ONION_PARSING_FAILED = "Could not parse onion.";

    public static readonly string INTERNAL_ERROR = "Internal error";
}
