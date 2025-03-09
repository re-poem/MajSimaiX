using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
#nullable enable
namespace MajSimai
{
    internal static class SimaiNoteParser
    {
        internal static SimaiNote[] GetNotes(double timing, double bpm, string noteContent)
        {
            if (string.IsNullOrEmpty(noteContent))
                return Array.Empty<SimaiNote>();
            var simaiNotes = new List<SimaiNote>();
            try
            {
                var dummy = 0;
                if (noteContent.Length == 2 && int.TryParse(noteContent, out dummy)) //连写数字
                {
                    simaiNotes.Add(GetSingleNote(timing, bpm, noteContent[0].ToString()));
                    simaiNotes.Add(GetSingleNote(timing, bpm, noteContent[1].ToString()));
                    return simaiNotes.ToArray();
                }

                if (noteContent.Contains('/'))
                {
                    var notes = noteContent.Split('/');
                    foreach (var note in notes)
                    {
                        if (note.Contains('*'))
                            simaiNotes.AddRange(GetSameHeadSlide(timing, bpm, note));
                        else
                            simaiNotes.Add(GetSingleNote(timing, bpm, note));
                    }
                    return simaiNotes.ToArray();
                }

                if (noteContent.Contains('*'))
                {
                    simaiNotes.AddRange(GetSameHeadSlide(timing, bpm, noteContent));
                    return simaiNotes.ToArray();
                }

                simaiNotes.Add(GetSingleNote(timing, bpm, noteContent));
                return simaiNotes.ToArray();
            }
            catch
            {
                return Array.Empty<SimaiNote>();
            }
        }

        internal static SimaiNote[] GetSameHeadSlide(double timing, double bpm, string content)
        {
            var simaiNotes = new List<SimaiNote>();
            var noteContents = content.Split('*');
            var note1 = GetSingleNote(timing, bpm, noteContents[0]);
            simaiNotes.Add(note1);
            var newNoteContent = noteContents.ToList();
            newNoteContent.RemoveAt(0);
            //删除第一个NOTE
            foreach (var item in newNoteContent)
            {
                var note2text = note1.StartPosition + item;
                var note2 = GetSingleNote(timing, bpm, note2text);
                note2.IsSlideNoHead = true;
                simaiNotes.Add(note2);
            }

            return simaiNotes.ToArray();
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
                simaiNote.SlideStartTime = timing + timeStarWait;
                if (noteText.Contains('!'))
                {
                    simaiNote.IsSlideNoHead = true;
                    noteText = noteText.Replace("!", "");
                }
                else if (noteText.Contains('?'))
                {
                    simaiNote.IsSlideNoHead = true;
                    noteText = noteText.Replace("?", "");
                }
                //Console.WriteLine("Slide:" + simaiNote.startPosition + " TimeLastFor:" + simaiNote.slideTime);
            }

            //break
            if (noteText.Contains('b'))
            {
                if (simaiNote.Type == SimaiNoteType.Slide)
                {
                    // 如果是Slide 则要检查这个b到底是星星头的还是Slide本体的

                    // !!! **SHIT CODE HERE** !!!
                    var startIndex = 0;
                    while ((startIndex = noteText.IndexOf('b', startIndex)) != -1)
                    {
                        if (startIndex < noteText.Length - 1)
                        {
                            // 如果b不是最后一个字符 我们就检查b之后一个字符是不是`[`符号：如果是 那么就是break slide
                            if (noteText[startIndex + 1] == '[')
                                simaiNote.IsSlideBreak = true;
                            else
                                // 否则 那么不管这个break出现在slide的哪一个地方 我们都认为他是星星头的break
                                // SHIT CODE!
                                simaiNote.IsBreak = true;
                        }
                        else
                        {
                            // 如果b符号是整个文本的最后一个字符 那么也是break slide（Simai语法）
                            simaiNote.IsSlideBreak = true;
                        }

                        startIndex++;
                    }
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

            simaiNote.RawContent = (noteText ?? string.Empty).Trim();
            return simaiNote;
        }

        internal static bool IsSlideNote(string noteText)
        {
            var SlideMarks = "-^v<>Vpqszw";
            foreach (var mark in SlideMarks)
                if (noteText.Contains(mark))
                    return true;
            return false;
        }

        internal static bool IsTouchNote(string noteText)
        {
            var SlideMarks = "ABCDE";
            foreach (var mark in SlideMarks)
                if (noteText.StartsWith(mark.ToString()))
                    return true;
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
    }
}
