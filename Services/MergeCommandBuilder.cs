using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using FFLiteGUI.Models;
using FFLiteGUI.Utils;

namespace FFLiteGUI.Services
{
    public class MergeCommandBuilder
    {
        private readonly string _ffmpegPath;
        private readonly string _ffprobePath;

        public MergeCommandBuilder(string ffmpegPath, string ffprobePath)
        {
            _ffmpegPath = ffmpegPath;
            _ffprobePath = ffprobePath;
        }

        public string BuildCommand(
            string mainVideoPath,
            List<TrackInfo> tracks,
            string outputPath,
            string container,
            bool copyChapters,
            string chapterFile,
            bool pipEnabled)
        {
            if (string.IsNullOrEmpty(_ffmpegPath))
                throw new InvalidOperationException("未找到 ffmpeg 可执行文件");

            var enabledTracks = tracks.Where(t => t.Enabled).ToList();
            if (!enabledTracks.Any())
                throw new InvalidOperationException("没有启用的轨道");

            // 收集所有输入文件（去重）
            var inputFiles = new List<string>();
            foreach (var track in enabledTracks)
            {
                if (!inputFiles.Contains(track.FilePath))
                    inputFiles.Add(track.FilePath);
            }

            var args = new List<string>();
            args.Add("-y");
            args.Add("-fflags +genpts");

            // 处理每个输入的截取参数（仅视频轨道）
            var fileTrim = new Dictionary<string, (string start, string end)>();
            foreach (var track in enabledTracks)
            {
                if (track.Type == "video")
                {
                    var settings = track.EncSettings;
                    if (settings.TryGetValue("trim_enabled", out string trimEnabled) && trimEnabled == "True")
                    {
                        string start = settings.GetValueOrDefault("trim_start", "");
                        string end = settings.GetValueOrDefault("trim_end", "");
                        if (!string.IsNullOrEmpty(start) || !string.IsNullOrEmpty(end))
                        {
                            fileTrim[track.FilePath] = (start, end);
                        }
                    }
                }
            }

            // 添加 -ss 和 -to 以及 -i 参数
            foreach (var file in inputFiles)
            {
                if (fileTrim.TryGetValue(file, out var trim))
                {
                    if (!string.IsNullOrEmpty(trim.start))
                        args.Add($"-ss {trim.start}");
                    if (!string.IsNullOrEmpty(trim.end))
                        args.Add($"-to {trim.end}");
                }
                args.Add($"-i \"{PathHelper.Normalize(file)}\"");
            }

            var videoTracks = enabledTracks.Where(t => t.Type == "video").ToList();
            var audioTracks = enabledTracks.Where(t => t.Type == "audio").ToList();
            var subtitleTracks = enabledTracks.Where(t => t.Type == "subtitle").ToList();

            if (!videoTracks.Any())
                throw new InvalidOperationException("没有启用的视频轨道");

            // 获取输入文件索引映射
            var inputIndexMap = new Dictionary<string, int>();
            for (int i = 0; i < inputFiles.Count; i++)
            {
                inputIndexMap[inputFiles[i]] = i;
            }

            if (pipEnabled)
            {
                // 画中画模式：使用 filter_complex
                var mainVideo = videoTracks[0];
                var subVideos = videoTracks.Skip(1).ToList();

                var filterParts = new List<string>();
                int mainIdx = inputIndexMap[mainVideo.FilePath];
                string mainFilters = BuildVideoFilterChain(mainVideo.EncSettings);
                if (!string.IsNullOrEmpty(mainFilters) && mainFilters != "null")
                {
                    filterParts.Add($"[{mainIdx}:v]{mainFilters}[v_main_proc]");
                    string currentV = "v_main_proc";

                    // 主视频画布偏移
                    if (mainVideo.PadEnabled && !string.IsNullOrEmpty(mainVideo.PadWidth) && !string.IsNullOrEmpty(mainVideo.PadHeight))
                    {
                        string pw = mainVideo.PadWidth.Trim();
                        string ph = mainVideo.PadHeight.Trim();
                        string ox = string.IsNullOrEmpty(mainVideo.OffsetX) ? "0" : mainVideo.OffsetX;
                        string oy = string.IsNullOrEmpty(mainVideo.OffsetY) ? "0" : mainVideo.OffsetY;
                        filterParts.Add($"nullsrc=size={pw}x{ph}[canvas]");
                        filterParts.Add($"[canvas][{currentV}]overlay={ox}:{oy}:shortest=1[v_main_pad]");
                        currentV = "v_main_pad";
                    }

                    for (int i = 0; i < subVideos.Count; i++)
                    {
                        var sv = subVideos[i];
                        int svIdx = inputIndexMap[sv.FilePath];
                        string svFilters = BuildVideoFilterChain(sv.EncSettings);
                        string subSrc;
                        if (!string.IsNullOrEmpty(svFilters) && svFilters != "null")
                        {
                            filterParts.Add($"[{svIdx}:v]{svFilters}[v_sub_{i}]");
                            subSrc = $"v_sub_{i}";
                        }
                        else
                        {
                            filterParts.Add($"[{svIdx}:v]null[v_sub_{i}]");
                            subSrc = $"v_sub_{i}";
                        }

                        if (sv.OverlayEnabled)
                        {
                            string x = string.IsNullOrEmpty(sv.OverlayX) ? "0" : sv.OverlayX;
                            string y = string.IsNullOrEmpty(sv.OverlayY) ? "0" : sv.OverlayY;
                            filterParts.Add($"[{currentV}][{subSrc}]overlay={x}:{y}[v_out_{i}]");
                            currentV = $"v_out_{i}";
                        }
                        else
                        {
                            filterParts.Add($"[{currentV}]null[{currentV}]");
                        }
                    }

                    args.Add($"-filter_complex \"{string.Join(";", filterParts)}\"");
                    args.Add($"-map \"[{currentV}]\"");
                }
                else
                {
                    // 无滤镜情况
                    filterParts.Add($"[{mainIdx}:v]null[v_main_proc]");
                    string currentV = "v_main_proc";
                    for (int i = 0; i < subVideos.Count; i++)
                    {
                        var sv = subVideos[i];
                        int svIdx = inputIndexMap[sv.FilePath];
                        filterParts.Add($"[{svIdx}:v]null[v_sub_{i}]");
                        if (sv.OverlayEnabled)
                        {
                            string x = string.IsNullOrEmpty(sv.OverlayX) ? "0" : sv.OverlayX;
                            string y = string.IsNullOrEmpty(sv.OverlayY) ? "0" : sv.OverlayY;
                            filterParts.Add($"[{currentV}][v_sub_{i}]overlay={x}:{y}[v_out_{i}]");
                            currentV = $"v_out_{i}";
                        }
                    }
                    args.Add($"-filter_complex \"{string.Join(";", filterParts)}\"");
                    args.Add($"-map \"[{currentV}]\"");
                }

                // 视频编码参数
                var vSettings = mainVideo.EncSettings;
                string vcodec = vSettings.GetValueOrDefault("encoder", "libx265");
                args.Add($"-c:v {vcodec}");
                if (vSettings.TryGetValue("preset", out string preset))
                    args.Add($"-preset {preset}");

                string rc = vSettings.GetValueOrDefault("rate_control_type", "crf");
                if (rc == "crf" && vSettings.TryGetValue("crf_value", out string crf))
                    args.Add($"-crf {crf}");
                else if (rc == "cq" && vSettings.TryGetValue("cq_value", out string cq))
                    args.Add($"-cq {cq}");
                else if (rc == "global_quality" && vSettings.TryGetValue("global_quality", out string gq))
                    args.Add($"-global_quality {gq}");
                else if (rc == "bitrate" && vSettings.TryGetValue("bitrate_video", out string bit))
                    args.Add($"-b:v {bit}");

                if (vSettings.TryGetValue("frame_rate_type", out string fpsType) && fpsType == "custom" &&
                    vSettings.TryGetValue("frame_rate_custom", out string fps))
                    args.Add($"-r {fps}");

                if (vSettings.TryGetValue("pix_fmt_enabled", out string pixEnabled) && pixEnabled == "True" &&
                    vSettings.TryGetValue("pix_fmt", out string pixFmt))
                    args.Add($"-pix_fmt {pixFmt}");
            }
            else
            {
                // 非画中画模式：直接复制或编码第一个视频轨道
                var videoTrack = videoTracks[0];
                int vIdx = inputIndexMap[videoTrack.FilePath];
                args.Add($"-map {vIdx}:v:0");

                var vSettings = videoTrack.EncSettings;
                string vcodec = vSettings.GetValueOrDefault("encoder", "copy");
                if (vcodec == "copy")
                {
                    args.Add("-c:v copy");
                }
                else
                {
                    args.Add($"-c:v {vcodec}");
                    if (vSettings.TryGetValue("preset", out string preset))
                        args.Add($"-preset {preset}");

                    string rc = vSettings.GetValueOrDefault("rate_control_type", "crf");
                    if (rc == "crf" && vSettings.TryGetValue("crf_value", out string crf))
                        args.Add($"-crf {crf}");
                    else if (rc == "cq" && vSettings.TryGetValue("cq_value", out string cq))
                        args.Add($"-cq {cq}");
                    else if (rc == "global_quality" && vSettings.TryGetValue("global_quality", out string gq))
                        args.Add($"-global_quality {gq}");
                    else if (rc == "bitrate" && vSettings.TryGetValue("bitrate_video", out string bit))
                        args.Add($"-b:v {bit}");

                    if (vSettings.TryGetValue("frame_rate_type", out string fpsType) && fpsType == "custom" &&
                        vSettings.TryGetValue("frame_rate_custom", out string fps))
                        args.Add($"-r {fps}");

                    if (vSettings.TryGetValue("pix_fmt_enabled", out string pixEnabled) && pixEnabled == "True" &&
                        vSettings.TryGetValue("pix_fmt", out string pixFmt))
                        args.Add($"-pix_fmt {pixFmt}");
                }
            }

            // 处理音频轨道
            int audioMapCount = 0;
            foreach (var audio in audioTracks)
            {
                int aIdx = inputIndexMap[audio.FilePath];
                args.Add($"-map {aIdx}:a:0");
                string enc = audio.EncSettings.GetValueOrDefault("encoder", "copy");
                if (enc == "copy")
                {
                    args.Add($"-c:a:{audioMapCount} copy");
                }
                else
                {
                    string bitrate = audio.EncSettings.GetValueOrDefault("bitrate", "128k");
                    string samplerate = audio.EncSettings.GetValueOrDefault("samplerate", "44100");
                    args.Add($"-c:a:{audioMapCount} {enc}");
                    args.Add($"-b:a:{audioMapCount} {bitrate}");
                    args.Add($"-ar:a:{audioMapCount} {samplerate}");
                }
                audioMapCount++;
            }
            if (audioMapCount == 0)
                args.Add("-an");

            // 处理字幕轨道
            int subMapCount = 0;
            bool firstSubDefault = false;
            foreach (var sub in subtitleTracks)
            {
                int sIdx = inputIndexMap[sub.FilePath];
                string enc = sub.EncSettings.GetValueOrDefault("encoder", "copy");
                string containerLower = container.ToLower();

                if (containerLower == "mp4")
                {
                    if (enc == "copy")
                    {
                        string origCodec = sub.Codec?.ToLower() ?? "";
                        if (origCodec != "mov_text" && origCodec != "mp4s")
                        {
                            enc = "mov_text";
                        }
                    }
                    else if (enc != "mov_text" && enc != "mp4s")
                    {
                        enc = "mov_text";
                    }
                }

                args.Add($"-map {sIdx}:s:0");
                args.Add($"-c:s:{subMapCount} {enc}");
                if (!firstSubDefault)
                {
                    args.Add($"-disposition:s:{subMapCount} default");
                    firstSubDefault = true;
                }
                subMapCount++;
            }

            // 章节处理
            if (copyChapters && inputFiles.Any())
            {
                args.Add("-map_chapters 0");
            }

            if (!string.IsNullOrEmpty(chapterFile) && File.Exists(chapterFile))
            {
                string chapterFileNorm = PathHelper.Normalize(chapterFile);
                // 章节文件作为额外的输入插入到最前面（索引1）
                args.Insert(1, "-i");
                args.Insert(2, $"\"{chapterFileNorm}\"");
                args.Add("-map_chapters 1");
            }

            // 容器优化
            if (container.ToLower() == "mp4" || container.ToLower() == "mov")
            {
                args.Add("-movflags +faststart");
            }

            args.Add($"\"{PathHelper.Normalize(outputPath)}\"");

            return $"\"{_ffmpegPath}\" {string.Join(" ", args)}";
        }

