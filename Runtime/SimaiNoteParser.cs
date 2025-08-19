using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
#nullable enable
namespace MajSimai
{
    using rString = ReadOnlySpan<char>;
    internal static class SimaiNoteParser
    {
        internal static SimaiNote[] GetNotes(double timing, double bpm, string noteContent)
        {
            var simaiNotes = new List<SimaiNote>();
            GetNotes(timing, bpm, noteContent, simaiNotes);

            return simaiNotes.ToArray();
        }
        internal static void GetNotes(double timing, double bpm, string noteContent, IList<SimaiNote> buffer)
        {
            if (string.IsNullOrEmpty(noteContent))
            {
                return;
            }
            try
            {
                var dummy = 0;
                if (noteContent.Length == 2 && int.TryParse(noteContent, out dummy)) //连写数字
                {
                    buffer.Add(GetSingleNote(timing, bpm, noteContent[0].ToString()));
                    buffer.Add(GetSingleNote(timing, bpm, noteContent[1].ToString()));
                    return;
                }

                if (noteContent.Contains('/'))
                {
                    var notes = noteContent.Split('/');
                    foreach (var note in notes)
                    {
                        if (note.Contains('*'))
                        {
                            GetSameHeadSlide(timing, bpm, note, buffer);
                        }
                        else
                        {
                            buffer.Add(GetSingleNote(timing, bpm, note));
                        }
                    }
                    return;
                }

                if (noteContent.Contains('*'))
                {
                    GetSameHeadSlide(timing, bpm, noteContent, buffer);
                    return;
                }

                buffer.Add(GetSingleNote(timing, bpm, noteContent));
            }
            catch(Exception e)
            {
                Debug.Fail(e.ToString());
                return;
            }
        }

        internal static void GetSameHeadSlide(double timing, double bpm, string content, IList<SimaiNote> buffer)
        {
            var noteContents = content.Split('*');
            var note1 = GetSingleNote(timing, bpm, noteContents[0]);
            buffer.Add(note1);
            var newNoteContent = noteContents.ToList();
            newNoteContent.RemoveAt(0);
            //删除第一个NOTE
            foreach (var item in newNoteContent)
            {
                var note2text = note1.StartPosition + item;
                var note2 = GetSingleNote(timing, bpm, note2text);
                note2.IsSlideNoHead = true;
                buffer.Add(note2);
            }
        }

