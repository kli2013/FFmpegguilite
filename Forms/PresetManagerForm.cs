using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Newtonsoft.Json;
using FFLiteGUI.Models;

namespace FFLiteGUI.Forms
{
    public partial class PresetManagerForm : Form
    {
        private readonly string _presetFilePath;
        private Dictionary<string, VideoSettings> _presets;
        private ListBox _presetListBox;
        private Button _loadButton;
        private Button _deleteButton;
        private Button _renameButton;
        private Button _exportButton;
        private Button _importButton;
        private Button _closeButton;

        public string SelectedPresetName { get; private set; }
        public VideoSettings SelectedPresetSettings { get; private set; }

        public PresetManagerForm(string presetFilePath)
        {
            _presetFilePath = presetFilePath;
            InitializeComponents();
            LoadPresets();
        }

        private void InitializeComponents()
        {
            this.Text = "预设管理器";
            this.Size = new System.Drawing.Size(500, 400);
            this.StartPosition = FormStartPosition.CenterParent;
            this.MinimumSize = new System.Drawing.Size(400, 300);

            var mainPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 2,
                Padding = new Padding(10)
            };
            mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            _presetListBox = new ListBox { Dock = DockStyle.Fill };
            _presetListBox.DoubleClick += OnPresetDoubleClick;
            mainPanel.Controls.Add(_presetListBox, 0, 0);

            var buttonPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                Padding = new Padding(5)
            };

            _loadButton = new Button { Text = "加载预设", Width = 100, Margin = new Padding(0, 0, 0, 5) };
            _loadButton.Click += OnLoadClick;
            buttonPanel.Controls.Add(_loadButton);

            _deleteButton = new Button { Text = "删除预设", Width = 100, Margin = new Padding(0, 0, 0, 5) };
            _deleteButton.Click += OnDeleteClick;
            buttonPanel.Controls.Add(_deleteButton);

            _renameButton = new Button { Text = "重命名", Width = 100, Margin = new Padding(0, 0, 0, 5) };
            _renameButton.Click += OnRenameClick;
            buttonPanel.Controls.Add(_renameButton);

            _exportButton = new Button { Text = "导出选中", Width = 100, Margin = new Padding(0, 0, 0, 5) };
            _exportButton.Click += OnExportClick;
            buttonPanel.Controls.Add(_exportButton);

            _importButton = new Button { Text = "导入预设", Width = 100, Margin = new Padding(0, 0, 0, 5) };
            _importButton.Click += OnImportClick;
            buttonPanel.Controls.Add(_importButton);

            mainPanel.Controls.Add(buttonPanel, 1, 0);

