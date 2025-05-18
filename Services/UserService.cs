using AutoMapper;
using Microsoft.AspNetCore.Identity;
using ShareVault.API.DTOs;
using ShareVault.API.Interfaces;
using ShareVault.API.Models;

namespace ShareVault.API.Services
{
    public interface IUserService
    {
        Task<UserDto> RegisterAsync(RegisterDto registerDto);
        Task<UserDto> LoginAsync(LoginDto loginDto);
        Task<UserDto> GetByIdAsync(string id);
        Task<UserDto> GetByEmailAsync(string email);
        Task<IEnumerable<UserDto>> GetAllAsync();
        Task<IEnumerable<UserDto>> GetUsersByRoleAsync(string role);
    }

    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly IMapper _mapper;
        private readonly IPasswordHasher<User> _passwordHasher;
        private readonly ILogService _logService;

        public UserService(
            IUserRepository userRepository,
            IMapper mapper,
            IPasswordHasher<User> passwordHasher,
            ILogService logService)
        {
            _userRepository = userRepository;
            _mapper = mapper;
            _passwordHasher = passwordHasher;
            _logService = logService;
        }

        public async Task<UserDto> RegisterAsync(RegisterDto registerDto)
        {
            if (await _userRepository.IsEmailUniqueAsync(registerDto.Email))
            {
                throw new CustomException("Email already exists", 400);
            }

            if (await _userRepository.IsUsernameUniqueAsync(registerDto.Username))
            {
                throw new CustomException("Username already exists", 400);
            }

            var user = _mapper.Map<User>(registerDto);
            user.Id = Guid.NewGuid().ToString();
            user.CreatedAt = DateTime.UtcNow;
            user.PasswordHash = _passwordHasher.HashPassword(user, registerDto.Password);

            await _userRepository.AddAsync(user);
            await _userRepository.SaveChangesAsync();

            await _logService.LogSecurityAsync("User registered", user.Id);

            return _mapper.Map<UserDto>(user);
        }

        public async Task<UserDto> LoginAsync(LoginDto loginDto)
        {
            var user = await _userRepository.GetByEmailAsync(loginDto.Email);
            if (user == null)
            {
                throw new CustomException("Invalid email or password", 401);
            }

            var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, loginDto.Password);
            if (result == PasswordVerificationResult.Failed)
            {
                await _logService.LogSecurityAsync("Failed login attempt", user.Id);
                throw new CustomException("Invalid email or password", 401);
            }

            user.LastLoginAt = DateTime.UtcNow;
            _userRepository.Update(user);
            await _userRepository.SaveChangesAsync();

            await _logService.LogSecurityAsync("User logged in", user.Id);

            return _mapper.Map<UserDto>(user);
        }

        public async Task<UserDto> GetByIdAsync(string id)
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null)
            {
                throw new CustomException("User not found", 404);
            }

            return _mapper.Map<UserDto>(user);
        }

        public async Task<UserDto> GetByEmailAsync(string email)
        {
            var user = await _userRepository.GetByEmailAsync(email);
            if (user == null)
            {
                throw new CustomException("User not found", 404);
            }

            return _mapper.Map<UserDto>(user);
        }

        public async Task<IEnumerable<UserDto>> GetAllAsync()
        {
            var users = await _userRepository.GetAllAsync();
            return _mapper.Map<IEnumerable<UserDto>>(users);
        }

        public async Task<IEnumerable<UserDto>> GetUsersByRoleAsync(string role)
        {
            var users = await _userRepository.GetUsersByRoleAsync(role);
            return _mapper.Map<IEnumerable<UserDto>>(users);
        }
    }
} 