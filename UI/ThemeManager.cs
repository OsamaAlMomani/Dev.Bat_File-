using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace AiSetupUi;

/// <summary>Holds the current theme and paints any form or control with it.</summary>
internal static class ThemeManager
{
    private static readonly ConditionalWeakTable<Control, object> Hooked = new();
    private static readonly ConditionalWeakTable<Button, object> ButtonHooked = new();

    public static Theme Current { get; private set; } = Themes.Light;
    public static event Action? Changed;

    public static void Initialize(AppSettings settings) => Current = Themes.ByName(settings.Theme);

    public static void Set(Theme theme, AppSettings settings)
    {
        if (ReferenceEquals(theme, Current)) return;
        Current = theme;
        settings.Theme = theme.Name;
        settings.Save();
        Changed?.Invoke();
    }

    /// <summary>Themes a form now and again every time the theme changes. onChanged is for custom-drawn parts.</summary>
    public static void Register(Form form, Action? onChanged = null)
    {
        void Reapply()
        {
            Apply(form);
            onChanged?.Invoke();
        }

        Changed += Reapply;
        form.FormClosed += (_, _) => Changed -= Reapply;
        form.HandleCreated += (_, _) => NativeTheme.UseDarkTitleBar(form.Handle, Current.IsDark);
        Apply(form);
    }

    /// <summary>Paints a control and everything inside it with the current theme.</summary>
    public static void Apply(Control root)
    {
        root.SuspendLayout();
        try
        {
            ApplyTo(root, Current);
            if (root is Form f && f.IsHandleCreated) NativeTheme.UseDarkTitleBar(f.Handle, Current.IsDark);
        }
        finally
        {
            root.ResumeLayout(true);
        }
    }

    /// <summary>Makes a details-view ListView follow the theme (the column headers need custom drawing).</summary>
    public static void PrepareListView(ListView lv)
    {
        lv.OwnerDraw = true;
        lv.DrawColumnHeader += (_, e) =>
        {
            var t = Current;
            using var bg = new SolidBrush(t.Neutral);
            using var pen = new Pen(t.Border);
            e.Graphics.FillRectangle(bg, e.Bounds);
            e.Graphics.DrawLine(pen, e.Bounds.Left, e.Bounds.Bottom - 1, e.Bounds.Right, e.Bounds.Bottom - 1);
            e.Graphics.DrawLine(pen, e.Bounds.Right - 1, e.Bounds.Top, e.Bounds.Right - 1, e.Bounds.Bottom - 1);
            TextRenderer.DrawText(e.Graphics, e.Header?.Text ?? "", lv.Font,
                Rectangle.Inflate(e.Bounds, -6, 0), t.ButtonText,
                TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis);
        };
        lv.DrawItem += (_, e) => e.DrawDefault = true;
        lv.DrawSubItem += (_, e) => e.DrawDefault = true;
    }

    private static void ApplyTo(Control c, Theme t)
    {
        switch (c)
        {
            case ProgressBar:
                break;

            case Button b:
                StyleButton(b, t);
                break;

            case TextBoxBase or ListView or TreeView:
                c.BackColor = t.Surface;
                c.ForeColor = t.Text;
                if (c is TextBox tb) tb.BorderStyle = BorderStyle.FixedSingle;
                if (c is TreeView tv) { tv.BorderStyle = BorderStyle.FixedSingle; tv.LineColor = t.Border; }
                HookScrollbars(c);
                break;

            case ComboBox cb:
                cb.FlatStyle = FlatStyle.Flat;
                cb.BackColor = t.Surface;
                cb.ForeColor = t.Text;
                break;

            case StatusStrip ss:
                ss.BackColor = t.Neutral;
                ss.ForeColor = t.Text;
                ss.Renderer = new ToolStripProfessionalRenderer(new StatusColorTable(t));
                foreach (ToolStripItem item in ss.Items)
                {
                    item.BackColor = t.Neutral;
                    item.ForeColor = item is ToolStripStatusLabel { Spring: true } ? t.Text : t.Muted;
                }
                break;

            case Label lbl when lbl.Tag is ThemeRole.Separator:
                lbl.BackColor = t.Border;
                lbl.Width = 1;
                break;

            case Panel p when p.Tag is ThemeRole.WarnBanner:
                p.BackColor = t.WarnBg;
                break;

            case Label or CheckBox:
                c.BackColor = Color.Transparent;
                c.ForeColor = c.Tag switch
                {
                    ThemeRole.Muted => t.Muted,
                    ThemeRole.WarnText => t.Warn,
                    _ => t.Text,
                };
                break;

            default: // forms, panels, table/flow layouts, split containers
                c.BackColor = t.Window;
                c.ForeColor = t.Text;
                break;
        }

        foreach (Control child in c.Controls) ApplyTo(child, t);
    }

