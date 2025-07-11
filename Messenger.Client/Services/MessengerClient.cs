using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR.Client;
using Messenger.Client.Interfaces;
using Messenger.Client.Models;
using Messenger.Shared;
using Microsoft.Extensions.Logging;

namespace Messenger.Client.Services;

public class MessengerClient : IMessengerClient, IAsyncDisposable
{
    private readonly HttpClient _httpClient;
    private HubConnection? _hubConnection;
    private string? _token;
    private readonly string _baseUrl;
    private readonly JsonSerializerOptions _jsonOptions;

    public MessengerClient(string baseUrl = "https://localhost:5001")
    {
        _baseUrl = baseUrl.TrimEnd('/');
        _httpClient = new();
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        SetupSignalREvents();
    }

    public event Action<MessageDto>? MessageReceived;
    public event Action<MessageDto>? MessageUpdated;
    public event Action<Guid>? MessageDeleted;
    public event Action<Guid>? UserJoinedChat;
    public event Action<Guid>? UserLeftChat;
    public event Action<Guid, bool>? UserTyping;
    public event Action<Guid, bool>? UserOnlineStatusChanged;

    public bool IsConnected => _hubConnection?.State == HubConnectionState.Connected;
    public string? Token => _token;

    public async Task<bool> AuthenticateAsync(string username, string password)
    {
        try
        {
            var request = new LoginRequest { Username = username, Password = password };
            var json = JsonSerializer.Serialize(request, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{_baseUrl}/api/auth/login", content);
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var authResponse = JsonSerializer.Deserialize<AuthResponse>(responseContent, _jsonOptions);
                if (authResponse?.Token != null)
                {
                    SetToken(authResponse.Token);
                    return true;
                }
            }
            return false;
        }
        catch (TaskCanceledException)
        {
            Console.WriteLine("Authentication request timed out");
            return false;
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"HTTP request failed during authentication: {ex.Message}");
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Unexpected error during authentication: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> RegisterAsync(string username, string email, string password)
    {
        try
        {
            var request = new CreateUserDto { Username = username, Email = email, Password = password };
            var json = JsonSerializer.Serialize(request, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{_baseUrl}/api/auth/register", content);
            return response.IsSuccessStatusCode;
        }
        catch (TaskCanceledException)
        {
            Console.WriteLine("Registration request timed out");
            return false;
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"HTTP request failed during registration: {ex.Message}");
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Unexpected error during registration: {ex.Message}");
            return false;
        }
    }

    public void SetToken(string token)
    {
        _token = token;
        _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
    }

    public async Task ConnectAsync()
    {
        if (_token == null)
        {
            throw new InvalidOperationException("Token is required. Call AuthenticateAsync or SetToken first.");
        }

        _hubConnection = new HubConnectionBuilder()
            .WithUrl($"{_baseUrl}/chatHub?access_token={_token}")
            .WithAutomaticReconnect()
            .ConfigureLogging(logging =>
            {
                logging.SetMinimumLevel(LogLevel.Debug);
            })
            .Build();

        SetupSignalREvents();
        
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        try
        {
            await _hubConnection.StartAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            throw new TimeoutException("SignalR connection timeout. Server might be unavailable.");
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to connect to SignalR hub: {ex.Message}", ex);
        }
    }

    public async Task DisconnectAsync()
    {
        if (_hubConnection != null)
        {
            Console.WriteLine($"Disconnecting SignalR connection. Current state: {_hubConnection.State}");
            
            if (_hubConnection.State == HubConnectionState.Disconnected)
            {
                try
                {
                    await _hubConnection.DisposeAsync();
                    Console.WriteLine("SignalR connection disposed successfully");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error disposing SignalR connection: {ex.Message}");
                }
                _hubConnection = null;
                return;
            }
            
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                await _hubConnection.StopAsync(cts.Token);
                Console.WriteLine("SignalR connection stopped successfully");
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("SignalR connection stop timeout");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error stopping SignalR connection: {ex.Message}");
            }
            finally
            {
                try
                {
                    await _hubConnection.DisposeAsync();
                    Console.WriteLine("SignalR connection disposed successfully");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error disposing SignalR connection: {ex.Message}");
                }
                _hubConnection = null;
            }
        }
        else
        {
            Console.WriteLine("No SignalR connection to disconnect");
        }
    }