        internal static SimaiNote GetSingleNote(double timing, double bpm, string noteText)
        {
            var simaiNote = new SimaiNote();

            if (IsTouchNote(noteText))
            {
                simaiNote.TouchArea = noteText[0];
                if (simaiNote.TouchArea != 'C') simaiNote.StartPosition = int.Parse(noteText[1].ToString());
                else simaiNote.StartPosition = 8;
                simaiNote.Type = SimaiNoteType.Touch;
            }
            else
            {
                simaiNote.StartPosition = int.Parse(noteText[0].ToString());
                simaiNote.Type = SimaiNoteType.Tap; //if nothing happen in following if
            }

            if (noteText.Contains('f')) simaiNote.IsHanabi = true;

            //hold
            if (noteText.Contains('h'))
            {
                if (IsTouchNote(noteText))
                {
                    simaiNote.Type = SimaiNoteType.TouchHold;
                    simaiNote.HoldTime = GetTimeFromBeats(bpm, noteText);
                    //Console.WriteLine("Hold:" +simaiNote.touchArea+ simaiNote.startPosition + " TimeLastFor:" + simaiNote.holdTime);
                }
                else
                {
                    simaiNote.Type = SimaiNoteType.Hold;
                    if (noteText.Last() == 'h')
                        simaiNote.HoldTime = 0;
                    else
                        simaiNote.HoldTime = GetTimeFromBeats(bpm, noteText);
                    //Console.WriteLine("Hold:" + simaiNote.startPosition + " TimeLastFor:" + simaiNote.holdTime);
                }
            }

            //slide
            if (IsSlideNote(noteText))
            {
                simaiNote.Type = SimaiNoteType.Slide;
                simaiNote.SlideTime = GetTimeFromBeats(bpm, noteText);
                var timeStarWait = GetStarWaitTime(bpm, noteText);
                
                if (noteText.Contains('!'))
                {
                    simaiNote.IsSlideNoHead = true;
                    noteText = noteText.Replace("!", "");
                    simaiNote.SlideStartTime = timing;
                }
                else if (noteText.Contains('?'))
                {
                    simaiNote.IsSlideNoHead = true;
                    noteText = noteText.Replace("?", "");
                    simaiNote.SlideStartTime = timing + timeStarWait;
                }
                //Console.WriteLine("Slide:" + simaiNote.startPosition + " TimeLastFor:" + simaiNote.slideTime);
            }

            //break
            if (noteText.Contains('b'))
            {
                if (simaiNote.Type == SimaiNoteType.Slide)
                {
                    var ret = CheckHeadOrSlide(noteText, 'b');
                    simaiNote.IsBreak = ret.Item1;
                    simaiNote.IsSlideBreak = ret.Item2;
                }
                else
                {
                    // 除此之外的Break就无所谓了
                    simaiNote.IsBreak = true;
                }

                noteText = noteText.Replace("b", "");
            }

            //EX
            if (noteText.Contains('x'))
            {
                simaiNote.IsEx = true;
                noteText = noteText.Replace("x", "");
            }

            //starHead
            if (noteText.Contains('$'))
            {
                simaiNote.IsForceStar = true;
                if (noteText.Count(o => o == '$') == 2)
                    simaiNote.IsFakeRotate = true;
                noteText = noteText.Replace("$", "");
            }

            if(noteText.Contains('m'))
            {
                if(simaiNote.Type == SimaiNoteType.Slide)
                {
                    var ret = CheckHeadOrSlide(noteText, 'm');
                    simaiNote.IsMine = ret.Item1;
                    simaiNote.IsMineSlide = ret.Item2;
                }
                else
                {
                    // 除此之外的Mine就无所谓了
                    simaiNote.IsMine = true;
                }
                noteText = noteText.Replace("m", "");
            }

            simaiNote.RawContent = (noteText ?? string.Empty).Trim();
            return simaiNote;
        }

        //1: 是星星头的break
        //2: 是slide本体的break
        private static (bool,bool) CheckHeadOrSlide(string noteText, char detectChar)
        {
            // 如果是Slide 则要检查这个b到底是星星头的还是Slide本体的

            // !!! **SHIT CODE HERE** !!!
            bool isBreak = false;
            bool isBreakSlide = false;
            var startIndex = 0;
            while ((startIndex = noteText.IndexOf(detectChar, startIndex)) != -1)
            {
                if (startIndex < noteText.Length - 1)
                {
                    // 如果b不是最后一个字符 我们就检查b之后一个字符是不是`[`符号：如果是 那么就是break slide
                    if (noteText[startIndex + 1] == '[')
                        isBreakSlide = true;
                    else
                        // 否则 那么不管这个break出现在slide的哪一个地方 我们都认为他是星星头的break
                        // SHIT CODE!
                        isBreak = true;
                }
                else
                {
                    // 如果b符号是整个文本的最后一个字符 那么也是break slide（Simai语法）
                    isBreakSlide = true;
                }

                startIndex++;
            }
            return (isBreak,isBreakSlide);
        }

        internal static bool IsSlideNote(rString noteText)
        {
            const string SLIDE_MARKS = "-^v<>Vpqszw";
            foreach (var mark in SLIDE_MARKS)
            {
                if (noteText.Contains(mark))
                {
                    return true;
                }
            }
            return false;
        }

        internal static bool IsTouchNote(rString noteText)
        {
            const string TOUCH_MARKS = "ABCDE";
            foreach (var mark in TOUCH_MARKS)
            {
                if (noteText.StartsWith(mark))
                {
                    return true;
                }
            }
            return false;
        }

