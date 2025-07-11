# Messenger API

Мессенджер с поддержкой реального времени, построенный на .NET 9 и SignalR.

## 🚀 Технологии

### Backend
- **.NET 9** - основная платформа
- **ASP.NET Core Web API** - REST API
- **Entity Framework Core 9** - ORM для работы с базой данных
- **SQL Server** - основная база данных
- **SignalR** - WebSocket соединения для реального времени
- **Redis** - кэширование и сессии
- **JWT Bearer** - аутентификация
- **AutoMapper** - маппинг объектов
- **FluentValidation** - валидация данных
- **Swagger/OpenAPI** - документация API

### UI Applications
- **WinForms** - десктопное приложение
- **Console Application** - пример использования

## 📋 Функциональность API

### 🔐 Аутентификация
- Регистрация пользователей
- Вход в систему с JWT токенами
- Автоматическое обновление токенов

### 👥 Пользователи
- Создание и управление профилями
- Поиск пользователей
- Отслеживание онлайн статуса

### 💬 Чаты
- Создание приватных и групповых чатов
- Управление участниками (добавление/удаление)
- Роли участников (Member, Admin, Owner) * без доп фич
- Отслеживание непрочитанных сообщений

### 📨 Сообщения
- Отправка текстовых сообщений
- Редактирование и удаление сообщений
- Ответы на сообщения

### 🔄 Реальное время
- Мгновенная доставка сообщений
- Изменения онлайн статуса
- Обновления в реальном времени

## 📦 Клиентская библиотека


### Базовое использование

```csharp
using Messenger.Client;
using Messenger.Shared;

// Создание клиента
var client = MessengerClientFactory.CreateClient("https://localhost:5001");

// Подписка на события
client.MessageReceived += message => 
    Console.WriteLine($"[NEW] {message.SenderUsername}: {message.Content}");

client.UserTyping += (userId, isTyping) => 
    Console.WriteLine($"[TYPING] User {userId} is {(isTyping ? "typing" : "not typing")}");

// Аутентификация
var isAuthenticated = await client.AuthenticateAsync("username", "password");
if (!isAuthenticated)
{
    // Регистрация если пользователь не существует
    await client.RegisterAsync("username", "email@example.com", "password");
    isAuthenticated = await client.AuthenticateAsync("username", "password");
}

// Подключение к SignalR
await client.ConnectAsync();

// Получение чатов
var chats = await client.GetChatsAsync();

// Создание нового чата
var newChat = await client.CreateChatAsync(new CreateChatDto
{
    Name = "My Chat",
    Description = "Chat description",
    Type = ChatType.Group,
    MemberIds = new List<Guid>()
});

// Отправка сообщения
var message = await client.SendMessageAsync(new CreateMessageDto
{
    Content = "Hello, world!",
    ChatId = newChat.Id,
    Type = MessageType.Text
});

// Получение сообщений чата
var messages = await client.GetChatMessagesAsync(newChat.Id, skip: 0, take: 50);

// Индикатор печати
await client.SendTypingIndicatorAsync(newChat.Id, true);
await Task.Delay(2000);
await client.SendTypingIndicatorAsync(newChat.Id, false);

// Поиск пользователей
var users = await client.SearchUsersAsync("john");

// Отключение
await client.DisconnectAsync();
```

### Основные методы

#### Аутентификация
- `AuthenticateAsync(username, password)` - вход в систему
- `RegisterAsync(username, email, password)` - регистрация
- `SetToken(token)` - установка JWT токена

#### Подключение
- `ConnectAsync()` - подключение к SignalR
- `DisconnectAsync()` - отключение
- `IsConnected` - статус подключения

#### Чаты
- `GetChatsAsync()` - получение списка чатов
- `GetChatAsync(chatId)` - получение информации о чате
- `CreateChatAsync(createChatDto)` - создание чата
- `UpdateChatAsync(chatId, updateChatDto)` - обновление чата
- `AddMemberToChatAsync(chatId, memberId)` - добавление участника
- `RemoveMemberFromChatAsync(chatId, memberId)` - удаление участника
- `LeaveChatAsync(chatId)` - выход из чата
- `DeleteChatAsync(chatId)` - удаление чата
- `MarkChatAsReadAsync(chatId)` - отметка как прочитанное

#### Сообщения
- `GetChatMessagesAsync(chatId, skip, take)` - получение сообщений
- `GetMessageAsync(messageId)` - получение сообщения
- `GetRepliesAsync(messageId)` - получение ответов
- `SendMessageAsync(createMessageDto)` - отправка сообщения
- `UpdateMessageAsync(messageId, updateMessageDto)` - обновление сообщения
- `DeleteMessageAsync(messageId)` - удаление сообщения
- `SendTypingIndicatorAsync(chatId, isTyping)` - индикатор печати

#### Пользователи
- `SearchUsersAsync(query)` - поиск пользователей
- `GetUserAsync(userId)` - получение пользователя
- `GetCurrentUserAsync()` - получение текущего пользователя
- `GetOnlineUsersAsync()` - получение онлайн пользователей

### События

```csharp
// Новое сообщение
client.MessageReceived += (MessageDto message) => { };

// Обновленное сообщение
client.MessageUpdated += (MessageDto message) => { };

// Удаленное сообщение
client.MessageDeleted += (Guid messageId) => { };

// Пользователь присоединился к чату
client.UserJoinedChat += (Guid chatId) => { };

// Пользователь покинул чат
client.UserLeftChat += (Guid chatId) => { };

// Индикатор печати
client.UserTyping += (Guid userId, bool isTyping) => { };

// Изменение онлайн статуса
client.UserOnlineStatusChanged += (Guid userId, bool isOnline) => { };
```

## 🏗️ Архитектура

Проект использует Clean Architecture с разделением на слои:

- **API** - контроллеры и SignalR хабы
- **Application** - бизнес-логика и сервисы
- **Domain** - сущности и интерфейсы
- **Infrastructure** - репозитории и внешние сервисы
- **Shared** - общие DTO и модели

## 🚀 Запуск

1. Настройте строки подключения в `appsettings.json`
2. Запустите миграции базы данных
3. Запустите API сервер
4. Используйте клиентскую библиотеку для подключения
5. Запуск редис сервака
6. Молитвы

## 📝 Примеры

Смотрите `Messenger.ConsoleExample` для полного примера использования клиентской библиотеки. 

## 📝 Демо
https://youtu.be/V47sOJG7r8o