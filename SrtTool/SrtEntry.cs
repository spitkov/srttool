namespace SrtTool;

public class SrtEntry
{
    public int Index { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public string Text { get; set; } = string.Empty;

    public override string ToString()
    {
        return $"{Index}\r\n{StartTime:hh\\:mm\\:ss\\,fff} --> {EndTime:hh\\:mm\\:ss\\,fff}\r\n{Text}";
    }
} 