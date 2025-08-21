using MajSimai.Runtime.Utils;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace MajSimai
{
    public static class SimaiParser
    {
        public static SimaiFile Parse(string filePath)
        {
            return ParseAsync(filePath).Result;
        }
        public static async Task<SimaiFile> ParseAsync(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"\"{filePath}\" could not be found");

            using var fileStream = File.OpenRead(filePath);
            using var memoryBuffer = new MemoryStream();
            await fileStream.CopyToAsync(memoryBuffer);

            var fileContent = Encoding.UTF8.GetString(memoryBuffer.ToArray());
            var metadata = await ParseMetadataAsync(fileContent);

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
            {
                await Task.Yield();
            }
            for(var i = 0; i < 7; i++)
            {
                var task = tasks[i];
                if (task.IsFaulted)
                {
                    charts[i] = new SimaiChart(metadata.Levels[i], metadata.Designers[i], metadata.Fumens[i], Array.Empty<SimaiTimingPoint>());
                }
                else
                {
                    charts[i] = task.Result;
                }
            }
            var simaiFile = new SimaiFile(filePath, metadata.Title, metadata.Artist, metadata.Offset, charts, null);
            var cmds = simaiFile.Commands;
            var cmdCount = metadata.Commands.Length;
            for (var i = 0; i < cmdCount; i++)
            {
                simaiFile.Commands.Add(metadata.Commands[i]);
            }

            return simaiFile;
        }
        
        public static SimaiMetadata ParseMetadata(ReadOnlySpan<char> content)
        {
            static void SetValue(ReadOnlySpan<char> kvStr, ref string valueStr)
            {
                if (!string.IsNullOrEmpty(valueStr))
                {
                    return;
                }
                var value = SimaiParser.GetValue(kvStr);
                if (value.Length == 0)
                {
                    return;
                }
                valueStr = new string(value);
            }
            var title = string.Empty;
            var artist = string.Empty;
            var designer = string.Empty;
            var first = 0f;
            var rentedArrayForDesigners = ArrayPool<string>.Shared.Rent(7);
            var rentedArrayForFumens = ArrayPool<string>.Shared.Rent(7);
            var rentedArrayForLevels = ArrayPool<string>.Shared.Rent(7);

            var designers = rentedArrayForDesigners.AsSpan(0, 7);
            var fumens = rentedArrayForFumens.AsSpan(0, 7);
            var levels = rentedArrayForLevels.AsSpan(0, 7);
            var commands = ArrayPool<SimaiCommand>.Shared.Rent(16);
            var cI = 0;// for commands
            var i = 0;
            try
            {
                designers.Fill(string.Empty);
                fumens.Fill(string.Empty);
                levels.Fill(string.Empty);

                var lineCount = content.Count('\n') + 1;
                Span<Range> ranges = stackalloc Range[lineCount];
                lineCount = content.Split(ranges, '\n');
                ranges = ranges.Slice(0, lineCount);
                for (i = 0; i < lineCount; i++)
                {
                    var range = ranges[i];
                    var maidataTxt = content[range].Trim();
                    if (maidataTxt.IsEmpty)
                    {
                        continue;
                    }

                    if (maidataTxt.StartsWith("&title="))
                    {
                        SetValue(maidataTxt, ref title);
                    }
                    else if (maidataTxt.StartsWith("&artist="))
                    {
                        SetValue(maidataTxt, ref artist);
                    }
                    else if (maidataTxt.StartsWith("&des"))
                    {
                        if (maidataTxt.StartsWith("&des="))
                        {
                            SetValue(maidataTxt, ref designer);
                        }
                        else
                        {
                            const string DES_1_STRING = "&des_1=";
                            const string DES_2_STRING = "&des_2=";
                            const string DES_3_STRING = "&des_3=";
                            const string DES_4_STRING = "&des_4=";
                            const string DES_5_STRING = "&des_5=";
                            const string DES_6_STRING = "&des_6=";
                            const string DES_7_STRING = "&des_7=";
                            for (var j = 0; j < 7; j++)
                            {
                                var desPrefix = j switch
                                {
                                    0 => DES_1_STRING,
                                    1 => DES_2_STRING,
                                    2 => DES_3_STRING,
                                    3 => DES_4_STRING,
                                    4 => DES_5_STRING,
                                    5 => DES_6_STRING,
                                    6 => DES_7_STRING,
                                    _ => throw new ArgumentOutOfRangeException()
                                };
                                if (maidataTxt.StartsWith(desPrefix))
                                {
                                    SetValue(maidataTxt, ref designers[j]);
                                }
                            }
                        }

                    }
                    else if (maidataTxt.StartsWith("&first="))
                    {
                        if (!float.TryParse(GetValue(maidataTxt), out first))
                        {
                            first = 0;
                        }
                    }
                    else if (maidataTxt.StartsWith("&lv_"))
                    {
                        const string LV_1_STRING = "&lv_1=";
                        const string LV_2_STRING = "&lv_2=";
                        const string LV_3_STRING = "&lv_3=";
                        const string LV_4_STRING = "&lv_4=";
                        const string LV_5_STRING = "&lv_5=";
                        const string LV_6_STRING = "&lv_6=";
                        const string LV_7_STRING = "&lv_7=";
                        for (var j = 1; j < 8; j++)
                        {
                            var lvPrefix = j switch
                            {
                                1 => LV_1_STRING,
                                2 => LV_2_STRING,
                                3 => LV_3_STRING,
                                4 => LV_4_STRING,
                                5 => LV_5_STRING,
                                6 => LV_6_STRING,
                                7 => LV_7_STRING,
                                _ => throw new ArgumentOutOfRangeException()
                            };
                            if (maidataTxt.StartsWith(lvPrefix))
                            {
                                SetValue(maidataTxt, ref levels[j - 1]);
                            }
                        }
                    }
                    else if (maidataTxt.StartsWith("&inote_"))
                    {
                        const string INOTE_1_STRING = "&inote_1=";
                        const string INOTE_2_STRING = "&inote_2=";
                        const string INOTE_3_STRING = "&inote_3=";
                        const string INOTE_4_STRING = "&inote_4=";
                        const string INOTE_5_STRING = "&inote_5=";
                        const string INOTE_6_STRING = "&inote_6=";
                        const string INOTE_7_STRING = "&inote_7=";

                        for (var j = 1; j < 8; j++)
                        {
                            var inotePrefix = j switch
                            {
                                1 => INOTE_1_STRING,
                                2 => INOTE_2_STRING,
                                3 => INOTE_3_STRING,
                                4 => INOTE_4_STRING,
                                5 => INOTE_5_STRING,
                                6 => INOTE_6_STRING,
                                7 => INOTE_7_STRING,
                                _ => throw new ArgumentOutOfRangeException()
                            };
                            if (maidataTxt.StartsWith(inotePrefix))
                            {
                                var buffer = ArrayPool<char>.Shared.Rent(32);
                                try
                                {
                                    Array.Clear(buffer, 0, buffer.Length);
                                    var bufferIndex = 0;
                                    var value = GetValue(maidataTxt);
                                    BufferHelper.EnsureBufferLength(value.Length + 1, ref buffer);
                                    value.CopyTo(buffer);
                                    bufferIndex += value.Length;
                                    buffer[bufferIndex++] = '\n';
                                    i++;
                                    for (; i < lineCount; i++)
                                    {
                                        range = ranges[i];
                                        maidataTxt = content[range].Trim();
                                        if (maidataTxt.IsEmpty)
                                        {
                                            continue;
                                        }
                                        else if (maidataTxt[0] == '&')
                                        {
                                            break;
                                        }
                                        for (var i2 = 0; i2 < maidataTxt.Length; i2++)
                                        {
                                            ref readonly var current = ref maidataTxt[i2];
                                            if (current == 'E')
                                            {
                                                break;
                                            }
                                            BufferHelper.EnsureBufferLength(bufferIndex + 1, ref buffer);
                                            buffer[bufferIndex++] = current;
                                        }
                                        BufferHelper.EnsureBufferLength(bufferIndex + 1, ref buffer);
                                        buffer[bufferIndex++] = '\n';
                                    }

                                    fumens[j - 1] = new string(buffer.AsSpan(0, bufferIndex).Trim());
                                }
                                finally
                                {
                                    ArrayPool<char>.Shared.Return(buffer);
                                }
                            }
                        }
                    }
                    else if (maidataTxt.StartsWith("&"))
                    {
                        if (!maidataTxt.Contains('=') || !SimaiCommand.TryParse(maidataTxt, out var cmd))
                        {
                            throw new InvalidSimaiMarkupException(i + 1, 0, maidataTxt.ToString());
                        }
                        BufferHelper.EnsureBufferLength(cI + 1, ref commands);
                        commands[i++] = cmd;
                    }
                }


                if (!string.IsNullOrEmpty(designer))
                {
                    for (var j = 0; j < 7; j++)
                    {
                        ref var d = ref designers[j];
                        if (string.IsNullOrEmpty(d))
                        {
                            d = designer;
                        }
                    }
                }
                var encoding = Encoding.UTF8;
                var byteCount = encoding.GetByteCount(content);
                var bytes = new byte[byteCount];
                encoding.GetBytes(content, bytes);
                return new SimaiMetadata(title,
                                         artist,
                                         first,
                                         designers,
                                         levels,
                                         fumens,
                                         commands.AsSpan(0, cI),
                                         MD5Helper.ComputeHashAsBase64String(bytes));
            }
            catch (InvalidSimaiMarkupException)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new InvalidSimaiMarkupException(i + 1, 0, "在maidata.txt第" + (i + 1) + "行:\n" + e.Message + "读取谱面时出现错误");
            }
            finally
            {
                ArrayPool<string>.Shared.Return(rentedArrayForDesigners);
                ArrayPool<string>.Shared.Return(rentedArrayForFumens);
                ArrayPool<string>.Shared.Return(rentedArrayForLevels);
                ArrayPool<SimaiCommand>.Shared.Return(commands);
            }
        }
        public static SimaiMetadata ParseMetadata(Stream contentStream)
        {
            return ParseMetadata(contentStream, Encoding.UTF8);
        }
        public static SimaiMetadata ParseMetadata(Stream contentStream, Encoding encoding)
        {
            using (var decodeStream = new StreamReader(contentStream, encoding))
            {
                var contentBuffer = ArrayPool<char>.Shared.Rent(4096);
                try
                {
                    var read = 0;
                    Span<char> buffer = stackalloc char[4096];
                    while (!decodeStream.EndOfStream)
                    {
                        var currentRead = decodeStream.Read(buffer);
                        if (currentRead == 0)
                        {
                            continue;
                        }
                        BufferHelper.EnsureBufferLength(contentBuffer.Length + currentRead, ref contentBuffer);
                        buffer.Slice(0, currentRead).CopyTo(contentBuffer.AsSpan(read));
                        read += currentRead;
                    }
                    return ParseMetadata(contentBuffer.AsSpan(read));
                }
                finally
                {
                    ArrayPool<char>.Shared.Return(contentBuffer);
                }
            }
        }
        public static Task<SimaiMetadata> ParseMetadataAsync(string content)
        {
            return Task.Run(() => ParseMetadata(content));
        }
        public static Task<SimaiMetadata> ParseMetadataAsync(ReadOnlyMemory<char> content)
        {
            return Task.Run(() => ParseMetadata(content.Span));
        }
        public static Task<SimaiMetadata> ParseMetadataAsync(Stream contentStream)
        {
            return ParseMetadataAsync(contentStream, Encoding.UTF8);
        }
        public static async Task<SimaiMetadata> ParseMetadataAsync(Stream contentStream, Encoding encoding)
        {
            using (var decodeStream = new StreamReader(contentStream, encoding))
            {
                var contentBuffer = ArrayPool<char>.Shared.Rent(4096);
                var buffer = ArrayPool<char>.Shared.Rent(4096);
                try
                {
                    var read = 0;
                    while (!decodeStream.EndOfStream)
                    {
                        var currentRead = await decodeStream.ReadAsync(buffer);
                        if (currentRead == 0)
                        {
                            continue;
                        }
                        BufferHelper.EnsureBufferLength(contentBuffer.Length + currentRead, ref contentBuffer);
                        Array.Copy(buffer, 0, contentBuffer, read, currentRead);
                        read += currentRead;
                    }
                    return await ParseMetadataAsync(contentBuffer.AsMemory(read));
                }
                finally
                {
                    ArrayPool<char>.Shared.Return(contentBuffer);
                    ArrayPool<char>.Shared.Return(buffer);
                }
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SimaiChart ParseChart(string fumen)
        {
            return ParseChart(string.Empty, string.Empty, fumen);
        }
        public static SimaiChart ParseChart(string level, string designer, ReadOnlySpan<char> fumen)
        {
            static bool IsNote(char c)
            {
                var isTapOrHoldOrSlide = c >= '0' && c <= '9';
                var isTouchOrTouchHold = c >= 'A' && c <= 'E';

                return isTapOrHoldOrSlide || isTouchOrTouchHold;
            }
            if (fumen.IsEmpty)
            {
                return new SimaiChart(level, designer, string.Empty, null);
            }
            var noteContentBuffer = ArrayPool<char>.Shared.Rent(16);
            var noteRawTimingBuffer = ArrayPool<SimaiRawTimingPoint>.Shared.Rent(16);
            var commaTimingBuffer = ArrayPool<SimaiTimingPoint>.Shared.Rent(16);

            var noteContentBufIndex = 0;
            var noteRawTimingBufIndex = 0;
            var commaTimingBufIndex = 0;

            float bpm = 0;
            var curHSpeed = 1f;
            double time = 0; //in seconds
            var beats = 4f; //{4}
            var haveNote = false;
            //var noteTemp = "";
            
            int Ycount = 1, Xcount = 0;

            /// Xcount| 1 2 3 4 5 6 7 8 9 10| 
            /// --------------------------------
            ///       | A B C D E F G H I J | 1
            ///       | K L N M O P Q R F T | 2
            ///       | U V W X Y Z 1 1 4 5 | 3
            ///       | 1 4 X M M C G H H H | 4
            /// ----------------------------| Ycount
            try
            {
                for (var i = 0; i < fumen.Length; i++)
                {
                    if (fumen[i] == '\n')
                    {
                        Ycount++;
                        Xcount = 0;
                        continue;
                    }
                    else
                    {
                        Xcount++;
                    }
                    switch(fumen[i])
                    {
                        case '|': // 跳过注释
                            {
                                var str = fumen[i..];
                                if (str.Length >= 2 && str[i + 1] == '|')
                                {
                                    i++;
                                    Xcount++;
                                    for (; i < fumen.Length;)
                                    {
                                        i++;
                                        Xcount++;
                                        if (fumen[i] == '\n')
                                        {
                                            Ycount++;
                                            Xcount = 0;
                                            break;
                                        }
                                    }
                                }
                                else
                                {
                                    var s = fumen[i].ToString();
                                    throw new InvalidSimaiMarkupException(Ycount, Xcount, s, $"Unexpected character \"{s}\"");
                                }
                            }
                            continue;
                        case '(': //Get bpm
                            {
                                haveNote = false;
                                //noteTemp = "";
                                noteContentBufIndex = 0;

                                if (fumen[i..].Length >= 3) // (x)
                                {
                                    var startAt = i + 1;
                                    for (; i < fumen.Length;)
                                    {
                                        i++;
                                        Xcount++;
                                        if (fumen[i] == '\n')
                                        {
                                            Ycount++;
                                            Xcount = 0;
                                            continue;
                                        }
                                        else if (fumen[i] == ')')
                                        {
                                            break;
                                        }
                                    }
                                    var endAt = i;
                                    var bpmStr = fumen[startAt..endAt].Trim();

                                    if (!float.TryParse(bpmStr, out bpm))
                                    {
                                        throw new InvalidSimaiSyntaxException(Ycount, Xcount, bpmStr.ToString(), "BPM value must be a number");
                                    }
                                }
                                else
                                {
                                    var s = fumen[i].ToString();
                                    throw new InvalidSimaiMarkupException(Ycount, Xcount, s, $"Unexpected character \"{s}\"");
                                }
                                //Console.WriteLine("BPM" + bpm);
                            }
                            continue;
                        case '{'://Get beats
                            {
                                haveNote = false;
                                //noteTemp = "";
                                noteContentBufIndex = 0;

                                if (fumen[i..].Length >= 3) // {x}
                                {
                                    var startAt = i + 1;
                                    for (; i < fumen.Length;)
                                    {
                                        i++;
                                        Xcount++;
                                        if (fumen[i] == '\n')
                                        {
                                            Ycount++;
                                            Xcount = 0;
                                            continue;
                                        }
                                        else if (fumen[i] == '}')
                                        {
                                            break;
                                        }
                                    }
                                    var endAt = i;
                                    var beatsStr = fumen[startAt..endAt].Trim();

                                    if (beatsStr.IsEmpty)
                                    {
                                        throw new InvalidSimaiSyntaxException(Ycount, Xcount, fumen[startAt..endAt].ToString(), "Beats value must be a number");
                                    }
                                    else if (beatsStr[0] == '#')
                                    {
                                        if (!float.TryParse(beatsStr[1..], out var beatInterval))
                                        {
                                            throw new InvalidSimaiSyntaxException(Ycount, Xcount, beatsStr.ToString(), "Beats value must be a number");
                                        }
                                        beats = 240f / (bpm * beatInterval);
                                    }
                                    else if (!float.TryParse(beatsStr, out beats))
                                    {
                                        throw new InvalidSimaiSyntaxException(Ycount, Xcount, beatsStr.ToString(), "Beats value must be a number");
                                    }
                                }
                                else
                                {
                                    var s = fumen[i].ToString();
                                    throw new InvalidSimaiMarkupException(Ycount, Xcount, s, $"Unexpected character \"{s}\"");
                                }
                                //Console.WriteLine("BEAT" + beats);
                            }
                            continue;
                        case '<':// Get HS: <HS*1.0>
                            {
                                if(haveNote)
                                {
                                    break;
                                }
                                haveNote = false;
                                //noteTemp = "";
                                noteContentBufIndex = 0;

                                if (fumen[i..].Length >= 4) // <HS*x>
                                {
                                    var startAt = i + 1;
                                    var buffer = ArrayPool<char>.Shared.Rent(16);
                                    var bufferIndex = 0;
                                    var tagIndex = -1; // position of '*'
                                    try
                                    {
                                        for (; i < fumen.Length;)
                                        {
                                            i++;
                                            Xcount++;
                                            ref readonly var currentChar = ref fumen[i];
                                            if (currentChar == '\n')
                                            {
                                                Ycount++;
                                                Xcount = 0;
                                                continue;
                                            }
                                            else if(currentChar == '*')
                                            {
                                                if(tagIndex != -1)
                                                {
                                                    throw new InvalidSimaiSyntaxException(Ycount, Xcount, fumen[(startAt - 1)..(i + 1)].ToString(), "Unexpected HS declaration syntax");
                                                }
                                                tagIndex = bufferIndex;
                                            }
                                            else if (currentChar == '>')
                                            {
                                                break;
                                            }
                                            BufferHelper.EnsureBufferLength(bufferIndex + 1, ref buffer);
                                            buffer[bufferIndex++] = currentChar;
                                        }
                                        var hsContent = buffer.AsSpan(0, bufferIndex);
                                        var isInvalid = hsContent.IsEmpty ||
                                                        hsContent.Length < 4 ||
                                                        hsContent[0] != 'H' ||
                                                        hsContent[1] != 'S' ||
                                                        tagIndex == -1;
                                        if (isInvalid) // min: HS*1
                                        {
                                            throw new InvalidSimaiSyntaxException(Ycount, Xcount, hsContent.ToString(), "Unexpected HS declaration syntax");
                                        }
                                        var hsValue = hsContent[(tagIndex + 1)..]; // get "1.0" from HS*1.0
                                        if(!float.TryParse(hsValue,out curHSpeed))
                                        {
                                            throw new InvalidSimaiMarkupException(Ycount, Xcount, hsContent.ToString(), "HSpeed value must be a number");
                                        }
                                        //Console.WriteLine("HS" + curHSpeed);
                                    }
                                    finally
                                    {
                                        ArrayPool<char>.Shared.Return(buffer);
                                    }
                                }
                                else
                                {
                                    var s = fumen[i].ToString();
                                    throw new InvalidSimaiMarkupException(Ycount, Xcount, s, $"Unexpected character \"{s}\"");
                                }
                            }
                            continue;
                    }

                    if (!haveNote && IsNote(fumen[i]))
                    {
                        haveNote = true;
                        noteContentBufIndex = 0;
                    }

                    if (fumen[i] == ',')
                    {
                        if (haveNote)
                        {
                            var noteContent = (ReadOnlySpan<char>)(noteContentBuffer.AsSpan(0, noteContentBufIndex));
                            var fakeEachTagCount = noteContent.Count('`');

                            if (fakeEachTagCount != 0)
                            {
                                var rentedBufferForRanges = ArrayPool<Range>.Shared.Rent(fakeEachTagCount + 1);
                                var ranges = rentedBufferForRanges.AsSpan(fakeEachTagCount + 1);
                                try
                                {
                                    // 伪双
                                    var tagCount = noteContent.Split(ranges, '`', StringSplitOptions.RemoveEmptyEntries);
                                    var fakeTime = time;
                                    var timeInterval = 1.875 / bpm; // 128分音

                                    for (var j = 0; j < tagCount; j++)
                                    {
                                        var fakeEachGroup = noteContent[ranges[j]];
                                        //Console.WriteLine(fakeEachGroup.ToString());
                                        var rawTp = new SimaiRawTimingPoint(fakeTime, 
                                                                            fakeEachGroup, 
                                                                            Xcount, 
                                                                            Ycount, 
                                                                            bpm,
                                                                            curHSpeed);
                                        BufferHelper.EnsureBufferLength(noteRawTimingBufIndex + 1, ref noteRawTimingBuffer);
                                        noteRawTimingBuffer[noteRawTimingBufIndex++] = rawTp;
                                        fakeTime += timeInterval;
                                    }
                                }
                                finally
                                {
                                    ArrayPool<Range>.Shared.Return(rentedBufferForRanges);
                                }
                            }
                            else
                            {
                                var rawTp = new SimaiRawTimingPoint(time, 
                                                                    noteContent, 
                                                                    Xcount, 
                                                                    Ycount, 
                                                                    bpm, 
                                                                    curHSpeed);
                                BufferHelper.EnsureBufferLength(noteRawTimingBufIndex + 1, ref noteRawTimingBuffer);
                                noteRawTimingBuffer[noteRawTimingBufIndex++] = rawTp;
                            }
                            //Console.WriteLine("Note:" + noteTemp);

                            //noteTemp = "";
                            noteContentBufIndex = 0;
                        }
                        BufferHelper.EnsureBufferLength(commaTimingBufIndex + 1, ref  commaTimingBuffer);
                        commaTimingBuffer[commaTimingBufIndex + 1] = new SimaiTimingPoint(time, null, string.Empty, Xcount, Ycount, bpm, 1, i);

                        time += 1d / (bpm / 60d) * 4d / beats;
                        //Console.WriteLine(time);
                        haveNote = false;
                        noteContentBufIndex = 0;
                    }
                    else if(haveNote)
                    {
                        ref readonly var curChar = ref fumen[i];
                        BufferHelper.EnsureBufferLength(noteContentBufIndex + 1, ref noteContentBuffer);
                        noteContentBuffer[noteContentBufIndex++] = curChar;
                    }
                }
                var noteTimingPoints = new SimaiTimingPoint[noteRawTimingBufIndex];

                Parallel.For(0, noteRawTimingBufIndex, i =>
                {
                    var rawTiming = noteRawTimingBuffer[i];
                    var timingPoint = rawTiming.Parse();
                    noteTimingPoints[i] = timingPoint;
                });

                return new SimaiChart(level, designer, fumen.ToString(), noteTimingPoints, commaTimingBuffer);
            }
            catch (InvalidSimaiMarkupException)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new Exception("Error at " + Ycount + "," + Xcount + "\n" + e.Message);
            }
            finally
            {
                ArrayPool<char>.Shared.Return(noteContentBuffer);
                ArrayPool<SimaiRawTimingPoint>.Shared.Return(noteRawTimingBuffer);
                ArrayPool<SimaiTimingPoint>.Shared.Return(commaTimingBuffer);
            }
        }

        public static Task<SimaiChart> ParseChartAsync(string level, string designer, string fumen)
        {
            return Task.Run(() => ParseChart(level, designer, fumen));
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static string GetValue(string varline)
        {
            return varline.Substring(varline.IndexOf("=") + 1).Trim();
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ReadOnlySpan<char> GetValue(ReadOnlySpan<char> varline)
        {
            var index = varline.IndexOf('=');
            if (index == -1)
            {
                return ReadOnlySpan<char>.Empty;
            }
            return varline.Slice(index + 1).Trim();
        }

        //Note: this method only deparse RawChart
        public static string Deparse(SimaiFile simaiFile)
        {
            var sb = new StringBuilder();
            var finalDesigner = string.Empty;

            sb.Append($"&title=")
              .AppendLine(simaiFile.Title)
              .Append($"&artist=")
              .AppendLine(simaiFile.Artist)
              .Append("&first=")
              .Append(simaiFile.Offset)
              .AppendLine();
            for (int i = 0; i < 7; i++)
            {
                var chart = simaiFile.Charts[i];
                if (chart is null)
                {
                    continue;
                }
                if (!string.IsNullOrEmpty(chart.Designer))
                {
                    finalDesigner = chart.Designer;
                    sb.Append("&des_")
                      .Append(i + 1)
                      .Append('=')
                      .AppendLine(chart.Designer);
                }
                if (!string.IsNullOrEmpty(chart.Level))
                {
                    sb.Append("&lv_")
                      .Append(i + 1)
                      .Append('=')
                      .AppendLine(chart.Level);
                }
            }
            sb.Append("&des_")
              .Append('=')
              .AppendLine(finalDesigner);
            foreach (var command in simaiFile.Commands)
            {
                sb.Append('&')
                  .Append(command.Prefix)
                  .Append('=')
                  .AppendLine(command.Value);
            }
            for (int i = 0; i < 7; i++)
            {
                var chart = simaiFile.Charts[i].Fumen;
                if (string.IsNullOrEmpty(chart))
                {
                    continue;
                }
                sb.Append("&inote_")
                  .Append(i + 1)
                  .Append('=')
                  .Append(chart)
                  .AppendLine()
                  .Append('E')
                  .AppendLine();
            }
            return sb.ToString();
        }
        public static void Deparse(SimaiFile simaiFile, Stream stream)
        {
            Deparse(simaiFile, stream, Encoding.UTF8);
        }
        public static void Deparse(SimaiFile simaiFile, Stream stream, Encoding encoding)
        {
            var fumen = Deparse(simaiFile);
            using var writer = new StreamWriter(stream, encoding);
            writer.Write(fumen);
        }
        public static Task<string> DeparseAsync(SimaiFile simaiFile)
        {
            return Task.Run(() => Deparse(simaiFile));
        }
        public static async Task DeparseAsync(SimaiFile simaiFile, Stream stream)
        {
            await DeparseAsync(simaiFile, stream, Encoding.UTF8);
        }
        public static async Task DeparseAsync(SimaiFile simaiFile, Stream stream, Encoding encoding)
        {
            var fumen = await DeparseAsync(simaiFile);
            using var writer = new StreamWriter(stream, encoding);
            await writer.WriteAsync(fumen);
        }

        static class MD5Helper
        {
            public static byte[] ComputeHash(byte[] data)
            {
                using (var md5 = MD5.Create())
                {
                    return md5.ComputeHash(data);
                }
            }
            public static string ComputeHashAsBase64String(byte[] data)
            {
                var hash = ComputeHash(data);

                return Convert.ToBase64String(hash);
            }
            public static async Task<byte[]> ComputeHashAsync(byte[] data)
            {
                using (var md5 = MD5.Create())
                {
                    return await Task.Run(() => md5.ComputeHash(data));
                }
            }
            public static async Task<string> ComputeHashAsBase64StringAsync(byte[] data)
            {
                var hash = await ComputeHashAsync(data);

                return Convert.ToBase64String(hash);
            }
        }
    }
}
