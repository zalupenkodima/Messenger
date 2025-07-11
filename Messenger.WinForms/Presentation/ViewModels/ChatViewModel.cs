using System.Collections.ObjectModel;
using System.Windows.Input;
using Messenger.WinForms.Core.Interfaces;
using Messenger.Shared;
using Microsoft.Extensions.Logging;

namespace Messenger.WinForms.Presentation.ViewModels;

public class ChatViewModel : BaseViewModel
{
    private readonly IMessengerService _messengerService;
    private readonly IUserSessionService _userSessionService;
    private readonly INavigationService _navigationService;
    private readonly ILogger<ChatViewModel> _logger;

    private ChatDto _chat;
    private ObservableCollection<MessageDto> _messages = new();
    private string _newMessageText = string.Empty;
    private bool _isLoading;
    private string _statusMessage = string.Empty;
    private bool _isTyping;
    private string _typingIndicator = string.Empty;

    public ChatViewModel(
        Guid chatId,
        IMessengerService messengerService,
        IUserSessionService userSessionService,
        INavigationService navigationService,
        ILogger<ChatViewModel> logger)
    {
        _messengerService = messengerService;
        _userSessionService = userSessionService;
        _navigationService = navigationService;
        _logger = logger;

        _chat = new ChatDto
        {
            Id = chatId,
            Name = "Загрузка...",
            Description = "",
            Type = ChatType.Private,
            CreatedAt = DateTime.UtcNow,
            MemberCount = 0,
            UnreadCount = 0,
            LastMessageAt = DateTime.UtcNow
        };

        SendMessageCommand = new RelayCommand(async () => await SendMessageAsync(), () => CanSendMessage());
        LoadMessagesCommand = new RelayCommand(async () => await LoadMessagesAsync());
        BackCommand = new RelayCommand(() => _navigationService.ShowMainForm());
        EditMessageCommand = new RelayCommand<MessageDto>(async (message) => 
        {
            if (message != null && CanEditMessage(message))
            {
                await EditMessageAsync(message);
            }
        }, (message) => CanEditMessage(message));
        DeleteMessageCommand = new RelayCommand<MessageDto>(async (message) => 
        {
            if (message != null && CanDeleteMessage(message))
            {
                await DeleteMessageAsync(message);
            }
        }, (message) => CanDeleteMessage(message));

        _messengerService.MessageReceived += OnMessageReceived;
        _messengerService.MessageUpdated += OnMessageUpdated;
        _messengerService.MessageDeleted += OnMessageDeleted;
        _messengerService.UserTyping += OnUserTyping;
        _messengerService.UserOnlineStatusChanged += OnUserOnlineStatusChanged;

        _ = LoadChatInfoAsync();
    }

    public ChatDto Chat
    {
        get => _chat;
        set => SetProperty(ref _chat, value);
    }

    public ObservableCollection<MessageDto> Messages
    {
        get => _messages;
        set => SetProperty(ref _messages, value);
    }

