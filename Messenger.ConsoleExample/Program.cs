using Messenger.Client;
using Messenger.Shared;

namespace Messenger.ConsoleExample;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("=== Messenger Client Example ===");
        
        var client = MessengerClientFactory.CreateClient("https://localhost:5001");
        
        try
        {
            client.MessageReceived += message => 
                Console.WriteLine($"[NEW] {message.SenderUsername}: {message.Content}");
            
            client.MessageUpdated += message => 
                Console.WriteLine($"[UPDATED] {message.SenderUsername}: {message.Content}");
            
            client.MessageDeleted += messageId => 
                Console.WriteLine($"[DELETED] Message {messageId}");
            
            client.UserTyping += (userId, isTyping) => 
                Console.WriteLine($"[TYPING] User {userId} is {(isTyping ? "typing" : "not typing")}");

            Console.Write("Username: ");
            var username = Console.ReadLine() ?? "testuser";
            
            Console.Write("Password: ");
            var password = Console.ReadLine() ?? "password123";

            Console.WriteLine("Authenticating...");
            var isAuthenticated = await client.AuthenticateAsync(username, password);
            
            if (!isAuthenticated)
            {
                Console.WriteLine("Authentication failed. Trying to register...");

                var randomEmail = GenerateRandomEmail();
                Console.WriteLine($"Generated email: {randomEmail}");
                
                var registered = await client.RegisterAsync(username, randomEmail, password);
                if (registered)
                {
                    Console.WriteLine("Registration successful. Authenticating...");
                    isAuthenticated = await client.AuthenticateAsync(username, password);
                }
                
                if (!isAuthenticated)
                {
                    Console.WriteLine("Authentication failed. Exiting...");
                    return;
                }
            }

            Console.WriteLine("Authentication successful!");

            Console.WriteLine("Connecting to SignalR...");
            await client.ConnectAsync();
            Console.WriteLine("Connected to SignalR!");

            Console.WriteLine("Getting chats...");
            var chats = await client.GetChatsAsync();
            
            if (!chats.Any())
            {
                Console.WriteLine("No chats found. Creating a test chat...");
                var testChat = await client.CreateChatAsync(new CreateChatDto
                {
                    Name = "Test Chat",
                    Description = "A test chat for demonstration",
                    Type = ChatType.Group,
                    MemberIds = [] 
                });
                chats = [testChat];
            }

            var currentChat = chats.First();
            Console.WriteLine($"Current chat: {currentChat.Name}");

            Console.WriteLine("Getting messages...");
            var messages = await client.GetChatMessagesAsync(currentChat.Id);
            Console.WriteLine($"Found {messages.Count()} messages");

            Console.WriteLine("Sending test message...");
            var testMessage = await client.SendMessageAsync(new CreateMessageDto
            {
                Content = "Hello from console client!",
                ChatId = currentChat.Id,
                Type = MessageType.Text
            });
            
            if (testMessage != null)
            {
                Console.WriteLine($"Message sent: {testMessage.Content}");
            }
            else
            {
                Console.WriteLine("Message sent but no response received");
            }

            Console.WriteLine("Demonstrating typing indicator...");
            await client.SendTypingIndicatorAsync(currentChat.Id, true);
            await Task.Delay(2000);
            await client.SendTypingIndicatorAsync(currentChat.Id, false);

            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();

            await client.DisconnectAsync();
            Console.WriteLine("Disconnected from SignalR");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }
        finally
        {
            if (client is IAsyncDisposable disposable)
                await disposable.DisposeAsync();
        }
    }

    private static string GenerateRandomEmail()
    {
        var random = new Random();
        var domains = new[] { "gmail.com", "yahoo.com", "hotmail.com", "outlook.com", "example.com" };
        var domain = domains[random.Next(domains.Length)];
        var username = $"user{random.Next(1000, 9999)}";
        return $"{username}@{domain}";
    }
}