using ProductApp.Models;
using ProductApp.Repositories;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ProductApp
{
    public partial class Form1 : Form
    {
        private readonly IProductRepository _repository;
        private readonly string _currentUser;
        private bool _isEditMode = false;

        // UI Controls
        private TextBox txtProductId, txtProductName, txtSearch, txtCurrentUser;
        private ComboBox cmbProductType, cmbProductStatus, cmbSearchStatus;
        private Button btnSearch, btnResetSearch, btnSaveNew, btnUpdate, btnCancelProduct, btnClearForm;
        private DataGridView dgvProducts;
        private Label lblRecordCount, lblModeBadge;

        private bool _isBusy = false;

        public Form1()
        {
            InitializeComponent();

            string connString = ConfigurationManager.ConnectionStrings["ProductDbConnection"]?.ConnectionString
                ?? @"Server=localhost\SQLEXPRESS;Database=Product;Trusted_Connection=True;TrustServerCertificate=True;";

            _repository = new ProductRepository(connString);

            string sysUser = Environment.UserName;
            _currentUser = string.IsNullOrWhiteSpace(sysUser) ? "ADMIN" : (sysUser.Length > 20 ? sysUser.Substring(0, 20) : sysUser);

            BuildEnterpriseUI();
            this.Load += Form1_Load;
        }
        private async void Form1_Load(object sender, EventArgs e)
        {
            await InitializeDataAsync();
        }

        private async Task InitializeDataAsync()
        {
            try
            {
                Cursor = Cursors.WaitCursor;
                DataTable dtTypes = await _repository.GetProductTypesAsync();
                cmbProductType.DataSource = dtTypes;
                cmbProductType.ValueMember = "PRODUCTTYPEID";
                cmbProductType.DisplayMember = "DISPLAY_NAME";

                SetFormState(isEditMode: false);
                await LoadGridDataAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"ไม่สามารถเชื่อมต่อฐานข้อมูลได้:\n{ex.Message}", "Database Connection Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }

        private void BuildEnterpriseUI()
        {
            this.Text = "ระบบจัดการข้อมูลรหัสสินค้า (Product Master Management)";
            this.Size = new Size(1050, 680);
            this.MinimumSize = new Size(950, 600);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Font = new Font("Segoe UI", 9.5F, FontStyle.Regular);
            this.BackColor = Color.FromArgb(245, 247, 250);

            Panel pnlHeader = new Panel { Dock = DockStyle.Top, Height = 50, BackColor = Color.FromArgb(30, 41, 59) };
            Label lblTitle = new Label
            {
                Text = "  ตั้งค่ารหัสสินค้า (PRODUCT MASTER)",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 13F, FontStyle.Bold),
                Dock = DockStyle.Left,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoSize = true
            };
            lblModeBadge = new Label
            {
                Text = "โหมด: เพิ่มข้อมูลใหม่",
                ForeColor = Color.FromArgb(254, 240, 138),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Dock = DockStyle.Right,
                TextAlign = ContentAlignment.MiddleRight,
                Width = 220,
                Padding = new Padding(0, 0, 15, 0)
            };
            pnlHeader.Controls.Add(lblTitle);
            pnlHeader.Controls.Add(lblModeBadge);

            GroupBox grpForm = new GroupBox
            {
                Text = " ข้อมูลรหัสสินค้า (Product Details) ",
                Location = new Point(15, 60),
                Size = new Size(1000, 145),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                BackColor = Color.White
            };

            Label lblId = new Label { Text = "รหัสสินค้า (ID) * :", Location = new Point(20, 32), AutoSize = true, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold) };
            txtProductId = new TextBox { Location = new Point(145, 29), Width = 180, MaxLength = 20, CharacterCasing = CharacterCasing.Upper };

            Label lblName = new Label { Text = "ชื่อสินค้า * :", Location = new Point(350, 32), AutoSize = true, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold) };
            txtProductName = new TextBox { Location = new Point(440, 29), Width = 230, MaxLength = 20 };

            Label lblType = new Label { Text = "ประเภทสินค้า * :", Location = new Point(695, 32), AutoSize = true, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold) };
            cmbProductType = new ComboBox { Location = new Point(805, 29), Width = 170, DropDownStyle = ComboBoxStyle.DropDownList };

            Label lblStatus = new Label { Text = "สถานะการใช้งาน :", Location = new Point(20, 72), AutoSize = true };
            cmbProductStatus = new ComboBox { Location = new Point(145, 69), Width = 180, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbProductStatus.Items.Add(new ComboItem("0 - ใช้งาน (Active)", ProductStatus.Active));
            cmbProductStatus.Items.Add(new ComboItem("9 - ยกเลิก (Canceled)", ProductStatus.Canceled));
            cmbProductStatus.SelectedIndex = 0;

            Label lblUser = new Label { Text = "ผู้ทำรายการ :", Location = new Point(350, 72), AutoSize = true };
            txtCurrentUser = new TextBox { Location = new Point(440, 69), Width = 150, MaxLength = 20, Text = _currentUser };

            btnSaveNew = CreateButton("บันทึกเพิ่มข้อมูล", new Point(145, 103), Color.FromArgb(22, 163, 74), Color.White);
            btnUpdate = CreateButton("บันทึกการแก้ไข", new Point(285, 103), Color.FromArgb(37, 99, 235), Color.White);
            btnCancelProduct = CreateButton("ยกเลิกรหัสสินค้า (9)", new Point(425, 103), Color.FromArgb(220, 38, 38), Color.White);
            btnClearForm = CreateButton("ล้างหน้าจอ / เพิ่มใหม่", new Point(565, 103), Color.FromArgb(100, 116, 139), Color.White);

            btnSaveNew.Click += async (s, e) => await SaveNewProductAsync();
            btnUpdate.Click += async (s, e) => await UpdateExistingProductAsync();
            btnCancelProduct.Click += async (s, e) => await CancelSelectedProductAsync();
            btnClearForm.Click += (s, e) => SetFormState(isEditMode: false);

            grpForm.Controls.AddRange(new Control[] {
                lblId, txtProductId, lblName, txtProductName, lblType, cmbProductType,
                lblStatus, cmbProductStatus, lblUser, txtCurrentUser,
                btnSaveNew, btnUpdate, btnCancelProduct, btnClearForm
            });

            GroupBox grpSearch = new GroupBox
            {
                Text = " ค้นหาและรายการสินค้า (Search && Product List) ",
                Location = new Point(15, 215),
                Size = new Size(1000, 410),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                BackColor = Color.White
            };

            Label lblKeyword = new Label { Text = "คำค้นหา (รหัส/ชื่อ) :", Location = new Point(20, 30), AutoSize = true };
            txtSearch = new TextBox { Location = new Point(145, 27), Width = 240 };
            txtSearch.KeyDown += async (s, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; await LoadGridDataAsync(); } };

            Label lblFilterStatus = new Label { Text = "กรองสถานะ :", Location = new Point(405, 30), AutoSize = true };
            cmbSearchStatus = new ComboBox { Location = new Point(490, 27), Width = 160, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbSearchStatus.Items.Add(new ComboItem("ทั้งหมด (All)", null));
            cmbSearchStatus.Items.Add(new ComboItem("0 - เฉพาะใช้งาน", ProductStatus.Active));
            cmbSearchStatus.Items.Add(new ComboItem("9 - เฉพาะยกเลิก", ProductStatus.Canceled));
            cmbSearchStatus.SelectedIndex = 0;
            cmbSearchStatus.SelectedIndexChanged += async (s, e) => await LoadGridDataAsync();

            btnSearch = CreateButton("ค้นหา", new Point(665, 25), Color.FromArgb(30, 41, 59), Color.White, 90);
            btnResetSearch = CreateButton("แสดงทั้งหมด", new Point(765, 25), Color.FromArgb(226, 232, 240), Color.Black, 100);

            btnSearch.Click += async (s, e) => await LoadGridDataAsync();
            btnResetSearch.Click += async (s, e) =>
            {
                txtSearch.Clear();
                cmbSearchStatus.SelectedIndex = 0;
                await LoadGridDataAsync();
            };

            lblRecordCount = new Label
            {
                Text = "พบข้อมูล 0 รายการ",
                Location = new Point(875, 30),
                AutoSize = true,
                ForeColor = Color.DimGray,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };

            dgvProducts = new DataGridView
            {
                Location = new Point(20, 65),
                Size = new Size(960, 330),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                MultiSelect = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                RowHeadersVisible = false,
                EnableHeadersVisualStyles = false
            };
            dgvProducts.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(241, 245, 249);
            dgvProducts.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            dgvProducts.ColumnHeadersHeight = 34;
            dgvProducts.RowTemplate.Height = 28;
            dgvProducts.CellClick += DgvProducts_CellClick;
            dgvProducts.CellFormatting += DgvProducts_CellFormatting;

            dgvProducts.Sorted += (s, e) =>
            {
                dgvProducts.ClearSelection();
                SetFormState(isEditMode: false);
            };
            grpSearch.Controls.AddRange(new Control[] {
                lblKeyword, txtSearch, lblFilterStatus, cmbSearchStatus,
                btnSearch, btnResetSearch, lblRecordCount, dgvProducts
            });

            this.Controls.Add(pnlHeader);
            this.Controls.Add(grpForm);
            this.Controls.Add(grpSearch);
        }

        private Button CreateButton(string text, Point location, Color backColor, Color foreColor, int width = 130)
        {
            return new Button
            {
                Text = text,
                Location = location,
                Size = new Size(width, 32),
                BackColor = backColor,
                ForeColor = foreColor,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
        }

        private async Task LoadGridDataAsync()
        {
            try
            {
                string keyword = txtSearch.Text.Trim();
                int? statusFilter = (cmbSearchStatus.SelectedItem as ComboItem)?.Value;

                DataTable dt = await _repository.SearchProductsAsync(keyword, statusFilter);
                dgvProducts.DataSource = dt;
                FormatGridColumns();
                lblRecordCount.Text = $"พบข้อมูล {dt.Rows.Count} รายการ";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"เกิดข้อผิดพลาดในการโหลดข้อมูล:\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void FormatGridColumns()
        {
            if (dgvProducts.Columns.Count == 0) return;

            dgvProducts.Columns["PRODUCTID"].HeaderText = "รหัสสินค้า";
            dgvProducts.Columns["PRODUCTNAME"].HeaderText = "ชื่อสินค้า";
            dgvProducts.Columns["PRODUCTTYPEID"].HeaderText = "รหัสประเภท";
            dgvProducts.Columns["PRODUCTTYPENAME"].HeaderText = "ชื่อประเภทสินค้า";
            dgvProducts.Columns["STATUS_DISPLAY"].HeaderText = "สถานะ";
            dgvProducts.Columns["CREATEUSER"].HeaderText = "ผู้สร้าง";
            dgvProducts.Columns["CREATEDATE"].HeaderText = "วันที่สร้าง";
            dgvProducts.Columns["UPDATEUSER"].HeaderText = "ผู้แก้ไขล่าสุด";
            dgvProducts.Columns["UPDATEDATE"].HeaderText = "วันที่แก้ไขล่าสุด";

            dgvProducts.Columns["PRODUCTSTATUS"].Visible = false;
            dgvProducts.Columns["CREATEDATE"].DefaultCellStyle.Format = "dd/MM/yyyy HH:mm";
            dgvProducts.Columns["UPDATEDATE"].DefaultCellStyle.Format = "dd/MM/yyyy HH:mm";
        }

        private void DgvProducts_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex >= 0 && dgvProducts.Columns.Contains("PRODUCTSTATUS"))
            {
                var statusObj = dgvProducts.Rows[e.RowIndex].Cells["PRODUCTSTATUS"].Value;
                if (statusObj != null && Convert.ToInt32(statusObj) == ProductStatus.Canceled)
                {
                    dgvProducts.Rows[e.RowIndex].DefaultCellStyle.ForeColor = Color.Crimson;
                    dgvProducts.Rows[e.RowIndex].DefaultCellStyle.BackColor = Color.FromArgb(254, 242, 242);
                }
            }
        }

        private bool ValidateInput(out string errorMessage)
        {
            errorMessage = string.Empty;
            if (string.IsNullOrWhiteSpace(txtProductId.Text))
                errorMessage = "กรุณาระบุ 'รหัสสินค้า (PRODUCTID)'";
            else if (txtProductId.Text.Trim().Length > 20)
                errorMessage = "รหัสสินค้าต้องมีความยาวไม่เกิน 20 ตัวอักษร";
            else if (string.IsNullOrWhiteSpace(txtProductName.Text))
                errorMessage = "กรุณาระบุ 'ชื่อสินค้า (PRODUCTNAME)'";
            else if (txtProductName.Text.Trim().Length > 20)
                errorMessage = "ชื่อสินค้าต้องมีความยาวไม่เกิน 20 ตัวอักษร";
            else if (cmbProductType.SelectedValue == null)
                errorMessage = "กรุณาเลือก 'ประเภทสินค้า (PRODUCTTYPE)'";
            else if (string.IsNullOrWhiteSpace(txtCurrentUser.Text))
                errorMessage = "กรุณาระบุชื่อผู้ทำรายการ";

            return string.IsNullOrEmpty(errorMessage);
        }

        private async Task SaveNewProductAsync()
        {
            if (_isEditMode || _isBusy) return;

            if (!ValidateInput(out string err))
            {
                MessageBox.Show(err, "ข้อมูลไม่ครบถ้วน", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string productId = txtProductId.Text.Trim();
            try
            {
                _isBusy = true;
                btnSaveNew.Enabled = false;

                if (await _repository.IsProductIdExistsAsync(productId))
                {
                    MessageBox.Show($"รหัสสินค้า '{productId}' มีอยู่ในระบบแล้ว ไม่สามารถเพิ่มซ้ำได้", "รหัสสินค้าซ้ำ (Duplicate Key)", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtProductId.Focus();
                    return;
                }

                var model = new ProductMasterModel
                {
                    ProductId = productId,
                    ProductName = txtProductName.Text.Trim(),
                    ProductTypeId = cmbProductType.SelectedValue.ToString(),
                    ProductStatus = ProductStatus.Active,
                    CreateUser = txtCurrentUser.Text.Trim()
                };

                await _repository.InsertProductAsync(model);
                MessageBox.Show("บันทึกเพิ่มข้อมูลรหัสสินค้าเรียบร้อยแล้ว", "สำเร็จ", MessageBoxButtons.OK, MessageBoxIcon.Information);

                SetFormState(isEditMode: false);
                await LoadGridDataAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"เกิดข้อผิดพลาดในการบันทึกข้อมูล:\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _isBusy = false;
                if (!_isEditMode) btnSaveNew.Enabled = true;
            }
        }

        private async Task UpdateExistingProductAsync()
        {
            if (!_isEditMode) return;
            if (!ValidateInput(out string err))
            {
                MessageBox.Show(err, "ข้อมูลไม่ครบถ้วน", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                int selectedStatus = (cmbProductStatus.SelectedItem as ComboItem)?.Value ?? ProductStatus.Active;
                var model = new ProductMasterModel
                {
                    ProductId = txtProductId.Text.Trim(),
                    ProductName = txtProductName.Text.Trim(),
                    ProductTypeId = cmbProductType.SelectedValue.ToString(),
                    ProductStatus = selectedStatus,
                    UpdateUser = txtCurrentUser.Text.Trim()
                };

                await _repository.UpdateProductAsync(model);
                MessageBox.Show("อัปเดตแก้ไขข้อมูลเรียบร้อยแล้ว", "สำเร็จ", MessageBoxButtons.OK, MessageBoxIcon.Information);

                SetFormState(isEditMode: false);
                await LoadGridDataAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"เกิดข้อผิดพลาดในการแก้ไขข้อมูล:\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task CancelSelectedProductAsync()
        {
            if (!_isEditMode || string.IsNullOrWhiteSpace(txtProductId.Text)) return;

            string productId = txtProductId.Text.Trim();
            var confirm = MessageBox.Show(
                $"คุณต้องการยกเลิกรหัสสินค้า '{productId}' (เปลี่ยนสถานะเป็น 9 - ยกเลิก) ใช่หรือไม่?",
                "ยืนยันการยกเลิกสินค้า",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirm != DialogResult.Yes) return;

            try
            {
                await _repository.CancelProductAsync(productId, txtCurrentUser.Text.Trim());
                MessageBox.Show($"ยกเลิกรหัสสินค้า '{productId}' เรียบร้อยแล้ว", "สำเร็จ", MessageBoxButtons.OK, MessageBoxIcon.Information);

                SetFormState(isEditMode: false);
                await LoadGridDataAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"เกิดข้อผิดพลาดในการยกเลิกสินค้า:\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void DgvProducts_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            DataGridViewRow row = dgvProducts.Rows[e.RowIndex];
            txtProductId.Text = row.Cells["PRODUCTID"].Value?.ToString();
            txtProductName.Text = row.Cells["PRODUCTNAME"].Value?.ToString();
            cmbProductType.SelectedValue = row.Cells["PRODUCTTYPEID"].Value?.ToString();

            int status = Convert.ToInt32(row.Cells["PRODUCTSTATUS"].Value ?? 0);
            SetFormState(isEditMode: true, currentStatus: status);
            cmbProductStatus.SelectedIndex = (status == ProductStatus.Canceled) ? 1 : 0;

            SetFormState(isEditMode: true);
        }

        private void SetFormState(bool isEditMode, int currentStatus = ProductStatus.Active)
        {
            _isEditMode = isEditMode;

            if (!isEditMode)
            {
                txtProductId.Clear();
                txtProductId.ReadOnly = false;
                txtProductId.BackColor = Color.White;
                txtProductName.Clear();
                if (cmbProductType.Items.Count > 0) cmbProductType.SelectedIndex = 0;
                cmbProductStatus.SelectedIndex = 0;
                cmbProductStatus.Enabled = false;

                btnSaveNew.Enabled = true;
                btnUpdate.Enabled = false;
                btnCancelProduct.Enabled = false;
                lblModeBadge.Text = "โหมด: เพิ่มข้อมูลใหม่ (Create)";
                lblModeBadge.ForeColor = Color.FromArgb(134, 239, 172);
                txtProductId.Focus();
                btnCancelProduct.Enabled = false;
            }
            else
            {
                txtProductId.ReadOnly = true;
                txtProductId.BackColor = Color.FromArgb(226, 232, 240);
                cmbProductStatus.Enabled = true;

                btnSaveNew.Enabled = false;
                btnUpdate.Enabled = true;
                btnCancelProduct.Enabled = true;
                lblModeBadge.Text = $"โหมด: แก้ไขข้อมูล ({txtProductId.Text})";
                lblModeBadge.ForeColor = Color.FromArgb(253, 224, 71);
                btnCancelProduct.Enabled = (currentStatus != ProductStatus.Canceled);
            }
        }

        private class ComboItem
        {
            public string Text { get; }
            public int? Value { get; }
            public ComboItem(string text, int? value) { Text = text; Value = value; }
            public override string ToString() => Text;
        }
    }
}
