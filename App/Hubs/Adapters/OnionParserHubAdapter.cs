using Enigma5.App.Hubs.Contracts;
using Enigma5.Message;
using Enigma5.App.Hubs.Extensions;

using Microsoft.AspNetCore.SignalR;

public class OnionParserHubAdapter : IOnionParserHub
{
    private readonly IOnionParserHub? onionParserHub;

    public OnionParserHubAdapter(Hub hub)
    {
        onionParserHub = hub.As<IOnionParserHub>();
    }

    public int Size
    {
        get => onionParserHub?.Size ?? default;
        set
        {
            if (onionParserHub != null)
            {
                onionParserHub.Size = value;
            }
        }
    }

    public string? Next
    {
        get => onionParserHub?.Next;
        set
        {
            if (onionParserHub != null)
            {
                onionParserHub.Next = value;
            }
        }
    }

    public byte[]? Content
    {
        get => onionParserHub?.Content;
        set
        {
            if (onionParserHub != null)
            {
                onionParserHub.Content = value;
            }
        }
    }
}