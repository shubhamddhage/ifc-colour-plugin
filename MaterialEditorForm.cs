using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace IfcColour
{
    public class MaterialEditorForm : System.Windows.Forms.Form
    {
        private readonly DataGridView _grid;
        private readonly ComboBox _modeComboBox;
        private List<MaterialColorItem> _ifcItems;
        private List<MaterialColorItem> _originalItems;
        private bool _isInternalChange;
        private int _activeModeIndex;

        public MaterialEditorForm(IList<MaterialColorItem> ifcItems, IList<MaterialColorItem> originalItems)
        {
            Text = "IFC Colour - Material Rule Editor";
            Width = 1100;
            Height = 560;
            StartPosition = FormStartPosition.CenterScreen;

            _ifcItems = DeepCopy(ifcItems);
            _originalItems = DeepCopy(originalItems);
            _activeModeIndex = 0;

            var modeLabel = new Label { Text = "Rule Set:", Left = 20, Top = 20, Width = 60 };
            _modeComboBox = new ComboBox { Left = 90, Top = 16, Width = 220, DropDownStyle = ComboBoxStyle.DropDownList };
            _modeComboBox.Items.Add("IFC Export Colours");
            _modeComboBox.Items.Add("Original Colours");
            _modeComboBox.SelectionChangeCommitted += ModeCommitted;

            _grid = new DataGridView
            {
                Left = 20,
                Top = 50,
                Width = 1040,
                Height = 390,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                AllowUserToAddRows = true,
                AllowUserToDeleteRows = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false
            };

            _grid.Columns.Add("MaterialName", "Material Name");
            _grid.Columns.Add("Red", "Red");
            _grid.Columns.Add("Green", "Green");
            _grid.Columns.Add("Blue", "Blue");
            _grid.RowValidating += Grid_RowValidating;

            var pickColorButton = new Button { Text = "Pick Color", Left = 20, Width = 110, Top = 460 };
            pickColorButton.Click += PickColor_Click;
            var deleteRuleButton = new Button { Text = "Delete Selected Rule", Left = 140, Width = 150, Top = 460 };
            deleteRuleButton.Click += DeleteRule_Click;
            var importButton = new Button { Text = "Import CSV", Left = 300, Width = 110, Top = 460 };
            importButton.Click += ImportCsv_Click;
            var exportButton = new Button { Text = "Export CSV", Left = 420, Width = 110, Top = 460 };
            exportButton.Click += ExportCsv_Click;
            var okButton = new Button { Text = "Apply", Left = 860, Width = 90, Top = 460, DialogResult = DialogResult.OK };
            okButton.Click += OkButton_Click;
            var cancelButton = new Button { Text = "Cancel", Left = 960, Width = 90, Top = 460, DialogResult = DialogResult.Cancel };

            Controls.Add(modeLabel);
            Controls.Add(_modeComboBox);
            Controls.Add(_grid);
            Controls.Add(pickColorButton);
            Controls.Add(deleteRuleButton);
            Controls.Add(importButton);
            Controls.Add(exportButton);
            Controls.Add(okButton);
            Controls.Add(cancelButton);

            AcceptButton = okButton;
            CancelButton = cancelButton;

            _isInternalChange = true;
            _modeComboBox.SelectedIndex = 0;
            LoadItems(_ifcItems);
            _isInternalChange = false;
        }

        private void ModeCommitted(object sender, System.EventArgs e)
        {
            if (_isInternalChange) return;

            if (!ValidateCurrentGrid(showMessage: true))
            {
                _isInternalChange = true;
                _modeComboBox.SelectedIndex = _activeModeIndex;
                _isInternalChange = false;
                return;
            }

            SaveGridToMode(_activeModeIndex);
            _activeModeIndex = _modeComboBox.SelectedIndex;
            LoadItems(_activeModeIndex == 0 ? _ifcItems : _originalItems);
        }

        private void OkButton_Click(object sender, System.EventArgs e)
        {
            if (!ValidateCurrentGrid(showMessage: true))
            {
                DialogResult = DialogResult.None;
                return;
            }
            SaveGridToMode(_activeModeIndex);
        }

        private void Grid_RowValidating(object sender, DataGridViewCellCancelEventArgs e)
        {
            if (_grid == null || e.RowIndex < 0 || e.RowIndex >= _grid.Rows.Count) return;
            var row = _grid.Rows[e.RowIndex];
            if (row == null || row.IsNewRow) return;

            string name = GetCellString(row, 0);
            string red = GetCellString(row, 1);
            string green = GetCellString(row, 2);
            string blue = GetCellString(row, 3);

            if (string.IsNullOrWhiteSpace(name) && string.IsNullOrWhiteSpace(red) && string.IsNullOrWhiteSpace(green) && string.IsNullOrWhiteSpace(blue))
                return;

            if (string.IsNullOrWhiteSpace(name))
            {
                row.ErrorText = "Material Name is required.";
                e.Cancel = true;
                return;
            }

            if (!IsRgbValue(red) || !IsRgbValue(green) || !IsRgbValue(blue))
            {
                row.ErrorText = "RGB values must be whole numbers between 0 and 255.";
                e.Cancel = true;
                return;
            }

            row.ErrorText = string.Empty;
        }

        private bool ValidateCurrentGrid(bool showMessage)
        {
            _grid.EndEdit();

            var errors = new List<string>();
            var seen = new Dictionary<string, int>();

            for (int i = 0; i < _grid.Rows.Count; i++)
            {
                var row = _grid.Rows[i];
                if (row == null || row.IsNewRow) continue;

                string name = GetCellString(row, 0).Trim();
                string red = GetCellString(row, 1).Trim();
                string green = GetCellString(row, 2).Trim();
                string blue = GetCellString(row, 3).Trim();

                row.ErrorText = string.Empty;

                if (string.IsNullOrWhiteSpace(name) && string.IsNullOrWhiteSpace(red) && string.IsNullOrWhiteSpace(green) && string.IsNullOrWhiteSpace(blue))
                    continue;

                if (string.IsNullOrWhiteSpace(name))
                {
                    errors.Add($"Row {i + 1}: Material Name is required.");
                    row.ErrorText = "Material Name is required.";
                }

                if (!IsRgbValue(red) || !IsRgbValue(green) || !IsRgbValue(blue))
                {
                    errors.Add($"Row {i + 1}: RGB values must be whole numbers between 0 and 255.");
                    row.ErrorText = "RGB values must be whole numbers between 0 and 255.";
                }

                if (!string.IsNullOrWhiteSpace(name))
                {
                    string key = name.ToUpperInvariant();
                    if (seen.ContainsKey(key))
                    {
                        errors.Add($"Duplicate material name found: '{name}' (rows {seen[key]} and {i + 1}).");
                        row.ErrorText = "Duplicate material name.";
                    }
                    else
                    {
                        seen[key] = i + 1;
                    }
                }
            }

            if (errors.Count == 0) return true;

            if (showMessage)
            {
                var sb = new StringBuilder();
                sb.AppendLine("Please fix these issues before continuing:");
                sb.AppendLine();
                foreach (var error in errors.Distinct()) sb.AppendLine("- " + error);
                MessageBox.Show(sb.ToString(), "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            return false;
        }

        private bool IsRgbValue(string text)
        {
            int number;
            return int.TryParse(text, out number) && number >= 0 && number <= 255;
        }

        private void SaveGridToMode(int modeIndex)
        {
            var snapshot = ReadGridItems();
            if (modeIndex == 0)
                _ifcItems = snapshot;
            else
                _originalItems = snapshot;
        }

        private void LoadItems(IList<MaterialColorItem> items)
        {
            _grid.Rows.Clear();
            foreach (var item in DeepCopy(items))
            {
                _grid.Rows.Add(item.MaterialName ?? "", item.MaterialColor.Red, item.MaterialColor.Green, item.MaterialColor.Blue);
            }
        }

        private List<MaterialColorItem> ReadGridItems()
        {
            var items = new List<MaterialColorItem>();
            foreach (DataGridViewRow row in _grid.Rows)
            {
                if (row == null || row.IsNewRow) continue;
                string name = GetCellString(row, 0);
                if (string.IsNullOrWhiteSpace(name)) continue;
                byte r = ToByte(GetCellString(row, 1, "0"));
                byte g = ToByte(GetCellString(row, 2, "0"));
                byte b = ToByte(GetCellString(row, 3, "0"));
                items.Add(new MaterialColorItem(name.Trim(), new Autodesk.Revit.DB.Color(r, g, b)));
            }
            return items;
        }

        public IList<MaterialColorItem> GetItems()
        {
            SaveGridToMode(_activeModeIndex);
            return DeepCopy(_activeModeIndex == 0 ? _ifcItems : _originalItems);
        }

        public IList<MaterialColorItem> GetIfcItems()
        {
            SaveGridToMode(_activeModeIndex);
            return DeepCopy(_ifcItems);
        }

        public IList<MaterialColorItem> GetOriginalItems()
        {
            SaveGridToMode(_activeModeIndex);
            return DeepCopy(_originalItems);
        }

        public string GetSelectedModeName()
        {
            return _activeModeIndex == 0 ? "IFC Export Colours" : "Original Colours";
        }

        private List<MaterialColorItem> DeepCopy(IList<MaterialColorItem> source)
        {
            if (source == null) return new List<MaterialColorItem>();
            return source.Where(x => x != null && !string.IsNullOrWhiteSpace(x.MaterialName))
                .Select(x => new MaterialColorItem(x.MaterialName.Trim(), new Autodesk.Revit.DB.Color(x.MaterialColor.Red, x.MaterialColor.Green, x.MaterialColor.Blue)))
                .ToList();
        }

        private void PickColor_Click(object sender, System.EventArgs e)
        {
            if (_grid.CurrentRow == null || _grid.CurrentRow.IsNewRow) return;
            using (var colorDialog = new ColorDialog())
            {
                if (colorDialog.ShowDialog() == DialogResult.OK)
                {
                    _grid.CurrentRow.Cells[1].Value = colorDialog.Color.R;
                    _grid.CurrentRow.Cells[2].Value = colorDialog.Color.G;
                    _grid.CurrentRow.Cells[3].Value = colorDialog.Color.B;
                }
            }
        }

        private void DeleteRule_Click(object sender, System.EventArgs e)
        {
            if (_grid.CurrentRow == null || _grid.CurrentRow.IsNewRow) return;
            _grid.Rows.Remove(_grid.CurrentRow);
        }

        private void ImportCsv_Click(object sender, System.EventArgs e)
        {
            using (var openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Title = "Import Material Rules";
                openFileDialog.Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*";
                if (openFileDialog.ShowDialog() != DialogResult.OK) return;
                if (!File.Exists(openFileDialog.FileName)) return;

                var lines = File.ReadAllLines(openFileDialog.FileName);
                if (lines == null || lines.Length == 0) return;

                _grid.Rows.Clear();
                int startIndex = lines[0].StartsWith("MaterialName") ? 1 : 0;
                for (int i = startIndex; i < lines.Length; i++)
                {
                    var line = lines[i]?.Trim();
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    var parts = line.Split(',');
                    if (parts.Length < 4) continue;
                    _grid.Rows.Add(parts[0].Trim(), parts[1].Trim(), parts[2].Trim(), parts[3].Trim());
                }
            }
        }

        private void ExportCsv_Click(object sender, System.EventArgs e)
        {
            if (!ValidateCurrentGrid(showMessage: true)) return;

            using (var saveFileDialog = new SaveFileDialog())
            {
                saveFileDialog.Title = "Export Material Rules";
                saveFileDialog.Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*";
                saveFileDialog.FileName = _activeModeIndex == 0 ? "IfcExportColours.csv" : "OriginalColours.csv";
                if (saveFileDialog.ShowDialog() != DialogResult.OK) return;

                using (var writer = new StreamWriter(saveFileDialog.FileName))
                {
                    writer.WriteLine("MaterialName,Red,Green,Blue");
                    foreach (DataGridViewRow row in _grid.Rows)
                    {
                        if (row == null || row.IsNewRow) continue;
                        string materialName = GetCellString(row, 0);
                        if (string.IsNullOrWhiteSpace(materialName)) continue;
                        writer.WriteLine($"{SafeCsv(materialName)},{GetCellString(row, 1, "0")},{GetCellString(row, 2, "0")},{GetCellString(row, 3, "0")}");
                    }
                }
            }
        }

        private string GetCellString(DataGridViewRow row, int cellIndex, string defaultValue = "")
        {
            if (row == null || row.Cells == null || cellIndex < 0 || cellIndex >= row.Cells.Count) return defaultValue;
            var value = row.Cells[cellIndex].Value;
            return value == null ? defaultValue : value.ToString();
        }

        private byte ToByte(string value)
        {
            int number;
            if (!int.TryParse(value, out number)) return 0;
            if (number < 0) number = 0;
            if (number > 255) number = 255;
            return (byte)number;
        }

        private string SafeCsv(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "";
            return value.Replace(",", " ").Replace("\r", " ").Replace("\n", " ");
        }
    }
}
