using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using FFLiteGUI.Models;
using FFLiteGUI.Services;
using FFLiteGUI.Utils;

namespace FFLiteGUI.Forms
{
    public partial class MainForm : Form
    {
        private string _ffmpegPath;
        private string _ffplayPath;
        private string _ffprobePath;

        private List<TaskInfo> _tasks = new List<TaskInfo>();
        private bool _isProcessing;
        private CancellationTokenSource _cts;
        private SemaphoreSlim _hwSemaphore;
        private int _maxParallel = 2;
        private int _maxHwParallel = 2;

        // 控件（简化声明，实际在设计器中生成）
        private TextBox txtInputFile;
        private TextBox txtOutputDir;
        private TextBox txtOutputSuffix;
        private TextBox txtCustomOutputName;
        private ComboBox cboOutputContainer;
        private ComboBox cboEncoder;
        private ComboBox cboPreset;
        private RadioButton rbCRF, rbCQ, rbGlobalQuality, rbBitrate;
        private TrackBar trkCRF, trkCQ, trkGlobalQuality;
        private Label lblCRF, lblCQ, lblGlobalQuality;
        private TextBox txtBitrate;
        private CheckBox chkHwaccel;
        private ComboBox cboHwaccelDecoder;
        private TextBox txtCustomArgs;
        private CheckBox chkAudioEnabled;
        private ComboBox cboAudioCodec;
        private TextBox txtAudioBitrate, txtAudioSamplerate;
        private CheckBox chkOnlyAudio;
        private ComboBox cboAudioFormat;
        private CheckBox chkScale, chkCrop, chkRotate, chkSpeed, chkSubtitle;
        private TextBox txtScaleW, txtScaleH, txtCropW, txtCropH, txtCropLeft, txtCropTop;
        private ComboBox cboScaleMethod, cboRotate, cboDeinterlace;
        private CheckBox chkVflip, chkHflip, chkPixFmt;
        private ComboBox cboPixFmt;
        private TextBox txtSpeedFactor, txtSubtitlePath;
        private CheckBox chkTrim;
        private TextBox txtTrimStart, txtTrimEnd;
        private ComboBox cboFrameRateType;
        private TextBox txtFrameRateCustom;
        private RichTextBox txtCommandPreview;
        private ListView lvTasks;
        private Button btnAddTask, btnStartQueue, btnStopQueue, btnClearAll, btnRemoveSelected;
        private NumericUpDown nudMaxParallel, nudMaxHwParallel;
        private TextBox txtInfoLog, txtDetailLog;

        public MainForm()
        {
            InitializeComponent();
            InitializeCustomComponents();
            FindFFmpeg();
            LoadSettings();
        }

        private void InitializeCustomComponents()
        {
            this.Size = new Size(1200, 800);
            // 此处省略具体控件布局（为节省篇幅，实际应使用设计器或手写）
            // 但为了编译通过，至少保证所有用到的控件都被初始化
            // 由于您已有设计器文件，此处仅给出核心逻辑，控件通过设计器生成。
        }

        private void FindFFmpeg()
        {
            _ffmpegPath = PathHelper.GetExecutablePath("ffmpeg.exe");
            _ffplayPath = PathHelper.GetExecutablePath("ffplay.exe");
            _ffprobePath = PathHelper.GetExecutablePath("ffprobe.exe");
            if (string.IsNullOrEmpty(_ffmpegPath))
                AppendInfo("⚠️ 未找到 ffmpeg.exe，请将 ffmpeg 放在程序目录或 PATH 中。");
        }

        private void AppendInfo(string text) => AppendText(txtInfoLog, text);
        private void AppendDetail(string text) => AppendText(txtDetailLog, text);
        private void AppendText(TextBox tb, string text)
        {
            if (tb.InvokeRequired) tb.Invoke(new Action(() => tb.AppendText(text + Environment.NewLine)));
            else tb.AppendText(text + Environment.NewLine);
        }

        private VideoSettings GetCurrentSettings()
        {
            return new VideoSettings
            {
                Encoder = cboEncoder.SelectedItem?.ToString(),
                Preset = cboPreset.SelectedItem?.ToString(),
                RateControlType = rbCRF.Checked ? "crf" : (rbCQ.Checked ? "cq" : (rbGlobalQuality.Checked ? "global_quality" : "bitrate")),
                CrfValue = trkCRF.Value,
                CqValue = trkCQ.Value,
                GlobalQuality = trkGlobalQuality.Value,
                BitrateVideo = txtBitrate.Text,
                HwaccelEnabled = chkHwaccel.Checked,
                HwaccelDecoder = cboHwaccelDecoder.SelectedItem?.ToString(),
                CustomArgs = txtCustomArgs.Text,
                OutputDir = txtOutputDir.Text,
                OutputSuffix = txtOutputSuffix.Text,
                CustomOutputName = txtCustomOutputName.Text,
                OutputContainer = cboOutputContainer.SelectedItem?.ToString(),
                AudioEnabled = chkAudioEnabled.Checked,
                AudioCodec = cboAudioCodec.SelectedItem?.ToString(),
                AudioBitrate = txtAudioBitrate.Text,
                AudioSamplerate = txtAudioSamplerate.Text,
                OnlyAudio = chkOnlyAudio.Checked,
                AudioFormat = cboAudioFormat.SelectedItem?.ToString(),
                FrameRateType = cboFrameRateType.SelectedIndex == 0 ? "keep" : "custom",
                FrameRateCustom = txtFrameRateCustom.Text,
                ScaleEnabled = chkScale.Checked,
                ScaleWidth = txtScaleW.Text,
                ScaleHeight = txtScaleH.Text,
                ScaleMethod = cboScaleMethod.SelectedItem?.ToString(),
                CropEnabled = chkCrop.Checked,
                CropWidth = txtCropW.Text,
                CropHeight = txtCropH.Text,
                CropLeft = txtCropLeft.Text,
                CropTop = txtCropTop.Text,
                Rotate = cboRotate.SelectedItem?.ToString(),
                Vflip = chkVflip.Checked,
                Hflip = chkHflip.Checked,
                SpeedEnabled = chkSpeed.Checked,
                SpeedFactor = double.TryParse(txtSpeedFactor.Text, out double sf) ? sf : 1.0,
                DeinterlaceFilter = cboDeinterlace.SelectedItem?.ToString(),
                PixFmtEnabled = chkPixFmt.Checked,
                PixFmt = cboPixFmt.SelectedItem?.ToString(),
                SubtitleEnabled = chkSubtitle.Checked,
                SubtitlePath = txtSubtitlePath.Text,
                TrimEnabled = chkTrim.Checked,
                TrimStart = txtTrimStart.Text,
                TrimEnd = txtTrimEnd.Text
            };
        }

