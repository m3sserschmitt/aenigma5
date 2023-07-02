using Enigma5.App.Hubs.Contracts;
using Enigma5.App.Hubs.Extensions;

using Microsoft.AspNetCore.SignalR;

public class OnionParsingHubAdapter : IOnionParsingHub
{
    private readonly IOnionParsingHub? onionParserHub;

    public OnionParsingHubAdapter(Hub hub)
    {
        onionParserHub = hub.As<IOnionParsingHub>();
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