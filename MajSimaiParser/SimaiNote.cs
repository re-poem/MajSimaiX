namespace MajSimaiParser
{
    public class SimaiNote
    {
        public SimaiNoteType Type { get; set; }
        public int StartPosition { get; set; } = 1; //键位（1-8）

        public double HoldTime { get; set; }
        public bool IsBreak { get; set; }
        public bool IsEx { get; set; }
        public bool IsFakeRotate { get; set; }
        public bool IsForceStar { get; set; }
        public bool IsHanabi { get; set; }
        public bool IsSlideBreak { get; set; }
        public bool IsSlideNoHead { get; set; }

        public string RawContent { get; set; } //used for star explain

        public double SlideStartTime { get; set; }
        public double SlideTime { get; set; }
        public char TouchArea { get; set; } = ' ';
    }
}
