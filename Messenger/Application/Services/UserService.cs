using AutoMapper;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Messenger.Application.Interfaces;
using Messenger.Domain.Entities;
using Messenger.Domain.Interfaces;
using Messenger.Shared;

namespace Messenger.Application.Services;

public class UserService(IUserRepository userRepository, IMapper mapper, IConfiguration configuration) : IUserService
{
    private readonly IUserRepository _userRepository = userRepository;
    private readonly IMapper _mapper = mapper;
    private readonly IConfiguration _configuration = configuration;

    public async Task<UserDto> CreateUserAsync(CreateUserDto createUserDto)
    {
        var existingUser = await _userRepository.GetByUsernameAsync(createUserDto.Username);
        if (existingUser != null)
            throw new InvalidOperationException("Username already exists");

        existingUser = await _userRepository.GetByEmailAsync(createUserDto.Email);
        if (existingUser != null)
            throw new InvalidOperationException("Email already exists");

        var user = _mapper.Map<User>(createUserDto);
        user.PasswordHash = HashPassword(createUserDto.Password);

        var savedUser = await _userRepository.AddAsync(user);
        return _mapper.Map<UserDto>(savedUser);
    }

    public async Task<UserDto> UpdateUserAsync(Guid userId, UpdateUserDto updateUserDto)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
            throw new InvalidOperationException("User not found");

        if (!string.IsNullOrEmpty(updateUserDto.Username) && updateUserDto.Username != user.Username)
        {
            var existingUser = await _userRepository.GetByUsernameAsync(updateUserDto.Username);
            if (existingUser != null)
                throw new InvalidOperationException("Username already exists");
            user.Username = updateUserDto.Username;
        }

        if (!string.IsNullOrEmpty(updateUserDto.Email) && updateUserDto.Email != user.Email)
        {
            var existingUser = await _userRepository.GetByEmailAsync(updateUserDto.Email);
            if (existingUser != null)
                throw new InvalidOperationException("Email already exists");
            user.Email = updateUserDto.Email;
        }

        if (!string.IsNullOrEmpty(updateUserDto.AvatarUrl))
            user.AvatarUrl = updateUserDto.AvatarUrl;

        var updatedUser = await _userRepository.UpdateAsync(user);
        return _mapper.Map<UserDto>(updatedUser);
    }

    public async Task<UserDto?> GetUserAsync(Guid userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        return user != null ? _mapper.Map<UserDto>(user) : null;
    }

    public async Task<UserDto?> GetUserByUsernameAsync(string username)
    {
        var user = await _userRepository.GetByUsernameAsync(username);
        return user != null ? _mapper.Map<UserDto>(user) : null;
    }

    public async Task<IEnumerable<UserDto>> GetOnlineUsersAsync()
    {
        var users = await _userRepository.GetOnlineUsersAsync();
        return _mapper.Map<IEnumerable<UserDto>>(users);
    }

    public async Task UpdateOnlineStatusAsync(Guid userId, bool isOnline)
    {
        await _userRepository.UpdateOnlineStatusAsync(userId, isOnline);
    }

    public async Task<string> AuthenticateUserAsync(string username, string password)
    {
        var user = await _userRepository.GetByUsernameAsync(username);
        if (user == null)
            throw new InvalidOperationException("Invalid username or password");

        if (!VerifyPassword(password, user.PasswordHash))
            throw new InvalidOperationException("Invalid username or password");

        return GenerateJwtToken(user);
    }

    public async Task<IEnumerable<UserDto>> SearchUsersAsync(string query)
    {
        var users = await _userRepository.SearchUsersAsync(query);
        var userDtos = _mapper.Map<IEnumerable<UserDto>>(users);
        
        // Добавляем отладочную информацию
        Console.WriteLine($"UserService.SearchUsersAsync: Found {users.Count()} users in database");
        foreach (var user in users)
        {
            Console.WriteLine($"UserService.SearchUsersAsync: User from DB - {user.Username} ({user.Email})");
        }
        
        Console.WriteLine($"UserService.SearchUsersAsync: Mapped to {userDtos.Count()} DTOs");
        foreach (var userDto in userDtos)
        {
            Console.WriteLine($"UserService.SearchUsersAsync: User DTO - {userDto.Username} ({userDto.Email})");
        }
        
        return userDtos;
    }

    private string HashPassword(string password)
    {
        var hashedBytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(hashedBytes);
    }

    private bool VerifyPassword(string password, string hash)
    {
        return HashPassword(password) == hash;
    }

    private string GenerateJwtToken(User user)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(_configuration["Jwt:Key"]!);
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email)
            ]),
            Expires = DateTime.UtcNow.AddDays(7),
            Issuer = _configuration["Jwt:Issuer"],
            Audience = _configuration["Jwt:Audience"],
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
} 