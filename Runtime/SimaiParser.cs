using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace MajSimai
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
            var allTask = Task.WhenAll(tasks);
            while (!allTask.IsCompleted)
                await Task.Yield();
            for(var i = 0; i < 7; i++)
            {
                var task = tasks[i];
                if (task.IsFaulted)
                    charts[i] = new SimaiChart(metadata.Levels[i], metadata.Designers[i], Array.Empty<SimaiTimingPoint>());
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
        public async Task<SimaiChart> ParseChartAsync(string level, string designer, string fumen)
        {
            return await Task.Run(async () =>
            {
                if (string.IsNullOrEmpty(fumen))
                {
                    return new SimaiChart(level, designer, null);
                }
                var noteRawTiminglist = new List<SimaiRawTimingPoint>();
                var timinglist = new List<SimaiTimingPoint>();

                float bpm = 0;
                var curHSpeed = 1f;
                double time = 0; //in seconds
                var beats = 4f;
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

                            if (!float.TryParse(bpm_s, out bpm))
                            {
                                throw new InvalidValueException(Ycount, bpm_s, "BPM value must be a floating point number");
                            }
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

                            if (!float.TryParse(beats_s, out beats))
                            {
                                throw new InvalidValueException(Ycount, beats_s, "Beats value must be a floating point number");
                            }
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

                            if (!float.TryParse(hs_s, out curHSpeed))
                            {
                                throw new InvalidValueException(Ycount, hs_s, "HSpeed value must be a floating point number");
                            }
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
                                        noteRawTiminglist.Add(new SimaiRawTimingPoint(fakeTime, Xcount, Ycount, fakeEachGroup, bpm,
                                            curHSpeed));
                                        fakeTime += timeInterval;
                                    }
                                }
                                else
                                {
                                    noteRawTiminglist.Add(new SimaiRawTimingPoint(time, Xcount, Ycount, noteTemp, bpm, curHSpeed));
                                }
                                //Console.WriteLine("Note:" + noteTemp);

                                noteTemp = "";
                            }

                            timinglist.Add(new SimaiTimingPoint(time, null, Xcount, Ycount, "", bpm));


                            time += 1d / (bpm / 60d) * 4d / beats;
                            //Console.WriteLine(time);
                            haveNote = false;
                        }
                    }
                    var noteTimingPoints = new SimaiTimingPoint[noteRawTiminglist.Count];
                    //for (var i = 0; i < noteTiminglist.Count; i++)
                    //{
                    //    var note = noteTiminglist[i];
                    //    var notes = new SimaiTimingPoint(note.time, note.rawTextPositionX, note.rawTextPositionY,
                    //        note.notesContent, note.currentBpm, note.HSpeed);
                    //    noteTiminglist[i].noteList = notes.getNotes();
                    //}
                    for (int i = 0; i < noteRawTiminglist.Count; i++)
                    {
                        var rawTiming = noteRawTiminglist[i];
                        var timingPoint = await rawTiming.ParseAsync();
                        noteTimingPoints[i] = timingPoint;
                    }

                    return new SimaiChart(level, designer, noteTimingPoints);
                }
                catch (InvalidValueException)
                {
                    throw;
                }
                catch (Exception e)
                {
                    throw new Exception("Error at " + Ycount + "," + Xcount + "\n" + e.Message);
                }
            });
        }

        async Task<SimaiMetadata> ReadMetadataAsync(string content)
        {
            return await Task.Run(async () =>
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
                        {
                            if (!float.TryParse(GetValue(maidataTxt[i]), out first))
                            {
                                first = 0;
                            }
                        }
                        else if (maidataTxt[i].StartsWith("&lv_") || maidataTxt[i].StartsWith("&inote_"))
                        {
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
                        designers[j] ??= designer;
                    return new SimaiMetadata(title, 
                                             artist, 
                                             first, 
                                             designers,
                                             levels, 
                                             fumens, 
                                             commands.ToArray(), 
                                             await ComputeHashAsBase64StringAsync(Encoding.UTF8.GetBytes(content)));
                }
                catch (Exception e)
                {
                    throw new InvalidSimaiMarkupException(i + 1, "", "在maidata.txt第" + (i + 1) + "行:\n" + e.Message + "读取谱面时出现错误");
                }
            });
        }
        string GetValue(string varline) => varline.Substring(varline.IndexOf("=") + 1);
        static bool IsNote(char noteText)
        {
            var SlideMarks = "1234567890ABCDE"; ///ABCDE for touch
            foreach (var mark in SlideMarks)
                if (noteText == mark)
                    return true;
            return false;
        }
        static async Task<byte[]> ComputeHashAsync(byte[] data)
        {
            using var md5 = MD5.Create();
            return await Task.Run(() => md5.ComputeHash(data));
        }
        static async Task<string> ComputeHashAsBase64StringAsync(byte[] data)
        {
            var hash = await ComputeHashAsync(data);

            return Convert.ToBase64String(hash);
        }
    }
}
