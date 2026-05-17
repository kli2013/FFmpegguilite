// Forms\EditTaskForm.cs
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using FFLiteGUI.Models;
using FFLiteGUI.Services;
using FFLiteGUI.Utils;

namespace FFLiteGUI.Forms
{
    public partial class EditTaskForm : Form
    {
        private readonly TaskInfo _task;
        private readonly string _ffmpegPath;
        private VideoSettings _originalSettings;
        private readonly string _inputFile;

        // 控件
        private TabControl _tabControl;
        private TextBox _outputDirTextBox;
        private TextBox _suffixTextBox;
        private TextBox _customNameTextBox;
        private ComboBox _containerComboBox;

        private ComboBox _encoderComboBox;
        private ComboBox _presetComboBox;
        private RadioButton _crfRadio;
        private RadioButton _cqRadio;
        private RadioButton _globalQualityRadio;
        private RadioButton _bitrateRadio;
        private TrackBar _crfTrackBar;
        private Label _crfLabel;
        private TrackBar _cqTrackBar;
        private Label _cqLabel;
        private TrackBar _gqTrackBar;
        private Label _gqLabel;
        private TextBox _bitrateTextBox;
        private CheckBox _hwaccelCheckBox;
        private ComboBox _hwaccelDecoderComboBox;
        private TextBox _customArgsTextBox;

        private ComboBox _frameRateTypeComboBox;
        private TextBox _frameRateCustomTextBox;
        private CheckBox _scaleCheckBox;
        private ComboBox _scaleMethodComboBox;
        private TextBox _scaleWidthTextBox;
        private TextBox _scaleHeightTextBox;
        private CheckBox _cropCheckBox;
        private TextBox _cropWidthTextBox;
        private TextBox _cropHeightTextBox;
        private TextBox _cropLeftTextBox;
        private TextBox _cropTopTextBox;
        private ComboBox _rotateComboBox;
        private CheckBox _vflipCheckBox;
        private CheckBox _hflipCheckBox;
        private CheckBox _speedCheckBox;
        private TextBox _speedFactorTextBox;
        private ComboBox _deinterlaceComboBox;
        private CheckBox _pixFmtCheckBox;
        private ComboBox _pixFmtComboBox;
        private CheckBox _subtitleCheckBox;
        private TextBox _subtitlePathTextBox;
        private Button _browseSubtitleButton;
        private CheckBox _trimCheckBox;
        private TextBox _trimStartTextBox;
        private TextBox _trimEndTextBox;

        private CheckBox _audioEnabledCheckBox;
        private ComboBox _audioCodecComboBox;
        private TextBox _audioBitrateTextBox;
        private TextBox _audioSamplerateTextBox;
        private CheckBox _onlyAudioCheckBox;
        private ComboBox _audioFormatComboBox;

        private RichTextBox _previewTextBox;

        public EditTaskForm(TaskInfo task, string ffmpegPath)
        {
            _task = task;
            _ffmpegPath = ffmpegPath;
            _originalSettings = DeepCopy(task.Settings);
            _inputFile = task.InputFile;
            InitializeComponent();
            LoadSettingsIntoUI();
            UpdateCommandPreview();
        }

        private void InitializeComponent()
        {
            this.Text = $"编辑任务 - {Path.GetFileName(_inputFile)}";
            this.Size = new Size(1100, 700);
            this.StartPosition = FormStartPosition.CenterParent;
            this.MinimumSize = new Size(900, 600);

            _tabControl = new TabControl { Dock = DockStyle.Fill };
            _tabControl.TabPages.Add(CreateIoPage());
            _tabControl.TabPages.Add(CreateVideoEncodingPage());
            _tabControl.TabPages.Add(CreateVideoFiltersPage());
            _tabControl.TabPages.Add(CreateAudioPage());

            var previewGroup = new GroupBox { Text = "新命令预览", Dock = DockStyle.Bottom, Height = 150 };
            _previewTextBox = new RichTextBox { Dock = DockStyle.Fill, ReadOnly = true, Font = new Font("Consolas", 9), BackColor = Color.LightYellow };
            previewGroup.Controls.Add(_previewTextBox);

            var mainPanel = new Panel { Dock = DockStyle.Fill };
            mainPanel.Controls.Add(_tabControl);
            mainPanel.Controls.Add(previewGroup);
            previewGroup.BringToFront();

            var buttonPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 40,
                FlowDirection = FlowDirection.RightToLeft,
                Padding = new Padding(5)
            };
            var saveBtn = new Button { Text = "保存修改", Width = 100 };
            saveBtn.Click += SaveChanges;
            var cancelBtn = new Button { Text = "取消", Width = 100 };
            cancelBtn.Click += (s, e) => this.DialogResult = DialogResult.Cancel;
            buttonPanel.Controls.Add(cancelBtn);
            buttonPanel.Controls.Add(saveBtn);
            mainPanel.Controls.Add(buttonPanel);

