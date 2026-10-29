namespace gitosfiesta
{
    public static class SessionContext
    {
        public static int UsuarioId { get; set; }
        public static string NombreCompleto { get; set; } = string.Empty;
        public static string Rol { get; set; } = string.Empty;
    }
}