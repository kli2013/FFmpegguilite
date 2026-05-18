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
        // 布局控件
        private TableLayoutPanel mainLayout;
        private Panel leftContainer;
        private SplitContainer splitVertical;
        private Panel rightPanel;

        // 输入/输出组
        private GroupBox ioGroup;
        private TextBox txtInputFile, txtOutputDir, txtOutputSuffix, txtCustomOutputName;
        private ComboBox cboOutputContainer;
        private Button btnBrowseInput, btnBrowseOutputDir, btnAddTask;

        // 预设组
        private GroupBox presetGroup;
        private ComboBox cboPresetList;
        private Button btnSavePreset, btnDeletePreset, btnExportPresets, btnImportPresets;

        // 标签页
        private TabControl topTabControl;
        private TabPage transcodePage;
        private TabControl transcodeSubTab;
        private TabPage videoEncodingPage, videoFiltersPage, audioPage;

        // 命令预览
        private GroupBox previewGroup;
        private RichTextBox txtCommandPreview;

        // 按钮行
        private FlowLayoutPanel singleRow;
        private Button btnSingleTranscode, btnRefreshPreview;
        private FlowLayoutPanel queueRow;
        private Button btnStartQueue, btnStopQueue, btnRemoveSelected, btnClearAll, btnClearFinished;
        private NumericUpDown nudMaxParallel, nudMaxHwParallel;

        // 视频编码页控件
        private ComboBox cboEncoder, cboPreset;
        private RadioButton rbCRF, rbCQ, rbGlobalQuality, rbBitrate;
        private TrackBar trkCRF, trkCQ, trkGlobalQuality;
        private Label lblCRFValue, lblCQValue, lblGlobalQualityValue;
        private TextBox txtBitrate;
        private CheckBox chkHwaccel;
        private ComboBox cboHwaccelDecoder;
        private TextBox txtCustomArgs;

        // 视频滤镜页控件
        private ComboBox cboFrameRateType;
        private TextBox txtFrameRateCustom;
        private CheckBox chkScale;
        private ComboBox cboScaleMethod;
        private TextBox txtScaleW, txtScaleH;
        private CheckBox chkCrop;
        private TextBox txtCropW, txtCropH, txtCropLeft, txtCropTop;
        private ComboBox cboRotate;
        private CheckBox chkVflip, chkHflip;
        private CheckBox chkSpeed;
        private TextBox txtSpeedFactor;
        private ComboBox cboDeinterlace;
        private CheckBox chkPixFmt;
        private ComboBox cboPixFmt;
        private CheckBox chkSubtitle;
        private TextBox txtSubtitlePath;
        private Button btnBrowseSubtitle;
        private CheckBox chkTrim;
        private TextBox txtTrimStart, txtTrimEnd;

        // 音频页控件
        private CheckBox chkAudioEnabled;
        private ComboBox cboAudioCodec;
        private ComboBox cboAudioBitrate;
        private ComboBox cboAudioSamplerate;
        private CheckBox chkOnlyAudio;
        private ComboBox cboAudioFormat;

        // 日志控件
        private RichTextBox txtInfoLog, txtDetailLog;

        // 任务列表
        private ListView lvTasks;

        // 业务字段
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

            this.AllowDrop = true;
            this.DragEnter += MainForm_DragEnter;
            this.DragDrop += MainForm_DragDrop;

            // 关键修复：所有 SplitContainer 的 MinSize 和 SplitterDistance 必须在 Load 事件中设置
            this.Load += (s, e) =>
            {
                splitVertical.Panel1MinSize = 300;
                splitVertical.Panel2MinSize = 200;
                if (splitVertical.Height > 0)
                    splitVertical.SplitterDistance = (int)(splitVertical.Height * 0.6);
            };
        }

        private void InitializeComponent()
        {
            this.Text = "FFLiteGUI - FFmpeg 多功能工具";
            this.Size = new Size(1350, 900);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MinimumSize = new Size(1100, 700);

            // 主布局：左右两列
            mainLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 420f));

            leftContainer = new Panel { Dock = DockStyle.Fill };
            rightPanel = new Panel { Dock = DockStyle.Fill };
            mainLayout.Controls.Add(leftContainer, 0, 0);
            mainLayout.Controls.Add(rightPanel, 1, 0);
            this.Controls.Add(mainLayout);

            // 左侧垂直分割容器（注意：不在此时设置 MinSize 和 SplitterDistance）
            splitVertical = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal };
            leftContainer.Controls.Add(splitVertical);

            // ========== 上半部：设置区域 ==========
            var topLayout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 6, ColumnCount = 1, Padding = new Padding(3) };
            topLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // 输入/输出
            topLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // 参数预设
            topLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f)); // 标签页
            topLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // 命令预览
            topLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // 单文件编码按钮
            topLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // 队列控制按钮行
            splitVertical.Panel1.Controls.Add(topLayout);

            // ---------- 输入/输出组 ----------
            ioGroup = new GroupBox { Text = "输入 / 输出", Dock = DockStyle.Fill, Padding = new Padding(5) };
            var ioLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 4, Padding = new Padding(3) };
            ioLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            ioLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
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
            topLayout.Controls.Add(ioGroup, 0, 0);

            // ---------- 参数预设组 ----------
            presetGroup = new GroupBox { Text = "参数预设", Dock = DockStyle.Fill, Padding = new Padding(5) };
            var presetLayout = new FlowLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(3) };
            presetLayout.Controls.Add(new Label { Text = "预设名称:" });
            cboPresetList = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 200 };
            cboPresetList.SelectedIndexChanged += (s, e) => LoadSelectedPreset();
            presetLayout.Controls.Add(cboPresetList);
            btnSavePreset = new Button { Text = "保存当前参数为预设", Width = 150 };
            btnSavePreset.Click += (s, e) => SavePreset();
            presetLayout.Controls.Add(btnSavePreset);
            btnDeletePreset = new Button { Text = "删除预设", Width = 100 };
            btnDeletePreset.Click += (s, e) => DeletePreset();
            presetLayout.Controls.Add(btnDeletePreset);
            btnExportPresets = new Button { Text = "导出所有预设", Width = 120 };
            btnExportPresets.Click += (s, e) => ExportAllPresets();
            presetLayout.Controls.Add(btnExportPresets);
            btnImportPresets = new Button { Text = "导入预设", Width = 100 };
            btnImportPresets.Click += (s, e) => ImportPresets();
            presetLayout.Controls.Add(btnImportPresets);
            presetGroup.Controls.Add(presetLayout);
            topLayout.Controls.Add(presetGroup, 0, 1);

            // ---------- 标签页 ----------
            topTabControl = new TabControl { Dock = DockStyle.Fill };
            transcodePage = new TabPage("视频转码");
            transcodeSubTab = new TabControl { Dock = DockStyle.Fill };
            videoEncodingPage = new TabPage("视频编码");
            videoFiltersPage = new TabPage("视频滤镜");
            audioPage = new TabPage("音频");
            transcodeSubTab.TabPages.Add(videoEncodingPage);
            transcodeSubTab.TabPages.Add(videoFiltersPage);
            transcodeSubTab.TabPages.Add(audioPage);
            transcodePage.Controls.Add(transcodeSubTab);
            topTabControl.TabPages.Add(transcodePage);
            var mergePage = new TabPage("封装/合并/画中画");
            mergePage.Controls.Add(new Label { Text = "此功能正在开发中", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter });
            topTabControl.TabPages.Add(mergePage);
            topLayout.Controls.Add(topTabControl, 0, 2);

            // 创建子页内容
            CreateVideoEncodingTab();
            CreateVideoFiltersTab();
            CreateAudioTab();

            // ---------- 命令预览 ----------
            previewGroup = new GroupBox { Text = "当前命令模板", Dock = DockStyle.Fill, Padding = new Padding(3) };
            txtCommandPreview = new RichTextBox { Dock = DockStyle.Fill, ReadOnly = true, Font = new Font("Consolas", 9), BackColor = Color.LightYellow };
            previewGroup.Controls.Add(txtCommandPreview);
            topLayout.Controls.Add(previewGroup, 0, 3);

            // ---------- 单文件转码按钮行 ----------
            singleRow = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, Padding = new Padding(3), Height = 40 };
            btnSingleTranscode = new Button { Text = "开始编码", BackColor = Color.LightGreen, Width = 120 };
            btnSingleTranscode.Click += (s, e) => TranscodeSingle();
            singleRow.Controls.Add(btnSingleTranscode);
            btnRefreshPreview = new Button { Text = "刷新命令预览", Width = 120 };
            btnRefreshPreview.Click += (s, e) => UpdateCommandPreview();
            singleRow.Controls.Add(btnRefreshPreview);
            topLayout.Controls.Add(singleRow, 0, 4);

            // ---------- 队列控制行 ----------
            queueRow = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, Padding = new Padding(3), Height = 40, WrapContents = false };
            btnStartQueue = new Button { Text = "开始队列", BackColor = Color.LightGreen, Width = 100 };
            btnStartQueue.Click += (s, e) => StartQueue();
            queueRow.Controls.Add(btnStartQueue);
            btnStopQueue = new Button { Text = "停止队列", BackColor = Color.LightCoral, Width = 100 };
            btnStopQueue.Click += (s, e) => StopQueue();
            queueRow.Controls.Add(btnStopQueue);
            btnRemoveSelected = new Button { Text = "移除选中", Width = 100 };
            btnRemoveSelected.Click += (s, e) => RemoveSelectedTasks();
            queueRow.Controls.Add(btnRemoveSelected);
            btnClearAll = new Button { Text = "清空全部", Width = 100 };
            btnClearAll.Click += (s, e) => ClearAllTasks();
            queueRow.Controls.Add(btnClearAll);
            btnClearFinished = new Button { Text = "清空已完成/失败", Width = 120 };
            btnClearFinished.Click += (s, e) => ClearFinishedTasks();
            queueRow.Controls.Add(btnClearFinished);
            queueRow.Controls.Add(new Label { Text = "并行任务数:" });
            nudMaxParallel = new NumericUpDown { Minimum = 1, Maximum = 5, Value = 2, Width = 50 };
            nudMaxParallel.ValueChanged += (s, e) => _maxParallel = (int)nudMaxParallel.Value;
            queueRow.Controls.Add(nudMaxParallel);
            queueRow.Controls.Add(new Label { Text = "硬编并发限制:" });
            nudMaxHwParallel = new NumericUpDown { Minimum = 1, Maximum = 4, Value = 2, Width = 50 };
            nudMaxHwParallel.ValueChanged += (s, e) => _maxHwParallel = (int)nudMaxHwParallel.Value;
            queueRow.Controls.Add(nudMaxHwParallel);
            topLayout.Controls.Add(queueRow, 0, 5);

            // ========== 下半部：任务列表 ==========
            var taskGroup = new GroupBox { Text = "任务队列", Dock = DockStyle.Fill, Padding = new Padding(3) };
            lvTasks = new ListView { Dock = DockStyle.Fill, View = View.Details, FullRowSelect = true, GridLines = true };
            lvTasks.Columns.Add("文件名", 150);
            lvTasks.Columns.Add("输出路径", 250);
            lvTasks.Columns.Add("命令(简洁)", 350);
            lvTasks.Columns.Add("状态", 80);
            lvTasks.Columns.Add("错误信息", 200);
            lvTasks.DoubleClick += (s, e) => EditSelectedTask();
            taskGroup.Controls.Add(lvTasks);
            splitVertical.Panel2.Controls.Add(taskGroup);

            // ========== 右侧：日志区域 ==========
            var rightLayout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1, Padding = new Padding(5) };
            rightLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
            rightLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50));

            var infoGroup = new GroupBox { Text = "关键信息", Dock = DockStyle.Fill };
            txtInfoLog = new RichTextBox { Dock = DockStyle.Fill, ReadOnly = true, BackColor = Color.White, Font = new Font("Consolas", 9) };
            infoGroup.Controls.Add(txtInfoLog);
            rightLayout.Controls.Add(infoGroup, 0, 0);

            var detailGroup = new GroupBox { Text = "转换进程信息", Dock = DockStyle.Fill };
            txtDetailLog = new RichTextBox { Dock = DockStyle.Fill, ReadOnly = true, BackColor = Color.White, Font = new Font("Consolas", 8) };
            detailGroup.Controls.Add(txtDetailLog);
            rightLayout.Controls.Add(detailGroup, 0, 1);

            rightPanel.Controls.Add(rightLayout);

            BindPreviewEvents();
        }

        private void CreateVideoEncodingTab()
        {
            // 整体布局：左侧编码参数、右侧高级选项（各占50%）
            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Padding = new Padding(5) };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45));
        
            // ---------- 左侧：编码参数（固定行高） ----------
            var leftGroup = new GroupBox { Text = "编码参数", Dock = DockStyle.Fill, Padding = new Padding(5) };
            // 使用 TableLayoutPanel 并指定绝对行高，确保控件不被挤压
            var leftLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 4, Padding = new Padding(5) };
            // 第0行：编码器+预设（合并为一格）高度 35
            leftLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 35));
            // 第1行：码率控制单选按钮 高度 35
            leftLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 35));
            // 第2行：动态面板（滑块或比特率）高度 45
            leftLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 45));
            // 第3行：留白填充（可选，也可不加）
            leftLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        
            // 设置列宽
            leftLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));  // 标签列
            leftLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); // 控件列
        
            // ---------- 第0行：编码器 + 预设 放在同一行 ----------
            // 编码器标签
            leftLayout.Controls.Add(new Label { Text = "编码器:", TextAlign = ContentAlignment.MiddleRight }, 0, 0);
            // 编码器和预设的容器（FlowLayoutPanel 水平排列）
            var encoderPresetPanel = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, Dock = DockStyle.Fill, Margin = new Padding(0) };
            cboEncoder = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 150 };
            cboEncoder.Items.AddRange(new[] { "libx264", "libx265", "libvpx-vp9", "libsvtav1", "mpeg4", "libxvid", "libtheora",
                "h264_nvenc", "hevc_nvenc", "av1_nvenc", "h264_qsv", "hevc_qsv", "av1_qsv",
                "h264_amf", "hevc_amf", "av1_amf", "h264_vaapi", "hevc_vaapi", "copy" });
            cboEncoder.SelectedIndex = 1;
            encoderPresetPanel.Controls.Add(cboEncoder);
            encoderPresetPanel.Controls.Add(new Label { Text = "预设:", TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(10, 0, 5, 0) });
            cboPreset = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 100 };
            cboPreset.Items.AddRange(new[] { "ultrafast", "superfast", "veryfast", "faster", "fast", "medium", "slow", "slower", "veryslow", "p1", "p2", "p3", "p4", "p5", "p6", "p7" });
            cboPreset.SelectedIndex = 5;
            encoderPresetPanel.Controls.Add(cboPreset);
            leftLayout.Controls.Add(encoderPresetPanel, 1, 0);
        
            // ---------- 第1行：码率控制单选按钮 ----------
            leftLayout.Controls.Add(new Label { Text = "码率控制:", TextAlign = ContentAlignment.MiddleRight }, 0, 1);
            var rcPanel = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, Dock = DockStyle.Fill };
            rbCRF = new RadioButton { Text = "CRF (CPU)", AutoSize = true, Checked = true };
            rbCQ = new RadioButton { Text = "CQ (NVENC)", AutoSize = true };
            rbGlobalQuality = new RadioButton { Text = "Global Quality (QSV)", AutoSize = true };
            rbBitrate = new RadioButton { Text = "固定比特率", AutoSize = true };
            rcPanel.Controls.AddRange(new Control[] { rbCRF, rbCQ, rbGlobalQuality, rbBitrate });
            leftLayout.Controls.Add(rcPanel, 1, 1);
        
            // ---------- 第2行：动态面板（滑块/比特率） ----------
            var dynamicPanel = new Panel { Dock = DockStyle.Fill };
            leftLayout.Controls.Add(dynamicPanel, 1, 2);
            // 创建各个子面板
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
        
            // 绑定事件（与原来相同）
            rbCRF.CheckedChanged += (s, e) => { crfPanel.Visible = rbCRF.Checked; UpdateCommandPreview(); };
            rbCQ.CheckedChanged += (s, e) => { cqPanel.Visible = rbCQ.Checked; UpdateCommandPreview(); };
            rbGlobalQuality.CheckedChanged += (s, e) => { gqPanel.Visible = rbGlobalQuality.Checked; UpdateCommandPreview(); };
            rbBitrate.CheckedChanged += (s, e) => { bitPanel.Visible = rbBitrate.Checked; UpdateCommandPreview(); };
            trkCRF.ValueChanged += (s, e) => { lblCRFValue.Text = trkCRF.Value.ToString(); UpdateCommandPreview(); };
            trkCQ.ValueChanged += (s, e) => { lblCQValue.Text = trkCQ.Value.ToString(); UpdateCommandPreview(); };
            trkGlobalQuality.ValueChanged += (s, e) => { lblGlobalQualityValue.Text = trkGlobalQuality.Value.ToString(); UpdateCommandPreview(); };
            txtBitrate.TextChanged += (s, e) => UpdateCommandPreview();
        
            leftGroup.Controls.Add(leftLayout);
            layout.Controls.Add(leftGroup, 0, 0);
        
            // ---------- 右侧：高级选项（保持原有布局，但也可以固定高度） ----------
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
        
            videoEncodingPage.Controls.Add(layout);
        
            // 可选：为 videoEncodingPage 设置固定高度（用户可手动修改此处数值）
            // 注意：如果不希望外部分割条影响，可以注释掉下面这行，让父容器决定。
            // videoEncodingPage.AutoScroll = true;  // 如果内容超出则滚动
            // 或者设置 MinimumSize 确保一定高度
            videoEncodingPage.AutoScroll = true;
        }

        private void CreateVideoFiltersTab()
        {
            // 主布局：左右两列（左侧70%用于所有滤镜设置，右侧30%用于截取片段）
            var mainLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Padding = new Padding(5) };
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 70));
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30));
        
            // ---------- 左侧：所有滤镜设置（垂直排列，行高自动适应） ----------
            var leftLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 5, Padding = new Padding(3) };
            leftLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));  // 标签/复选框列
            leftLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); // 控件列
            // 设置行高为 AutoSize
            for (int i = 0; i < 5; i++)
                leftLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        
            int row = 0;
        
            // ----- 第0行：帧率设置 + 烧录字幕（合并到一行）-----
            // 左侧添加一个空白占位（因为这一行没有左侧标签）
            leftLayout.Controls.Add(new Panel(), 0, row);
            var topLinePanel = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, Dock = DockStyle.Fill, WrapContents = false };
            // 帧率部分
            topLinePanel.Controls.Add(new Label { Text = "帧率:", TextAlign = ContentAlignment.MiddleRight });
            cboFrameRateType = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 80 };
            cboFrameRateType.Items.AddRange(new[] { "保持源", "指定" });
            topLinePanel.Controls.Add(cboFrameRateType);
            txtFrameRateCustom = new TextBox { Text = "30", Width = 50, Enabled = false };
            topLinePanel.Controls.Add(txtFrameRateCustom);
            topLinePanel.Controls.Add(new Label { Text = "fps" });
            // 分隔间距
            topLinePanel.Controls.Add(new Label { Text = "    ", Width = 20 });
            // 烧录字幕部分
            chkSubtitle = new CheckBox { Text = "烧录字幕", AutoSize = true };
            topLinePanel.Controls.Add(chkSubtitle);
            txtSubtitlePath = new TextBox { Width = 200, Enabled = false };
            topLinePanel.Controls.Add(txtSubtitlePath);
            btnBrowseSubtitle = new Button { Text = "浏览...", Width = 60, Enabled = false };
            btnBrowseSubtitle.Click += (sender, e) =>
            {
                var dlg = new OpenFileDialog();
                dlg.Filter = "字幕文件|*.srt;*.ass;*.ssa;*.vtt";
                if (dlg.ShowDialog() == DialogResult.OK)
                    txtSubtitlePath.Text = PathHelper.Normalize(dlg.FileName);
            };
            topLinePanel.Controls.Add(btnBrowseSubtitle);
            leftLayout.Controls.Add(topLinePanel, 1, row);
            row++;
        
            // ----- 第1行：缩放设置 -----
            leftLayout.Controls.Add(chkScale = new CheckBox { Text = "启用缩放", AutoSize = true }, 0, row);
            var scalePanel = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, Dock = DockStyle.Fill, WrapContents = false };
            cboScaleMethod = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 120 };
            cboScaleMethod.Items.AddRange(new[] { "宽度(高度自动)", "高度(宽度自动)", "精确宽×高" });
            txtScaleW = new TextBox { Width = 50 };
            txtScaleH = new TextBox { Width = 50, Enabled = false };
            cboScaleMethod.SelectedIndexChanged += (s, e) =>
            {
                int idx = cboScaleMethod.SelectedIndex;
                txtScaleW.Enabled = (idx == 0 || idx == 2);
                txtScaleH.Enabled = (idx == 1 || idx == 2);
                if (idx == 0) txtScaleH.Text = "";
                if (idx == 1) txtScaleW.Text = "";
                UpdateCommandPreview();
            };
            cboScaleMethod.SelectedIndex = 0;
            txtScaleW.Enabled = true;
            txtScaleH.Enabled = false;
            scalePanel.Controls.Add(cboScaleMethod);
            scalePanel.Controls.Add(new Label { Text = "宽:" });
            scalePanel.Controls.Add(txtScaleW);
            scalePanel.Controls.Add(new Label { Text = "高:" });
            scalePanel.Controls.Add(txtScaleH);
            leftLayout.Controls.Add(scalePanel, 1, row);
            row++;
        
            // ----- 第2行：裁剪设置 -----
            leftLayout.Controls.Add(chkCrop = new CheckBox { Text = "启用裁剪", AutoSize = true }, 0, row);
            var cropPanel = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, Dock = DockStyle.Fill, WrapContents = false };
            txtCropW = new TextBox { Width = 60 };
            txtCropH = new TextBox { Width = 60 };
            txtCropLeft = new TextBox { Width = 50 };
            txtCropTop = new TextBox { Width = 50 };
            cropPanel.Controls.Add(new Label { Text = "宽:" });
            cropPanel.Controls.Add(txtCropW);
            cropPanel.Controls.Add(new Label { Text = "高:" });
            cropPanel.Controls.Add(txtCropH);
            cropPanel.Controls.Add(new Label { Text = "左:" });
            cropPanel.Controls.Add(txtCropLeft);
            cropPanel.Controls.Add(new Label { Text = "上:" });
            cropPanel.Controls.Add(txtCropTop);
            leftLayout.Controls.Add(cropPanel, 1, row);
            row++;
        
            // ----- 第3行：旋转 + 翻转 -----
            leftLayout.Controls.Add(new Panel(), 0, row); // 占位
            var rotateFlipPanel = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, Dock = DockStyle.Fill, WrapContents = false };
            rotateFlipPanel.Controls.Add(new Label { Text = "旋转:" });
            cboRotate = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 100 };
            cboRotate.Items.AddRange(new[] { "无", "90°顺时针", "180°", "90°逆时针" });
            cboRotate.SelectedIndex = 0;
            rotateFlipPanel.Controls.Add(cboRotate);
            rotateFlipPanel.Controls.Add(new Label { Text = "    翻转:" });
            chkVflip = new CheckBox { Text = "上下翻转", AutoSize = true };
            chkHflip = new CheckBox { Text = "左右翻转", AutoSize = true };
            rotateFlipPanel.Controls.Add(chkVflip);
            rotateFlipPanel.Controls.Add(chkHflip);
            leftLayout.Controls.Add(rotateFlipPanel, 1, row);
            row++;
        
            // ----- 第4行：变速 + 反交错 + 像素格式 -----
            leftLayout.Controls.Add(new Panel(), 0, row); // 占位
            var miscPanel = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, Dock = DockStyle.Fill, WrapContents = false };
            // 变速
            chkSpeed = new CheckBox { Text = "启用变速", AutoSize = true };
            miscPanel.Controls.Add(chkSpeed);
            txtSpeedFactor = new TextBox { Text = "1.0", Width = 60 };
            miscPanel.Controls.Add(new Label { Text = "速度倍数:" });
            miscPanel.Controls.Add(txtSpeedFactor);
            miscPanel.Controls.Add(new Label { Text = "    " });
            // 反交错
            miscPanel.Controls.Add(new Label { Text = "反交错:" });
            cboDeinterlace = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 100 };
            cboDeinterlace.Items.AddRange(new[] { "none", "bwdif", "yadif", "kerndeint", "pp=lb", "fieldorder" });
            cboDeinterlace.SelectedIndex = 0;
            miscPanel.Controls.Add(cboDeinterlace);
            miscPanel.Controls.Add(new Label { Text = "    " });
            // 像素格式
            chkPixFmt = new CheckBox { Text = "指定像素格式", AutoSize = true };
            miscPanel.Controls.Add(chkPixFmt);
            cboPixFmt = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 100, Enabled = false };
            cboPixFmt.Items.AddRange(new[] { "yuv420p", "yuv422p", "yuv444p", "yuv420p10le", "yuv422p10le", "yuv444p10le", "p010le", "nv12" });
            miscPanel.Controls.Add(cboPixFmt);
            leftLayout.Controls.Add(miscPanel, 1, row);
            row++;
        
            // 将左侧布局添加到主布局
            mainLayout.Controls.Add(leftLayout, 0, 0);
        
            // ---------- 右侧：截取片段（跨所有行，垂直居中） ----------
            var trimGroup = new GroupBox { Text = "截取片段", Dock = DockStyle.Fill, Padding = new Padding(5) };
            var trimLayout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3, ColumnCount = 1, Padding = new Padding(5) };
            trimLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            trimLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            trimLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        
            chkTrim = new CheckBox { Text = "启用截取片段", AutoSize = true };
            trimLayout.Controls.Add(chkTrim, 0, 0);
            var startPanel = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight };
            startPanel.Controls.Add(new Label { Text = "开始:" });
            txtTrimStart = new TextBox { Width = 100, Enabled = false };
            startPanel.Controls.Add(txtTrimStart);
            trimLayout.Controls.Add(startPanel, 0, 1);
            var endPanel = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight };
            endPanel.Controls.Add(new Label { Text = "结束:" });
            txtTrimEnd = new TextBox { Width = 100, Enabled = false };
            endPanel.Controls.Add(txtTrimEnd);
            trimLayout.Controls.Add(endPanel, 0, 2);
        
            chkTrim.CheckedChanged += (s, e) =>
            {
                bool enabled = chkTrim.Checked;
                txtTrimStart.Enabled = enabled;
                txtTrimEnd.Enabled = enabled;
                UpdateCommandPreview();
            };
            trimGroup.Controls.Add(trimLayout);
            mainLayout.Controls.Add(trimGroup, 1, 0);
        
            videoFiltersPage.Controls.Add(mainLayout);
        
            // 辅助事件绑定（保持原有功能）
            chkSubtitle.CheckedChanged += (s, e) =>
            {
                bool enabled = chkSubtitle.Checked;
                txtSubtitlePath.Enabled = enabled;
                btnBrowseSubtitle.Enabled = enabled;
                UpdateCommandPreview();
            };
            chkPixFmt.CheckedChanged += (s, e) =>
            {
                cboPixFmt.Enabled = chkPixFmt.Checked;
                UpdateCommandPreview();
            };
            chkScale.CheckedChanged += (s, e) => UpdateCommandPreview();
            chkCrop.CheckedChanged += (s, e) => UpdateCommandPreview();
            chkSpeed.CheckedChanged += (s, e) => UpdateCommandPreview();
            cboFrameRateType.SelectedIndexChanged += (s, e) =>
            {
                txtFrameRateCustom.Enabled = cboFrameRateType.SelectedIndex == 1;
                UpdateCommandPreview();
            };
            txtFrameRateCustom.TextChanged += (s, e) => UpdateCommandPreview();
            txtScaleW.TextChanged += (s, e) => UpdateCommandPreview();
            txtScaleH.TextChanged += (s, e) => UpdateCommandPreview();
            txtCropW.TextChanged += (s, e) => UpdateCommandPreview();
            txtCropH.TextChanged += (s, e) => UpdateCommandPreview();
            txtCropLeft.TextChanged += (s, e) => UpdateCommandPreview();
            txtCropTop.TextChanged += (s, e) => UpdateCommandPreview();
            cboRotate.SelectedIndexChanged += (s, e) => UpdateCommandPreview();
            chkVflip.CheckedChanged += (s, e) => UpdateCommandPreview();
            chkHflip.CheckedChanged += (s, e) => UpdateCommandPreview();
            txtSpeedFactor.TextChanged += (s, e) => UpdateCommandPreview();
            cboDeinterlace.SelectedIndexChanged += (s, e) => UpdateCommandPreview();
            cboPixFmt.SelectedIndexChanged += (s, e) => UpdateCommandPreview();
            txtSubtitlePath.TextChanged += (s, e) => UpdateCommandPreview();
            txtTrimStart.TextChanged += (s, e) => UpdateCommandPreview();
            txtTrimEnd.TextChanged += (s, e) => UpdateCommandPreview();
        }

        private void CreateAudioTab()
        {
            // 主布局：两行（使用 FlowLayoutPanel 实现水平排列）
            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Padding = new Padding(10) };
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        
            // ---------- 第0行：保留音频 + 仅提取音频 + 输出容器 ----------
            var row0Panel = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, WrapContents = false, Dock = DockStyle.Fill };
            chkAudioEnabled = new CheckBox { Text = "保留音频", AutoSize = true, Checked = true };
            row0Panel.Controls.Add(chkAudioEnabled);
            chkOnlyAudio = new CheckBox { Text = "仅提取音频", AutoSize = true };
            row0Panel.Controls.Add(chkOnlyAudio);
            row0Panel.Controls.Add(new Label { Text = "输出容器:", TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(10, 0, 5, 0) });
            cboAudioFormat = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 80 };
            cboAudioFormat.Items.AddRange(new[] { "mp3", "aac", "m4a", "flac", "opus", "wav", "ac3" });
            cboAudioFormat.SelectedIndex = 0;
            row0Panel.Controls.Add(cboAudioFormat);
            layout.Controls.Add(row0Panel, 0, 0);
        
            // ---------- 第1行：编码器 + 比特率 + 采样率 ----------
            var row1Panel = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, WrapContents = false, Dock = DockStyle.Fill };
            row1Panel.Controls.Add(new Label { Text = "编码器:", TextAlign = ContentAlignment.MiddleRight });
            cboAudioCodec = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 100 };
            cboAudioCodec.Items.AddRange(new[] { "aac", "libmp3lame", "opus", "ac3", "flac", "alac", "pcm_s16le", "copy" });
            cboAudioCodec.SelectedIndex = 0;
            row1Panel.Controls.Add(cboAudioCodec);
            row1Panel.Controls.Add(new Label { Text = "比特率:", TextAlign = ContentAlignment.MiddleRight, Padding = new Padding(10, 0, 5, 0) });
            cboAudioBitrate = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 80 };
            cboAudioBitrate.Items.AddRange(new[] { "64k", "96k", "128k", "192k", "256k", "320k" });
            cboAudioBitrate.SelectedItem = "128k";
            row1Panel.Controls.Add(cboAudioBitrate);
            row1Panel.Controls.Add(new Label { Text = "采样率:", TextAlign = ContentAlignment.MiddleRight, Padding = new Padding(10, 0, 5, 0) });
            cboAudioSamplerate = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 90 };
            cboAudioSamplerate.Items.AddRange(new[] { "8000", "12000", "16000", "22050", "32000", "44100", "48000", "96000" });
            cboAudioSamplerate.SelectedItem = "44100";
            row1Panel.Controls.Add(cboAudioSamplerate);
            layout.Controls.Add(row1Panel, 0, 1);
        
            audioPage.Controls.Add(layout);
        
            // 绑定事件
            chkAudioEnabled.CheckedChanged += (s, e) => UpdateCommandPreview();
            chkOnlyAudio.CheckedChanged += (s, e) => UpdateCommandPreview();
            cboAudioFormat.SelectedIndexChanged += (s, e) => UpdateCommandPreview();
            cboAudioCodec.SelectedIndexChanged += (s, e) => UpdateCommandPreview();
            cboAudioBitrate.SelectedIndexChanged += (s, e) => UpdateCommandPreview();
            cboAudioSamplerate.SelectedIndexChanged += (s, e) => UpdateCommandPreview();
        }

        private void BindPreviewEvents()
        {
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
            cboAudioBitrate.SelectedIndexChanged += (s, e) => UpdateCommandPreview();
            cboAudioSamplerate.SelectedIndexChanged += (s, e) => UpdateCommandPreview();
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

        // ================= 业务方法 =================
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
            s.AudioBitrate = cboAudioBitrate.SelectedItem?.ToString() ?? "128k";
            s.AudioSamplerate = cboAudioSamplerate.SelectedItem?.ToString() ?? "44100";
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
            if (txtInputFile == null || txtCommandPreview == null) return;
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

        private void LoadPresetList()
        {
            var mgr = new PresetManager(Path.Combine(Application.UserAppDataPath, "ffmpeg_presets.json"));
            var presets = mgr.LoadPresets();
            cboPresetList.Items.Clear();
            foreach (var name in presets.Keys)
                cboPresetList.Items.Add(name);
            if (cboPresetList.Items.Count > 0)
                cboPresetList.SelectedIndex = 0;
        }

        private void LoadSelectedPreset()
        {
            string name = cboPresetList.SelectedItem?.ToString();
            if (string.IsNullOrEmpty(name)) return;
            var mgr = new PresetManager(Path.Combine(Application.UserAppDataPath, "ffmpeg_presets.json"));
            var presets = mgr.LoadPresets();
            if (presets.TryGetValue(name, out var settings))
                LoadSettingsIntoUI(settings);
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
            LoadPresetList();
            AppendInfo($"预设已保存: {name}");
        }

        private void DeletePreset()
        {
            string name = cboPresetList.SelectedItem?.ToString();
            if (string.IsNullOrEmpty(name)) return;
            if (MessageBox.Show($"确定删除预设 '{name}' 吗？", "确认", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                var mgr = new PresetManager(Path.Combine(Application.UserAppDataPath, "ffmpeg_presets.json"));
                var presets = mgr.LoadPresets();
                if (presets.Remove(name))
                {
                    mgr.SavePresets(presets);
                    LoadPresetList();
                    AppendInfo($"预设已删除: {name}");
                }
            }
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
                LoadPresetList();
                AppendInfo($"已导入 {imported.Count} 个预设");
            }
        }

        private void LoadSettingsIntoUI(VideoSettings settings)
        {
            cboEncoder.SelectedItem = settings.Encoder;
            cboPreset.SelectedItem = settings.Preset;
            if (settings.RateControlType == "crf") rbCRF.Checked = true;
            else if (settings.RateControlType == "cq") rbCQ.Checked = true;
            else if (settings.RateControlType == "global_quality") rbGlobalQuality.Checked = true;
            else rbBitrate.Checked = true;
            trkCRF.Value = settings.CrfValue;
            trkCQ.Value = settings.CqValue;
            trkGlobalQuality.Value = settings.GlobalQuality;
            txtBitrate.Text = settings.BitrateVideo;
            chkHwaccel.Checked = settings.HwaccelEnabled;
            cboHwaccelDecoder.SelectedItem = settings.HwaccelDecoder;
            txtCustomArgs.Text = settings.CustomArgs;
            txtOutputDir.Text = settings.OutputDir;
            txtOutputSuffix.Text = settings.OutputSuffix;
            txtCustomOutputName.Text = settings.CustomOutputName;
            cboOutputContainer.SelectedItem = settings.OutputContainer;
            chkAudioEnabled.Checked = settings.AudioEnabled;
            cboAudioCodec.SelectedItem = settings.AudioCodec;
            cboAudioBitrate.SelectedItem = settings.AudioBitrate;
            cboAudioSamplerate.SelectedItem = settings.AudioSamplerate;
            chkOnlyAudio.Checked = settings.OnlyAudio;
            cboAudioFormat.SelectedItem = settings.AudioFormat;
            cboFrameRateType.SelectedIndex = settings.FrameRateType == "keep" ? 0 : 1;
            txtFrameRateCustom.Text = settings.FrameRateCustom;
            chkScale.Checked = settings.ScaleEnabled;
            txtScaleW.Text = settings.ScaleWidth;
            txtScaleH.Text = settings.ScaleHeight;
            if (settings.ScaleMethod == "width") cboScaleMethod.SelectedIndex = 0;
            else if (settings.ScaleMethod == "height") cboScaleMethod.SelectedIndex = 1;
            else cboScaleMethod.SelectedIndex = 2;
            chkCrop.Checked = settings.CropEnabled;
            txtCropW.Text = settings.CropWidth;
            txtCropH.Text = settings.CropHeight;
            txtCropLeft.Text = settings.CropLeft;
            txtCropTop.Text = settings.CropTop;
            if (settings.Rotate == "none") cboRotate.SelectedIndex = 0;
            else if (settings.Rotate == "90") cboRotate.SelectedIndex = 1;
            else if (settings.Rotate == "180") cboRotate.SelectedIndex = 2;
            else cboRotate.SelectedIndex = 3;
            chkVflip.Checked = settings.Vflip;
            chkHflip.Checked = settings.Hflip;
            chkSpeed.Checked = settings.SpeedEnabled;
            txtSpeedFactor.Text = settings.SpeedFactor.ToString();
            cboDeinterlace.SelectedItem = settings.DeinterlaceFilter;
            chkPixFmt.Checked = settings.PixFmtEnabled;
            cboPixFmt.SelectedItem = settings.PixFmt;
            chkSubtitle.Checked = settings.SubtitleEnabled;
            txtSubtitlePath.Text = settings.SubtitlePath;
            chkTrim.Checked = settings.TrimEnabled;
            txtTrimStart.Text = settings.TrimStart;
            txtTrimEnd.Text = settings.TrimEnd;
        }

        private void LoadSettings()
        {
            LoadPresetList();
        }

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
