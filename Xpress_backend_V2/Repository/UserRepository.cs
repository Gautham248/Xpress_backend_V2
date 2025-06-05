using Microsoft.EntityFrameworkCore;
using System;
using System.Security.Cryptography;
using System.Text;
using Xpress_backend_V2.Data;
using Xpress_backend_V2.Interface;
using Xpress_backend_V2.Models;
using Xpress_backend_V2.Models.DTO;

namespace Xpress_backend_V2.Repository
{
    public class UserRepository : IUserServices , IUserRepository
    {
        private readonly ApiDbContext _context;

        public UserRepository(ApiDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<User>> GetAllAsync()
        {
            return await _context.Users
                .Include(u => u.TravelRequests)
                .Include(u => u.CreatedTicketOptions)
                .Where(u => u.IsActive)
                .ToListAsync();
        }

        public async Task<User> GetByIdAsync(int userId)
        {
            return await _context.Users
                .Include(u => u.TravelRequests)
                .Include(u => u.CreatedTicketOptions)
                .FirstOrDefaultAsync(u => u.UserId == userId && u.IsActive);
        }

        public async Task AddAsync(User user)
        {
            user.CreatedAt = DateTime.UtcNow;
            user.UpdatedAt = DateTime.UtcNow;
            user.IsActive = true;
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(User user)
        {
            user.UpdatedAt = DateTime.UtcNow;
            _context.Entry(user).State = EntityState.Modified;
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                user.IsActive = false;
                await _context.SaveChangesAsync();
            }
        }

        public async Task<User> GetByEmployeeNameAsync(string employeeName)
        {
            return await _context.Users
                .Include(u => u.TravelRequests)
                .Include(u => u.CreatedTicketOptions)
                .FirstOrDefaultAsync(u => u.EmployeeName == employeeName && u.IsActive);
        }


        public async Task<User> LoginUser(string email, string password)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.EmployeeEmail == email);

            if (user != null)
            {
                if (user.Password == HashPassword(password))
                    return user;
            }

            return null;
        }


        public async Task<User> RegisterUser(UserRegisterDTO user)
        {
            try
            {
                Console.WriteLine($"User Email: {user.EmployeeEmail}");

                var userExist = await _context.Users.FirstOrDefaultAsync(u => u.EmployeeEmail == user.EmployeeEmail);
                if (userExist == null)
                {
                    var newUser = new User
                    {
                        EmployeeName = user.EmployeeName,
                        EmployeeEmail = user.EmployeeEmail,
                        Password = HashPassword(user.Password),
                        PhoneNumber = user.PhoneNumber ?? "",
                        UserRole = user.UserRole,
                        Department = user.Department,
                        IsActive = true, // Assuming new users are active by default
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    await _context.Users.AddAsync(newUser);
                    await _context.SaveChangesAsync();
                    return newUser;
                }
                else
                {
                    return null; // Email already exists
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error: {e.Message}");
                return null;
            }
        }




        public string HashPassword(string password)
        {
            using (SHA512 sha512 = SHA512.Create())
            {
                byte[] hashedString = sha512.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(hashedString);
            }
        }








    }
}