        private void UpdateCommandPreview()
        {
            if (string.IsNullOrEmpty(txtInputFile.Text)) return;
            var settings = GetCurrentSettings();
            string output = GenerateOutputPath(txtInputFile.Text, settings);
            var builder = new FFmpegCommandBuilder(_ffmpegPath);
            try
            {
                string cmd = builder.BuildCommand(txtInputFile.Text, output, settings);
                txtCommandPreview.Text = cmd;
            }
            catch (Exception ex)
            {
                txtCommandPreview.Text = $"生成命令失败: {ex.Message}";
            }
        }

        private string GenerateOutputPath(string input, VideoSettings settings)
        {
            string dir = string.IsNullOrEmpty(settings.OutputDir) ? Path.GetDirectoryName(input) : settings.OutputDir;
            string baseName = Path.GetFileNameWithoutExtension(input);
            string container = settings.OnlyAudio ? settings.AudioFormat : settings.OutputContainer;
            string custom = settings.CustomOutputName?.Trim();
            if (!string.IsNullOrEmpty(custom))
            {
                if (!Path.HasExtension(custom)) custom += "." + container;
                return PathHelper.Normalize(Path.Combine(dir, custom));
            }
            string suffix = settings.OutputSuffix?.Trim();
            if (string.IsNullOrEmpty(suffix) && string.Equals(dir, Path.GetDirectoryName(input), StringComparison.OrdinalIgnoreCase))
                suffix = "_new";
            string outName = $"{baseName}{suffix}.{container}";
            return PathHelper.Normalize(Path.Combine(dir, outName));
        }

        private async void StartQueue()
        {
            if (_isProcessing) return;
            var pending = _tasks.Where(t => t.Status == "等待").ToList();
            if (!pending.Any()) return;
            _isProcessing = true;
            _cts = new CancellationTokenSource();
            _hwSemaphore = new SemaphoreSlim(_maxHwParallel);
            AppendInfo($"🚀 启动队列，最大并行: {_maxParallel}，硬编并发限制: {_maxHwParallel}");
            var options = new ParallelOptions { MaxDegreeOfParallelism = _maxParallel, CancellationToken = _cts.Token };
            try
            {
                await Parallel.ForEachAsync(pending, options, async (task, token) =>
                {
                    bool isHw = IsHardwareEncoder(task.Settings.Encoder);
                    if (isHw) await _hwSemaphore.WaitAsync(token);
                    try
                    {
                        await ProcessTask(task, token);
                    }
                    finally { if (isHw) _hwSemaphore.Release(); }
                });
            }
            catch (OperationCanceledException) { AppendInfo("队列已停止"); }
            finally
            {
                _isProcessing = false;
                _cts?.Dispose();
                _cts = null;
                _hwSemaphore?.Dispose();
                AppendInfo("队列处理完成");
            }
        }

        private bool IsHardwareEncoder(string encoder)
        {
            if (string.IsNullOrEmpty(encoder)) return false;
            string enc = encoder.ToLower();
            return enc.Contains("nvenc") || enc.Contains("qsv") || enc.Contains("amf") || enc.Contains("vaapi") || enc.Contains("videotoolbox");
        }

        private async Task ProcessTask(TaskInfo task, CancellationToken token)
        {
            task.Status = "转码中";
            UpdateTaskList();
            AppendInfo($"开始: {Path.GetFileName(task.InputFile)}");
            var runner = new FFmpegProcessRunner();
            var result = await runner.RunAsync(task.Command, token,
                (line) => AppendDetail(line));
            task.Status = result.Success ? "完成" : "失败";
            task.ErrorMsg = result.Error;
            AppendInfo(result.Success ? $"✅ 完成: {Path.GetFileName(task.InputFile)}" : $"❌ 失败: {Path.GetFileName(task.InputFile)}");
            UpdateTaskList();
        }

        private void UpdateTaskList()
        {
            if (lvTasks.InvokeRequired)
                lvTasks.Invoke(new Action(UpdateTaskList));
            else
            {
                lvTasks.Items.Clear();
                foreach (var t in _tasks)
                {
                    var item = new ListViewItem(Path.GetFileName(t.InputFile));
                    item.SubItems.Add(t.OutputFile);
                    item.SubItems.Add(t.GetShortCommand());
                    item.SubItems.Add(t.Status);
                    item.SubItems.Add(t.ErrorMsg);
                    lvTasks.Items.Add(item);
                }
            }
        }

        private void LoadSettings() { /* 加载预设等 */ }
        private void InitializeComponent() { /* 设计器生成 */ }
    }
}
