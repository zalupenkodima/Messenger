# Messenger WinForms Application

## Описание

WinForms приложение для демонстрации функционала Messenger библиотеки. Приложение реализовано с использованием чистой архитектуры, принципов SOLID и паттерна MVVM.

## Архитектура

### Слои приложения

#### 1. Core (Ядро)
- **Interfaces** - интерфейсы для сервисов приложения
  - `IMessengerService` - интерфейс для работы с мессенджером
  - `INavigationService` - интерфейс для навигации между формами
  - `IUserSessionService` - интерфейс для управления сессией пользователя

#### 2. Infrastructure (Инфраструктура)
- **Services** - реализации сервисов
  - `MessengerService` - обертка над клиентской библиотекой
  - `NavigationService` - управление навигацией между формами
  - `UserSessionService` - управление сессией пользователя

#### 3. Presentation (Представление)
- **ViewModels** - модели представления (MVVM)
  - `BaseViewModel` - базовый класс с поддержкой уведомлений
  - `LoginViewModel` - модель для формы входа
  - `MainViewModel` - модель для главной формы
- **Forms** - пользовательский интерфейс
  - `LoginForm` - форма входа/регистрации
  - `MainForm` - главная форма со списком чатов

### Принципы SOLID

#### Single Responsibility Principle (SRP)
- Каждый класс имеет одну ответственность
- `MessengerService` - только работа с API
- `NavigationService` - только навигация
- `UserSessionService` - только управление сессией

#### Open/Closed Principle (OCP)
- Расширение функционала через интерфейсы
- Легкое добавление новых форм и ViewModels

#### Liskov Substitution Principle (LSP)
- Все реализации интерфейсов взаимозаменяемы
- Базовый класс `BaseViewModel` может быть заменен наследниками

#### Interface Segregation Principle (ISP)
- Интерфейсы разделены по функциональности
- `IMessengerService` - только для работы с мессенджером
- `INavigationService` - только для навигации

#### Dependency Inversion Principle (DIP)
- Зависимости от абстракций, а не от конкретных реализаций
- Использование DI контейнера для инъекции зависимостей

### Паттерны

#### MVVM (Model-View-ViewModel)
- **Model** - DTOs из Messenger.Shared
- **View** - WinForms формы
- **ViewModel** - классы в папке ViewModels

#### Dependency Injection
- Использование Microsoft.Extensions.DependencyInjection
- Регистрация сервисов в Program.cs

#### Observer Pattern
- События в ViewModels для уведомления об изменениях
- Подписка на события мессенджера

## Функциональность

### Реализовано
- ✅ Аутентификация пользователей
- ✅ Регистрация новых пользователей
- ✅ Подключение к SignalR хабу
- ✅ Просмотр списка чатов
- ✅ Поиск чатов
- ✅ Навигация между формами
- ✅ Управление сессией пользователя
- ✅ Современный UI дизайн

### В разработке
- 🔄 Форма чата (отправка/получение сообщений)
- 🔄 Поиск пользователей
- 🔄 Создание новых чатов
- 🔄 Редактирование сообщений
- 🔄 Индикаторы печати

## Запуск

1. Убедитесь, что API сервер запущен на `https://localhost:5001`
2. Соберите решение: `dotnet build`
3. Запустите WinForms приложение: `dotnet run --project Messenger.WinForms`

## Конфигурация

Настройки приложения находятся в файле `appsettings.json`:
```json
{
  "MessengerApi": {
    "BaseUrl": "https://localhost:5001"
  }
}
```

## Структура проекта

```
Messenger.WinForms/
├── Core/
│   └── Interfaces/
│       ├── IMessengerService.cs
│       ├── INavigationService.cs
│       └── IUserSessionService.cs
├── Infrastructure/
│   └── Services/
│       ├── MessengerService.cs
│       ├── NavigationService.cs
│       └── UserSessionService.cs
├── Presentation/
│   ├── ViewModels/
│   │   ├── BaseViewModel.cs
│   │   ├── LoginViewModel.cs
│   │   └── MainViewModel.cs
│   └── Forms/
│       ├── LoginForm.cs
│       └── MainForm.cs
├── Program.cs
├── appsettings.json
└── Messenger.WinForms.csproj
```

## Технологии

- **.NET 9.0** - платформа разработки
- **WinForms** - пользовательский интерфейс
- **Microsoft.Extensions.DependencyInjection** - DI контейнер
- **Microsoft.Extensions.Hosting** - хостинг приложения
- **Microsoft.Extensions.Logging** - логирование
- **Messenger.Client** - клиентская библиотека
- **Messenger.Shared** - общие DTOs 