        private string BuildVideoFilterChain(Dictionary<string, string> settings)
        {
            var filters = new List<string>();

            if (settings.TryGetValue("crop_enabled", out string cropEnabled) && cropEnabled == "True")
            {
                string w = settings.GetValueOrDefault("crop_width", "").Trim();
                string h = settings.GetValueOrDefault("crop_height", "").Trim();
                string left = settings.GetValueOrDefault("crop_left", "0").Trim();
                string top = settings.GetValueOrDefault("crop_top", "0").Trim();
                if (!string.IsNullOrEmpty(w) && !string.IsNullOrEmpty(h))
                {
                    filters.Add($"crop={w}:{h}:{left}:{top}");
                }
            }

            if (settings.TryGetValue("scale_enabled", out string scaleEnabled) && scaleEnabled == "True")
            {
                string method = settings.GetValueOrDefault("scale_method", "width");
                string w = settings.GetValueOrDefault("scale_width", "").Trim();
                string h = settings.GetValueOrDefault("scale_height", "").Trim();
                if (method == "width" && !string.IsNullOrEmpty(w))
                    filters.Add($"scale={w}:-2");
                else if (method == "height" && !string.IsNullOrEmpty(h))
                    filters.Add($"scale=-2:{h}");
                else if (method == "exact" && !string.IsNullOrEmpty(w) && !string.IsNullOrEmpty(h))
                    filters.Add($"scale={w}:{h}");
            }

            string rotate = settings.GetValueOrDefault("rotate", "none");
            if (rotate == "90")
                filters.Add("transpose=1");
            else if (rotate == "180")
                filters.Add("transpose=2,transpose=2");
            else if (rotate == "270")
                filters.Add("transpose=2");

            if (settings.TryGetValue("vflip", out string vflip) && vflip == "True")
                filters.Add("vflip");
            if (settings.TryGetValue("hflip", out string hflip) && hflip == "True")
                filters.Add("hflip");

            string deint = settings.GetValueOrDefault("deinterlace_filter", "none");
            if (deint != "none")
                filters.Add(deint);

            return filters.Any() ? string.Join(",", filters) : "null";
        }
    }
}
