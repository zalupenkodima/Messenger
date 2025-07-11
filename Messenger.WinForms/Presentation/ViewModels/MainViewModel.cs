using System.Collections.ObjectModel;
using System.Windows.Input;
using Messenger.WinForms.Core.Interfaces;
using Messenger.Shared;
using Microsoft.Extensions.Logging;

namespace Messenger.WinForms.Presentation.ViewModels;

public class MainViewModel : BaseViewModel, IDisposable
{
    private readonly IMessengerService _messengerService;
    private readonly IUserSessionService _userSessionService;
    private readonly INavigationService _navigationService;
    private readonly ILogger<MainViewModel> _logger;
    private readonly IServiceProvider _serviceProvider;
    private bool _disposed = false;

    private ObservableCollection<ChatDto> _chats = new();
    private ChatDto? _selectedChat;
    private bool _isLoading;
    private string _statusMessage = string.Empty;
    private string _searchQuery = string.Empty;

    public MainViewModel(
        IMessengerService messengerService,
        IUserSessionService userSessionService,
        INavigationService navigationService,
        ILogger<MainViewModel> logger,
        IServiceProvider serviceProvider)
    {
        _messengerService = messengerService;
        _userSessionService = userSessionService;
        _navigationService = navigationService;
        _logger = logger;
        _serviceProvider = serviceProvider;

        LoadChatsCommand = new RelayCommand(async () => await LoadChatsAsync());
        OpenChatCommand = new RelayCommand(() => OpenSelectedChat(), () => SelectedChat != null);
        CreateChatCommand = new RelayCommand(() => _navigationService.ShowCreateChatForm());
        SearchUsersCommand = new RelayCommand(() => _navigationService.ShowUserSearchForm());
        LogoutCommand = new RelayCommand(async () => await LogoutAsync());
        RefreshCommand = new RelayCommand(async () => await LoadChatsAsync());

        SubscribeToMessengerEvents();

        _ = LoadChatsAsync();
    }

    public ObservableCollection<ChatDto> Chats
    {
        get => _chats;
        set => SetProperty(ref _chats, value);
    }

    public ChatDto? SelectedChat
    {
        get => _selectedChat;
        set => SetProperty(ref _selectedChat, value);
    }

    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public string SearchQuery
    {
        get => _searchQuery;
        set
        {
            if (SetProperty(ref _searchQuery, value))
            {
                _ = FilterChatsAsync();
            }
        }
    }

    public string CurrentUsername => _userSessionService.CurrentUser?.Username ?? "Неизвестный пользователь";
    public bool IsConnected => _messengerService.IsConnected;

    public ICommand LoadChatsCommand { get; }
    public ICommand OpenChatCommand { get; }
    public ICommand CreateChatCommand { get; }
    public ICommand SearchUsersCommand { get; }
    public ICommand LogoutCommand { get; }
    public ICommand RefreshCommand { get; }

