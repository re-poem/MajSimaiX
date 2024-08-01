using System;
using System.IO;
using System.Text;

namespace MajSimaiDecode
{
    public class Maidata
    {
        public string? title;
        public string? artist;
        public string? designer;
        public string? other_commands;
        public float first;
        public string[] fumens = new string[7];
        public string[] levels = new string[7];
        public Maidata(string path) {

            title = "";
            artist = "";
            designer = "";
            first = 0;
            fumens = new string[7];
            levels = new string[7];
            var i = 0;
            other_commands = "";
            try
            {
                var maidataTxt = File.ReadAllLines(path, Encoding.UTF8);
                for (i = 0; i < maidataTxt.Length; i++)
                    if (maidataTxt[i].StartsWith("&title="))
                        title = GetValue(maidataTxt[i]);
                    else if (maidataTxt[i].StartsWith("&artist="))
                        artist = GetValue(maidataTxt[i]);
                    else if (maidataTxt[i].StartsWith("&des="))
                        designer = GetValue(maidataTxt[i]);
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
                    else
                        other_commands += maidataTxt[i].Trim() + "\n";

                other_commands = other_commands.Trim();
            }
            catch (Exception e)
            {
                throw new Exception("在maidata.txt第" + (i + 1) + "行:\n" + e.Message+ "读取谱面时出现错误");
            }
        }
        private string GetValue(string varline)
        {
            return varline.Substring(varline.IndexOf("=") + 1);
        }
    }
}
