namespace VeSCU.ConfigDialogs;

internal interface IConfigDialog<out T> : IDisposable
    where T : notnull
{
    DialogResult ShowDialog();
    T Value { get; }
}