        internal static double GetTimeFromBeats(double bpm, string noteText)
        {
            if (noteText.Count(c => c == '[') > 1)
            {
                // 组合slide 有多个时长
                double wholeTime = 0;

                var partStartIndex = 0;
                while (noteText.IndexOf('[', partStartIndex) >= 0)
                {
                    var startIndex = noteText.IndexOf('[', partStartIndex);
                    var overIndex = noteText.IndexOf(']', partStartIndex);
                    partStartIndex = overIndex + 1;
                    var innerString = noteText.Substring(startIndex + 1, overIndex - startIndex - 1);
                    var timeOneBeat = 1d / (bpm / 60d);
                    if (innerString.Count(o => o == '#') == 1)
                    {
                        var times = innerString.Split('#');
                        if (times[1].Contains(':'))
                        {
                            innerString = times[1];
                            timeOneBeat = 1d / (double.Parse(times[0]) / 60d);
                        }
                        else
                        {
                            wholeTime += double.Parse(times[1]);
                            continue;
                        }
                    }

                    if (innerString.Count(o => o == '#') == 2)
                    {
                        var times = innerString.Split('#');
                        wholeTime += double.Parse(times[2]);
                        continue;
                    }

                    var numbers = innerString.Split(':');
                    var divide = int.Parse(numbers[0]);
                    var count = int.Parse(numbers[1]);


                    wholeTime += timeOneBeat * 4d / divide * count;
                }

                return wholeTime;
            }

            {
                var startIndex = noteText.IndexOf('[');
                var overIndex = noteText.IndexOf(']');
                var innerString = noteText.Substring(startIndex + 1, overIndex - startIndex - 1);
                var timeOneBeat = 1d / (bpm / 60d);
                if (innerString.Count(o => o == '#') == 1)
                {
                    var times = innerString.Split('#');
                    if (times[1].Contains(':'))
                    {
                        innerString = times[1];
                        timeOneBeat = 1d / (double.Parse(times[0]) / 60d);
                    }
                    else
                    {
                        return double.Parse(times[1]);
                    }
                }

                if (innerString.Count(o => o == '#') == 2)
                {
                    var times = innerString.Split('#');
                    return double.Parse(times[2]);
                }

                var numbers = innerString.Split(':'); //TODO:customBPM
                var divide = int.Parse(numbers[0]);
                var count = int.Parse(numbers[1]);


                return timeOneBeat * 4d / divide * count;
            }
        }

        internal static double GetStarWaitTime(double bpm, string noteText)
        {
            var startIndex = noteText.IndexOf('[');
            var overIndex = noteText.IndexOf(']');
            var innerString = noteText.Substring(startIndex + 1, overIndex - startIndex - 1);

            if (innerString.Count(o => o == '#') == 1)
            {
                var times = innerString.Split('#');
                bpm = double.Parse(times[0]);
            }

            if (innerString.Count(o => o == '#') == 2)
            {
                var times = innerString.Split('#');
                return double.Parse(times[0]);
            }

            return 1d / (bpm / 60d);
            
        }
        static class NoteHelper
        {
            internal static bool TryGetStarWaitTime(double bpm, rString noteText,out double time)
            {
                time = default;

                var startIndex = noteText.IndexOf('[');
                var overIndex = noteText.IndexOf(']');

                if(startIndex == -1 || overIndex == -1)
                {
                    return false;
                }

                var slideParamStr = noteText.Slice(startIndex + 1, overIndex - startIndex - 1);
                Span<Range> ranges = stackalloc Range[3];
                var tagCount = slideParamStr.Split(ranges, '#', StringSplitOptions.None);

                // 160#8:3 or 160#2s
                if (tagCount == 1)
                {
                    var timeStr = slideParamStr[ranges[0]];
                    if (timeStr.IsEmpty || !double.TryParse(timeStr, out bpm))
                    {
                        return false;
                    }
                }
                else if (tagCount == 2 || tagCount == 3)// 3s##1.5s or 3s##8:3 or 3s##160#8:3
                {
                    var timeStr = slideParamStr[ranges[0]];

                    return !timeStr.IsEmpty && double.TryParse(timeStr, out time);
                }

                time = 1d / (bpm / 60d);
                return true;
            }
        }
    }
}
