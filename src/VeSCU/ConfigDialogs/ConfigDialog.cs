namespace VeSCU.ConfigDialogs;

internal static class ConfigDialog
{
    public static bool Prompt<TDialog, TSection>(
        [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out TSection? result)
        where TDialog : Form, IConfigDialog<TSection>, new()
        where TSection : notnull
    {
        using var dialog = new TDialog();
        if (dialog.ShowDialog() == DialogResult.OK)
        {
            result = dialog.Value;
            return true;
        }

        result = default;
        return false;
    }
}
