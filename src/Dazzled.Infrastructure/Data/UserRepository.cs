using Ardalis.Result;
using Dazzled.Application.Abstractions;
using Dazzled.Domain.Entities;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Dazzled.Infrastructure.Data;

public class UserRepository(DazzledDbContext db) : IUserRepository
{
    // SQL Server: duplicate key in a unique index / unique constraint.
    private const int DuplicateKeyError = 2601;
    private const int DuplicateConstraintError = 2627;

    public async Task<Result<User>> GetByEmailAsync(string email, CancellationToken ct = default)
    {
        try
        {
            var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);
            return user is null ? Result.NotFound() : Result.Success(user);
        }
        catch (Exception ex)
        {
            return Result.Error(ex.Message);
        }
    }

    public async Task<Result<User>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            var user = await db.Users.FirstOrDefaultAsync(u => u.Id == id, ct);
            return user is null ? Result.NotFound() : Result.Success(user);
        }
        catch (Exception ex)
        {
            return Result.Error(ex.Message);
        }
    }

    public async Task<Result<List<User>>> GetUsersAsync(CancellationToken ct = default)
    {
        try
        {
            return Result.Success(await db.Users.OrderBy(u => u.Name).ToListAsync(ct));
        }
        catch (Exception ex)
        {
            return Result.Error(ex.Message);
        }
    }

    public async Task<Result<User>> CreateUserAsync(User user, CancellationToken ct = default)
    {
        try
        {
            db.Users.Add(user);
            await db.SaveChangesAsync(ct);
            return Result.Success(user);
        }
        catch (DbUpdateException ex) when (IsDuplicateEmail(ex))
        {
            return Result.Conflict($"A user with email '{user.Email}' already exists.");
        }
        catch (Exception ex)
        {
            return Result.Error(ex.Message);
        }
    }

    public async Task<Result<User>> UpdateUserAsync(User user, CancellationToken ct = default)
    {
        try
        {
            db.Users.Update(user);
            await db.SaveChangesAsync(ct);
            return Result.Success(user);
        }
        catch (DbUpdateException ex) when (IsDuplicateEmail(ex))
        {
            return Result.Conflict($"A user with email '{user.Email}' already exists.");
        }
        catch (Exception ex)
        {
            return Result.Error(ex.Message);
        }
    }

    private static bool IsDuplicateEmail(DbUpdateException ex) =>
        ex.InnerException is SqlException sql &&
        sql.Number is DuplicateKeyError or DuplicateConstraintError;
}
