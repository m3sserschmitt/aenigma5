namespace Enigma5.App.Hubs;

public class InvocationErrors
{
    public static readonly string NONCE_GENERATION_ERROR = "Failed to generate authentication Nonce due to internal errors.";

    public static readonly string INVALID_NONCE_SIGNATURE = "Authentications Nonce signature could not be verified.";

    public static readonly string NONCE_SIGNATURE_FAILED = "Nonce signature failed due to internal errors.";

    public static readonly string BROADCAST_HANDLING_ERROR = "Failed to handle vertex broadcast; it will not be forwarded to other nodes.";

    public static readonly string BROADCAST_FORWARDING_ERROR = "Vertex broadcast could not be received by some of the neighbors.";

    public static readonly string BROADCAST_TRIGGERING_FAILED = "Failed to trigger broadcast or some neighbors were not able to receive.";

    public static readonly string ROUTING_FAILED = "Failed to route or store message.";
}
