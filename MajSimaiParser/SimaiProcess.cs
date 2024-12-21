using System;
using System.Collections.Generic;

namespace MajSimaiParser
{
    public class SimaiProcess
    {
        /// <summary>
        ///     the timing points that contains notedata
        /// </summary>
        public List<SimaiTimingPoint> notelist = new List<SimaiTimingPoint>();

        /// <summary>
        ///     the timing points made by "," in maidata
        /// </summary>
        public List<SimaiTimingPoint> timinglist = new List<SimaiTimingPoint>();

        /// <summary>
        ///     This method serialize the fumen data and load it into the static class.
        /// </summary>
        /// <param name="text">fumen text</param>
        /// <param name="position">the position of the cusor, to get the return time</param>
        /// <returns>the song time at the position</returns>
        public SimaiProcess(string text)
        {
            var _notelist = new List<SimaiTimingPoint>();
            var _timinglist = new List<SimaiTimingPoint>();

            float bpm = 0;
            var curHSpeed = 1f;
            double time = 0; //in seconds
            var beats = 4;
            var haveNote = false;
            var noteTemp = "";
            int Ycount = 0, Xcount = 0;

            try
            {
                for (var i = 0; i < text.Length; i++)
                {
                    if (text[i] == '|' && i + 1 < text.Length && text[i + 1] == '|')
                    {
                        // 跳过注释
                        Xcount++;
                        while (i < text.Length && text[i] != '\n')
                        {
                            i++;
                            Xcount++;
                        }

                        Ycount++;
                        Xcount = 0;
                        continue;
                    }

                    if (text[i] == '\n')
                    {
                        Ycount++;
                        Xcount = 0;
                    }
                    else
                    {
                        Xcount++;
                    }

                    if (text[i] == '(')
                    //Get bpm
                    {
                        haveNote = false;
                        noteTemp = "";
                        var bpm_s = "";
                        i++;
                        Xcount++;
                        while (text[i] != ')')
                        {
                            bpm_s += text[i];
                            i++;
                            Xcount++;
                        }

                        bpm = float.Parse(bpm_s);
                        //Console.WriteLine("BPM" + bpm);
                        continue;
                    }

                    if (text[i] == '{')
                    //Get beats
                    {
                        haveNote = false;
                        noteTemp = "";
                        var beats_s = "";
                        i++;
                        Xcount++;
                        while (text[i] != '}')
                        {
                            beats_s += text[i];
                            i++;
                            Xcount++;
                        }

                        beats = int.Parse(beats_s);
                        //Console.WriteLine("BEAT" + beats);
                        continue;
                    }

                    if (text[i] == 'H')
                    //Get HS
                    {
                        haveNote = false;
                        noteTemp = "";
                        var hs_s = "";
                        if (text[i + 1] == 'S' && text[i + 2] == '*')
                        {
                            i += 3;
                            Xcount += 3;
                        }

                        while (text[i] != '>')
                        {
                            hs_s += text[i];
                            i++;
                            Xcount++;
                        }

                        curHSpeed = float.Parse(hs_s);
                        //Console.WriteLine("HS" + curHSpeed);
                        continue;
                    }

                    if (isNote(text[i])) haveNote = true;
                    if (haveNote && text[i] != ',') noteTemp += text[i];
                    if (text[i] == ',')
                    {
                        if (haveNote)
                        {
                            if (noteTemp.Contains('`'))
                            {
                                // 伪双
                                var fakeEachList = noteTemp.Split('`');
                                var fakeTime = time;
                                var timeInterval = 1.875 / bpm; // 128分音
                                foreach (var fakeEachGroup in fakeEachList)
                                {
                                    Console.WriteLine(fakeEachGroup);
                                    _notelist.Add(new SimaiTimingPoint(fakeTime, Xcount, Ycount, fakeEachGroup, bpm,
                                        curHSpeed));
                                    fakeTime += timeInterval;
                                }
                            }
                            else
                            {
                                _notelist.Add(new SimaiTimingPoint(time, Xcount, Ycount, noteTemp, bpm, curHSpeed));
                            }
                            //Console.WriteLine("Note:" + noteTemp);

                            noteTemp = "";
                        }

                        _timinglist.Add(new SimaiTimingPoint(time, Xcount, Ycount, "", bpm));


                        time += 1d / (bpm / 60d) * 4d / beats;
                        //Console.WriteLine(time);
                        haveNote = false;
                    }
                }

                notelist = _notelist;
                timinglist = _timinglist;

                for (var i = 0; i < notelist.Count; i++)
                {
                    var note = notelist[i];
                    var notes = new SimaiTimingPoint(note.time, note.rawTextPositionX, note.rawTextPositionY,
                        note.notesContent, note.currentBpm, note.HSpeed);
                    notelist[i].noteList = notes.getNotes();
                }

                //Console.WriteLine(notelist.ToArray());
                return;
            }
            catch (Exception e)
            {
                throw new Exception("Error at " + Ycount + "," + Xcount + "\n" + e.Message);
            }
        }

        private static bool isNote(char noteText)
        {
            var SlideMarks = "1234567890ABCDE"; ///ABCDE for touch
            foreach (var mark in SlideMarks)
                if (noteText == mark)
                    return true;
            return false;
        }
    }
}
