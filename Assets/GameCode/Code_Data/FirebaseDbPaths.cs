public static class FirebaseDbPaths
{
    public const string Users = "users";
    public const string Profile = "profile";
    public const string Inventory = "inventory";
    public const string Stats = "stats";

    public static string UserRoot(string userId)
    {
        return IsValidUserId(userId) ? $"{Users}/{userId}" : string.Empty;
    }

    public static string UserProfile(string userId)
    {
        return IsValidUserId(userId) ? $"{Users}/{userId}/{Profile}" : string.Empty;
    }

    public static string UserInventory(string userId)
    {
        return IsValidUserId(userId) ? $"{Users}/{userId}/{Inventory}" : string.Empty;
    }

    public static string UserStats(string userId)
    {
        return IsValidUserId(userId) ? $"{Users}/{userId}/{Stats}" : string.Empty;
    }

    public static bool IsValidUserId(string userId)
    {
        return !string.IsNullOrWhiteSpace(userId);
    }
}
