namespace ShareVault.API.Models
{
    /// <summary>
    /// Bildirim türlerini temsil eden enum
    /// </summary>
    public enum NotificationType
    {
        FileShared,
        FolderShared,
        NewVersion,
        SystemMessage,
        SecurityAlert
    }
}
