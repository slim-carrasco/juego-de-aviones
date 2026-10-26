namespace JuegoAviones;

static class Program
{
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main()
    {
        // To customize application configuration such as set high DPI settings or default font,
        // see https://aka.ms/applicationconfiguration.
        ApplicationConfiguration.Initialize();
        using var acceso = new LoginDialog();
        if (acceso.ShowDialog() != DialogResult.OK || acceso.Sesion is not { } sesion) return;
        Application.Run(new Form1(sesion, acceso.Servidor));
    }
}
