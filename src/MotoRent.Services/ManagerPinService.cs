using System.Security.Cryptography;
using MotoRent.Domain.Core;
using MotoRent.Domain.DataContext;

namespace MotoRent.Services;

/// <summary>
/// Service for manager PIN management and verification.
/// Used for void approval workflow.
/// </summary>
public class ManagerPinService(CoreDataContext coreContext)
{
    private readonly CoreDataContext m_coreContext = coreContext;

    // Track lockouts per user (in-memory for MVP, consider server-side persistence for production)
    private static readonly Dictionary<string, LockoutInfo> s_lockouts = new();
    private static readonly object s_lockoutLock = new();

    private const int c_maxAttempts = 3;
    private const int c_lockoutMinutes = 5;
    private const int c_pbkdf2Iterations = 10000;
    private const int c_hashByteSize = 32;
    private const int c_saltByteSize = 16;

    /// <summary>
    /// Sets or updates a manager's PIN.
    /// PIN must be 4-6 digits.
    /// </summary>
    public async Task<SubmitOperation> SetPinAsync(string userName, string pin, string changedBy)
    {
        // Validate PIN format
        if (string.IsNullOrWhiteSpace(pin) || pin.Length < 4 || pin.Length > 6 || !pin.All(char.IsDigit))
            return SubmitOperation.CreateFailure("PIN must be 4-6 digits");

        var user = await m_coreContext.LoadOneAsync<User>(u => u.UserName == userName);
        if (user == null)
            return SubmitOperation.CreateFailure("User not found");

        // Generate salt and hash
        var salt = GenerateSalt();
        var hash = HashPin(pin, salt);

        user.ManagerPinSalt = Convert.ToBase64String(salt);
        user.ManagerPinHash = hash;

        using var session = m_coreContext.OpenSession(changedBy);
        session.Attach(user);
        return await session.SubmitChanges("SetManagerPin");
    }

    /// <summary>
    /// Verifies a manager's PIN for void approval.
    /// Returns success if PIN matches and not locked out.
    /// Tracks failed attempts and enforces lockout.
    /// </summary>
    public (bool IsValid, string? Error, int RemainingAttempts) VerifyPin(User manager, string enteredPin)
    {
        // Check lockout
        var lockoutInfo = GetLockoutInfo(manager.UserName);
        if (lockoutInfo.IsLockedOut)
        {
            var remainingSeconds = (int)(lockoutInfo.LockoutUntil!.Value - DateTimeOffset.Now).TotalSeconds;
            return (false, $"Too many attempts. Try again in {remainingSeconds} seconds.", 0);
        }

        // Verify PIN is set
        if (string.IsNullOrEmpty(manager.ManagerPinHash) || string.IsNullOrEmpty(manager.ManagerPinSalt))
            return (false, "Manager PIN not configured", 0);

        // Verify PIN
        var expectedHash = manager.ManagerPinHash;
        var salt = Convert.FromBase64String(manager.ManagerPinSalt);
        var actualHash = HashPin(enteredPin, salt);

        if (actualHash == expectedHash)
        {
            // Success - clear any failed attempts
            ClearLockout(manager.UserName);
            return (true, null, c_maxAttempts);
        }

        // Failed attempt
        var remaining = RecordFailedAttempt(manager.UserName);
        if (remaining == 0)
            return (false, $"Incorrect PIN. Account locked for {c_lockoutMinutes} minutes.", 0);

        return (false, $"Incorrect PIN. {remaining} attempts remaining.", remaining);
    }

    /// <summary>
    /// Checks if a user is currently locked out.
    /// </summary>
    public bool IsLockedOut(string userName)
    {
        return GetLockoutInfo(userName).IsLockedOut;
    }

    /// <summary>
    /// Gets seconds remaining in lockout, or 0 if not locked out.
    /// </summary>
    public int GetLockoutSecondsRemaining(string userName)
    {
        var info = GetLockoutInfo(userName);
        if (!info.IsLockedOut) return 0;
        return (int)(info.LockoutUntil!.Value - DateTimeOffset.Now).TotalSeconds;
    }

    /// <summary>
    /// Removes a manager's PIN (used by admin or when manager leaves).
    /// </summary>
    public async Task<SubmitOperation> RemovePinAsync(string userName, string changedBy)
    {
        var user = await m_coreContext.LoadOneAsync<User>(u => u.UserName == userName);
        if (user == null)
            return SubmitOperation.CreateFailure("User not found");

        user.ManagerPinSalt = null;
        user.ManagerPinHash = null;

        using var session = m_coreContext.OpenSession(changedBy);
        session.Attach(user);
        return await session.SubmitChanges("RemoveManagerPin");
    }

    #region Private Helpers

    private static byte[] GenerateSalt()
    {
        var salt = new byte[c_saltByteSize];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(salt);
        return salt;
    }

    private static string HashPin(string pin, byte[] salt)
    {
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            pin,
            salt,
            c_pbkdf2Iterations,
            HashAlgorithmName.SHA256,
            c_hashByteSize);
        return Convert.ToBase64String(hash);
    }

    private static LockoutInfo GetLockoutInfo(string userName)
    {
        lock (s_lockoutLock)
        {
            if (!s_lockouts.TryGetValue(userName, out var info))
                return new LockoutInfo();

            // Check if lockout has expired
            if (info.LockoutUntil.HasValue && info.LockoutUntil <= DateTimeOffset.Now)
            {
                s_lockouts.Remove(userName);
                return new LockoutInfo();
            }

            return info;
        }
    }

    private static int RecordFailedAttempt(string userName)
    {
        lock (s_lockoutLock)
        {
            if (!s_lockouts.TryGetValue(userName, out var info))
            {
                info = new LockoutInfo();
                s_lockouts[userName] = info;
            }

            info.FailedAttempts++;

            if (info.FailedAttempts >= c_maxAttempts)
            {
                info.LockoutUntil = DateTimeOffset.Now.AddMinutes(c_lockoutMinutes);
                return 0;
            }

            return c_maxAttempts - info.FailedAttempts;
        }
    }

    private static void ClearLockout(string userName)
    {
        lock (s_lockoutLock)
        {
            s_lockouts.Remove(userName);
        }
    }

    private class LockoutInfo
    {
        public int FailedAttempts { get; set; }
        public DateTimeOffset? LockoutUntil { get; set; }
        public bool IsLockedOut => LockoutUntil.HasValue && LockoutUntil > DateTimeOffset.Now;
    }

    #endregion
}
