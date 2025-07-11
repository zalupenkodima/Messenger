using System.Collections.ObjectModel;
using System.Windows.Input;
using Messenger.WinForms.Core.Interfaces;
using Messenger.Shared;
using Microsoft.Extensions.Logging;

namespace Messenger.WinForms.Presentation.ViewModels;

public class UserSearchViewModel : BaseViewModel
{
    private readonly IMessengerService _messengerService;
    private readonly INavigationService _navigationService;
    private readonly ILogger<UserSearchViewModel> _logger;

    private ObservableCollection<UserDto> _users = new();
    private string _searchQuery = string.Empty;
    private bool _isLoading;
    private string _statusMessage = string.Empty;

    public UserSearchViewModel(
        IMessengerService messengerService,
        INavigationService navigationService,
        ILogger<UserSearchViewModel> logger)
    {
        _messengerService = messengerService;
        _navigationService = navigationService;
        _logger = logger;

        SearchCommand = new RelayCommand(async () => await SearchUsersAsync(), () => CanSearch());
        BackCommand = new RelayCommand(() => _navigationService.ShowMainForm());
        CreateChatCommand = new RelayCommand(() => CreateChatWithSelectedUser(), () => SelectedUser != null);
        
        _messengerService.UserOnlineStatusChanged += OnUserOnlineStatusChanged;
    }

    public ObservableCollection<UserDto> Users
    {
        get => _users;
        set => SetProperty(ref _users, value);
    }

    public string SearchQuery
    {
        get => _searchQuery;
        set => SetProperty(ref _searchQuery, value);
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

    public UserDto? SelectedUser { get; set; }

    public ICommand SearchCommand { get; }
    public ICommand BackCommand { get; }
    public ICommand CreateChatCommand { get; }

    private bool CanSearch()
    {
        return !IsLoading && !string.IsNullOrWhiteSpace(SearchQuery);
    }

    private async Task SearchUsersAsync()
    {
        if (!CanSearch()) return;

        IsLoading = true;
        StatusMessage = "Поиск пользователей...";

        try
        {
            var users = await _messengerService.SearchUsersAsync(SearchQuery);
            
            Users.Clear();
            foreach (var user in users)
            {
                Users.Add(user);
            }

            StatusMessage = $"Найдено {users.Count()} пользователей";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search users with query: {Query}", SearchQuery);
            StatusMessage = "Ошибка при поиске пользователей";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void CreateChatWithSelectedUser()
    {
        if (SelectedUser == null) return;

        try
        {
            MessageBox.Show($"Создание чата с пользователем {SelectedUser.Username} - функция в разработке", 
                "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create chat with user: {UserId}", SelectedUser.Id);
            StatusMessage = "Ошибка при создании чата";
        }
    }
    
    private void OnUserOnlineStatusChanged(Guid userId, bool isOnline)
    {
        var user = Users.FirstOrDefault(u => u.Id == userId);
        if (user != null)
        {
            user.IsOnline = isOnline;
            OnPropertyChanged(nameof(Users));
        }
    }
} 