            var closeButtonPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft
            };
            _closeButton = new Button { Text = "关闭", Width = 80 };
            _closeButton.Click += (s, e) => this.DialogResult = DialogResult.Cancel;
            closeButtonPanel.Controls.Add(_closeButton);
            mainPanel.Controls.Add(closeButtonPanel, 1, 1);
            mainPanel.SetColumnSpan(closeButtonPanel, 2);

            this.Controls.Add(mainPanel);
        }

        private void LoadPresets()
        {
            _presets = new Dictionary<string, VideoSettings>();
            if (File.Exists(_presetFilePath))
            {
                try
                {
                    string json = File.ReadAllText(_presetFilePath);
                    _presets = JsonConvert.DeserializeObject<Dictionary<string, VideoSettings>>(json) ?? new Dictionary<string, VideoSettings>();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"读取预设文件失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            RefreshPresetList();
        }

        private void RefreshPresetList()
        {
            _presetListBox.Items.Clear();
            foreach (var name in _presets.Keys.OrderBy(n => n))
            {
                _presetListBox.Items.Add(name);
            }
            if (_presetListBox.Items.Count > 0)
                _presetListBox.SelectedIndex = 0;
        }

        private void OnPresetDoubleClick(object sender, EventArgs e)
        {
            if (_presetListBox.SelectedItem != null)
            {
                LoadSelectedPreset();
            }
        }

        private void OnLoadClick(object sender, EventArgs e)
        {
            LoadSelectedPreset();
        }

        private void LoadSelectedPreset()
        {
            if (_presetListBox.SelectedItem == null)
            {
                MessageBox.Show("请先选择一个预设", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            string name = _presetListBox.SelectedItem.ToString();
            if (_presets.TryGetValue(name, out VideoSettings settings))
            {
                SelectedPresetName = name;
                SelectedPresetSettings = DeepCopy(settings);
                this.DialogResult = DialogResult.OK;
                Close();
            }
            else
            {
                MessageBox.Show("预设数据不存在", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OnDeleteClick(object sender, EventArgs e)
        {
            if (_presetListBox.SelectedItem == null)
            {
                MessageBox.Show("请先选择一个预设", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            string name = _presetListBox.SelectedItem.ToString();
            if (MessageBox.Show($"确定要删除预设 \"{name}\" 吗？", "确认删除", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                _presets.Remove(name);
                SavePresetsToFile();
                RefreshPresetList();
            }
        }

        private void OnRenameClick(object sender, EventArgs e)
        {
            if (_presetListBox.SelectedItem == null)
            {
                MessageBox.Show("请先选择一个预设", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            string oldName = _presetListBox.SelectedItem.ToString();
            string newName = Microsoft.VisualBasic.Interaction.InputBox("输入新的预设名称:", "重命名预设", oldName);
            if (string.IsNullOrWhiteSpace(newName))
                return;
            if (_presets.ContainsKey(newName))
            {
                MessageBox.Show("预设名称已存在", "错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            var settings = _presets[oldName];
            _presets.Remove(oldName);
            _presets[newName] = settings;
            SavePresetsToFile();
            RefreshPresetList();
            // 选中重命名后的项
            int idx = _presetListBox.Items.IndexOf(newName);
            if (idx >= 0) _presetListBox.SelectedIndex = idx;
        }

        private void OnExportClick(object sender, EventArgs e)
        {
            if (_presetListBox.SelectedItem == null)
            {
                MessageBox.Show("请先选择一个预设", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            string name = _presetListBox.SelectedItem.ToString();
            var settings = _presets[name];
            var saveDialog = new SaveFileDialog
            {
                Title = "导出预设",
                Filter = "JSON 文件 (*.json)|*.json",
                FileName = $"{name}.json"
            };
            if (saveDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    string json = JsonConvert.SerializeObject(settings, Formatting.Indented);
                    File.WriteAllText(saveDialog.FileName, json);
                    MessageBox.Show($"预设已导出到:\n{saveDialog.FileName}", "导出成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"导出失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void OnImportClick(object sender, EventArgs e)
        {
            var openDialog = new OpenFileDialog
            {
                Title = "导入预设",
                Filter = "JSON 文件 (*.json)|*.json"
            };
            if (openDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    string json = File.ReadAllText(openDialog.FileName);
                    var settings = JsonConvert.DeserializeObject<VideoSettings>(json);
                    if (settings == null)
                    {
                        MessageBox.Show("无效的预设文件", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                    string presetName = Path.GetFileNameWithoutExtension(openDialog.FileName);
                    string finalName = presetName;
                    int counter = 1;
                    while (_presets.ContainsKey(finalName))
                    {
                        finalName = $"{presetName}_{counter++}";
                    }
                    _presets[finalName] = settings;
                    SavePresetsToFile();
                    RefreshPresetList();
                    MessageBox.Show($"预设已导入为: {finalName}", "导入成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"导入失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void SavePresetsToFile()
        {
            try
            {
                string dir = Path.GetDirectoryName(_presetFilePath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
                string json = JsonConvert.SerializeObject(_presets, Formatting.Indented);
                File.WriteAllText(_presetFilePath, json);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存预设文件失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private VideoSettings DeepCopy(VideoSettings original)
        {
            var json = JsonConvert.SerializeObject(original);
            return JsonConvert.DeserializeObject<VideoSettings>(json);
        }
    }
}
