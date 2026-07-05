namespace RadI0;

using System;
using Terminal.Gui;

public static class EqualizerDialog
{
    public static bool Show(View owner, int[] values, EventHandler? onEqualizerChanged)
    {
        if (values == null || values.Length != 10)
            throw new ArgumentException("Equalizer requires exactly 10 values.");

        bool accepted = false;

        var dlg = new Dialog("Equalizer", 60, 24);

        dlg.ColorScheme = Colors.ColorSchemes["Dialog"];

        // clone working values
        var eq = new EqualizerView(onEqualizerChanged, values)
        {
            X = 1,
            Y = 1,
            Width = Dim.Fill(1),
            Height = 18
        };

        dlg.Add(eq);

        // RESET button
        var reset = new Button("Reset")
        {
            X = 2,
            Y = Pos.Bottom(eq) + 1
        };

        reset.Clicked += () =>
        {
            eq.Reset();
            dlg.SetNeedsDisplay();
        };

        dlg.Add(reset);

        // OK button
        var ok = new Button("OK", is_default: true)
        {
            X = Pos.Right(reset) + 2,
            Y = Pos.Bottom(eq) + 1
        };

        ok.Clicked += () =>
        {
            for (int i = 0; i < 10; i++)
                values[i] = eq.Values[i];

            accepted = true;
            Application.RequestStop();
        };

        dlg.Add(ok);

        // Cancel button
        var cancel = new Button("Cancel")
        {
            X = Pos.Right(ok) + 2,
            Y = Pos.Bottom(eq) + 1
        };

        cancel.Clicked += () =>
        {
            accepted = false;
            Application.RequestStop();
        };

        dlg.Add(cancel);

        // ESC closes dialog
        dlg.KeyPress += (args) =>
        {
            if (args.KeyEvent.Key == Key.Esc)
            {
                accepted = false;
                Application.RequestStop();
                args.Handled = true;
            }
        };

        Application.Run(dlg);

        return accepted;
    }
}