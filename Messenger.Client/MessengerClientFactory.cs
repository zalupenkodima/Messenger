using Messenger.Client.Interfaces;
using Messenger.Client.Services;

namespace Messenger.Client;

public static class MessengerClientFactory
{
    public static IMessengerClient CreateClient(string baseUrl = "https://localhost:5001")
    {
        return new MessengerClient(baseUrl);
    }

    public static IMessengerClient CreateClient(Uri baseUri)
    {
        return new MessengerClient(baseUri.ToString());
    }
} 