    private async Task LoadChatsAsync()
    {
        IsLoading = true;
        StatusMessage = "Загрузка чатов...";

        try
        {
            var chats = await _messengerService.GetChatsAsync();
            
            Chats.Clear();
            foreach (var chat in chats)
            {
                Chats.Add(chat);
            }

            StatusMessage = $"Загружено {chats.Count()} чатов";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load chats");
            StatusMessage = "Ошибка при загрузке чатов";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task FilterChatsAsync()
    {
        if (string.IsNullOrWhiteSpace(SearchQuery))
        {
            await LoadChatsAsync();
            return;
        }

        IsLoading = true;
        StatusMessage = "Поиск чатов...";

        try
        {
            var allChats = await _messengerService.GetChatsAsync();
            var filteredChats = allChats.Where(c => 
                c.Name.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase) ||
                c.Description?.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase) == true);

            Chats.Clear();
            foreach (var chat in filteredChats)
            {
                Chats.Add(chat);
            }

            StatusMessage = $"Найдено {filteredChats.Count()} чатов";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to filter chats");
            StatusMessage = "Ошибка при поиске чатов";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async void OpenSelectedChat()
    {
        if (SelectedChat != null)
        {
            _logger.LogInformation("Opening chat: {ChatId} - {ChatName}", SelectedChat.Id, SelectedChat.Name);
            
            try
            {
                await _messengerService.MarkChatAsReadAsync(SelectedChat.Id);
                
                var index = Chats.IndexOf(SelectedChat);
                if (index >= 0)
                {
                    SelectedChat.UnreadCount = 0;
                    Chats[index] = SelectedChat;
                }
                
                _logger.LogInformation("Chat {ChatId} marked as read, unread count reset to 0", SelectedChat.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to mark chat {ChatId} as read", SelectedChat.Id);
            }
            
            _navigationService.ShowChatForm(SelectedChat.Id);
        }
        else
        {
            _logger.LogWarning("Attempted to open chat but no chat is selected");
        }
    }

    private async Task LogoutAsync()
    {
        try
        {
            _logger.LogInformation("Starting logout process");
            
            UnsubscribeFromMessengerEvents();
            _logger.LogInformation("Unsubscribed from messenger events");
            
            await _messengerService.DisconnectAsync();
            _logger.LogInformation("Disconnected from SignalR");
            
            _userSessionService.ClearSession();
            _logger.LogInformation("User session cleared");
            
            var localAuthStorageService = _serviceProvider.GetService(typeof(ILocalAuthStorageService)) as ILocalAuthStorageService;
            localAuthStorageService?.ClearToken();
            _logger.LogInformation("Token cleared");
            
            _navigationService.ShowLoginForm();
            _logger.LogInformation("Logout completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Logout failed");
            _navigationService.ShowLoginForm();
        }
    }

    private void SubscribeToMessengerEvents()
    {
        _messengerService.MessageReceived += OnMessageReceived;
        _messengerService.UserJoinedChat += OnUserJoinedChat;
        _messengerService.UserLeftChat += OnUserLeftChat;
        _messengerService.UserOnlineStatusChanged += OnUserOnlineStatusChanged;
    }

    private void UnsubscribeFromMessengerEvents()
    {
        _messengerService.MessageReceived -= OnMessageReceived;
        _messengerService.UserJoinedChat -= OnUserJoinedChat;
        _messengerService.UserLeftChat -= OnUserLeftChat;
        _messengerService.UserOnlineStatusChanged -= OnUserOnlineStatusChanged;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            UnsubscribeFromMessengerEvents();
            _logger.LogInformation("MainViewModel disposed");
            _disposed = true;
        }
    }

    private void OnMessageReceived(MessageDto message)
    {
        var chat = Chats.FirstOrDefault(c => c.Id == message.ChatId);
        if (chat != null)
        {
            var index = Chats.IndexOf(chat);
            chat.LastMessageAt = message.CreatedAt;
            chat.UnreadCount++;
            Chats[index] = chat;
        }
    }

    private void OnUserJoinedChat(Guid chatId)
    {

        var chat = Chats.FirstOrDefault(c => c.Id == chatId);
        if (chat != null)
        {
            var index = Chats.IndexOf(chat);
            chat.MemberCount++;
            Chats[index] = chat;
        }
    }

    private void OnUserLeftChat(Guid chatId)
    {
        _logger.LogInformation("User left chat: {ChatId}", chatId);
        
        _ = LoadChatsAsync();
    }

    private void OnUserOnlineStatusChanged(Guid userId, bool isOnline)
    {
        _logger.LogInformation("User online status changed: {UserId}, IsOnline: {IsOnline}", userId, isOnline);
        
        foreach (var chat in Chats)
        {
            var member = chat.Members.FirstOrDefault(m => m.UserId == userId);
            if (member != null)
            {
                member.IsOnline = isOnline;
                member.LastSeen = DateTime.UtcNow;
                
                OnPropertyChanged(nameof(Chats));
                break;
            }
        }
    }
} 