    public async Task<IEnumerable<ChatDto>> GetChatsAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/api/chats");
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<IEnumerable<ChatDto>>(content, _jsonOptions) ?? [];
        }
        catch (TaskCanceledException)
        {
            Console.WriteLine("GetChatsAsync request timed out");
            return [];
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"HTTP request failed in GetChatsAsync: {ex.Message}");
            return [];
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Unexpected error in GetChatsAsync: {ex.Message}");
            return [];
        }
    }

    public async Task<ChatDto?> GetChatAsync(Guid chatId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/api/chats/{chatId}");
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return null;
                
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<ChatDto>(content, _jsonOptions);
        }
        catch (TaskCanceledException)
        {
            Console.WriteLine("GetChatAsync request timed out");
            return null;
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"HTTP request failed in GetChatAsync: {ex.Message}");
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Unexpected error in GetChatAsync: {ex.Message}");
            return null;
        }
    }

    public async Task<ChatDto> CreateChatAsync(CreateChatDto createChatDto)
    {
        var json = JsonSerializer.Serialize(createChatDto, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync($"{_baseUrl}/api/chats", content);
        response.EnsureSuccessStatusCode();
        
        var responseContent = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<ChatDto>(responseContent, _jsonOptions) 
               ?? throw new InvalidOperationException("Failed to deserialize chat response");
    }

    public async Task<ChatDto> UpdateChatAsync(Guid chatId, UpdateChatDto updateChatDto)
    {
        var json = JsonSerializer.Serialize(updateChatDto, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PutAsync($"{_baseUrl}/api/chats/{chatId}", content);
        response.EnsureSuccessStatusCode();
        
        var responseContent = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<ChatDto>(responseContent, _jsonOptions) 
               ?? throw new InvalidOperationException("Failed to deserialize chat response");
    }

    public async Task AddMemberToChatAsync(Guid chatId, Guid memberId)
    {
        var request = new AddMemberRequest { MemberId = memberId };
        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync($"{_baseUrl}/api/chats/{chatId}/members", content);
        response.EnsureSuccessStatusCode();
    }

    public async Task RemoveMemberFromChatAsync(Guid chatId, Guid memberId)
    {
        var response = await _httpClient.DeleteAsync($"{_baseUrl}/api/chats/{chatId}/members/{memberId}");
        response.EnsureSuccessStatusCode();
    }

    public async Task LeaveChatAsync(Guid chatId)
    {
        var response = await _httpClient.PostAsync($"{_baseUrl}/api/chats/{chatId}/leave", null);
        response.EnsureSuccessStatusCode();
    }

    public async Task DeleteChatAsync(Guid chatId)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"{_baseUrl}/api/chats/{chatId}");
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in DeleteChatAsync: {ex.Message}");
            throw;
        }
    }

    public async Task MarkChatAsReadAsync(Guid chatId)
    {
        try
        {
            var response = await _httpClient.PostAsync($"{_baseUrl}/api/chats/{chatId}/mark-as-read", null);
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in MarkChatAsReadAsync: {ex.Message}");
            throw;
        }
    }

    public async Task<IEnumerable<MessageDto>> GetChatMessagesAsync(Guid chatId, int skip = 0, int take = 50)
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/api/messages/chat/{chatId}?skip={skip}&take={take}");
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<IEnumerable<MessageDto>>(content, _jsonOptions) ?? [];
        }
        catch (TaskCanceledException)
        {
            Console.WriteLine("GetChatMessagesAsync request timed out");
            return [];
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"HTTP request failed in GetChatMessagesAsync: {ex.Message}");
            return [];
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Unexpected error in GetChatMessagesAsync: {ex.Message}");
            return [];
        }
    }

    public async Task<MessageDto?> GetMessageAsync(Guid messageId)
    {
        var response = await _httpClient.GetAsync($"{_baseUrl}/api/messages/{messageId}");
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return null;
            
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<MessageDto>(content, _jsonOptions);
    }

    public async Task<IEnumerable<MessageDto>> GetRepliesAsync(Guid messageId)
    {
        var response = await _httpClient.GetAsync($"{_baseUrl}/api/messages/{messageId}/replies");
        response.EnsureSuccessStatusCode();
        
        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<IEnumerable<MessageDto>>(content, _jsonOptions) ?? [];
    }

    public async Task<MessageDto> SendMessageAsync(CreateMessageDto createMessageDto)
    {
        if (_hubConnection?.State != HubConnectionState.Connected)
        {
            throw new InvalidOperationException("SignalR connection is not established. Call ConnectAsync first.");
        }

        try
        {
            return await _hubConnection.InvokeAsync<MessageDto>("SendMessage", createMessageDto);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to send message: {ex.Message}", ex);
        }
    }

    public async Task<MessageDto> UpdateMessageAsync(Guid messageId, UpdateMessageDto updateMessageDto)
    {
        if (_hubConnection?.State != HubConnectionState.Connected)
        {
            throw new InvalidOperationException("SignalR connection is not established. Call ConnectAsync first.");
        }

        try
        {
            return await _hubConnection.InvokeAsync<MessageDto>("UpdateMessage", messageId, updateMessageDto);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to update message: {ex.Message}", ex);
        }
    }

    public async Task DeleteMessageAsync(Guid messageId)
    {
        if (_hubConnection?.State != HubConnectionState.Connected)
        {
            throw new InvalidOperationException("SignalR connection is not established. Call ConnectAsync first.");
        }

        try
        {
            await _hubConnection.InvokeAsync("DeleteMessage", messageId);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to delete message: {ex.Message}", ex);
        }
    }

    public async Task SendTypingIndicatorAsync(Guid chatId, bool isTyping)
    {
        if (_hubConnection?.State != HubConnectionState.Connected)
        {
            throw new InvalidOperationException("SignalR connection is not established. Call ConnectAsync first.");
        }

        try
        {
            await _hubConnection.InvokeAsync("Typing", chatId, isTyping);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to send typing indicator: {ex.Message}", ex);
        }
    }

    public async Task<IEnumerable<UserDto>> SearchUsersAsync(string query)
    {
        try
        {
            Console.WriteLine($"SearchUsersAsync: Searching for '{query}'");
            var response = await _httpClient.GetAsync($"{_baseUrl}/api/users/search?query={Uri.EscapeDataString(query)}");
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"SearchUsersAsync: Response content: {content}");
            
            var users = JsonSerializer.Deserialize<IEnumerable<UserDto>>(content, _jsonOptions) ?? [];
            Console.WriteLine($"SearchUsersAsync: Deserialized {users.Count()} users");
            
            foreach (var user in users)
            {
                Console.WriteLine($"SearchUsersAsync: User - {user.Username} ({user.Email})");
            }
            
            return users;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in SearchUsersAsync: {ex.Message}");
            return [];
        }
    }

    public async Task<UserDto?> GetUserAsync(Guid userId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/api/users/{userId}");
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return null;
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<UserDto>(content, _jsonOptions);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in GetUserAsync: {ex.Message}");
            return null;
        }
    }

    public async Task<UserDto?> GetCurrentUserAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/api/auth/user");
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return null;
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<UserDto>(content, _jsonOptions);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in GetCurrentUserAsync: {ex.Message}");
            return null;
        }
    }

    public async Task<IEnumerable<UserDto>> GetOnlineUsersAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/api/users/online");
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<IEnumerable<UserDto>>(content, _jsonOptions) ?? [];
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in GetOnlineUsersAsync: {ex.Message}");
            return [];
        }
    }

    private void SetupSignalREvents()
    {
        if (_hubConnection == null) return;

        _hubConnection.On<MessageDto>("MessageReceived", message => MessageReceived?.Invoke(message));
        _hubConnection.On<MessageDto>("MessageUpdated", message => MessageUpdated?.Invoke(message));
        _hubConnection.On<Guid>("MessageDeleted", messageId => MessageDeleted?.Invoke(messageId));
        _hubConnection.On<Guid>("UserJoinedChat", userId => UserJoinedChat?.Invoke(userId));
        _hubConnection.On<Guid>("UserLeftChat", userId => UserLeftChat?.Invoke(userId));
        _hubConnection.On<Guid, bool>("UserTyping", (userId, isTyping) => UserTyping?.Invoke(userId, isTyping));
        _hubConnection.On<Guid, bool>("UserOnlineStatusChanged", (userId, isOnline) => UserOnlineStatusChanged?.Invoke(userId, isOnline));
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            await DisconnectAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during dispose: {ex.Message}");
        }
        finally
        {
            _httpClient.Dispose();
        }
    }
} 