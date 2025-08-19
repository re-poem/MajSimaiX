using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
#nullable enable
namespace MajSimai
{
    using zString = ReadOnlySpan<char>;
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
                if (noteContent.Length == 2 && int.TryParse(noteContent, out _)) //连写数字
                {
                    if (TryGetSingleNote(timing, bpm, noteContent.AsSpan().Slice(0, 1), out var note1))
                    {
                        buffer.Add(note1);
                    }
                    if (TryGetSingleNote(timing, bpm, noteContent.AsSpan().Slice(1, 1), out var note2))
                    {
                        buffer.Add(note2);
                    }
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
                            if(TryGetSingleNote(timing, bpm, note, out var simaiNote))
                            {
                                buffer.Add(simaiNote);
                            }
                            else
                            {
                                Debug.WriteLine($"Cannot parse note from text: \"{note}\"");
                            }    
                        }
                    }
                }
                else
                {
                    if (noteContent.Contains('*'))
                    {
                        GetSameHeadSlide(timing, bpm, noteContent, buffer);
                    }
                    else
                    {
                        if (TryGetSingleNote(timing, bpm, noteContent, out var note))
                        {
                            buffer.Add(note);
                        }
                        else
                        {
                            Debug.WriteLine($"Cannot parse note from text: \"{noteContent}\"");
                        }
                    }
                }
            }
            catch(Exception e)
            {
                Debug.WriteLine(e.ToString());
                return;
            }
        }

        internal static void GetSameHeadSlide(double timing, double bpm, zString content, IList<SimaiNote> buffer)
        {
            Span<Range> ranges = stackalloc Range[content.Count('*') + 1];
            _ = content.Split(ranges, '*', StringSplitOptions.RemoveEmptyEntries);
            if (TryGetSingleNote(timing, bpm, content[ranges[0]],out var note1))
            {
                buffer.Add(note1);
            }
            else
            {
                Debug.WriteLine($"Cannot parse slide from text: \"{new string(content[ranges[0]])}\"");
                return;
            }
            //var newNoteContent = noteContents.ToList();
            //newNoteContent.RemoveAt(0);
            ////删除第一个NOTE

            for (var i = 1; i < ranges.Length; i++)
            {
                var partNoteText = content[ranges[i]];
                var rentedArray = ArrayPool<char>.Shared.Rent(partNoteText.Length + 1);
                try
                {
                    rentedArray[0] = content[ranges[0]][0];
                    var noteText = rentedArray.AsSpan();
                    partNoteText.CopyTo(noteText.Slice(1));
                    noteText = noteText.Slice(0, partNoteText.Length + 1);

                    if (TryGetSingleNote(timing, bpm, noteText, out var note2))
                    {
                        note2.IsSlideNoHead = true;
                        buffer.Add(note2);
                    }
                    else
                    {
                        Debug.WriteLine($"Cannot parse slide from text: \"{new string(noteText)}\"");
                        continue;
                    }
                }
                finally
                {
                    ArrayPool<char>.Shared.Return(rentedArray, true);
                }
            }
        }

        internal static bool TryGetSingleNote(double timing, double bpm, zString noteText,[NotNullWhen(true)] out SimaiNote? outSimaiNote)
        {
            outSimaiNote = default;
            Span<char> noteTextCopy = stackalloc char[noteText.Length];
            noteText.CopyTo(noteTextCopy);
            var simaiNote = new SimaiNote();

            if (NoteHelper.IsTouchNote(noteTextCopy))
            {
                simaiNote.TouchArea = noteTextCopy[0];
                if (simaiNote.TouchArea != 'C')
                {
                    if(noteTextCopy.Length < 2)
                    {
                        return false;
                    }
                    else if(int.TryParse(noteTextCopy.Slice(1, 1), out var startPosition))
                    {
                        simaiNote.StartPosition = startPosition;
                    }
                    else
                    {
                        return false;
                    }
                }
                else 
                {
                    simaiNote.StartPosition = 8;
                }
                simaiNote.Type = SimaiNoteType.Touch;
            }
            else
            {
                if (int.TryParse(noteTextCopy.Slice(0, 1), out var startPosition))
                {
                    simaiNote.StartPosition = startPosition;
                }
                else
                {
                    return false;
                }
                simaiNote.Type = SimaiNoteType.Tap; //if nothing happen in following if
            }
            if (noteTextCopy.Contains('f'))
            {
                simaiNote.IsHanabi = true;
            }

            //hold
            if (noteTextCopy.Contains('h'))
            {
                if (NoteHelper.IsTouchNote(noteTextCopy))
                {
                    simaiNote.Type = SimaiNoteType.TouchHold;
                    if(NoteHelper.TryGetHoldTimeFromBeats(bpm, noteTextCopy, out var holdTime))
                    {
                        simaiNote.HoldTime = holdTime;
                    }
                    else
                    {
                        return false;
                    }
                    //Console.WriteLine("Hold:" +simaiNote.touchArea+ simaiNote.startPosition + " TimeLastFor:" + simaiNote.holdTime);
                }
                else
                {
                    simaiNote.Type = SimaiNoteType.Hold;
                    if (noteTextCopy[^1] == 'h')
                    {
                        simaiNote.HoldTime = 0;
                    }
                    else
                    {
                        if (NoteHelper.TryGetHoldTimeFromBeats(bpm, noteTextCopy, out var holdTime))
                        {
                            simaiNote.HoldTime = holdTime;
                        }
                        else
                        {
                            return false;
                        }
                    }
                    //Console.WriteLine("Hold:" + simaiNote.startPosition + " TimeLastFor:" + simaiNote.holdTime);
                }
            }

            //slide
            if (NoteHelper.IsSlideNote(noteTextCopy))
            {
                simaiNote.Type = SimaiNoteType.Slide;
                simaiNote.SlideTime = GetTimeFromBeats(bpm, noteTextCopy);
                if(!NoteHelper.TryGetStarWaitTime(bpm, noteTextCopy, out var timeStarWait))
                {
                    return false;
                }
                
                if (noteTextCopy.Contains('!'))
                {
                    simaiNote.IsSlideNoHead = true;
                    noteTextCopy = noteTextCopy.RemoveAll('!');
                    simaiNote.SlideStartTime = timing;
                }
                else if (noteTextCopy.Contains('?'))
                {
                    simaiNote.IsSlideNoHead = true;
                    noteTextCopy = noteTextCopy.RemoveAll('?');
                    simaiNote.SlideStartTime = timing + timeStarWait;
                }
                //Console.WriteLine("Slide:" + simaiNote.startPosition + " TimeLastFor:" + simaiNote.slideTime);
            }

            //break
            if (noteTextCopy.Contains('b'))
            {
                if (simaiNote.Type == SimaiNoteType.Slide)
                {
                    (simaiNote.IsBreak, simaiNote.IsSlideBreak) = NoteHelper.CheckHeadOrSlide(noteTextCopy, 'b');
                }
                else
                {
                    // 除此之外的Break就无所谓了
                    simaiNote.IsBreak = true;
                }

                noteTextCopy = noteTextCopy.RemoveAll('b');
            }

            //EX
            if (noteTextCopy.Contains('x'))
            {
                simaiNote.IsEx = true;
                noteTextCopy = noteTextCopy.RemoveAll('x');
            }

            //starHead
            if (noteTextCopy.Contains('$'))
            {
                simaiNote.IsForceStar = true;
                if (noteTextCopy.Count('$') == 2)
                {
                    simaiNote.IsFakeRotate = true;
                }
                noteTextCopy = noteTextCopy.RemoveAll('$');
            }

            if(noteTextCopy.Contains('m'))
            {
                if(simaiNote.Type == SimaiNoteType.Slide)
                {
                    var ret = NoteHelper.CheckHeadOrSlide(noteTextCopy, 'm');
                    simaiNote.IsMine = ret.Item1;
                    simaiNote.IsMineSlide = ret.Item2;
                }
                else
                {
                    // 除此之外的Mine就无所谓了
                    simaiNote.IsMine = true;
                }
                noteTextCopy = noteTextCopy.RemoveAll('m');
            }

            simaiNote.RawContent = new string(noteTextCopy.Trim());
            outSimaiNote = simaiNote;
            return true;
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
        static class NoteHelper
        {
            //1: 是星星头的break
            //2: 是slide本体的break
            public static (bool isBreak, bool isBreakSlide) CheckHeadOrSlide(zString noteText, char detectChar)
            {
                // 如果是Slide 则要检查这个b到底是星星头的还是Slide本体的

                // !!! **SHIT CODE HERE** !!!
                bool isBreak = false;
                bool isBreakSlide = false;
                var startIndex = 0;
                while ((startIndex = noteText.Slice(startIndex).IndexOf(detectChar)) != -1)
                {
                    if (startIndex < noteText.Length - 1)
                    {
                        // 如果b不是最后一个字符 我们就检查b之后一个字符是不是`[`符号：如果是 那么就是break slide
                        // startIndex + 1 < noteText.Length 防越界
                        if (startIndex + 1 < noteText.Length && noteText[startIndex + 1] == '[')
                        {
                            isBreakSlide = true;
                        }
                        else
                        {
                            // 否则 那么不管这个break出现在slide的哪一个地方 我们都认为他是星星头的break
                            // SHIT CODE!
                            isBreak = true;
                        }
                    }
                    else
                    {
                        // 如果b符号是整个文本的最后一个字符 那么也是break slide（Simai语法）
                        isBreakSlide = true;
                    }

                    startIndex++;
                }
                return (isBreak, isBreakSlide);
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool IsSlideNote(zString noteText)
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
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool IsTouchNote(zString noteText)
            {
                if(noteText.IsEmpty)
                {
                    return false;
                }
                //const string TOUCH_MARKS = "ABCDE";

                var c = noteText[0];

                return c >= 'A' && c <= 'E';
                //foreach (var mark in TOUCH_MARKS)
                //{
                //    if (noteText.StartsWith(mark))
                //    {
                //        return true;
                //    }
                //}
                //return false;
            }
            public static bool TryGetStarWaitTime(double bpm, zString noteText,out double time)
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
            public static bool TryGetHoldTimeFromBeats(double bpm, zString noteText, out double time)
            {
                time = default;
                var startIndex = noteText.IndexOf('[');
                var endIndex = noteText.IndexOf(']');
                if (startIndex == -1 || endIndex == -1)
                {
                    time = 0;
                    return true;
                }
                var holdParamBody = noteText.Slice(startIndex + 1, endIndex - startIndex - 1);
                Span<Range> ranges = stackalloc Range[2];
                var tagCount = holdParamBody.Split(ranges, '#', StringSplitOptions.None);

                switch(tagCount)
                {
                    case 0: // 2h[8:3]
                        return TryGetTimeFromRatio(bpm, holdParamBody, out time);
                    case 1: // 2h[#5.678] or 2h[150#2:1]
                        var param1 = holdParamBody[ranges[0]];
                        var param2 = holdParamBody[ranges[1]];

                        if (param1.IsEmpty) //2h[#5.678]
                        {
                            return double.TryParse(param2, out time);
                        }
                        else //2h[150#2:1]
                        {
                            if (param2.IsEmpty)
                            {
                                return false;
                            }
                            else if (!double.TryParse(param1, out bpm))
                            {
                                return false;
                            }
                            return TryGetTimeFromRatio(bpm, param2, out time);
                        }
                    default:
                        return false; //undefined behavior
                }
            }
            /// <summary>
            /// Calculates the <paramref name="time"/> from <paramref name="noteText"/> in "x:y" format according to a given <paramref name="bpm"/>
            /// </summary>
            /// <param name="bpm"></param>
            /// <param name="noteText"></param>
            /// <param name="time"></param>
            /// <returns></returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static bool TryGetTimeFromRatio(double bpm, zString noteText, out double time)
            {
                time = default;
                var timeOneBeat = 1d / (bpm / 60d);
                Span<Range> ranges = stackalloc Range[2];
                var tagCount = noteText.Split(ranges, ':', StringSplitOptions.None);
                if (tagCount != 2)
                {
                    return false;
                }
                var divideStr = noteText[ranges[0]];
                var countStr = noteText[ranges[1]];
                if (divideStr.IsEmpty || countStr.IsEmpty)
                {
                    return false;
                }
                if (!int.TryParse(divideStr, out var divide) || !int.TryParse(countStr, out var count))
                {
                    return false;
                }
                time = timeOneBeat * 4d / divide * count;

                return true;
            }
        }
    }
}
