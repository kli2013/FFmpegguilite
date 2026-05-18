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
using FFLiteGUI.Strategies;
using FFLiteGUI.Utils;
using FFLiteGUI.Validators;

namespace FFLiteGUI.Forms
{
    public class MainForm : Form
    {
        // 控件字段（完整保留，与原版本完全一致）
        private TextBox txtInputFile; private TextBox txtOutputDir; private TextBox txtOutputSuffix; private TextBox txtCustomOutputName;
        private ComboBox cboOutputContainer; private ComboBox cboEncoder; private ComboBox cboPreset;
        private RadioButton rbCRF; private RadioButton rbCQ; private RadioButton rbGlobalQuality; private RadioButton rbBitrate;
        private TrackBar trkCRF; private Label lblCRFValue; private TrackBar trkCQ; private Label lblCQValue;
        private TrackBar trkGlobalQuality; private Label lblGlobalQualityValue; private TextBox txtBitrate;
        private CheckBox chkHwaccel; private ComboBox cboHwaccelDecoder; private TextBox txtCustomArgs;
        private CheckBox chkAudioEnabled; private ComboBox cboAudioCodec; private TextBox txtAudioBitrate; private TextBox txtAudioSamplerate;
        private CheckBox chkOnlyAudio; private ComboBox cboAudioFormat; private ComboBox cboFrameRateType; private TextBox txtFrameRateCustom;
        private CheckBox chkScale; private ComboBox cboScaleMethod; private TextBox txtScaleW; private TextBox txtScaleH;
        private CheckBox chkCrop; private TextBox txtCropW; private TextBox txtCropH; private TextBox txtCropLeft; private TextBox txtCropTop;
        private ComboBox cboRotate; private CheckBox chkVflip; private CheckBox chkHflip;
        private CheckBox chkSpeed; private TextBox txtSpeedFactor; private ComboBox cboDeinterlace;
        private CheckBox chkPixFmt; private ComboBox cboPixFmt;
        private CheckBox chkSubtitle; private TextBox txtSubtitlePath; private Button btnBrowseSubtitle;
        private CheckBox chkTrim; private TextBox txtTrimStart; private TextBox txtTrimEnd;
        private Button btnAddTask; private Button btnStartQueue; private Button btnStopQueue; private Button btnClearAll; private Button btnRemoveSelected;
        private NumericUpDown nudMaxParallel; private NumericUpDown nudMaxHwParallel;
        private ListView lvTasks; private RichTextBox txtCommandPreview; private TextBox txtInfoLog; private TextBox txtDetailLog;
        private Button btnBrowseInput; private Button btnBrowseOutputDir; private Button btnRefreshPreview;

        private string _ffmpegPath;
        private List<TaskInfo> _tasks = new List<TaskInfo>();
        private bool _isProcessing;
        private CancellationTokenSource _cts;
        private SemaphoreSlim _hwSemaphore;
        private int _maxParallel = 2;
        private int _maxHwParallel = 2;

        public MainForm()
        {
            InitializeComponent();
            FindFFmpeg();
            LoadSettings();
            UpdateCommandPreview();

            // ---- 新增：拖拽文件支持 ----
            this.AllowDrop = true;
            this.DragEnter += MainForm_DragEnter;
            this.DragDrop += MainForm_DragDrop;
        }

