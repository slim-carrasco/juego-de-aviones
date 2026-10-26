using System.Runtime.InteropServices;

namespace JuegoAviones;

internal static class UiTheme
{
    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr handle, int attribute, ref int value, int size);

    public static readonly Color Background = Color.FromArgb(14, 23, 36);
    public static readonly Color Surface = Color.FromArgb(23, 38, 55);
    public static readonly Color Input = Color.FromArgb(29, 49, 68);
    public static readonly Color Button = Color.FromArgb(38, 67, 91);
    public static readonly Color ButtonHover = Color.FromArgb(49, 86, 115);
    public static readonly Color Accent = Color.FromArgb(19, 124, 121);
    public static readonly Color AccentHover = Color.FromArgb(26, 151, 146);
    public static readonly Color Border = Color.FromArgb(100, 137, 158);
    public static readonly Color Text = Color.White;

    public static void StyleTitleBar(Form form)
    {
        form.HandleCreated += (_, _) =>
        {
            // Windows 11: COLORREF para el fondo y el texto de la barra nativa.
            int caption = ColorTranslator.ToWin32(Surface);
            int foreground = ColorTranslator.ToWin32(Text);
            DwmSetWindowAttribute(form.Handle, 35, ref caption, sizeof(int));
            DwmSetWindowAttribute(form.Handle, 36, ref foreground, sizeof(int));
        };
    }

    public static void StyleButton(Button button, bool accent = false)
    {
        button.Height = 30;
        button.BackColor = accent ? Accent : Button;
        button.ForeColor = Text;
        button.UseVisualStyleBackColor = false;
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderColor = accent ? AccentHover : Border;
        button.FlatAppearance.BorderSize = 1;
        button.FlatAppearance.MouseOverBackColor = accent ? AccentHover : ButtonHover;
        button.FlatAppearance.MouseDownBackColor = accent ? Accent : Button;
    }
}
