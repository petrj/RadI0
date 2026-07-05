namespace RadI0;

using System;
using Terminal.Gui;

public class EqualizerView : View
{
    private const int BandCount = 10;
    private const int Min = -12;
    private const int Max = 12;

    private readonly EventHandler? _onEqualizerChanged;

    private readonly string[] labels =
    {
        "31","62","125","250","500",
        "1k","2k","4k","8k","16k"
    };

    public int[] Values { get; private set; }

    private int selected = 0;

    private int top = 1;
    private int bottom = 15;
    private int center;

    public EqualizerView(EventHandler? onEqualizerChanged, int[] initial)
    {
        CanFocus = true;
        TabStop = true;
        _onEqualizerChanged = onEqualizerChanged;

        Values = new int[BandCount];

        if (initial != null)
        {
            for (int i = 0; i < BandCount && i < initial.Length; i++)
                Values[i] = initial[i];
        }
    }

    public void Reset()
    {
        for (int i = 0; i < BandCount; i++)
            Values[i] = 0;

        SetNeedsDisplay();
        _onEqualizerChanged?.Invoke(this, new EqualizerEventArgs() { Values = Values });
    }

    public override bool ProcessKey(KeyEvent keyEvent)
    {
        try
        {
            var key = keyEvent.Key;

            switch (key)
            {
                case Key.CursorLeft:
                    selected = Math.Max(0, selected - 1);
                    SetNeedsDisplay();
                    return true;

                case Key.CursorRight:
                    selected = Math.Min(BandCount - 1, selected + 1);
                    SetNeedsDisplay();
                    return true;

                case Key.CursorUp:
                    Values[selected] = Math.Min(Max, Values[selected] + 1);
                    SetNeedsDisplay();
                    return true;

                case Key.CursorDown:
                    Values[selected] = Math.Max(Min, Values[selected] - 1);
                    SetNeedsDisplay();
                    return true;

                case Key.Home:
                    Values[selected] = Max;
                    SetNeedsDisplay();
                    return true;

                case Key.End:
                    Values[selected] = Min;
                    SetNeedsDisplay();
                    return true;

                case Key.Space:
                    Values[selected] = 0;
                    SetNeedsDisplay();
                    return true;
            }

            return base.ProcessKey(keyEvent);
        } finally
        {
            _onEqualizerChanged?.Invoke(this, new EqualizerEventArgs() { Values = Values });
        }
    }

    public override void Redraw(Rect bounds)
    {
        Driver.SetAttribute(ColorScheme.Normal);

        Clear();

        int width = Bounds.Width;

        center = (top + bottom) / 2;

        int spacing = width / BandCount;

        for (int i = 0; i < BandCount; i++)
        {
            int x = 2 + i * spacing;
            DrawBand(i, x);
        }

        // help text
        Move(1, bottom + 2);
        Driver.AddStr("← → select  ↑ ↓ change  Space reset band");
    }

    private void DrawBand(int index, int x)
    {
        bool active = (index == selected) && HasFocus;

        // vertical guide line
        for (int y = top; y <= bottom; y++)
        {
            Move(x, y);
            Driver.AddRune('│');
        }

        // center line
        Move(x - 1, center);
        Driver.AddRune('─');
        Move(x, center);
        Driver.AddRune('┼');
        Move(x + 1, center);
        Driver.AddRune('─');

        int value = Values[index];

        //Driver.SetAttribute(active ? ColorScheme.Focus : ColorScheme.Normal);

        // positive
        if (value > 0)
        {
            for (int i = 1; i <= value; i++)
            {
                int y = center - i;
                if (y < top) break;

                Move(x, y);
                Driver.AddRune('#');
            }
        }
        // negative
        else if (value < 0)
        {
            for (int i = 1; i <= -value; i++)
            {
                int y = center + i;
                if (y > bottom) break;

                Move(x, y);
                Driver.AddRune('#');
            }
        }

        // frequency label
        Driver.SetAttribute(ColorScheme.Normal);
        string label = labels[index];
        Move(x - label.Length / 2, bottom + 1);
        Driver.AddStr(label);

        // value label
        string val = value.ToString();
        Move(x - val.Length / 2, bottom + 2);
        Driver.AddStr(val);

        // highlight indicator
        if (active)
        {
            Move(x, top - 1);
            Driver.AddRune('*');
        }
    }
}