            this.Controls.Add(mainPanel);
        }

        private TabPage CreateIoPage()
        {
            var page = new TabPage("输入/输出");
            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 4, Padding = new Padding(10) };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            layout.Controls.Add(new Label { Text = "输出目录:", TextAlign = ContentAlignment.MiddleRight }, 0, 0);
            _outputDirTextBox = new TextBox { Dock = DockStyle.Fill };
            layout.Controls.Add(_outputDirTextBox, 1, 0);
            var browseDirBtn = new Button { Text = "浏览", Width = 70 };
            browseDirBtn.Click += (s, e) => { var dlg = new FolderBrowserDialog(); if (dlg.ShowDialog() == DialogResult.OK) _outputDirTextBox.Text = PathHelper.Normalize(dlg.SelectedPath); UpdateCommandPreview(); };
            layout.Controls.Add(browseDirBtn, 2, 0);

            layout.Controls.Add(new Label { Text = "文件名后缀:", TextAlign = ContentAlignment.MiddleRight }, 0, 1);
            _suffixTextBox = new TextBox { Dock = DockStyle.Fill };
            layout.Controls.Add(_suffixTextBox, 1, 1);
            layout.Controls.Add(new Panel(), 2, 1);

            layout.Controls.Add(new Label { Text = "自定义完整名称:", TextAlign = ContentAlignment.MiddleRight }, 0, 2);
            _customNameTextBox = new TextBox { Dock = DockStyle.Fill };
            layout.Controls.Add(_customNameTextBox, 1, 2);
            layout.Controls.Add(new Panel(), 2, 2);