        private void InitializeComponent()
        {
            this.Text = "FFLiteGUI - FFmpeg 多功能工具";
            this.Size = new Size(1300, 900);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MinimumSize = new Size(1000, 700);

            // 主布局
            var splitContainer = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Vertical };
            splitContainer.SplitterDistance = 850;
            var leftPanel = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 4, ColumnCount = 1, Padding = new Padding(5) };
            leftPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            leftPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            leftPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            leftPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            // 输入输出区域
            var ioGroup = new GroupBox { Text = "输入 / 输出", Dock = DockStyle.Fill, Padding = new Padding(5) };
            var ioLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 4, Padding = new Padding(3) };
            ioLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            ioLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            ioLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            ioLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            int row = 0;
            ioLayout.Controls.Add(new Label { Text = "输入文件:", TextAlign = ContentAlignment.MiddleRight }, 0, row);
            txtInputFile = new TextBox { Dock = DockStyle.Fill };
            ioLayout.Controls.Add(txtInputFile, 1, row);
            btnBrowseInput = new Button { Text = "浏览", Width = 60 };
            btnBrowseInput.Click += (s, e) => SelectInputFile();
            ioLayout.Controls.Add(btnBrowseInput, 2, row);
            btnAddTask = new Button { Text = "添加到任务列表", Width = 120 };
            btnAddTask.Click += (s, e) => AddCurrentAsTask();
            ioLayout.Controls.Add(btnAddTask, 3, row);
            row++;

            ioLayout.Controls.Add(new Label { Text = "输出目录:", TextAlign = ContentAlignment.MiddleRight }, 0, row);
            txtOutputDir = new TextBox { Dock = DockStyle.Fill };
            ioLayout.Controls.Add(txtOutputDir, 1, row);
            btnBrowseOutputDir = new Button { Text = "浏览", Width = 60 };
            btnBrowseOutputDir.Click += (s, e) => SelectOutputDir();
            ioLayout.Controls.Add(btnBrowseOutputDir, 2, row);
            ioLayout.Controls.Add(new Panel(), 3, row);
            row++;

            ioLayout.Controls.Add(new Label { Text = "文件名后缀:", TextAlign = ContentAlignment.MiddleRight }, 0, row);
            txtOutputSuffix = new TextBox { Width = 150 };
            ioLayout.Controls.Add(txtOutputSuffix, 1, row);
            ioLayout.Controls.Add(new Label { Text = "自定义完整名称:", TextAlign = ContentAlignment.MiddleRight }, 2, row);
            txtCustomOutputName = new TextBox { Width = 200 };
            ioLayout.Controls.Add(txtCustomOutputName, 3, row);
            row++;

            ioLayout.Controls.Add(new Label { Text = "输出容器:", TextAlign = ContentAlignment.MiddleRight }, 0, row);
            cboOutputContainer = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 80 };
            cboOutputContainer.Items.AddRange(new[] { "mp4", "mkv", "mov", "avi", "webm" });
            cboOutputContainer.SelectedIndex = 0;
            ioLayout.Controls.Add(cboOutputContainer, 1, row);
            ioGroup.Controls.Add(ioLayout);
            leftPanel.Controls.Add(ioGroup, 0, 0);

            // 预设区域
            var presetGroup = new GroupBox { Text = "参数预设", Dock = DockStyle.Fill, Padding = new Padding(5) };
            var presetLayout = new FlowLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(3) };
            presetLayout.Controls.Add(new Label { Text = "预设名称:" });
            var cboPresetList = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 200 };
            presetLayout.Controls.Add(cboPresetList);
            var btnSavePreset = new Button { Text = "保存当前参数为预设", Width = 150 };
            btnSavePreset.Click += (s, e) => SavePreset();
            presetLayout.Controls.Add(btnSavePreset);
            var btnDeletePreset = new Button { Text = "删除预设", Width = 100 };
            btnDeletePreset.Click += (s, e) => DeletePreset();
            presetLayout.Controls.Add(btnDeletePreset);
            var btnExportPresets = new Button { Text = "导出所有预设", Width = 120 };
            btnExportPresets.Click += (s, e) => ExportAllPresets();
            presetLayout.Controls.Add(btnExportPresets);
            var btnImportPresets = new Button { Text = "导入预设", Width = 100 };
            btnImportPresets.Click += (s, e) => ImportPresets();
            presetLayout.Controls.Add(btnImportPresets);
            presetGroup.Controls.Add(presetLayout);
            leftPanel.Controls.Add(presetGroup, 0, 1);

            // 主要参数标签页（增加封装合并标签页）
            var paramTab = new TabControl { Dock = DockStyle.Fill };
            paramTab.TabPages.Add(CreateVideoEncodingTab());
            paramTab.TabPages.Add(CreateVideoFiltersTab());
            paramTab.TabPages.Add(CreateAudioTab());
            paramTab.TabPages.Add(CreateMergeTab());  // 新增，但只放简单提示，不包含复杂逻辑
            leftPanel.Controls.Add(paramTab, 0, 2);

            // 底部按钮
            var bottomBtnPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, Padding = new Padding(5) };
            var btnSingleTranscode = new Button { Text = "开始编码（单文件）", BackColor = Color.LightGreen, Width = 150 };
            btnSingleTranscode.Click += (s, e) => TranscodeSingle();
            bottomBtnPanel.Controls.Add(btnSingleTranscode);
            btnRefreshPreview = new Button { Text = "刷新命令预览", Width = 120 };
            btnRefreshPreview.Click += (s, e) => UpdateCommandPreview();
            bottomBtnPanel.Controls.Add(btnRefreshPreview);
            bottomBtnPanel.Controls.Add(new Label { Text = "并行任务数:" });
            nudMaxParallel = new NumericUpDown { Minimum = 1, Maximum = 5, Value = 2, Width = 50 };
            nudMaxParallel.ValueChanged += (s, e) => _maxParallel = (int)nudMaxParallel.Value;
            bottomBtnPanel.Controls.Add(nudMaxParallel);
            bottomBtnPanel.Controls.Add(new Label { Text = "硬编并发限制:" });
            nudMaxHwParallel = new NumericUpDown { Minimum = 1, Maximum = 4, Value = 2, Width = 50 };
            nudMaxHwParallel.ValueChanged += (s, e) => _maxHwParallel = (int)nudMaxHwParallel.Value;
            bottomBtnPanel.Controls.Add(nudMaxHwParallel);
            leftPanel.Controls.Add(bottomBtnPanel, 0, 3);

            splitContainer.Panel1.Controls.Add(leftPanel);

            // 右侧面板：修改行高比例（任务列表占60%，命令预览20%，日志20%）
            var rightPanel = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3, ColumnCount = 1, Padding = new Padding(5) };
            rightPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 60));
            rightPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 20));
            rightPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 20));

            // 任务列表
            var taskGroup = new GroupBox { Text = "任务队列", Dock = DockStyle.Fill };
            var taskLayout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1 };
            taskLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            taskLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            var taskBtnPanel = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, Padding = new Padding(3) };
            btnStartQueue = new Button { Text = "开始队列", BackColor = Color.LightGreen, Width = 100 };
            btnStartQueue.Click += (s, e) => StartQueue();
            btnStopQueue = new Button { Text = "停止队列", BackColor = Color.LightCoral, Width = 100 };
            btnStopQueue.Click += (s, e) => StopQueue();
            btnRemoveSelected = new Button { Text = "移除选中", Width = 100 };
            btnRemoveSelected.Click += (s, e) => RemoveSelectedTasks();
            btnClearAll = new Button { Text = "清空全部", Width = 100 };
            btnClearAll.Click += (s, e) => ClearAllTasks();
            var btnClearFinished = new Button { Text = "清空已完成/失败", Width = 150 };
            btnClearFinished.Click += (s, e) => ClearFinishedTasks();
            taskBtnPanel.Controls.AddRange(new Control[] { btnStartQueue, btnStopQueue, btnRemoveSelected, btnClearAll, btnClearFinished });
            taskLayout.Controls.Add(taskBtnPanel, 0, 0);
            lvTasks = new ListView { Dock = DockStyle.Fill, View = View.Details, FullRowSelect = true, GridLines = true };
            lvTasks.Columns.Add("文件名", 150);
            lvTasks.Columns.Add("输出路径", 250);
            lvTasks.Columns.Add("命令(简洁)", 350);
            lvTasks.Columns.Add("状态", 80);
            lvTasks.Columns.Add("错误信息", 200);
            lvTasks.DoubleClick += (s, e) => EditSelectedTask();
            taskLayout.Controls.Add(lvTasks, 0, 1);
            taskGroup.Controls.Add(taskLayout);
            rightPanel.Controls.Add(taskGroup, 0, 0);

            // 命令预览
            var previewGroup = new GroupBox { Text = "当前命令模板", Dock = DockStyle.Fill };
            txtCommandPreview = new RichTextBox { Dock = DockStyle.Fill, ReadOnly = true, Font = new Font("Consolas", 9), BackColor = Color.LightYellow };
            previewGroup.Controls.Add(txtCommandPreview);
            rightPanel.Controls.Add(previewGroup, 0, 1);

            // 日志
            var logGroup = new GroupBox { Text = "转换日志", Dock = DockStyle.Fill };
            var logLayout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1 };
            logLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
            logLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
            var infoGroup = new GroupBox { Text = "关键信息", Dock = DockStyle.Fill };
            txtInfoLog = new TextBox { Dock = DockStyle.Fill, Multiline = true, ReadOnly = true, BackColor = Color.White, Font = new Font("Consolas", 9) };
            infoGroup.Controls.Add(txtInfoLog);
            var detailGroup = new GroupBox { Text = "转换进程信息", Dock = DockStyle.Fill };
            txtDetailLog = new TextBox { Dock = DockStyle.Fill, Multiline = true, ReadOnly = true, BackColor = Color.White, Font = new Font("Consolas", 8) };
            detailGroup.Controls.Add(txtDetailLog);
            logLayout.Controls.Add(infoGroup, 0, 0);
            logLayout.Controls.Add(detailGroup, 0, 1);
            logGroup.Controls.Add(logLayout);
            rightPanel.Controls.Add(logGroup, 0, 2);

            splitContainer.Panel2.Controls.Add(rightPanel);
            this.Controls.Add(splitContainer);

            // 事件绑定（与原版本完全一致）
            txtInputFile.TextChanged += (s, e) => UpdateCommandPreview();
            txtOutputDir.TextChanged += (s, e) => UpdateCommandPreview();
            txtOutputSuffix.TextChanged += (s, e) => UpdateCommandPreview();
            txtCustomOutputName.TextChanged += (s, e) => UpdateCommandPreview();
            cboOutputContainer.SelectedIndexChanged += (s, e) => UpdateCommandPreview();
            cboEncoder.SelectedIndexChanged += (s, e) => { OnEncoderChanged(); UpdateCommandPreview(); };
            rbCRF.CheckedChanged += (s, e) => UpdateCommandPreview();
            rbCQ.CheckedChanged += (s, e) => UpdateCommandPreview();
            rbGlobalQuality.CheckedChanged += (s, e) => UpdateCommandPreview();
            rbBitrate.CheckedChanged += (s, e) => UpdateCommandPreview();
            trkCRF.ValueChanged += (s, e) => { lblCRFValue.Text = trkCRF.Value.ToString(); UpdateCommandPreview(); };
            trkCQ.ValueChanged += (s, e) => { lblCQValue.Text = trkCQ.Value.ToString(); UpdateCommandPreview(); };
            trkGlobalQuality.ValueChanged += (s, e) => { lblGlobalQualityValue.Text = trkGlobalQuality.Value.ToString(); UpdateCommandPreview(); };
            txtBitrate.TextChanged += (s, e) => UpdateCommandPreview();
            chkHwaccel.CheckedChanged += (s, e) => { cboHwaccelDecoder.Enabled = chkHwaccel.Checked; if (chkHwaccel.Checked && cboHwaccelDecoder.SelectedIndex < 0) cboHwaccelDecoder.SelectedIndex = 1; UpdateCommandPreview(); };
            cboHwaccelDecoder.SelectedIndexChanged += (s, e) => UpdateCommandPreview();
            txtCustomArgs.TextChanged += (s, e) => UpdateCommandPreview();
            chkAudioEnabled.CheckedChanged += (s, e) => UpdateCommandPreview();
            cboAudioCodec.SelectedIndexChanged += (s, e) => UpdateCommandPreview();
            txtAudioBitrate.TextChanged += (s, e) => UpdateCommandPreview();
            txtAudioSamplerate.TextChanged += (s, e) => UpdateCommandPreview();
            chkOnlyAudio.CheckedChanged += (s, e) => UpdateCommandPreview();
            cboAudioFormat.SelectedIndexChanged += (s, e) => UpdateCommandPreview();
            cboFrameRateType.SelectedIndexChanged += (s, e) => { txtFrameRateCustom.Enabled = cboFrameRateType.SelectedIndex == 1; UpdateCommandPreview(); };
            txtFrameRateCustom.TextChanged += (s, e) => UpdateCommandPreview();
            chkScale.CheckedChanged += (s, e) => UpdateCommandPreview();
            cboScaleMethod.SelectedIndexChanged += (s, e) => UpdateCommandPreview();
            txtScaleW.TextChanged += (s, e) => UpdateCommandPreview();
            txtScaleH.TextChanged += (s, e) => UpdateCommandPreview();
            chkCrop.CheckedChanged += (s, e) => UpdateCommandPreview();
            txtCropW.TextChanged += (s, e) => UpdateCommandPreview();
            txtCropH.TextChanged += (s, e) => UpdateCommandPreview();
            txtCropLeft.TextChanged += (s, e) => UpdateCommandPreview();
            txtCropTop.TextChanged += (s, e) => UpdateCommandPreview();
            cboRotate.SelectedIndexChanged += (s, e) => UpdateCommandPreview();
            chkVflip.CheckedChanged += (s, e) => UpdateCommandPreview();
            chkHflip.CheckedChanged += (s, e) => UpdateCommandPreview();
            chkSpeed.CheckedChanged += (s, e) => UpdateCommandPreview();
            txtSpeedFactor.TextChanged += (s, e) => UpdateCommandPreview();
            cboDeinterlace.SelectedIndexChanged += (s, e) => UpdateCommandPreview();
            chkPixFmt.CheckedChanged += (s, e) => { cboPixFmt.Enabled = chkPixFmt.Checked; UpdateCommandPreview(); };
            cboPixFmt.SelectedIndexChanged += (s, e) => UpdateCommandPreview();
            chkSubtitle.CheckedChanged += (s, e) => { txtSubtitlePath.Enabled = chkSubtitle.Checked; btnBrowseSubtitle.Enabled = chkSubtitle.Checked; UpdateCommandPreview(); };
            txtSubtitlePath.TextChanged += (s, e) => UpdateCommandPreview();
            chkTrim.CheckedChanged += (s, e) => { txtTrimStart.Enabled = chkTrim.Checked; txtTrimEnd.Enabled = chkTrim.Checked; UpdateCommandPreview(); };
            txtTrimStart.TextChanged += (s, e) => UpdateCommandPreview();
            txtTrimEnd.TextChanged += (s, e) => UpdateCommandPreview();
        }

        private TabPage CreateVideoEncodingTab()
        {
            var page = new TabPage("视频编码");
            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Padding = new Padding(5) };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45));

            // 左侧编码参数
            var leftGroup = new GroupBox { Text = "编码参数", Dock = DockStyle.Fill };
            var leftLayout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 5, ColumnCount = 2, Padding = new Padding(5) };
            leftLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            leftLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            leftLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            leftLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            leftLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            int row = 0;
            leftLayout.Controls.Add(new Label { Text = "编码器:", TextAlign = ContentAlignment.MiddleRight }, 0, row);
            cboEncoder = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 180 };
            cboEncoder.Items.AddRange(new[] { "libx264", "libx265", "libvpx-vp9", "libsvtav1", "mpeg4", "libxvid", "libtheora",
                "h264_nvenc", "hevc_nvenc", "av1_nvenc", "h264_qsv", "hevc_qsv", "av1_qsv",
                "h264_amf", "hevc_amf", "av1_amf", "h264_vaapi", "hevc_vaapi", "copy" });
            cboEncoder.SelectedIndex = 1;
            leftLayout.Controls.Add(cboEncoder, 1, row++);

            leftLayout.Controls.Add(new Label { Text = "编码预设:", TextAlign = ContentAlignment.MiddleRight }, 0, row);
            cboPreset = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 120 };
            cboPreset.Items.AddRange(new[] { "ultrafast", "superfast", "veryfast", "faster", "fast", "medium", "slow", "slower", "veryslow", "p1", "p2", "p3", "p4", "p5", "p6", "p7" });
            cboPreset.SelectedIndex = 5;
            leftLayout.Controls.Add(cboPreset, 1, row++);

            leftLayout.Controls.Add(new Label { Text = "码率控制:", TextAlign = ContentAlignment.MiddleRight }, 0, row);
            var rcPanel = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight };
            rbCRF = new RadioButton { Text = "CRF (CPU)", AutoSize = true, Checked = true };
            rbCQ = new RadioButton { Text = "CQ (NVENC)", AutoSize = true };
            rbGlobalQuality = new RadioButton { Text = "Global Quality (QSV)", AutoSize = true };
            rbBitrate = new RadioButton { Text = "固定比特率", AutoSize = true };
            rcPanel.Controls.AddRange(new Control[] { rbCRF, rbCQ, rbGlobalQuality, rbBitrate });
            leftLayout.Controls.Add(rcPanel, 1, row++);

            var dynamicPanel = new Panel { Dock = DockStyle.Fill };
            leftLayout.Controls.Add(dynamicPanel, 1, row);
            leftLayout.SetRowSpan(dynamicPanel, 2);

            trkCRF = new TrackBar { Minimum = 0, Maximum = 51, Value = 25, TickFrequency = 5, Width = 200 };
            lblCRFValue = new Label { Text = "25", Width = 30 };
            trkCQ = new TrackBar { Minimum = 0, Maximum = 51, Value = 35, TickFrequency = 5, Width = 200 };
            lblCQValue = new Label { Text = "35", Width = 30 };
            trkGlobalQuality = new TrackBar { Minimum = 1, Maximum = 51, Value = 25, TickFrequency = 5, Width = 200 };
            lblGlobalQualityValue = new Label { Text = "25", Width = 30 };
            txtBitrate = new TextBox { Text = "1900k", Width = 100 };

            var crfPanel = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, Controls = { new Label { Text = "CRF (0~51):" }, trkCRF, lblCRFValue }, Visible = true };
            var cqPanel = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, Controls = { new Label { Text = "CQ (0~51):" }, trkCQ, lblCQValue }, Visible = false };
            var gqPanel = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, Controls = { new Label { Text = "Global Quality (1~51):" }, trkGlobalQuality, lblGlobalQualityValue }, Visible = false };
            var bitPanel = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, Controls = { new Label { Text = "比特率 (kbps):" }, txtBitrate }, Visible = false };
            dynamicPanel.Controls.AddRange(new Control[] { crfPanel, cqPanel, gqPanel, bitPanel });

            rbCRF.CheckedChanged += (s, e) => { crfPanel.Visible = rbCRF.Checked; UpdateCommandPreview(); };
            rbCQ.CheckedChanged += (s, e) => { cqPanel.Visible = rbCQ.Checked; UpdateCommandPreview(); };
            rbGlobalQuality.CheckedChanged += (s, e) => { gqPanel.Visible = rbGlobalQuality.Checked; UpdateCommandPreview(); };
            rbBitrate.CheckedChanged += (s, e) => { bitPanel.Visible = rbBitrate.Checked; UpdateCommandPreview(); };

            leftGroup.Controls.Add(leftLayout);
            layout.Controls.Add(leftGroup, 0, 0);

            // 右侧高级选项
            var rightGroup = new GroupBox { Text = "高级选项 (硬件解码/自定义参数)", Dock = DockStyle.Fill };
            var rightLayout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3, ColumnCount = 1, Padding = new Padding(5) };
            rightLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            rightLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            rightLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            var hwPanel = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight };
            chkHwaccel = new CheckBox { Text = "启用硬件解码", AutoSize = true };
            cboHwaccelDecoder = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 200, Enabled = false };
            cboHwaccelDecoder.Items.AddRange(HardwareDecoderHelper.GetDecoderOptions());
            cboHwaccelDecoder.SelectedIndex = 0;
            hwPanel.Controls.AddRange(new Control[] { chkHwaccel, cboHwaccelDecoder });
            rightLayout.Controls.Add(hwPanel, 0, 0);

            rightLayout.Controls.Add(new Label { Text = "自定义FFmpeg参数 (例如: -tune grain -profile:v high):", AutoSize = true }, 0, 1);
            txtCustomArgs = new TextBox { Dock = DockStyle.Fill, Multiline = true, Height = 60 };
            rightLayout.Controls.Add(txtCustomArgs, 0, 2);

            rightGroup.Controls.Add(rightLayout);
            layout.Controls.Add(rightGroup, 1, 0);

            page.Controls.Add(layout);
            return page;
        }

        private TabPage CreateVideoFiltersTab()
        {
            var page = new TabPage("视频滤镜");
            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 12, Padding = new Padding(5), AutoSize = true };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            int row = 0;
            layout.Controls.Add(new Label { Text = "帧率:", TextAlign = ContentAlignment.MiddleRight }, 0, row);
            var fpsPanel = new FlowLayoutPanel();
            cboFrameRateType = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 80 };
            cboFrameRateType.Items.AddRange(new[] { "保持源", "指定" });
            txtFrameRateCustom = new TextBox { Text = "30", Width = 50, Enabled = false };
            fpsPanel.Controls.AddRange(new Control[] { cboFrameRateType, txtFrameRateCustom, new Label { Text = "fps" } });
            layout.Controls.Add(fpsPanel, 1, row++);

            // 缩放控件（修复逻辑：宽度/高度/精确模式正确启用对应输入框）
            chkScale = new CheckBox { Text = "启用缩放", AutoSize = true };
            layout.Controls.Add(chkScale, 0, row);
            var scalePanel = new FlowLayoutPanel();
            cboScaleMethod = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 100 };
            cboScaleMethod.Items.AddRange(new[] { "宽度(高度自动)", "高度(宽度自动)", "精确宽×高" });
            txtScaleW = new TextBox { Width = 50 };
            txtScaleH = new TextBox { Width = 50, Enabled = false };
            // 修复：根据缩放模式启用/禁用宽高输入框
            cboScaleMethod.SelectedIndexChanged += (s, e) =>
            {
                int idx = cboScaleMethod.SelectedIndex;
                txtScaleW.Enabled = (idx == 0 || idx == 2);
                txtScaleH.Enabled = (idx == 1 || idx == 2);
                if (idx == 0) txtScaleH.Text = "";
                if (idx == 1) txtScaleW.Text = "";
                UpdateCommandPreview();
            };
            cboScaleMethod.SelectedIndex = 0;  // 默认宽度模式
            txtScaleW.Enabled = true;
            txtScaleH.Enabled = false;
            scalePanel.Controls.AddRange(new Control[] { cboScaleMethod, new Label { Text = "宽:" }, txtScaleW, new Label { Text = "高:" }, txtScaleH });
            layout.Controls.Add(scalePanel, 1, row++);

            chkCrop = new CheckBox { Text = "启用裁剪", AutoSize = true };
            layout.Controls.Add(chkCrop, 0, row);
            var cropPanel = new FlowLayoutPanel();
            txtCropW = new TextBox { Width = 60 };
            txtCropH = new TextBox { Width = 60 };
            txtCropLeft = new TextBox { Width = 50 };
            txtCropTop = new TextBox { Width = 50 };
            cropPanel.Controls.AddRange(new Control[] { new Label { Text = "宽:" }, txtCropW, new Label { Text = "高:" }, txtCropH, new Label { Text = "左:" }, txtCropLeft, new Label { Text = "上:" }, txtCropTop });
            layout.Controls.Add(cropPanel, 1, row++);

            layout.Controls.Add(new Label { Text = "旋转:", TextAlign = ContentAlignment.MiddleRight }, 0, row);
            cboRotate = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 100 };
            cboRotate.Items.AddRange(new[] { "无", "90°顺时针", "180°", "90°逆时针" });
            cboRotate.SelectedIndex = 0;   // 默认“无”，避免自动添加旋转滤镜
            layout.Controls.Add(cboRotate, 1, row++);

            var flipPanel = new FlowLayoutPanel();
            chkVflip = new CheckBox { Text = "上下翻转", AutoSize = true };
            chkHflip = new CheckBox { Text = "左右翻转", AutoSize = true };
            flipPanel.Controls.AddRange(new Control[] { chkVflip, chkHflip });
            layout.Controls.Add(new Label { Text = "翻转:", TextAlign = ContentAlignment.MiddleRight }, 0, row);
            layout.Controls.Add(flipPanel, 1, row++);

            chkSpeed = new CheckBox { Text = "启用变速", AutoSize = true };
            layout.Controls.Add(chkSpeed, 0, row);
            var speedPanel = new FlowLayoutPanel();
            txtSpeedFactor = new TextBox { Text = "1.0", Width = 60 };
            speedPanel.Controls.AddRange(new Control[] { new Label { Text = "速度倍数 (0.5慢,2.0快):" }, txtSpeedFactor });
            layout.Controls.Add(speedPanel, 1, row++);

            layout.Controls.Add(new Label { Text = "反交错:", TextAlign = ContentAlignment.MiddleRight }, 0, row);
            cboDeinterlace = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 120 };
            cboDeinterlace.Items.AddRange(new[] { "none", "bwdif", "yadif", "kerndeint", "pp=lb", "fieldorder" });
            layout.Controls.Add(cboDeinterlace, 1, row++);

            chkPixFmt = new CheckBox { Text = "指定像素格式", AutoSize = true };
            layout.Controls.Add(chkPixFmt, 0, row);
            cboPixFmt = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 120, Enabled = false };
            cboPixFmt.Items.AddRange(new[] { "yuv420p", "yuv422p", "yuv444p", "yuv420p10le", "yuv422p10le", "yuv444p10le", "p010le", "nv12" });
            layout.Controls.Add(cboPixFmt, 1, row++);

            chkSubtitle = new CheckBox { Text = "烧录字幕", AutoSize = true };
            layout.Controls.Add(chkSubtitle, 0, row);
            var subPanel = new FlowLayoutPanel();
            txtSubtitlePath = new TextBox { Width = 250, Enabled = false };
            btnBrowseSubtitle = new Button { Text = "浏览...", Enabled = false };
            btnBrowseSubtitle.Click += (sender, e) =>
            {
                var dlg = new OpenFileDialog();
                dlg.Filter = "字幕文件|*.srt;*.ass;*.ssa;*.vtt";
                if (dlg.ShowDialog() == DialogResult.OK)
                    txtSubtitlePath.Text = PathHelper.Normalize(dlg.FileName);
            };
            subPanel.Controls.AddRange(new Control[] { txtSubtitlePath, btnBrowseSubtitle });
            layout.Controls.Add(subPanel, 1, row++);

            chkTrim = new CheckBox { Text = "启用截取片段", AutoSize = true };
            layout.Controls.Add(chkTrim, 0, row);
            var trimPanel = new FlowLayoutPanel();
            txtTrimStart = new TextBox { Width = 100, Enabled = false };
            txtTrimEnd = new TextBox { Width = 100, Enabled = false };
            trimPanel.Controls.AddRange(new Control[] { new Label { Text = "开始:" }, txtTrimStart, new Label { Text = "结束:" }, txtTrimEnd });
            layout.Controls.Add(trimPanel, 1, row++);

            page.Controls.Add(layout);
            return page;
        }

        private TabPage CreateAudioTab()
        {
            var page = new TabPage("音频");
            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 7, Padding = new Padding(10) };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            int row = 0;
            chkAudioEnabled = new CheckBox { Text = "保留音频", AutoSize = true, Checked = true };
            layout.Controls.Add(chkAudioEnabled, 0, row);
            layout.Controls.Add(new Panel(), 1, row++);

            chkOnlyAudio = new CheckBox { Text = "仅提取音频", AutoSize = true };
            layout.Controls.Add(chkOnlyAudio, 0, row);
            var formatPanel = new FlowLayoutPanel();
            formatPanel.Controls.Add(new Label { Text = "输出容器:" });
            cboAudioFormat = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 60 };
            cboAudioFormat.Items.AddRange(new[] { "mp3", "aac", "m4a", "flac", "opus", "wav", "ac3" });
            formatPanel.Controls.Add(cboAudioFormat);
            layout.Controls.Add(formatPanel, 1, row++);

            layout.Controls.Add(new Label { Text = "编码器:", TextAlign = ContentAlignment.MiddleRight }, 0, row);
            cboAudioCodec = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 100 };
            cboAudioCodec.Items.AddRange(new[] { "aac", "libmp3lame", "opus", "ac3", "flac", "alac", "pcm_s16le", "copy" });
            cboAudioCodec.SelectedIndex = 0;  // 默认 aac
            layout.Controls.Add(cboAudioCodec, 1, row++);

            layout.Controls.Add(new Label { Text = "比特率:", TextAlign = ContentAlignment.MiddleRight }, 0, row);
            txtAudioBitrate = new TextBox { Text = "128k", Width = 80 };
            layout.Controls.Add(txtAudioBitrate, 1, row++);

            layout.Controls.Add(new Label { Text = "采样率:", TextAlign = ContentAlignment.MiddleRight }, 0, row);
            txtAudioSamplerate = new TextBox { Text = "44100", Width = 80 };
            layout.Controls.Add(txtAudioSamplerate, 1, row++);

            page.Controls.Add(layout);
            return page;
        }

        // 新增：封装/合并/画中画标签页（简单占位，后续可替换为完整控件）
        private TabPage CreateMergeTab()
        {
            var page = new TabPage("封装/合并/画中画");
            var label = new Label
            {
                Text = "此功能正在开发中，请使用独立工具或后续版本。\n拖拽文件可直接添加到任务列表进行转码。",
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill,
                Font = new Font("Microsoft YaHei", 12)
            };
            page.Controls.Add(label);
            return page;
        }

        // ========== 功能方法（与原版本完全相同，未做任何改动） ==========
        private void FindFFmpeg()
        {
            _ffmpegPath = PathHelper.GetExecutablePath("ffmpeg.exe");
            if (string.IsNullOrEmpty(_ffmpegPath))
                AppendInfo("⚠️ 未找到 ffmpeg.exe，请将 ffmpeg 放在程序目录或 PATH 中。");
        }

        private void AppendInfo(string text)
        {
            if (txtInfoLog.InvokeRequired)
                txtInfoLog.Invoke(new Action(() => txtInfoLog.AppendText(text + Environment.NewLine)));
            else
                txtInfoLog.AppendText(text + Environment.NewLine);
        }

        private void AppendDetail(string text)
        {
            if (txtDetailLog.InvokeRequired)
                txtDetailLog.Invoke(new Action(() => txtDetailLog.AppendText(text + Environment.NewLine)));
            else
                txtDetailLog.AppendText(text + Environment.NewLine);
        }

        private VideoSettings GetCurrentSettings()
        {
            var s = new VideoSettings();
            s.Encoder = cboEncoder.SelectedItem?.ToString();
            s.Preset = cboPreset.SelectedItem?.ToString();
            if (rbCRF.Checked) s.RateControlType = "crf";
            else if (rbCQ.Checked) s.RateControlType = "cq";
            else if (rbGlobalQuality.Checked) s.RateControlType = "global_quality";
            else s.RateControlType = "bitrate";
            s.CrfValue = trkCRF.Value;
            s.CqValue = trkCQ.Value;
            s.GlobalQuality = trkGlobalQuality.Value;
            s.BitrateVideo = txtBitrate.Text;
            s.HwaccelEnabled = chkHwaccel.Checked;
            s.HwaccelDecoder = cboHwaccelDecoder.SelectedItem?.ToString();
            s.CustomArgs = txtCustomArgs.Text;
            s.OutputDir = txtOutputDir.Text;
            s.OutputSuffix = txtOutputSuffix.Text;
            s.CustomOutputName = txtCustomOutputName.Text;
            s.OutputContainer = cboOutputContainer.SelectedItem?.ToString();
            s.AudioEnabled = chkAudioEnabled.Checked;
            s.AudioCodec = cboAudioCodec.SelectedItem?.ToString();
            s.AudioBitrate = txtAudioBitrate.Text;
            s.AudioSamplerate = txtAudioSamplerate.Text;
            s.OnlyAudio = chkOnlyAudio.Checked;
            s.AudioFormat = cboAudioFormat.SelectedItem?.ToString();
            s.FrameRateType = cboFrameRateType.SelectedIndex == 0 ? "keep" : "custom";
            s.FrameRateCustom = txtFrameRateCustom.Text;
            s.ScaleEnabled = chkScale.Checked;
            s.ScaleWidth = txtScaleW.Text;
            s.ScaleHeight = txtScaleH.Text;
            s.ScaleMethod = cboScaleMethod.SelectedIndex == 0 ? "width" : (cboScaleMethod.SelectedIndex == 1 ? "height" : "exact");
            s.CropEnabled = chkCrop.Checked;
            s.CropWidth = txtCropW.Text;
            s.CropHeight = txtCropH.Text;
            s.CropLeft = txtCropLeft.Text;
            s.CropTop = txtCropTop.Text;
            int rotIdx = cboRotate.SelectedIndex;
            s.Rotate = rotIdx == 0 ? "none" : (rotIdx == 1 ? "90" : (rotIdx == 2 ? "180" : "270"));
            s.Vflip = chkVflip.Checked;
            s.Hflip = chkHflip.Checked;
            s.SpeedEnabled = chkSpeed.Checked;
            if (double.TryParse(txtSpeedFactor.Text, out double sf)) s.SpeedFactor = sf;
            s.DeinterlaceFilter = cboDeinterlace.SelectedItem?.ToString();
            s.PixFmtEnabled = chkPixFmt.Checked;
            s.PixFmt = cboPixFmt.SelectedItem?.ToString();
            s.SubtitleEnabled = chkSubtitle.Checked;
            s.SubtitlePath = txtSubtitlePath.Text;
            s.TrimEnabled = chkTrim.Checked;
            s.TrimStart = txtTrimStart.Text;
            s.TrimEnd = txtTrimEnd.Text;
            return s;
        }

        private string GenerateOutputPath(string input, VideoSettings s)
        {
            string dir = string.IsNullOrEmpty(s.OutputDir) ? Path.GetDirectoryName(input) : s.OutputDir;
            string baseName = Path.GetFileNameWithoutExtension(input);
            string container = s.OnlyAudio ? s.AudioFormat : s.OutputContainer;
            string custom = s.CustomOutputName?.Trim();
            if (!string.IsNullOrEmpty(custom))
            {
                if (!Path.HasExtension(custom)) custom += "." + container;
                return PathHelper.Normalize(Path.Combine(dir, custom));
            }
            string suffix = s.OutputSuffix?.Trim();
            if (string.IsNullOrEmpty(suffix) && string.Equals(dir, Path.GetDirectoryName(input), StringComparison.OrdinalIgnoreCase))
                suffix = "_new";
            string outName = $"{baseName}{suffix}.{container}";
            return PathHelper.Normalize(Path.Combine(dir, outName));
        }

        private void UpdateCommandPreview()
        {
            if (string.IsNullOrEmpty(txtInputFile.Text))
            {
                txtCommandPreview.Text = "请选择输入文件";
                return;
            }
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

        private void OnEncoderChanged()
        {
            string enc = cboEncoder.SelectedItem?.ToString();
            if (enc == null) return;
            if (enc.Contains("libx") || enc.Contains("libsvtav1"))
                rbCRF.Checked = true;
            else if (enc.Contains("nvenc"))
                rbCQ.Checked = true;
            else if (enc.Contains("qsv"))
                rbGlobalQuality.Checked = true;
            else
                rbBitrate.Checked = true;
            UpdateCommandPreview();
        }

        private void SelectInputFile()
        {
            var dlg = new OpenFileDialog { Filter = "媒体文件|*.mp4;*.mkv;*.avi;*.mov;*.flv;*.webm;*.ts|所有文件|*.*" };
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                txtInputFile.Text = PathHelper.Normalize(dlg.FileName);
                if (string.IsNullOrEmpty(txtOutputDir.Text))
                    txtOutputDir.Text = Path.GetDirectoryName(txtInputFile.Text);
                UpdateCommandPreview();
            }
        }

        private void SelectOutputDir()
        {
            var dlg = new FolderBrowserDialog();
            if (dlg.ShowDialog() == DialogResult.OK)
                txtOutputDir.Text = PathHelper.Normalize(dlg.SelectedPath);
        }

        private void AddCurrentAsTask()
        {
            if (string.IsNullOrEmpty(txtInputFile.Text) || !File.Exists(txtInputFile.Text))
            {
                MessageBox.Show("请先选择有效的输入文件", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            AddTask(txtInputFile.Text, GetCurrentSettings());
        }

        private void AddTask(string input, VideoSettings settings)
        {
            string output = GenerateOutputPath(input, settings);
            if (_tasks.Any(t => t.InputFile == input && t.OutputFile == output))
            {
                MessageBox.Show("任务已存在", "重复任务", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            var builder = new FFmpegCommandBuilder(_ffmpegPath);
            string cmd = builder.BuildCommand(input, output, settings);
            var task = new TaskInfo(input, output, settings, cmd);
            _tasks.Add(task);
            UpdateTaskList();
            AppendInfo($"已添加任务: {Path.GetFileName(input)} -> {output}");
        }

        private void TranscodeSingle()
        {
            if (string.IsNullOrEmpty(txtInputFile.Text) || !File.Exists(txtInputFile.Text))
            {
                MessageBox.Show("请先选择输入文件", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            var settings = GetCurrentSettings();
            string output = GenerateOutputPath(txtInputFile.Text, settings);
            var builder = new FFmpegCommandBuilder(_ffmpegPath);
            string cmd = builder.BuildCommand(txtInputFile.Text, output, settings);
            AppendInfo($"开始单文件转码: {Path.GetFileName(txtInputFile.Text)}");
            AppendInfo($">>> {cmd}");
            Task.Run(() => RunSingleTranscode(cmd, txtInputFile.Text));
        }

        private void RunSingleTranscode(string cmd, string inputName)
        {
            var runner = new FFmpegProcessRunner();
            var result = runner.RunAsync(cmd, CancellationToken.None, (line) => AppendDetail(line)).Result;
            if (result.Success)
                AppendInfo($"✅ 完成: {Path.GetFileName(inputName)}");
            else
                AppendInfo($"❌ 失败: {Path.GetFileName(inputName)} - {result.Error}");
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

        private void RemoveSelectedTasks()
        {
            if (lvTasks.SelectedItems.Count == 0) return;
            var indices = lvTasks.SelectedItems.Cast<ListViewItem>().Select(item => item.Index).OrderByDescending(i => i).ToList();
            foreach (int idx in indices)
            {
                if (_tasks[idx].Status == "转码中")
                    MessageBox.Show($"任务 {Path.GetFileName(_tasks[idx].InputFile)} 正在转码，无法删除", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                else
                    _tasks.RemoveAt(idx);
            }
            UpdateTaskList();
        }

        private void ClearAllTasks()
        {
            if (_isProcessing)
            {
                MessageBox.Show("请先停止队列", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            _tasks.Clear();
            UpdateTaskList();
        }

        private void ClearFinishedTasks()
        {
            _tasks.RemoveAll(t => t.Status == "完成" || t.Status == "失败");
            UpdateTaskList();
        }

        private void EditSelectedTask()
        {
            if (lvTasks.SelectedItems.Count == 0) return;
            int idx = lvTasks.SelectedItems[0].Index;
            var task = _tasks[idx];
            if (task.Status == "转码中")
            {
                MessageBox.Show("任务正在转码，无法编辑", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            using (var dlg = new EditTaskForm(task, _ffmpegPath))
            {
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    UpdateTaskList();
                    AppendInfo($"已编辑任务: {Path.GetFileName(task.InputFile)}");
                }
            }
        }

        private async void StartQueue()
        {
            if (_isProcessing) return;
            var pending = _tasks.Where(t => t.Status == "等待").ToList();
            if (pending.Count == 0)
            {
                AppendInfo("没有等待中的任务");
                return;
            }
            _isProcessing = true;
            _cts = new CancellationTokenSource();
            _hwSemaphore = new SemaphoreSlim(_maxHwParallel);
            AppendInfo($"🚀 启动队列，最大并行: {_maxParallel}，硬编并发限制: {_maxHwParallel}");
            var token = _cts.Token;
            
            var runningTasks = new List<Task>();
            var queue = new Queue<TaskInfo>(pending);
            
            async Task ProcessWithLimit(TaskInfo task)
            {
                bool isHw = IsHardwareEncoder(task.Settings.Encoder);
                if (isHw) await _hwSemaphore.WaitAsync(token);
                try
                {
                    await ProcessTask(task, token);
                }
                finally { if (isHw) _hwSemaphore.Release(); }
            }
            
            while (queue.Count > 0 && !token.IsCancellationRequested)
            {
                while (runningTasks.Count < _maxParallel && queue.Count > 0)
                {
                    var task = queue.Dequeue();
                    var t = ProcessWithLimit(task);
                    runningTasks.Add(t);
                }
                var completed = await Task.WhenAny(runningTasks);
                runningTasks.Remove(completed);
            }
            await Task.WhenAll(runningTasks);
            
            _isProcessing = false;
            _cts?.Dispose();
            _cts = null;
            _hwSemaphore?.Dispose();
            AppendInfo("队列处理完成");
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
            var result = await runner.RunAsync(task.Command, token, (line) => AppendDetail(line));
            task.Status = result.Success ? "完成" : "失败";
            task.ErrorMsg = result.Error;
            AppendInfo(result.Success ? $"✅ 完成: {Path.GetFileName(task.InputFile)}" : $"❌ 失败: {Path.GetFileName(task.InputFile)}");
            UpdateTaskList();
        }

        private void StopQueue()
        {
            if (_isProcessing && _cts != null)
            {
                _cts.Cancel();
                AppendInfo("正在停止队列...");
            }
        }

        private void SavePreset()
        {
            string name = Microsoft.VisualBasic.Interaction.InputBox("请输入预设名称:", "保存预设");
            if (string.IsNullOrWhiteSpace(name)) return;
            var settings = GetCurrentSettings();
            var mgr = new PresetManager(Path.Combine(Application.UserAppDataPath, "ffmpeg_presets.json"));
            var presets = mgr.LoadPresets();
            presets[name] = settings;
            mgr.SavePresets(presets);
            AppendInfo($"预设已保存: {name}");
        }

        private void DeletePreset()
        {
            MessageBox.Show("预设删除功能需要预设列表控件，请自行实现或使用预设管理窗体", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void ExportAllPresets()
        {
            var dlg = new SaveFileDialog { Filter = "JSON文件|*.json", DefaultExt = "json", FileName = "ffmpeg_presets_backup.json" };
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                var mgr = new PresetManager(Path.Combine(Application.UserAppDataPath, "ffmpeg_presets.json"));
                var presets = mgr.LoadPresets();
                string json = Newtonsoft.Json.JsonConvert.SerializeObject(presets, Newtonsoft.Json.Formatting.Indented);
                File.WriteAllText(dlg.FileName, json);
                AppendInfo($"预设已导出: {dlg.FileName}");
            }
        }

        private void ImportPresets()
        {
            var dlg = new OpenFileDialog { Filter = "JSON文件|*.json" };
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                string json = File.ReadAllText(dlg.FileName);
                var imported = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, VideoSettings>>(json);
                if (imported == null) return;
                var mgr = new PresetManager(Path.Combine(Application.UserAppDataPath, "ffmpeg_presets.json"));
                var presets = mgr.LoadPresets();
                foreach (var kv in imported)
                    presets[kv.Key] = kv.Value;
                mgr.SavePresets(presets);
                AppendInfo($"已导入 {imported.Count} 个预设");
            }
        }

        private void LoadSettings()
        {
            // 可加载历史设置
        }

        // ---------- 拖拽事件 ----------
        private void MainForm_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.Copy;
        }

        private void MainForm_DragDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            foreach (string file in files)
            {
                if (File.Exists(file))
                    AddTask(file, GetCurrentSettings());
            }
        }
    }
}
