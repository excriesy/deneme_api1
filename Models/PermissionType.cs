namespace ShareVault.API.Models
{
    /// <summary>
    /// Dosya ve klasör paylaşımları için izin türlerini temsil eden enum
    /// </summary>
    public enum PermissionType
    {
        Read,
        Write,
        Delete,
        Share,
        FullControl
    }
}