    private static void StyleButton(Button b, Theme t)
    {
        var role = b.Tag is ThemeRole r ? r : ThemeRole.Default;
        b.UseVisualStyleBackColor = false;
        b.FlatStyle = FlatStyle.Flat;
        b.FlatAppearance.BorderSize = 1;
        b.FlatAppearance.BorderColor = t.Border;

        var bg = role switch
        {
            ThemeRole.Primary => t.Primary,
            ThemeRole.Danger => t.Danger,
            ThemeRole.Selected => t.Selected,
            _ => t.Neutral,
        };

        b.BackColor = bg;
        b.ForeColor = role == ThemeRole.Secondary ? t.Muted : t.ButtonText;
        b.FlatAppearance.MouseOverBackColor = Shift(bg, t.IsDark ? 22 : -15);
        b.FlatAppearance.MouseDownBackColor = Shift(bg, t.IsDark ? 32 : -25);

        if (!b.Enabled)
        {
            b.ForeColor = t.Muted;
            b.FlatAppearance.BorderColor = t.Border;
        }

        if (!ButtonHooked.TryGetValue(b, out _))
        {
            ButtonHooked.Add(b, new object());
            b.EnabledChanged += (_, _) => StyleButton(b, Current);
        }
    }

    private static Color Shift(Color c, int delta)
    {
        int Clamp(int v) => Math.Max(0, Math.Min(255, v));
        return Color.FromArgb(Clamp(c.R + delta), Clamp(c.G + delta), Clamp(c.B + delta));
    }

    private static void HookScrollbars(Control c)
    {
        if (!Hooked.TryGetValue(c, out _))
        {
            Hooked.Add(c, new object());
            c.HandleCreated += (_, _) => NativeTheme.SetControlTheme(c.Handle, Current.IsDark);
        }
        if (c.IsHandleCreated) NativeTheme.SetControlTheme(c.Handle, Current.IsDark);
    }

    private sealed class StatusColorTable : ProfessionalColorTable
    {
        private readonly Theme _t;
        public StatusColorTable(Theme t) => _t = t;
        public override Color StatusStripGradientBegin => _t.Neutral;
        public override Color StatusStripGradientEnd => _t.Neutral;
        public override Color ToolStripBorder => _t.Border;
    }
}

/// <summary>Windows-only extras: dark title bar and dark scrollbars. Failures are ignored (older Windows builds).</summary>
internal static class NativeTheme
{
    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

    [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
    private static extern int SetWindowTheme(IntPtr hwnd, string? subAppName, string? subIdList);

    public static void UseDarkTitleBar(IntPtr hwnd, bool dark)
    {
        try
        {
            var v = dark ? 1 : 0;
            if (DwmSetWindowAttribute(hwnd, 20, ref v, sizeof(int)) != 0)
                DwmSetWindowAttribute(hwnd, 19, ref v, sizeof(int));
        }
        catch { }
    }

    public static void SetControlTheme(IntPtr hwnd, bool dark)
    {
        try { SetWindowTheme(hwnd, dark ? "DarkMode_Explorer" : null, null); }
        catch { }
    }
}