            layout.Controls.Add(new Label { Text = "输出容器:", TextAlign = ContentAlignment.MiddleRight }, 0, 3);
            _containerComboBox = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 80 };
            _containerComboBox.Items.AddRange(new[] { "mp4", "mkv", "mov", "avi", "webm" });
            layout.Controls.Add(_containerComboBox, 1, 3);
            layout.Controls.Add(new Panel(), 2, 3);

            page.Controls.Add(layout);
            return page;
        }

        private TabPage CreateVideoEncodingPage()
        {
            var page = new TabPage("视频编码");
            var mainLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Padding = new Padding(5) };
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55));
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45));

            var leftGroup = new GroupBox { Text = "编码参数", Dock = DockStyle.Fill };
            var leftLayout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 5, ColumnCount = 2, Padding = new Padding(5) };
            leftLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            leftLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            leftLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            leftLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            leftLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            int row = 0;
            leftLayout.Controls.Add(new Label { Text = "编码器:", TextAlign = ContentAlignment.MiddleRight }, 0, row);
            _encoderComboBox = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 180 };
            _encoderComboBox.Items.AddRange(new[] { "libx264", "libx265", "libvpx-vp9", "libsvtav1", "mpeg4", "libxvid", "libtheora",
                "h264_nvenc", "hevc_nvenc", "av1_nvenc", "h264_qsv", "hevc_qsv", "av1_qsv",
                "h264_amf", "hevc_amf", "av1_amf", "h264_vaapi", "hevc_vaapi", "copy" });
            _encoderComboBox.SelectedIndexChanged += (s, e) => OnEncoderChanged();
            leftLayout.Controls.Add(_encoderComboBox, 1, row++);

            leftLayout.Controls.Add(new Label { Text = "编码预设:", TextAlign = ContentAlignment.MiddleRight }, 0, row);
            _presetComboBox = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 120 };
            _presetComboBox.Items.AddRange(new[] { "ultrafast", "superfast", "veryfast", "faster", "fast", "medium", "slow", "slower", "veryslow", "p1", "p2", "p3", "p4", "p5", "p6", "p7" });
            leftLayout.Controls.Add(_presetComboBox, 1, row++);

            leftLayout.Controls.Add(new Label { Text = "码率控制:", TextAlign = ContentAlignment.MiddleRight }, 0, row);
            var rcPanel = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight };
            _crfRadio = new RadioButton { Text = "CRF (CPU)", AutoSize = true };
            _cqRadio = new RadioButton { Text = "CQ (NVENC)", AutoSize = true };
            _globalQualityRadio = new RadioButton { Text = "Global Quality (QSV)", AutoSize = true };
            _bitrateRadio = new RadioButton { Text = "固定比特率", AutoSize = true };
            rcPanel.Controls.AddRange(new Control[] { _crfRadio, _cqRadio, _globalQualityRadio, _bitrateRadio });
            leftLayout.Controls.Add(rcPanel, 1, row++);

            var dynamicPanel = new Panel { Dock = DockStyle.Fill };
            leftLayout.Controls.Add(dynamicPanel, 1, row);
            leftLayout.SetRowSpan(dynamicPanel, 2);

            _crfTrackBar = new TrackBar { Minimum = 0, Maximum = 51, Value = 25, TickFrequency = 5, Width = 200 };
            _crfLabel = new Label { Text = "25", Width = 30 };
            _cqTrackBar = new TrackBar { Minimum = 0, Maximum = 51, Value = 35, TickFrequency = 5, Width = 200 };
            _cqLabel = new Label { Text = "35", Width = 30 };
            _gqTrackBar = new TrackBar { Minimum = 1, Maximum = 51, Value = 25, TickFrequency = 5, Width = 200 };
            _gqLabel = new Label { Text = "25", Width = 30 };
            _bitrateTextBox = new TextBox { Text = "1900k", Width = 100 };

            var crfPanel = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, Controls = { new Label { Text = "CRF (0~51):" }, _crfTrackBar, _crfLabel }, Visible = true };
            var cqPanel = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, Controls = { new Label { Text = "CQ (0~51):" }, _cqTrackBar, _cqLabel }, Visible = false };
            var gqPanel = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, Controls = { new Label { Text = "Global Quality (1~51):" }, _gqTrackBar, _gqLabel }, Visible = false };
            var bitPanel = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, Controls = { new Label { Text = "比特率 (kbps):" }, _bitrateTextBox }, Visible = false };
            dynamicPanel.Controls.AddRange(new Control[] { crfPanel, cqPanel, gqPanel, bitPanel });

            _crfRadio.CheckedChanged += (s, e) => { crfPanel.Visible = _crfRadio.Checked; UpdateCommandPreview(); };
            _cqRadio.CheckedChanged += (s, e) => { cqPanel.Visible = _cqRadio.Checked; UpdateCommandPreview(); };
            _globalQualityRadio.CheckedChanged += (s, e) => { gqPanel.Visible = _globalQualityRadio.Checked; UpdateCommandPreview(); };
            _bitrateRadio.CheckedChanged += (s, e) => { bitPanel.Visible = _bitrateRadio.Checked; UpdateCommandPreview(); };
            _crfTrackBar.ValueChanged += (s, e) => { _crfLabel.Text = _crfTrackBar.Value.ToString(); UpdateCommandPreview(); };
            _cqTrackBar.ValueChanged += (s, e) => { _cqLabel.Text = _cqTrackBar.Value.ToString(); UpdateCommandPreview(); };
            _gqTrackBar.ValueChanged += (s, e) => { _gqLabel.Text = _gqTrackBar.Value.ToString(); UpdateCommandPreview(); };
            _bitrateTextBox.TextChanged += (s, e) => UpdateCommandPreview();

            leftGroup.Controls.Add(leftLayout);
            mainLayout.Controls.Add(leftGroup, 0, 0);

            var rightGroup = new GroupBox { Text = "高级选项 (硬件解码/自定义参数)", Dock = DockStyle.Fill };
            var rightLayout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3, ColumnCount = 1, Padding = new Padding(5) };
            rightLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            rightLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            rightLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            var hwPanel = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight };
            _hwaccelCheckBox = new CheckBox { Text = "启用硬件解码", AutoSize = true };
            _hwaccelDecoderComboBox = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 200, Enabled = false };
            _hwaccelDecoderComboBox.Items.AddRange(HardwareDecoderHelper.GetDecoderOptions());
            _hwaccelDecoderComboBox.SelectedIndex = 0;
            _hwaccelCheckBox.CheckedChanged += (s, e) => { _hwaccelDecoderComboBox.Enabled = _hwaccelCheckBox.Checked; if (_hwaccelCheckBox.Checked && _hwaccelDecoderComboBox.SelectedIndex < 0) _hwaccelDecoderComboBox.SelectedIndex = 1; UpdateCommandPreview(); };
            _hwaccelDecoderComboBox.SelectedIndexChanged += (s, e) => UpdateCommandPreview();
            hwPanel.Controls.AddRange(new Control[] { _hwaccelCheckBox, _hwaccelDecoderComboBox });
            rightLayout.Controls.Add(hwPanel, 0, 0);

            rightLayout.Controls.Add(new Label { Text = "自定义FFmpeg参数 (例如: -tune grain -profile:v high):", AutoSize = true }, 0, 1);
            _customArgsTextBox = new TextBox { Dock = DockStyle.Fill, Multiline = true, Height = 60 };
            _customArgsTextBox.TextChanged += (s, e) => UpdateCommandPreview();
            rightLayout.Controls.Add(_customArgsTextBox, 0, 2);

            rightGroup.Controls.Add(rightLayout);
            mainLayout.Controls.Add(rightGroup, 1, 0);

            page.Controls.Add(mainLayout);
            return page;
        }

        private TabPage CreateVideoFiltersPage()
        {
            var page = new TabPage("视频滤镜");
            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 12, Padding = new Padding(5), AutoSize = true };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            int row = 0;
            // 帧率
            layout.Controls.Add(new Label { Text = "帧率:", TextAlign = ContentAlignment.MiddleRight }, 0, row);
            var fpsPanel = new FlowLayoutPanel();
            _frameRateTypeComboBox = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 80 };
            _frameRateTypeComboBox.Items.AddRange(new[] { "保持源", "指定" });
            _frameRateCustomTextBox = new TextBox { Text = "30", Width = 50, Enabled = false };
            _frameRateTypeComboBox.SelectedIndexChanged += (s, e) => { _frameRateCustomTextBox.Enabled = _frameRateTypeComboBox.SelectedIndex == 1; UpdateCommandPreview(); };
            _frameRateCustomTextBox.TextChanged += (s, e) => UpdateCommandPreview();
            fpsPanel.Controls.AddRange(new Control[] { _frameRateTypeComboBox, _frameRateCustomTextBox, new Label { Text = "fps" } });
            layout.Controls.Add(fpsPanel, 1, row++);

            // 缩放
            _scaleCheckBox = new CheckBox { Text = "启用缩放", AutoSize = true };
            _scaleCheckBox.CheckedChanged += (s, e) => UpdateCommandPreview();
            layout.Controls.Add(_scaleCheckBox, 0, row);
            var scalePanel = new FlowLayoutPanel();
            _scaleMethodComboBox = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 100 };
            _scaleMethodComboBox.Items.AddRange(new[] { "宽度(高度自动)", "高度(宽度自动)", "精确宽×高" });
            _scaleWidthTextBox = new TextBox { Width = 50 };
            _scaleHeightTextBox = new TextBox { Width = 50, Enabled = false };
            _scaleMethodComboBox.SelectedIndexChanged += (s, e) => { _scaleHeightTextBox.Enabled = _scaleMethodComboBox.SelectedIndex == 2; UpdateCommandPreview(); };
            _scaleWidthTextBox.TextChanged += (s, e) => UpdateCommandPreview();
            _scaleHeightTextBox.TextChanged += (s, e) => UpdateCommandPreview();
            scalePanel.Controls.AddRange(new Control[] { _scaleMethodComboBox, new Label { Text = "宽:" }, _scaleWidthTextBox, new Label { Text = "高:" }, _scaleHeightTextBox });
            layout.Controls.Add(scalePanel, 1, row++);

            // 裁剪
            _cropCheckBox = new CheckBox { Text = "启用裁剪", AutoSize = true };
            _cropCheckBox.CheckedChanged += (s, e) => UpdateCommandPreview();
            layout.Controls.Add(_cropCheckBox, 0, row);
            var cropPanel = new FlowLayoutPanel();
            _cropWidthTextBox = new TextBox { Width = 60 };
            _cropHeightTextBox = new TextBox { Width = 60 };
            _cropLeftTextBox = new TextBox { Width = 50 };
            _cropTopTextBox = new TextBox { Width = 50 };
            _cropWidthTextBox.TextChanged += (s, e) => UpdateCommandPreview();
            _cropHeightTextBox.TextChanged += (s, e) => UpdateCommandPreview();
            _cropLeftTextBox.TextChanged += (s, e) => UpdateCommandPreview();
            _cropTopTextBox.TextChanged += (s, e) => UpdateCommandPreview();
            cropPanel.Controls.AddRange(new Control[] { new Label { Text = "宽:" }, _cropWidthTextBox, new Label { Text = "高:" }, _cropHeightTextBox,
                new Label { Text = "左:" }, _cropLeftTextBox, new Label { Text = "上:" }, _cropTopTextBox });
            layout.Controls.Add(cropPanel, 1, row++);

            // 旋转
            layout.Controls.Add(new Label { Text = "旋转:", TextAlign = ContentAlignment.MiddleRight }, 0, row);
            _rotateComboBox = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 100 };
            _rotateComboBox.Items.AddRange(new[] { "无", "90°顺时针", "180°", "90°逆时针" });
            _rotateComboBox.SelectedIndexChanged += (s, e) => UpdateCommandPreview();
            layout.Controls.Add(_rotateComboBox, 1, row++);

            // 翻转
            var flipPanel = new FlowLayoutPanel();
            _vflipCheckBox = new CheckBox { Text = "上下翻转", AutoSize = true };
            _hflipCheckBox = new CheckBox { Text = "左右翻转", AutoSize = true };
            _vflipCheckBox.CheckedChanged += (s, e) => UpdateCommandPreview();
            _hflipCheckBox.CheckedChanged += (s, e) => UpdateCommandPreview();
            flipPanel.Controls.AddRange(new Control[] { _vflipCheckBox, _hflipCheckBox });
            layout.Controls.Add(new Label { Text = "翻转:", TextAlign = ContentAlignment.MiddleRight }, 0, row);
            layout.Controls.Add(flipPanel, 1, row++);

            // 变速
            _speedCheckBox = new CheckBox { Text = "启用变速", AutoSize = true };
            _speedCheckBox.CheckedChanged += (s, e) => UpdateCommandPreview();
            layout.Controls.Add(_speedCheckBox, 0, row);
            var speedPanel = new FlowLayoutPanel();
            _speedFactorTextBox = new TextBox { Text = "1.0", Width = 60 };
            _speedFactorTextBox.TextChanged += (s, e) => UpdateCommandPreview();
            speedPanel.Controls.AddRange(new Control[] { new Label { Text = "速度倍数 (0.5慢,2.0快):" }, _speedFactorTextBox });
            layout.Controls.Add(speedPanel, 1, row++);

            // 反交错
            layout.Controls.Add(new Label { Text = "反交错:", TextAlign = ContentAlignment.MiddleRight }, 0, row);
            _deinterlaceComboBox = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 120 };
            _deinterlaceComboBox.Items.AddRange(new[] { "none", "bwdif", "yadif", "kerndeint", "pp=lb", "fieldorder" });
            _deinterlaceComboBox.SelectedIndexChanged += (s, e) => UpdateCommandPreview();
            layout.Controls.Add(_deinterlaceComboBox, 1, row++);

            // 像素格式
            _pixFmtCheckBox = new CheckBox { Text = "指定像素格式", AutoSize = true };
            _pixFmtCheckBox.CheckedChanged += (s, e) => { _pixFmtComboBox.Enabled = _pixFmtCheckBox.Checked; UpdateCommandPreview(); };
            layout.Controls.Add(_pixFmtCheckBox, 0, row);
            _pixFmtComboBox = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 120, Enabled = false };
            _pixFmtComboBox.Items.AddRange(new[] { "yuv420p", "yuv422p", "yuv444p", "yuv420p10le", "yuv422p10le", "yuv444p10le", "p010le", "nv12" });
            _pixFmtComboBox.SelectedIndexChanged += (s, e) => UpdateCommandPreview();
            layout.Controls.Add(_pixFmtComboBox, 1, row++);

            // 烧录字幕
            _subtitleCheckBox = new CheckBox { Text = "烧录字幕", AutoSize = true };
            _subtitleCheckBox.CheckedChanged += (s, e) => { _subtitlePathTextBox.Enabled = _subtitleCheckBox.Checked; _browseSubtitleButton.Enabled = _subtitleCheckBox.Checked; UpdateCommandPreview(); };
            layout.Controls.Add(_subtitleCheckBox, 0, row);
            var subPanel = new FlowLayoutPanel();
            _subtitlePathTextBox = new TextBox { Width = 250, Enabled = false };
            _subtitlePathTextBox.TextChanged += (s, e) => UpdateCommandPreview();
            _browseSubtitleButton = new Button { Text = "浏览...", Enabled = false };
            _browseSubtitleButton.Click += (s, e) => { var dlg = new OpenFileDialog { Filter = "字幕文件|*.srt;*.ass;*.ssa;*.vtt"; if (dlg.ShowDialog() == DialogResult.OK) { _subtitlePathTextBox.Text = PathHelper.Normalize(dlg.FileName); UpdateCommandPreview(); } } };
            subPanel.Controls.AddRange(new Control[] { _subtitlePathTextBox, _browseSubtitleButton });
            layout.Controls.Add(subPanel, 1, row++);

            // 截取片段
            _trimCheckBox = new CheckBox { Text = "启用截取片段", AutoSize = true };
            _trimCheckBox.CheckedChanged += (s, e) => { _trimStartTextBox.Enabled = _trimCheckBox.Checked; _trimEndTextBox.Enabled = _trimCheckBox.Checked; UpdateCommandPreview(); };
            layout.Controls.Add(_trimCheckBox, 0, row);
            var trimPanel = new FlowLayoutPanel();
            _trimStartTextBox = new TextBox { Width = 100, Enabled = false };
            _trimEndTextBox = new TextBox { Width = 100, Enabled = false };
            _trimStartTextBox.TextChanged += (s, e) => UpdateCommandPreview();
            _trimEndTextBox.TextChanged += (s, e) => UpdateCommandPreview();
            trimPanel.Controls.AddRange(new Control[] { new Label { Text = "开始:" }, _trimStartTextBox, new Label { Text = "结束:" }, _trimEndTextBox });
            layout.Controls.Add(trimPanel, 1, row++);

            page.Controls.Add(layout);
            return page;
        }

        private TabPage CreateAudioPage()
        {
            var page = new TabPage("音频");
            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 6, Padding = new Padding(10) };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            int row = 0;
            _audioEnabledCheckBox = new CheckBox { Text = "保留音频", AutoSize = true, Checked = true };
            _audioEnabledCheckBox.CheckedChanged += (s, e) => UpdateCommandPreview();
            layout.Controls.Add(_audioEnabledCheckBox, 0, row);
            layout.Controls.Add(new Panel(), 1, row++);

            _onlyAudioCheckBox = new CheckBox { Text = "仅提取音频", AutoSize = true };
            _onlyAudioCheckBox.CheckedChanged += (s, e) => UpdateCommandPreview();
            layout.Controls.Add(_onlyAudioCheckBox, 0, row);
            var formatPanel = new FlowLayoutPanel();
            formatPanel.Controls.Add(new Label { Text = "输出容器:" });
            _audioFormatComboBox = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 60 };
            _audioFormatComboBox.Items.AddRange(new[] { "mp3", "aac", "m4a", "flac", "opus", "wav", "ac3" });
            _audioFormatComboBox.SelectedIndexChanged += (s, e) => UpdateCommandPreview();
            formatPanel.Controls.Add(_audioFormatComboBox);
            layout.Controls.Add(formatPanel, 1, row++);

            layout.Controls.Add(new Label { Text = "编码器:", TextAlign = ContentAlignment.MiddleRight }, 0, row);
            _audioCodecComboBox = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 100 };
            _audioCodecComboBox.Items.AddRange(new[] { "aac", "libmp3lame", "opus", "ac3", "flac", "alac", "pcm_s16le", "copy" });
            _audioCodecComboBox.SelectedIndexChanged += (s, e) => UpdateCommandPreview();
            layout.Controls.Add(_audioCodecComboBox, 1, row++);

            layout.Controls.Add(new Label { Text = "比特率:", TextAlign = ContentAlignment.MiddleRight }, 0, row);
            _audioBitrateTextBox = new TextBox { Text = "128k", Width = 80 };
            _audioBitrateTextBox.TextChanged += (s, e) => UpdateCommandPreview();
            layout.Controls.Add(_audioBitrateTextBox, 1, row++);

            layout.Controls.Add(new Label { Text = "采样率:", TextAlign = ContentAlignment.MiddleRight }, 0, row);
            _audioSamplerateTextBox = new TextBox { Text = "44100", Width = 80 };
            _audioSamplerateTextBox.TextChanged += (s, e) => UpdateCommandPreview();
            layout.Controls.Add(_audioSamplerateTextBox, 1, row++);

            page.Controls.Add(layout);
            return page;
        }

        private void LoadSettingsIntoUI()
        {
            var s = _task.Settings;
            // IO
            _outputDirTextBox.Text = s.OutputDir;
            _suffixTextBox.Text = s.OutputSuffix;
            _customNameTextBox.Text = s.CustomOutputName;
            _containerComboBox.SelectedItem = s.OutputContainer;

            // 视频编码
            _encoderComboBox.SelectedItem = s.Encoder;
            _presetComboBox.SelectedItem = s.Preset;
            switch (s.RateControlType)
            {
                case "crf": _crfRadio.Checked = true; _crfTrackBar.Value = s.CrfValue; break;
                case "cq": _cqRadio.Checked = true; _cqTrackBar.Value = s.CqValue; break;
                case "global_quality": _globalQualityRadio.Checked = true; _gqTrackBar.Value = s.GlobalQuality; break;
                case "bitrate": _bitrateRadio.Checked = true; _bitrateTextBox.Text = s.BitrateVideo; break;
            }
            _hwaccelCheckBox.Checked = s.HwaccelEnabled;
            _hwaccelDecoderComboBox.SelectedItem = s.HwaccelDecoder ?? "无";
            _customArgsTextBox.Text = s.CustomArgs;

            // 滤镜
            _frameRateTypeComboBox.SelectedIndex = s.FrameRateType == "keep" ? 0 : 1;
            _frameRateCustomTextBox.Text = s.FrameRateCustom;
            _scaleCheckBox.Checked = s.ScaleEnabled;
            _scaleWidthTextBox.Text = s.ScaleWidth;
            _scaleHeightTextBox.Text = s.ScaleHeight;
            _scaleMethodComboBox.SelectedIndex = s.ScaleMethod == "width" ? 0 : (s.ScaleMethod == "height" ? 1 : 2);
            _cropCheckBox.Checked = s.CropEnabled;
            _cropWidthTextBox.Text = s.CropWidth;
            _cropHeightTextBox.Text = s.CropHeight;
            _cropLeftTextBox.Text = s.CropLeft;
            _cropTopTextBox.Text = s.CropTop;
            _rotateComboBox.SelectedIndex = s.Rotate == "none" ? 0 : (s.Rotate == "90" ? 1 : (s.Rotate == "180" ? 2 : 3));
            _vflipCheckBox.Checked = s.Vflip;
            _hflipCheckBox.Checked = s.Hflip;
            _speedCheckBox.Checked = s.SpeedEnabled;
            _speedFactorTextBox.Text = s.SpeedFactor.ToString();
            _deinterlaceComboBox.SelectedItem = s.DeinterlaceFilter;
            _pixFmtCheckBox.Checked = s.PixFmtEnabled;
            _pixFmtComboBox.SelectedItem = s.PixFmt;
            _subtitleCheckBox.Checked = s.SubtitleEnabled;
            _subtitlePathTextBox.Text = s.SubtitlePath;
            _trimCheckBox.Checked = s.TrimEnabled;
            _trimStartTextBox.Text = s.TrimStart;
            _trimEndTextBox.Text = s.TrimEnd;

            // 音频
            _audioEnabledCheckBox.Checked = s.AudioEnabled;
            _onlyAudioCheckBox.Checked = s.OnlyAudio;
            _audioFormatComboBox.SelectedItem = s.AudioFormat;
            _audioCodecComboBox.SelectedItem = s.AudioCodec;
            _audioBitrateTextBox.Text = s.AudioBitrate;
            _audioSamplerateTextBox.Text = s.AudioSamplerate;
        }

        private void OnEncoderChanged()
        {
            string enc = _encoderComboBox.SelectedItem?.ToString();
            if (enc == null) return;
            if (enc.Contains("libx") || enc.Contains("libsvtav1"))
                _crfRadio.Checked = true;
            else if (enc.Contains("nvenc"))
                _cqRadio.Checked = true;
            else if (enc.Contains("qsv"))
                _globalQualityRadio.Checked = true;
            else
                _bitrateRadio.Checked = true;
        }

        private void UpdateCommandPreview()
        {
            var newSettings = CollectSettings();
            string outputPath = GenerateOutputPath(newSettings);
            var builder = new FFmpegCommandBuilder(_ffmpegPath);
            try
            {
                string cmd = builder.BuildCommand(_inputFile, outputPath, newSettings);
                _previewTextBox.Text = cmd;
            }
            catch (Exception ex)
            {
                _previewTextBox.Text = $"生成命令时出错: {ex.Message}";
            }
        }

        private VideoSettings CollectSettings()
        {
            var s = new VideoSettings();
            // IO
            s.OutputDir = _outputDirTextBox.Text;
            s.OutputSuffix = _suffixTextBox.Text;
            s.CustomOutputName = _customNameTextBox.Text;
            s.OutputContainer = _containerComboBox.SelectedItem?.ToString() ?? "mp4";
            // 视频编码
            s.Encoder = _encoderComboBox.SelectedItem?.ToString();
            s.Preset = _presetComboBox.SelectedItem?.ToString();
            if (_crfRadio.Checked) { s.RateControlType = "crf"; s.CrfValue = _crfTrackBar.Value; }
            else if (_cqRadio.Checked) { s.RateControlType = "cq"; s.CqValue = _cqTrackBar.Value; }
            else if (_globalQualityRadio.Checked) { s.RateControlType = "global_quality"; s.GlobalQuality = _gqTrackBar.Value; }
            else { s.RateControlType = "bitrate"; s.BitrateVideo = _bitrateTextBox.Text; }
            s.HwaccelEnabled = _hwaccelCheckBox.Checked;
            s.HwaccelDecoder = _hwaccelDecoderComboBox.SelectedItem?.ToString();
            s.CustomArgs = _customArgsTextBox.Text;
            // 滤镜
            s.FrameRateType = _frameRateTypeComboBox.SelectedIndex == 0 ? "keep" : "custom";
            s.FrameRateCustom = _frameRateCustomTextBox.Text;
            s.ScaleEnabled = _scaleCheckBox.Checked;
            s.ScaleWidth = _scaleWidthTextBox.Text;
            s.ScaleHeight = _scaleHeightTextBox.Text;
            s.ScaleMethod = _scaleMethodComboBox.SelectedIndex == 0 ? "width" : (_scaleMethodComboBox.SelectedIndex == 1 ? "height" : "exact");
            s.CropEnabled = _cropCheckBox.Checked;
            s.CropWidth = _cropWidthTextBox.Text;
            s.CropHeight = _cropHeightTextBox.Text;
            s.CropLeft = _cropLeftTextBox.Text;
            s.CropTop = _cropTopTextBox.Text;
            s.Rotate = _rotateComboBox.SelectedIndex == 0 ? "none" : (_rotateComboBox.SelectedIndex == 1 ? "90" : (_rotateComboBox.SelectedIndex == 2 ? "180" : "270"));
            s.Vflip = _vflipCheckBox.Checked;
            s.Hflip = _hflipCheckBox.Checked;
            s.SpeedEnabled = _speedCheckBox.Checked;
            if (double.TryParse(_speedFactorTextBox.Text, out double factor)) s.SpeedFactor = factor;
            s.DeinterlaceFilter = _deinterlaceComboBox.SelectedItem?.ToString();
            s.PixFmtEnabled = _pixFmtCheckBox.Checked;
            s.PixFmt = _pixFmtComboBox.SelectedItem?.ToString();
            s.SubtitleEnabled = _subtitleCheckBox.Checked;
            s.SubtitlePath = _subtitlePathTextBox.Text;
            s.TrimEnabled = _trimCheckBox.Checked;
            s.TrimStart = _trimStartTextBox.Text;
            s.TrimEnd = _trimEndTextBox.Text;
            // 音频
            s.AudioEnabled = _audioEnabledCheckBox.Checked;
            s.OnlyAudio = _onlyAudioCheckBox.Checked;
            s.AudioFormat = _audioFormatComboBox.SelectedItem?.ToString();
            s.AudioCodec = _audioCodecComboBox.SelectedItem?.ToString();
            s.AudioBitrate = _audioBitrateTextBox.Text;
            s.AudioSamplerate = _audioSamplerateTextBox.Text;
            return s;
        }

        private string GenerateOutputPath(VideoSettings settings)
        {
            string dir = string.IsNullOrEmpty(settings.OutputDir) ? Path.GetDirectoryName(_inputFile) : settings.OutputDir;
            string baseName = Path.GetFileNameWithoutExtension(_inputFile);
            string container = settings.OnlyAudio ? settings.AudioFormat : settings.OutputContainer;
            string custom = settings.CustomOutputName?.Trim();
            if (!string.IsNullOrEmpty(custom))
            {
                if (!Path.HasExtension(custom)) custom += "." + container;
                return PathHelper.Normalize(Path.Combine(dir, custom));
            }
            string suffix = settings.OutputSuffix?.Trim();
            if (string.IsNullOrEmpty(suffix) && string.Equals(dir, Path.GetDirectoryName(_inputFile), StringComparison.OrdinalIgnoreCase))
                suffix = "_new";
            string outputName = $"{baseName}{suffix}.{container}";
            return PathHelper.Normalize(Path.Combine(dir, outputName));
        }

        private void SaveChanges(object sender, EventArgs e)
        {
            var newSettings = CollectSettings();
            var errors = ParamValidator.ValidateSettings(newSettings);
            if (errors.Any())
            {
                MessageBox.Show($"参数错误:\n{string.Join("\n", errors)}", "验证失败", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            string newOutput = GenerateOutputPath(newSettings);
            try
            {
                var builder = new FFmpegCommandBuilder(_ffmpegPath);
                string cmd = builder.BuildCommand(_inputFile, newOutput, newSettings);
                _task.Settings = newSettings;
                _task.OutputFile = newOutput;
                _task.Command = cmd;
                _task.Status = "等待";
                this.DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"生成命令失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private VideoSettings DeepCopy(VideoSettings original)
        {
            var json = Newtonsoft.Json.JsonConvert.SerializeObject(original);
            return Newtonsoft.Json.JsonConvert.DeserializeObject<VideoSettings>(json);
        }
    }
}
