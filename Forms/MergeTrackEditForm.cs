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
    public partial class MergeTrackEditForm : Form
    {
        private readonly TrackInfo _track;
        private readonly bool _isPipEnabled;
        private readonly List<TrackInfo> _allTracks;
        private TabControl _tabControl;
        
        // 视频专用控件
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
        private ComboBox _deinterlaceComboBox;
        private CheckBox _pixFmtCheckBox;
        private ComboBox _pixFmtComboBox;
        private CheckBox _trimCheckBox;
        private TextBox _trimStartTextBox;
        private TextBox _trimEndTextBox;
        private ComboBox _frameRateTypeComboBox;
        private TextBox _frameRateCustomTextBox;
        
        // 画中画专用控件 (仅非主视频且画中画模式)
        private CheckBox _overlayEnabledCheckBox;
        private TextBox _overlayXTextBox;
        private TextBox _overlayYTextBox;
        private CheckBox _padEnabledCheckBox;
        private TextBox _padWidthTextBox;
        private TextBox _padHeightTextBox;
        private TextBox _offsetXTextBox;
        private TextBox _offsetYTextBox;
        
        // 音频专用控件
        private ComboBox _audioEncoderComboBox;
        private TextBox _audioBitrateTextBox;
        private TextBox _audioSamplerateTextBox;
        
        // 字幕专用控件
        private ComboBox _subtitleEncoderComboBox;
        
        public MergeTrackEditForm(TrackInfo track, bool isPipEnabled, List<TrackInfo> allTracks)
        {
            _track = track;
            _isPipEnabled = isPipEnabled;
            _allTracks = allTracks;
            InitializeComponents();
            LoadSettingsIntoUI();
        }
        
        private void InitializeComponents()
        {
            this.Text = $"编辑轨道 - {_track.Type} ({Path.GetFileName(_track.FilePath)})";
            this.Size = new Size(1000, 450);
            this.StartPosition = FormStartPosition.CenterParent;
            this.MinimumSize = new Size(800, 400);
            
            _tabControl = new TabControl { Dock = DockStyle.Fill };
            
            if (_track.Type == "video")
            {
                _tabControl.TabPages.Add(CreateVideoEncodingPage());
                _tabControl.TabPages.Add(CreateVideoFiltersPage());
                if (_isPipEnabled)
                {
                    _tabControl.TabPages.Add(CreateOverlayPage());
                }
            }
            else if (_track.Type == "audio")
            {
                _tabControl.TabPages.Add(CreateAudioPage());
            }
            else if (_track.Type == "subtitle")
            {
                _tabControl.TabPages.Add(CreateSubtitlePage());
            }
            
            var buttonPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 40,
                FlowDirection = FlowDirection.RightToLeft,
                Padding = new Padding(5)
            };
            var saveBtn = new Button { Text = "保存", Width = 100 };
            saveBtn.Click += SaveChanges;
            var cancelBtn = new Button { Text = "取消", Width = 100 };
            cancelBtn.Click += (s, e) => this.DialogResult = DialogResult.Cancel;
            buttonPanel.Controls.Add(cancelBtn);
            buttonPanel.Controls.Add(saveBtn);
            
            var mainPanel = new Panel { Dock = DockStyle.Fill };
            mainPanel.Controls.Add(_tabControl);
            mainPanel.Controls.Add(buttonPanel);
            this.Controls.Add(mainPanel);
        }
        
        private TabPage CreateVideoEncodingPage()
        {
            var page = new TabPage("编码器与质量");
            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 5, Padding = new Padding(10) };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            
            int row = 0;
            layout.Controls.Add(new Label { Text = "编码器:", TextAlign = ContentAlignment.MiddleRight }, 0, row);
            _encoderComboBox = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 180 };
            _encoderComboBox.Items.AddRange(new[] { "copy", "libx264", "libx265", "libvpx-vp9", "libsvtav1",
                "h264_nvenc", "hevc_nvenc", "av1_nvenc", "h264_qsv", "hevc_qsv", "av1_qsv" });
            layout.Controls.Add(_encoderComboBox, 1, row++);
            
            layout.Controls.Add(new Label { Text = "编码预设:", TextAlign = ContentAlignment.MiddleRight }, 0, row);
            _presetComboBox = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 120 };
            _presetComboBox.Items.AddRange(new[] { "ultrafast", "superfast", "veryfast", "faster", "fast", "medium", "slow", "slower", "veryslow", "p1", "p2", "p3", "p4", "p5", "p6", "p7" });
            layout.Controls.Add(_presetComboBox, 1, row++);
            
            layout.Controls.Add(new Label { Text = "码率控制:", TextAlign = ContentAlignment.MiddleRight }, 0, row);
            var rcPanel = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight };
            _crfRadio = new RadioButton { Text = "CRF", AutoSize = true };
            _cqRadio = new RadioButton { Text = "CQ (NVENC)", AutoSize = true };
            _globalQualityRadio = new RadioButton { Text = "Global Quality (QSV)", AutoSize = true };
            _bitrateRadio = new RadioButton { Text = "固定比特率", AutoSize = true };
            rcPanel.Controls.AddRange(new Control[] { _crfRadio, _cqRadio, _globalQualityRadio, _bitrateRadio });
            layout.Controls.Add(rcPanel, 1, row++);
            
            var dynamicPanel = new Panel { Dock = DockStyle.Fill };
            layout.Controls.Add(dynamicPanel, 1, row);
            layout.SetRowSpan(dynamicPanel, 2);
            CreateDynamicControls(dynamicPanel);
            
            page.Controls.Add(layout);
            return page;
        }
        
        private void CreateDynamicControls(Panel container)
        {
            var crfPanel = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, Visible = true };
            crfPanel.Controls.Add(new Label { Text = "CRF (0~51):" });
            _crfTrackBar = new TrackBar { Minimum = 0, Maximum = 51, Value = 25, TickFrequency = 5, Width = 300 };
            _crfLabel = new Label { Text = "25", Width = 30 };
            crfPanel.Controls.AddRange(new Control[] { _crfTrackBar, _crfLabel });
            _crfTrackBar.ValueChanged += (s, e) => _crfLabel.Text = _crfTrackBar.Value.ToString();
            
            var cqPanel = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, Visible = false };
            cqPanel.Controls.Add(new Label { Text = "CQ (0~51):" });
            _cqTrackBar = new TrackBar { Minimum = 0, Maximum = 51, Value = 35, TickFrequency = 5, Width = 300 };
            _cqLabel = new Label { Text = "35", Width = 30 };
            cqPanel.Controls.AddRange(new Control[] { _cqTrackBar, _cqLabel });
            _cqTrackBar.ValueChanged += (s, e) => _cqLabel.Text = _cqTrackBar.Value.ToString();
            
            var gqPanel = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, Visible = false };
            gqPanel.Controls.Add(new Label { Text = "Global Quality (1~51):" });
            _gqTrackBar = new TrackBar { Minimum = 1, Maximum = 51, Value = 25, TickFrequency = 5, Width = 300 };
            _gqLabel = new Label { Text = "25", Width = 30 };
            gqPanel.Controls.AddRange(new Control[] { _gqTrackBar, _gqLabel });
            _gqTrackBar.ValueChanged += (s, e) => _gqLabel.Text = _gqTrackBar.Value.ToString();
            
            var bitratePanel = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, Visible = false };
            bitratePanel.Controls.Add(new Label { Text = "比特率 (kbps):" });
            _bitrateTextBox = new TextBox { Text = "1900k", Width = 100 };
            bitratePanel.Controls.Add(_bitrateTextBox);
            
            container.Controls.AddRange(new Control[] { crfPanel, cqPanel, gqPanel, bitratePanel });
            
            _crfRadio.CheckedChanged += (s, e) => { if (_crfRadio.Checked) SwitchDynamicPanel(crfPanel); };
            _cqRadio.CheckedChanged += (s, e) => { if (_cqRadio.Checked) SwitchDynamicPanel(cqPanel); };
            _globalQualityRadio.CheckedChanged += (s, e) => { if (_globalQualityRadio.Checked) SwitchDynamicPanel(gqPanel); };
            _bitrateRadio.CheckedChanged += (s, e) => { if (_bitrateRadio.Checked) SwitchDynamicPanel(bitratePanel); };
        }
        
        private void SwitchDynamicPanel(FlowLayoutPanel activePanel)
        {
            foreach (Control ctrl in activePanel.Parent.Controls)
                if (ctrl is FlowLayoutPanel panel)
                    panel.Visible = (panel == activePanel);
        }
        
        private TabPage CreateVideoFiltersPage()
        {
            var page = new TabPage("视频滤镜");
            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 9, Padding = new Padding(10), AutoSize = true };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            
            int row = 0;
            layout.Controls.Add(new Label { Text = "帧率:", TextAlign = ContentAlignment.MiddleRight }, 0, row);
            var fpsPanel = new FlowLayoutPanel();
            _frameRateTypeComboBox = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 80 };
            _frameRateTypeComboBox.Items.AddRange(new[] { "保持源", "指定" });
            _frameRateCustomTextBox = new TextBox { Text = "30", Width = 50, Enabled = false };
            _frameRateTypeComboBox.SelectedIndexChanged += (s, e) => _frameRateCustomTextBox.Enabled = (_frameRateTypeComboBox.SelectedIndex == 1);
            fpsPanel.Controls.AddRange(new Control[] { _frameRateTypeComboBox, _frameRateCustomTextBox, new Label { Text = "fps" } });
            layout.Controls.Add(fpsPanel, 1, row++);
            
            _scaleCheckBox = new CheckBox { Text = "启用缩放", AutoSize = true };
            layout.Controls.Add(_scaleCheckBox, 0, row);
            var scalePanel = new FlowLayoutPanel();
            _scaleMethodComboBox = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 100 };
            _scaleMethodComboBox.Items.AddRange(new[] { "宽度(高度自动)", "高度(宽度自动)", "精确宽×高" });
            _scaleWidthTextBox = new TextBox { Width = 50 };
            _scaleHeightTextBox = new TextBox { Width = 50, Enabled = false };
            _scaleMethodComboBox.SelectedIndexChanged += (s, e) =>
            {
                bool exact = _scaleMethodComboBox.SelectedIndex == 2;
                _scaleHeightTextBox.Enabled = exact;
                if (!exact) _scaleHeightTextBox.Text = "";
            };
            scalePanel.Controls.AddRange(new Control[] { _scaleMethodComboBox, new Label { Text = "宽:" }, _scaleWidthTextBox, new Label { Text = "高:" }, _scaleHeightTextBox });
            layout.Controls.Add(scalePanel, 1, row++);
            
            _cropCheckBox = new CheckBox { Text = "启用裁剪", AutoSize = true };
            layout.Controls.Add(_cropCheckBox, 0, row);
            var cropPanel = new FlowLayoutPanel();
            _cropWidthTextBox = new TextBox { Width = 60 };
            _cropHeightTextBox = new TextBox { Width = 60 };
            _cropLeftTextBox = new TextBox { Width = 50 };
            _cropTopTextBox = new TextBox { Width = 50 };
            cropPanel.Controls.AddRange(new Control[] { new Label { Text = "宽:" }, _cropWidthTextBox, new Label { Text = "高:" }, _cropHeightTextBox,
                new Label { Text = "左:" }, _cropLeftTextBox, new Label { Text = "上:" }, _cropTopTextBox });
            layout.Controls.Add(cropPanel, 1, row++);
            
            layout.Controls.Add(new Label { Text = "旋转:", TextAlign = ContentAlignment.MiddleRight }, 0, row);
            _rotateComboBox = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 100 };
            _rotateComboBox.Items.AddRange(new[] { "无", "90°顺时针", "180°", "90°逆时针" });
            layout.Controls.Add(_rotateComboBox, 1, row++);
            
            var flipPanel = new FlowLayoutPanel();
            _vflipCheckBox = new CheckBox { Text = "上下翻转", AutoSize = true };
            _hflipCheckBox = new CheckBox { Text = "左右翻转", AutoSize = true };
            flipPanel.Controls.AddRange(new Control[] { _vflipCheckBox, _hflipCheckBox });
            layout.Controls.Add(new Label { Text = "翻转:", TextAlign = ContentAlignment.MiddleRight }, 0, row);
            layout.Controls.Add(flipPanel, 1, row++);
            
            layout.Controls.Add(new Label { Text = "反交错:", TextAlign = ContentAlignment.MiddleRight }, 0, row);
            _deinterlaceComboBox = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 120 };
            _deinterlaceComboBox.Items.AddRange(new[] { "none", "bwdif", "yadif", "kerndeint", "pp=lb", "fieldorder" });
            layout.Controls.Add(_deinterlaceComboBox, 1, row++);
            
            _pixFmtCheckBox = new CheckBox { Text = "指定像素格式", AutoSize = true };
            layout.Controls.Add(_pixFmtCheckBox, 0, row);
            _pixFmtComboBox = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 120, Enabled = false };
            _pixFmtComboBox.Items.AddRange(new[] { "yuv420p", "yuv422p", "yuv444p", "yuv420p10le", "yuv422p10le", "yuv444p10le", "p010le", "nv12" });
            _pixFmtCheckBox.CheckedChanged += (s, e) => _pixFmtComboBox.Enabled = _pixFmtCheckBox.Checked;
            layout.Controls.Add(_pixFmtComboBox, 1, row++);
            
            _trimCheckBox = new CheckBox { Text = "启用截取片段", AutoSize = true };
            layout.Controls.Add(_trimCheckBox, 0, row);
            var trimPanel = new FlowLayoutPanel();
            _trimStartTextBox = new TextBox { Width = 100, Enabled = false };
            _trimEndTextBox = new TextBox { Width = 100, Enabled = false };
            trimPanel.Controls.AddRange(new Control[] { new Label { Text = "开始:" }, _trimStartTextBox, new Label { Text = "结束:" }, _trimEndTextBox });
            _trimCheckBox.CheckedChanged += (s, e) => { _trimStartTextBox.Enabled = _trimCheckBox.Checked; _trimEndTextBox.Enabled = _trimCheckBox.Checked; };
            layout.Controls.Add(trimPanel, 1, row++);
            
            page.Controls.Add(layout);
            return page;
        }
        
        private TabPage CreateOverlayPage()
        {
            var page = new TabPage("叠加/偏移");
            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 6, Padding = new Padding(10) };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            
            bool isMainVideo = (_allTracks != null && _allTracks.Count > 0 && _allTracks[0] == _track);
            
            if (isMainVideo)
            {
                int row = 0;
                _padEnabledCheckBox = new CheckBox { Text = "启用画布偏移", AutoSize = true };
                layout.Controls.Add(_padEnabledCheckBox, 0, row);
                layout.Controls.Add(new Panel(), 1, row++);
                
                layout.Controls.Add(new Label { Text = "画布宽度:", TextAlign = ContentAlignment.MiddleRight }, 0, row);
                _padWidthTextBox = new TextBox { Width = 100 };
                layout.Controls.Add(_padWidthTextBox, 1, row++);
                
                layout.Controls.Add(new Label { Text = "画布高度:", TextAlign = ContentAlignment.MiddleRight }, 0, row);
                _padHeightTextBox = new TextBox { Width = 100 };
                layout.Controls.Add(_padHeightTextBox, 1, row++);
                
                layout.Controls.Add(new Label { Text = "偏移 X:", TextAlign = ContentAlignment.MiddleRight }, 0, row);
                _offsetXTextBox = new TextBox { Text = "0", Width = 100 };
                layout.Controls.Add(_offsetXTextBox, 1, row++);
                
                layout.Controls.Add(new Label { Text = "偏移 Y:", TextAlign = ContentAlignment.MiddleRight }, 0, row);
                _offsetYTextBox = new TextBox { Text = "0", Width = 100 };
                layout.Controls.Add(_offsetYTextBox, 1, row++);
                
                var tipLabel = new Label { Text = "⚠ 预览模式下无法体现主视频偏移效果，请转码后查看", ForeColor = Color.Red, AutoSize = true };
                layout.Controls.Add(tipLabel, 0, row);
                layout.SetColumnSpan(tipLabel, 2);
            }
            else
            {
                int row = 0;
                _overlayEnabledCheckBox = new CheckBox { Text = "启用叠加", AutoSize = true };
                layout.Controls.Add(_overlayEnabledCheckBox, 0, row);
                layout.Controls.Add(new Panel(), 1, row++);
                
                layout.Controls.Add(new Label { Text = "X 位置 (支持表达式，如 W-w-10):", TextAlign = ContentAlignment.MiddleRight }, 0, row);
                _overlayXTextBox = new TextBox { Text = "W-w-10", Width = 200 };
                layout.Controls.Add(_overlayXTextBox, 1, row++);
                
                layout.Controls.Add(new Label { Text = "Y 位置 (支持表达式):", TextAlign = ContentAlignment.MiddleRight }, 0, row);
                _overlayYTextBox = new TextBox { Text = "H-h-10", Width = 200 };
                layout.Controls.Add(_overlayYTextBox, 1, row++);
                
                var presetGroup = new GroupBox { Text = "快速预设", Dock = DockStyle.Fill };
                var presetFlow = new FlowLayoutPanel { Dock = DockStyle.Fill };
                var positions = new Dictionary<string, (string, string)>
                {
                    {"左上角", ("10", "10")},
                    {"右上角", ("W-w-10", "10")},
                    {"左下角", ("10", "H-h-10")},
                    {"右下角", ("W-w-10", "H-h-10")},
                    {"居中", ("(W-w)/2", "(H-h)/2")}
                };
                foreach (var pos in positions)
                {
                    var btn = new Button { Text = pos.Key, Width = 80 };
                    btn.Click += (s, e) => { _overlayXTextBox.Text = pos.Value.Item1; _overlayYTextBox.Text = pos.Value.Item2; };
                    presetFlow.Controls.Add(btn);
                }
                presetGroup.Controls.Add(presetFlow);
                layout.Controls.Add(presetGroup, 0, row);
                layout.SetColumnSpan(presetGroup, 2);
            }
            
            page.Controls.Add(layout);
            return page;
        }
        
        private TabPage CreateAudioPage()
        {
            var page = new TabPage("音频编码");
            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 3, Padding = new Padding(10) };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            
            int row = 0;
            layout.Controls.Add(new Label { Text = "编码器:", TextAlign = ContentAlignment.MiddleRight }, 0, row);
            _audioEncoderComboBox = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 120 };
            _audioEncoderComboBox.Items.AddRange(new[] { "copy", "aac", "libmp3lame", "opus", "ac3", "flac", "alac", "pcm_s16le" });
            layout.Controls.Add(_audioEncoderComboBox, 1, row++);
            
            layout.Controls.Add(new Label { Text = "比特率:", TextAlign = ContentAlignment.MiddleRight }, 0, row);
            _audioBitrateTextBox = new TextBox { Text = "128k", Width = 100 };
            layout.Controls.Add(_audioBitrateTextBox, 1, row++);
            
            layout.Controls.Add(new Label { Text = "采样率:", TextAlign = ContentAlignment.MiddleRight }, 0, row);
            _audioSamplerateTextBox = new TextBox { Text = "44100", Width = 100 };
            layout.Controls.Add(_audioSamplerateTextBox, 1, row++);
            
            page.Controls.Add(layout);
            return page;
        }
        
        private TabPage CreateSubtitlePage()
        {
            var page = new TabPage("字幕编码");
            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 2, Padding = new Padding(10) };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            
            var tipLabel = new Label { Text = "提示：对于 ASS/SSA 字幕，推荐使用 MKV 容器并选择「copy」流，MP4 容器需用 mov_text", ForeColor = Color.Gray, AutoSize = true };
            layout.Controls.Add(tipLabel, 0, 0);
            layout.SetColumnSpan(tipLabel, 2);
            
            layout.Controls.Add(new Label { Text = "编码器:", TextAlign = ContentAlignment.MiddleRight }, 0, 1);
            _subtitleEncoderComboBox = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 120 };
            _subtitleEncoderComboBox.Items.AddRange(new[] { "copy", "mov_text", "srt" });
            layout.Controls.Add(_subtitleEncoderComboBox, 1, 1);
            
            page.Controls.Add(layout);
            return page;
        }
        
        private void LoadSettingsIntoUI()
        {
            if (_track.Type == "video")
            {
                var s = _track.EncSettings;
                _encoderComboBox.SelectedItem = s.ContainsKey("encoder") ? s["encoder"] : "copy";
                _presetComboBox.SelectedItem = s.ContainsKey("preset") ? s["preset"] : "medium";
                string rc = s.ContainsKey("rate_control_type") ? s["rate_control_type"] : "crf";
                if (rc == "crf") { _crfRadio.Checked = true; _crfTrackBar.Value = int.Parse(s.GetValueOrDefault("crf_value", "25")); }
                else if (rc == "cq") { _cqRadio.Checked = true; _cqTrackBar.Value = int.Parse(s.GetValueOrDefault("cq_value", "35")); }
                else if (rc == "global_quality") { _globalQualityRadio.Checked = true; _gqTrackBar.Value = int.Parse(s.GetValueOrDefault("global_quality", "25")); }
                else { _bitrateRadio.Checked = true; _bitrateTextBox.Text = s.GetValueOrDefault("bitrate_video", "1900k"); }
                
                _frameRateTypeComboBox.SelectedIndex = (s.GetValueOrDefault("frame_rate_type") == "keep") ? 0 : 1;
                _frameRateCustomTextBox.Text = s.GetValueOrDefault("frame_rate_custom", "30");
                _scaleCheckBox.Checked = s.GetValueOrDefault("scale_enabled") == "True";
                _scaleWidthTextBox.Text = s.GetValueOrDefault("scale_width", "");
                _scaleHeightTextBox.Text = s.GetValueOrDefault("scale_height", "");
                string scaleMethod = s.GetValueOrDefault("scale_method", "width");
                _scaleMethodComboBox.SelectedIndex = scaleMethod == "width" ? 0 : (scaleMethod == "height" ? 1 : 2);
                _cropCheckBox.Checked = s.GetValueOrDefault("crop_enabled") == "True";
                _cropWidthTextBox.Text = s.GetValueOrDefault("crop_width", "iw/2");
                _cropHeightTextBox.Text = s.GetValueOrDefault("crop_height", "ih");
                _cropLeftTextBox.Text = s.GetValueOrDefault("crop_left", "0");
                _cropTopTextBox.Text = s.GetValueOrDefault("crop_top", "0");
                string rot = s.GetValueOrDefault("rotate", "none");
                _rotateComboBox.SelectedIndex = rot == "none" ? 0 : (rot == "90" ? 1 : (rot == "180" ? 2 : 3));
                _vflipCheckBox.Checked = s.GetValueOrDefault("vflip") == "True";
                _hflipCheckBox.Checked = s.GetValueOrDefault("hflip") == "True";
                _deinterlaceComboBox.SelectedItem = s.GetValueOrDefault("deinterlace_filter", "none");
                _pixFmtCheckBox.Checked = s.GetValueOrDefault("pix_fmt_enabled") != "False";
                _pixFmtComboBox.SelectedItem = s.GetValueOrDefault("pix_fmt", "yuv420p");
                _trimCheckBox.Checked = s.GetValueOrDefault("trim_enabled") == "True";
                _trimStartTextBox.Text = s.GetValueOrDefault("trim_start", "0");
                _trimEndTextBox.Text = s.GetValueOrDefault("trim_end", "");
                
                if (_isPipEnabled)
                {
                    bool isMain = (_allTracks != null && _allTracks.Count > 0 && _allTracks[0] == _track);
                    if (isMain)
                    {
                        _padEnabledCheckBox.Checked = s.GetValueOrDefault("pad_enabled") == "True";
                        _padWidthTextBox.Text = s.GetValueOrDefault("pad_width", "");
                        _padHeightTextBox.Text = s.GetValueOrDefault("pad_height", "");
                        _offsetXTextBox.Text = s.GetValueOrDefault("offset_x", "0");
                        _offsetYTextBox.Text = s.GetValueOrDefault("offset_y", "0");
                    }
                    else
                    {
                        _overlayEnabledCheckBox.Checked = s.GetValueOrDefault("overlay_enabled") != "False";
                        _overlayXTextBox.Text = s.GetValueOrDefault("overlay_x", "W-w-10");
                        _overlayYTextBox.Text = s.GetValueOrDefault("overlay_y", "H-h-10");
                    }
                }
            }
            else if (_track.Type == "audio")
            {
                var s = _track.EncSettings;
                _audioEncoderComboBox.SelectedItem = s.ContainsKey("encoder") ? s["encoder"] : "copy";
                _audioBitrateTextBox.Text = s.ContainsKey("bitrate") ? s["bitrate"] : "128k";
                _audioSamplerateTextBox.Text = s.ContainsKey("samplerate") ? s["samplerate"] : "44100";
            }
            else if (_track.Type == "subtitle")
            {
                var s = _track.EncSettings;
                _subtitleEncoderComboBox.SelectedItem = s.ContainsKey("encoder") ? s["encoder"] : "copy";
            }
        }
        
        private void SaveChanges(object sender, EventArgs e)
        {
            var newSettings = new Dictionary<string, string>();
            if (_track.Type == "video")
            {
                newSettings["encoder"] = _encoderComboBox.SelectedItem?.ToString();
                newSettings["preset"] = _presetComboBox.SelectedItem?.ToString();
                if (_crfRadio.Checked) { newSettings["rate_control_type"] = "crf"; newSettings["crf_value"] = _crfTrackBar.Value.ToString(); }
                else if (_cqRadio.Checked) { newSettings["rate_control_type"] = "cq"; newSettings["cq_value"] = _cqTrackBar.Value.ToString(); }
                else if (_globalQualityRadio.Checked) { newSettings["rate_control_type"] = "global_quality"; newSettings["global_quality"] = _gqTrackBar.Value.ToString(); }
                else { newSettings["rate_control_type"] = "bitrate"; newSettings["bitrate_video"] = _bitrateTextBox.Text; }
                
                newSettings["frame_rate_type"] = _frameRateTypeComboBox.SelectedIndex == 0 ? "keep" : "custom";
                newSettings["frame_rate_custom"] = _frameRateCustomTextBox.Text;
                newSettings["scale_enabled"] = _scaleCheckBox.Checked.ToString();
                newSettings["scale_width"] = _scaleWidthTextBox.Text;
                newSettings["scale_height"] = _scaleHeightTextBox.Text;
                newSettings["scale_method"] = _scaleMethodComboBox.SelectedIndex == 0 ? "width" : (_scaleMethodComboBox.SelectedIndex == 1 ? "height" : "exact");
                newSettings["crop_enabled"] = _cropCheckBox.Checked.ToString();
                newSettings["crop_width"] = _cropWidthTextBox.Text;
                newSettings["crop_height"] = _cropHeightTextBox.Text;
                newSettings["crop_left"] = _cropLeftTextBox.Text;
                newSettings["crop_top"] = _cropTopTextBox.Text;
                string rot = _rotateComboBox.SelectedIndex == 0 ? "none" : (_rotateComboBox.SelectedIndex == 1 ? "90" : (_rotateComboBox.SelectedIndex == 2 ? "180" : "270"));
                newSettings["rotate"] = rot;
                newSettings["vflip"] = _vflipCheckBox.Checked.ToString();
                newSettings["hflip"] = _hflipCheckBox.Checked.ToString();
                newSettings["deinterlace_filter"] = _deinterlaceComboBox.SelectedItem?.ToString();
                newSettings["pix_fmt_enabled"] = _pixFmtCheckBox.Checked.ToString();
                newSettings["pix_fmt"] = _pixFmtComboBox.SelectedItem?.ToString();
                newSettings["trim_enabled"] = _trimCheckBox.Checked.ToString();
                newSettings["trim_start"] = _trimStartTextBox.Text;
                newSettings["trim_end"] = _trimEndTextBox.Text;
                
                if (_isPipEnabled)
                {
                    bool isMain = (_allTracks != null && _allTracks.Count > 0 && _allTracks[0] == _track);
                    if (isMain)
                    {
                        newSettings["pad_enabled"] = _padEnabledCheckBox.Checked.ToString();
                        newSettings["pad_width"] = _padWidthTextBox.Text;
                        newSettings["pad_height"] = _padHeightTextBox.Text;
                        newSettings["offset_x"] = _offsetXTextBox.Text;
                        newSettings["offset_y"] = _offsetYTextBox.Text;
                    }
                    else
                    {
                        newSettings["overlay_enabled"] = _overlayEnabledCheckBox.Checked.ToString();
                        newSettings["overlay_x"] = _overlayXTextBox.Text;
                        newSettings["overlay_y"] = _overlayYTextBox.Text;
                    }
                }
            }
            else if (_track.Type == "audio")
            {
                newSettings["encoder"] = _audioEncoderComboBox.SelectedItem?.ToString();
                newSettings["bitrate"] = _audioBitrateTextBox.Text;
                newSettings["samplerate"] = _audioSamplerateTextBox.Text;
            }
            else if (_track.Type == "subtitle")
            {
                newSettings["encoder"] = _subtitleEncoderComboBox.SelectedItem?.ToString();
            }
            
            _track.EncSettings = newSettings;
            this.DialogResult = DialogResult.OK;
            Close();
        }
    }
}
