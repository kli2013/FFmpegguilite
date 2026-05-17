using System;
using System.Collections.Generic;

namespace FFLiteGUI.Models
{
    public class TaskInfo
    {
        public string InputFile { get; set; }
        public string OutputFile { get; set; }
        public VideoSettings Settings { get; set; }
        public string Command { get; set; }
        public string Status { get; set; }
        public string ErrorMsg { get; set; }

        public TaskInfo()
        {
            Status = "等待";
            ErrorMsg = "";
            Settings = new VideoSettings();
        }

        public TaskInfo(string inputFile, string outputFile, VideoSettings settings, string command)
        {
            InputFile = inputFile;
            OutputFile = outputFile;
            Settings = settings ?? new VideoSettings();
            Command = command;
            Status = "等待";
            ErrorMsg = "";
        }

        public string GetShortCommand()
        {
            if (string.IsNullOrEmpty(Command))
                return "";
            string shortCmd = Command;
            if (!string.IsNullOrEmpty(InputFile))
                shortCmd = System.Text.RegularExpressions.Regex.Replace(shortCmd, 
                    @"(-i\s+)([""'])(.*?)\2", @"$1{input}");
            if (!string.IsNullOrEmpty(OutputFile))
                shortCmd = System.Text.RegularExpressions.Regex.Replace(shortCmd,
                    @"([""'][^""']+\.\w+[""'])$", @"{output}");
            return shortCmd;
        }
    }
}
