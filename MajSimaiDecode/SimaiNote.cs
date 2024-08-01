namespace MajSimaiDecode
{
    public class SimaiNote
    {
        public double holdTime;
        public bool isBreak;
        public bool isEx;
        public bool isFakeRotate;
        public bool isForceStar;
        public bool isHanabi;
        public bool isSlideBreak;
        public bool isSlideNoHead;

        public string? noteContent; //used for star explain
        public SimaiNoteType noteType;

        public double slideStartTime;
        public double slideTime;

        public int startPosition = 1; //键位（1-8）
        public char touchArea = ' ';
    }
}
