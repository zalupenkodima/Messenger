using System.Windows.Input;
using Messenger.WinForms.Core.Interfaces;
using Messenger.Shared;
using Microsoft.Extensions.Logging;

namespace Messenger.WinForms.Presentation.ViewModels;

public class CreateChatViewModel : BaseViewModel
{
    private readonly IMessengerService _messengerService;
    private readonly INavigationService _navigationService;
    private readonly ILogger<CreateChatViewModel> _logger;

    private string _chatName = string.Empty;
    private string _chatDescription = string.Empty;
    private ChatType _selectedChatType = ChatType.Private;
    private bool _isLoading;
    private string _statusMessage = string.Empty;

    public CreateChatViewModel(
        IMessengerService messengerService,
        INavigationService navigationService,
        ILogger<CreateChatViewModel> logger)
    {
        _messengerService = messengerService;
        _navigationService = navigationService;
        _logger = logger;

        CreateChatCommand = new RelayCommand(async () => await CreateChatAsync(), () => CanCreateChat());
        BackCommand = new RelayCommand(() => _navigationService.ShowMainForm());
    }

    public string ChatName
    {
        get => _chatName;
        set => SetProperty(ref _chatName, value);
    }

    public string ChatDescription
    {
        get => _chatDescription;
        set => SetProperty(ref _chatDescription, value);
    }

    public ChatType SelectedChatType
    {
        get => _selectedChatType;
        set => SetProperty(ref _selectedChatType, value);
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

    public ICommand CreateChatCommand { get; }
    public ICommand BackCommand { get; }

    private bool CanCreateChat()
    {
        return !IsLoading && !string.IsNullOrWhiteSpace(ChatName.Trim());
    }

    private async Task CreateChatAsync()
    {
        if (!CanCreateChat()) return;

        IsLoading = true;
        StatusMessage = "Создание чата...";

        try
        {
            var createChatDto = new CreateChatDto
            {
                Name = ChatName.Trim(),
                Description = ChatDescription.Trim(),
                Type = SelectedChatType
            };

            var chat = await _messengerService.CreateChatAsync(createChatDto);
            
            StatusMessage = "Чат успешно создан!";
            _logger.LogInformation("Chat created successfully: {ChatId}", chat.Id);

            await Task.Delay(1000);
            _navigationService.ShowMainForm();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create chat");
            StatusMessage = "Ошибка при создании чата";
        }
        finally
        {
            IsLoading = false;
        }
    }
} 