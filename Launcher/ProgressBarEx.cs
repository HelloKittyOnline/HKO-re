using System.ComponentModel;

namespace Launcher;

/// <summary>
/// Version of progress bar with additional features, such as displaying text over progress
/// </summary>
/// <remarks>
/// Created by Arlen Feldman. see article at https://cowthulu.com/winforms-progress-bar-with-text
/// Freely distributable, modifiable, collagable, inflatable. Use any way you like. Attribution/blame not required.
/// </remarks>
public class ProgressBarEx : ProgressBar {
    // ------------------------------------------------------------------------------------------------------
    // Constants and fields

    private const int WM_PAINT = 0xF;
    private const int WS_EX_COMPOSITED = 0x2000_000;

    private TextDisplayType _style = TextDisplayType.Percent;
    private string _manualText = "";

    // ------------------------------------------------------------------------------------------------------
    // Construction

    /// <summary>Constructor</summary>
    public ProgressBarEx() {

    }

    // ------------------------------------------------------------------------------------------------------
    // Properties

    /// <summary>What text to display</summary>
    [Category("Appearance")]
    [Description("What type of text to display on the progress bar.")]
    [DefaultValue(TextDisplayType.Percent)]
    public TextDisplayType DisplayType {
        get => _style;
        set {
            _style = value;
            Invalidate();
        }
    }

    /// <summary>If the TextStyle is set to Manual, the text to display</summary>
    [Category("Appearance")]
    [Description("If DisplayType is Manual, the text to display.")]
    [DefaultValue("")]
    public string ManualText {
        get => _manualText;
        set {
            _manualText = value;
            Invalidate();
        }
    }

    /// <summary>Color for the text on the bar</summary>
    /// <remarks>Can't use the ForeColor because it <i>technically</i> is the bar color, although this is ignored when VisualStyles are enabled</remarks>
    [Category("Appearance")]
    [Description("Color of text on bar.")]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public Color TextColor { get; set; } = SystemColors.ControlText;

    /// <summary>The font for the text</summary>
    /// <remarks>Have to override this just to restore the Browsable flags so that it will show up in the designer. ProgressBar hides it.</remarks>
    [Browsable(true), EditorBrowsable(EditorBrowsableState.Always)]
    public override Font Font { get => base.Font; set => base.Font = value; }

    // ------------------------------------------------------------------------------------------------------
    // Implementation

    /// <summary>Windows-control creation parameters</summary>
    protected override CreateParams CreateParams {
        get {
            var parms = base.CreateParams;

            // Force control to double-buffer painting
            parms.ExStyle |= WS_EX_COMPOSITED;

            return parms;
        }
    }

    /// <summary>Handle Windows messages</summary>
    /// <param name="m"></param>
    protected override void WndProc(ref Message m) {
        base.WndProc(ref m);

        if(m.Msg == WM_PAINT)
            AdditionalPaint(m);
    }

    /// <summary>Does the actual painting of the text</summary>
    /// <param name="m"></param>
    private void AdditionalPaint(Message m) {
        if(DisplayType == TextDisplayType.None)
            return;

        using var g = Graphics.FromHwnd(Handle);

        var rect = new Rectangle(0, 0, Width, Height);
        var format = new StringFormat(StringFormatFlags.NoWrap) {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center
        };

        using var textBrush = new SolidBrush(TextColor);
        g.DrawString(GetDisplayText(), Font, textBrush, rect, format);
    }

    private string GetDisplayText() {
        return DisplayType switch {
            TextDisplayType.Percent => Maximum != 0 ? $"{(int)((float)Value / Maximum * 100)} %" : "",
            TextDisplayType.Count => $"{Value} / {Maximum}",
            TextDisplayType.Manual => ManualText,
            _ => throw new ArgumentOutOfRangeException(nameof(DisplayType))
        };
    }

    // ------------------------------------------------------------------------------------------------------
    // Nested classes and enums

    /// <summary>Supported display options</summary>
    public enum TextDisplayType {
        /// <summary>No display text</summary>
        None,
        /// <summary>Display percentage of progress</summary>
        Percent,
        /// <summary>Show x / y of progress</summary>
        Count,
        /// <summary>Display a manually-provided string</summary>
        Manual
    }
}