    public string NewMessageText
    {
        get => _newMessageText;
        set
        {
            if (SetProperty(ref _newMessageText, value))
            {
                _ = SendTypingIndicatorAsync(true);
            }
        }
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

    public bool IsTyping
    {
        get => _isTyping;
        set => SetProperty(ref _isTyping, value);
    }

    public string TypingIndicator
    {
        get => _typingIndicator;
        set => SetProperty(ref _typingIndicator, value);
    }

    public string CurrentUsername => _userSessionService.CurrentUser?.Username ?? "Неизвестный пользователь";
    public bool IsConnected => _messengerService.IsConnected;
    public Guid? CurrentUserId => _userSessionService.CurrentUser?.Id;

    public ICommand SendMessageCommand { get; }
    public ICommand LoadMessagesCommand { get; }
    public ICommand BackCommand { get; }
    public RelayCommand<MessageDto> EditMessageCommand { get; }
    public RelayCommand<MessageDto> DeleteMessageCommand { get; }

    public void RefreshCommands()
    {
        OnPropertyChanged(nameof(EditMessageCommand));
        OnPropertyChanged(nameof(DeleteMessageCommand));
    }

    private bool CanSendMessage()
    {
        return !IsLoading && !string.IsNullOrWhiteSpace(NewMessageText) && IsConnected;
    }

    private bool CanEditMessage(MessageDto? message)
    {
        var currentUser = _userSessionService.CurrentUser;
        _logger.LogInformation("[CanEditMessage] CurrentUser: {CurrentUserId}, Message.SenderId: {MessageSenderId}, IsLoading: {IsLoading}, IsConnected: {IsConnected}", 
            currentUser?.Id, message?.SenderId, IsLoading, IsConnected);
        _logger.LogInformation("[CanEditMessage] CurrentUser.Id type: {CurrentUserIdType}, Message.SenderId type: {MessageSenderIdType}", 
            currentUser?.Id.GetType().Name, message?.SenderId.GetType().Name);
        _logger.LogInformation("[CanEditMessage] CurrentUser username: {CurrentUsername}", currentUser?.Username);
        _logger.LogInformation("[CanEditMessage] Comparison result: {ComparisonResult}", 
            message?.SenderId == currentUser?.Id);
        
        if (message == null) return false;
        return !IsLoading && IsConnected && currentUser != null && 
               message.SenderId == currentUser.Id && 
               DateTime.UtcNow.Subtract(message.CreatedAt).TotalMinutes < 60;
    }

    private bool CanDeleteMessage(MessageDto? message)
    {
        var currentUser = _userSessionService.CurrentUser;
        _logger.LogInformation("[CanDeleteMessage] CurrentUser: {CurrentUserId}, Message.SenderId: {MessageSenderId}, IsLoading: {IsLoading}, IsConnected: {IsConnected}", 
            currentUser?.Id, message?.SenderId, IsLoading, IsConnected);
        if (message == null) return false;
        return !IsLoading && IsConnected && currentUser != null && 
               message.SenderId == currentUser.Id;
    }

    private async Task SendMessageAsync()
    {
        if (!CanSendMessage()) return;

        var messageText = NewMessageText.Trim();
        NewMessageText = string.Empty;

        try
        {
            var createMessageDto = new CreateMessageDto
            {
                ChatId = Chat.Id,
                Content = messageText
            };

            var message = await _messengerService.SendMessageAsync(createMessageDto);
            
            _logger.LogInformation("Message sent successfully: {MessageId}", message.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send message");
            StatusMessage = "Ошибка при отправке сообщения";
            
            NewMessageText = messageText;
        }
    }

    public async Task EditMessageAsync(MessageDto message)
    {
        try
        {
            _logger.LogInformation("Starting to edit message: {MessageId}", message.Id);
            
            using var editForm = new Form
            {
                Text = "Редактировать сообщение",
                Size = new Size(400, 200),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false
            };

            var textBox = new TextBox
            {
                Text = message.Content,
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                Dock = DockStyle.Top,
                Height = 100
            };

            var buttonPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 50
            };

            var btnSave = new Button
            {
                Text = "Сохранить",
                DialogResult = DialogResult.OK,
                Location = new Point(200, 10)
            };

            var btnCancel = new Button
            {
                Text = "Отмена",
                DialogResult = DialogResult.Cancel,
                Location = new Point(300, 10)
            };

            buttonPanel.Controls.AddRange(new Control[] { btnSave, btnCancel });
            editForm.Controls.AddRange(new Control[] { textBox, buttonPanel });

            if (editForm.ShowDialog() == DialogResult.OK)
            {
                var newContent = textBox.Text.Trim();
                if (!string.IsNullOrEmpty(newContent) && newContent != message.Content)
                {
                    var updateMessageDto = new UpdateMessageDto
                    {
                        Content = newContent
                    };

                    await _messengerService.UpdateMessageAsync(message.Id, updateMessageDto);
                    _logger.LogInformation("Message updated successfully: {MessageId}", message.Id);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to edit message: {MessageId}", message.Id);
            StatusMessage = "Ошибка при редактировании сообщения";
        }
    }

    public async Task DeleteMessageAsync(MessageDto message)
    {
        try
        {
            _logger.LogInformation("Starting to delete message: {MessageId}", message.Id);
            
            var result = MessageBox.Show(
                $"Вы уверены, что хотите удалить сообщение?\n\n\"{message.Content}\"",
                "Подтверждение удаления",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                await _messengerService.DeleteMessageAsync(message.Id);
                _logger.LogInformation("Message deleted successfully: {MessageId}", message.Id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete message: {MessageId}", message.Id);
            StatusMessage = "Ошибка при удалении сообщения";
        }
    }

    private async Task LoadMessagesAsync()
    {
        _logger.LogInformation("Starting to load messages for chat: {ChatId}", Chat.Id);
        IsLoading = true;
        StatusMessage = "Загрузка сообщений...";

        try
        {
            var messages = await _messengerService.GetChatMessagesAsync(Chat.Id);
            _logger.LogInformation("Retrieved {Count} messages for chat: {ChatId}", messages.Count(), Chat.Id);
            
            Messages.Clear();
            foreach (var message in messages.OrderBy(m => m.CreatedAt))
            {
                Messages.Add(message);
                _logger.LogDebug("Added message: {MessageId} - {Content}", message.Id, message.Content);
            }

            StatusMessage = $"Загружено {messages.Count()} сообщений";
            _logger.LogInformation("Messages loaded successfully for chat: {ChatId}, count: {Count}", Chat.Id, messages.Count());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load messages for chat: {ChatId}", Chat.Id);
            StatusMessage = "Ошибка при загрузке сообщений";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LoadChatInfoAsync()
    {
        try
        {
            _logger.LogInformation("Starting to load chat info for chat: {ChatId}", Chat.Id);
            IsLoading = true;
            StatusMessage = "Загрузка информации о чате...";
            
            var chat = await _messengerService.GetChatAsync(Chat.Id);
            if (chat != null)
            {
                Chat = chat;
                _logger.LogInformation("Chat info loaded successfully: {ChatName} (ID: {ChatId})", chat.Name, chat.Id);
                
                await _messengerService.MarkChatAsReadAsync(Chat.Id);

                Chat.UnreadCount = 0;
                OnPropertyChanged(nameof(Chat));
                _logger.LogInformation("[ChatViewModel] UnreadCount reset to 0 for chat: {ChatId}", Chat.Id);
                
                await LoadMessagesAsync();
            }
            else
            {
                StatusMessage = "Чат не найден";
                _logger.LogWarning("Chat not found: {ChatId}", Chat.Id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load chat info: {ChatId}", Chat.Id);
            StatusMessage = "Ошибка при загрузке информации о чате";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task SendTypingIndicatorAsync(bool isTyping)
    {
        try
        {
            await _messengerService.SendTypingIndicatorAsync(Chat.Id, isTyping);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send typing indicator");
        }
    }

    private void OnMessageReceived(MessageDto message)
    {
        _logger.LogInformation("ChatViewModel: OnMessageReceived - {MessageId} in chat {ChatId}, current chat: {CurrentChatId}", 
            message.Id, message.ChatId, Chat.Id);
            
        if (message.ChatId == Chat.Id)
        {
            _logger.LogInformation("Adding message to chat: {MessageContent}", message.Content);

            Messages.Add(message);
            
            IsTyping = false;
            TypingIndicator = string.Empty;
        }
        else
        {
            _logger.LogWarning("Message received for different chat: {MessageChatId} vs {CurrentChatId}", 
                message.ChatId, Chat.Id);
        }
    }

    private void OnMessageUpdated(MessageDto message)
    {
        _logger.LogInformation("[OnMessageUpdated] MessageId: {MessageId}, ChatId: {ChatId}, CurrentChatId: {CurrentChatId}", message.Id, message.ChatId, Chat.Id);
        if (message.ChatId == Chat.Id)
        {
            var updateAction = new Action(() =>
            {
                var existingMessage = Messages.FirstOrDefault(m => m.Id == message.Id);
                if (existingMessage != null)
                {
                    var index = Messages.IndexOf(existingMessage);
                    Messages[index] = message;
                    _logger.LogInformation("[OnMessageUpdated] Message updated in collection: {MessageId}", message.Id);
                }
                else
                {
                    _logger.LogWarning("[OnMessageUpdated] Message not found in collection: {MessageId}", message.Id);
                }
            });
            var openForms = System.Windows.Forms.Application.OpenForms;
            if (openForms.Count > 0 && openForms[0]?.InvokeRequired == true)
            {
                openForms[0].Invoke(updateAction);
            }
            else
            {
                updateAction();
            }
        }
    }

    private void OnMessageDeleted(Guid messageId)
    {
        _logger.LogInformation("[OnMessageDeleted] MessageId: {MessageId}, CurrentChatId: {CurrentChatId}", messageId, Chat.Id);
        var deleteAction = new Action(() =>
        {
            var messageToRemove = Messages.FirstOrDefault(m => m.Id == messageId);
            if (messageToRemove != null)
            {
                Messages.Remove(messageToRemove);
                _logger.LogInformation("[OnMessageDeleted] Message removed from collection: {MessageId}", messageId);
            }
            else
            {
                _logger.LogWarning("[OnMessageDeleted] Message not found in collection: {MessageId}", messageId);
            }
        });
        var openForms = System.Windows.Forms.Application.OpenForms;
        if (openForms.Count > 0 && openForms[0]?.InvokeRequired == true)
        {
            openForms[0].Invoke(deleteAction);
        }
        else
        {
            deleteAction();
        }
    }

    private void OnUserTyping(Guid chatId, bool isTyping)
    {
        if (chatId == Chat.Id && isTyping)
        {
            IsTyping = true;
            TypingIndicator = "Кто-то печатает...";
            
            _ = Task.Delay(3000).ContinueWith(_ =>
            {
                IsTyping = false;
                TypingIndicator = string.Empty;
            });
        }
    }

    private void OnUserOnlineStatusChanged(Guid userId, bool isOnline)
    {
        _logger.LogInformation("ChatViewModel: OnUserOnlineStatusChanged - UserId: {UserId}, IsOnline: {IsOnline}", userId, isOnline);
        
        var member = Chat.Members.FirstOrDefault(m => m.UserId == userId);
        if (member != null)
        {
            member.IsOnline = isOnline;
            member.LastSeen = DateTime.UtcNow;
            
            OnPropertyChanged(nameof(Chat));
        }
        
        if (isOnline && _userSessionService.CurrentUser?.Id == userId)
        {
            IsTyping = false;
            TypingIndicator = string.Empty;
        }
    }

    public void Dispose()
    {
        _messengerService.MessageReceived -= OnMessageReceived;
        _messengerService.MessageUpdated -= OnMessageUpdated;
        _messengerService.MessageDeleted -= OnMessageDeleted;
        _messengerService.UserTyping -= OnUserTyping;
        _messengerService.UserOnlineStatusChanged -= OnUserOnlineStatusChanged;
        
        _ = SendTypingIndicatorAsync(false);
    }
} 