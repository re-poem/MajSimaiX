using MajSimaiParser.Exceptions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace MajSimaiParser
{
    public partial class SimaiParser
    {
        public static SimaiParser Shared { get; } = new SimaiParser();

        public async Task<SimaiFile> ParseAsync(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"\"{filePath}\" could not be found");

            using var fileStream = File.OpenRead(filePath);
            using var memoryBuffer = new MemoryStream();
            await fileStream.CopyToAsync(memoryBuffer);

            var fileContent = Encoding.UTF8.GetString(memoryBuffer.ToArray());
            var metadata = await ReadMetadataAsync(fileContent);

            SimaiChart[] charts = new SimaiChart[7];
            Task<SimaiChart>[] tasks = new Task<SimaiChart>[7];
            for (var i = 0; i < 7; i++)
            {
                var fumen = metadata.Fumens[i];
                var designer = metadata.Designers[i];
                var level = metadata.Levels[i];
                tasks[i] = ParseChartAsync(level, designer, fumen);
            }
            await Task.WhenAll(tasks);
            for(var i = 0; i < 7; i++)
            {
                var task = tasks[i];
                if (task.IsFaulted)
                    throw task.Exception;
                else
                    charts[i] = task.Result;
            }
            return new SimaiFile(filePath, metadata.Title, metadata.Artist, metadata.Offset, charts, metadata.Fumens, metadata.Commands);
        }
        public async Task<SimaiMetadata> ParseMetadataAsync(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"\"{filePath}\" could not be found");

            using var fileStream = File.OpenRead(filePath);
            using var memoryBuffer = new MemoryStream();
            await fileStream.CopyToAsync(memoryBuffer);

            var fileContent = Encoding.UTF8.GetString(memoryBuffer.ToArray());
            var metadata = await ReadMetadataAsync(fileContent);

            return metadata;
        }
        public SimaiFile Parse(string filePath)
        {
            return ParseAsync(filePath).Result;
        }
        public SimaiMetadata ParseMetadata(string filePath)
        {
            return ParseMetadataAsync(filePath).Result;
        }


        async Task<SimaiMetadata> ReadMetadataAsync(string content)
        {
            return await Task.Run(() =>
            {
                var title = "";
                var artist = "";
                var designer = "";
                var first = 0f;
                var designers = new string[7];
                var fumens = new string[7];
                var levels = new string[7];
                var i = 0;
                List<SimaiCommand> commands = new List<SimaiCommand>();
                try
                {
                    var maidataTxt = content.Split("\n", StringSplitOptions.RemoveEmptyEntries);
                    for (i = 0; i < maidataTxt.Length; i++)
                    {
                        if (maidataTxt[i].StartsWith("&title="))
                            title = GetValue(maidataTxt[i]);
                        else if (maidataTxt[i].StartsWith("&artist="))
                            artist = GetValue(maidataTxt[i]);
                        else if (maidataTxt[i].StartsWith("&des"))
                        {
                            if (maidataTxt[i].StartsWith("&des="))
                            {
                                designer = GetValue(maidataTxt[i]);
                            }
                            else
                            {
                                for (var j = 0; j < 7; j++)
                                {
                                    if (maidataTxt[i].StartsWith($"&des_{j}="))
                                        designers[j] = GetValue(maidataTxt[i]);
                                }
                            }

                        }
                        else if (maidataTxt[i].StartsWith("&first="))
                            first = float.Parse(GetValue(maidataTxt[i]));
                        else if (maidataTxt[i].StartsWith("&lv_") || maidataTxt[i].StartsWith("&inote_"))
                            for (var j = 1; j < 8 && i < maidataTxt.Length; j++)
                            {
                                if (maidataTxt[i].StartsWith("&lv_" + j + "="))
                                    levels[j - 1] = GetValue(maidataTxt[i]);
                                if (maidataTxt[i].StartsWith("&inote_" + j + "="))
                                {
                                    var TheNote = "";
                                    TheNote += GetValue(maidataTxt[i]) + "\n";
                                    i++;
                                    for (; i < maidataTxt.Length; i++)
                                    {
                                        if (i < maidataTxt.Length)
                                            if (maidataTxt[i].StartsWith("&"))
                                                break;
                                        TheNote += maidataTxt[i] + "\n";
                                    }

                                    fumens[j - 1] = TheNote;
                                }
                            }
                        else if (maidataTxt[i].StartsWith("&"))
                        {
                            if (!maidataTxt[i].Contains("="))
                                throw new InvalidSimaiMarkupException(i + 1, maidataTxt[i]);
                            var a = maidataTxt[i].Split("=");
                            var prefix = a[0][1..];
                            var value = a[1];
                            commands.Add(new SimaiCommand(prefix, value));
                        }
                    }
                    for (var j = 0; j < 7; j++)
                    {
                        if (designers[j] is null)
                            designers[j] = designer;
                    }
                    return new SimaiMetadata(title, artist, first, designers,levels, fumens, commands.ToArray());
                }
                catch (Exception e)
                {
                    throw new InvalidSimaiMarkupException(i + 1, "", "在maidata.txt第" + (i + 1) + "行:\n" + e.Message + "读取谱面时出现错误");
                }
            });
        }
        async Task<SimaiChart> ParseChartAsync(string level,string designer, string fumen)
        {
            return await Task.Run(() =>
            {
                if(string.IsNullOrEmpty(fumen))
                {
                    return new SimaiChart(level, designer, null, null);
                }
                var notelist = new List<SimaiTimingPoint>();
                var timinglist = new List<SimaiTimingPoint>();

                float bpm = 0;
                var curHSpeed = 1f;
                double time = 0; //in seconds
                var beats = 4;
                var haveNote = false;
                var noteTemp = "";
                int Ycount = 0, Xcount = 0;

                try
                {
                    for (var i = 0; i < fumen.Length; i++)
                    {
                        if (fumen[i] == '|' && i + 1 < fumen.Length && fumen[i + 1] == '|')
                        {
                            // 跳过注释
                            Xcount++;
                            while (i < fumen.Length && fumen[i] != '\n')
                            {
                                i++;
                                Xcount++;
                            }

                            Ycount++;
                            Xcount = 0;
                            continue;
                        }

                        if (fumen[i] == '\n')
                        {
                            Ycount++;
                            Xcount = 0;
                        }
                        else
                        {
                            Xcount++;
                        }

                        if (fumen[i] == '(')
                        //Get bpm
                        {
                            haveNote = false;
                            noteTemp = "";
                            var bpm_s = "";
                            i++;
                            Xcount++;
                            while (fumen[i] != ')')
                            {
                                bpm_s += fumen[i];
                                i++;
                                Xcount++;
                            }

                            bpm = float.Parse(bpm_s);
                            //Console.WriteLine("BPM" + bpm);
                            continue;
                        }

                        if (fumen[i] == '{')
                        //Get beats
                        {
                            haveNote = false;
                            noteTemp = "";
                            var beats_s = "";
                            i++;
                            Xcount++;
                            while (fumen[i] != '}')
                            {
                                beats_s += fumen[i];
                                i++;
                                Xcount++;
                            }

                            beats = int.Parse(beats_s);
                            //Console.WriteLine("BEAT" + beats);
                            continue;
                        }

                        if (fumen[i] == 'H')
                        //Get HS
                        {
                            haveNote = false;
                            noteTemp = "";
                            var hs_s = "";
                            if (fumen[i + 1] == 'S' && fumen[i + 2] == '*')
                            {
                                i += 3;
                                Xcount += 3;
                            }

                            while (fumen[i] != '>')
                            {
                                hs_s += fumen[i];
                                i++;
                                Xcount++;
                            }

                            curHSpeed = float.Parse(hs_s);
                            //Console.WriteLine("HS" + curHSpeed);
                            continue;
                        }

                        if (IsNote(fumen[i])) haveNote = true;
                        if (haveNote && fumen[i] != ',') noteTemp += fumen[i];
                        if (fumen[i] == ',')
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
                                        notelist.Add(new SimaiTimingPoint(fakeTime, Xcount, Ycount, fakeEachGroup, bpm,
                                            curHSpeed));
                                        fakeTime += timeInterval;
                                    }
                                }
                                else
                                {
                                    notelist.Add(new SimaiTimingPoint(time, Xcount, Ycount, noteTemp, bpm, curHSpeed));
                                }
                                //Console.WriteLine("Note:" + noteTemp);

                                noteTemp = "";
                            }

                            timinglist.Add(new SimaiTimingPoint(time, Xcount, Ycount, "", bpm));


                            time += 1d / (bpm / 60d) * 4d / beats;
                            //Console.WriteLine(time);
                            haveNote = false;
                        }
                    }

                    for (var i = 0; i < notelist.Count; i++)
                    {
                        var note = notelist[i];
                        var notes = new SimaiTimingPoint(note.time, note.rawTextPositionX, note.rawTextPositionY,
                            note.notesContent, note.currentBpm, note.HSpeed);
                        notelist[i].noteList = notes.getNotes();
                    }

                    return new SimaiChart(level, designer, notelist.ToArray(), timinglist.ToArray());
                }
                catch (Exception e)
                {
                    throw new Exception("Error at " + Ycount + "," + Xcount + "\n" + e.Message);
                }
            });
        }
        string GetValue(string varline)
        {
            return varline.Substring(varline.IndexOf("=") + 1);
        }
        static bool IsNote(char noteText)
        {
            var SlideMarks = "1234567890ABCDE"; ///ABCDE for touch
            foreach (var mark in SlideMarks)
                if (noteText == mark)
                    return true;
            return false;
        }
    }
}
