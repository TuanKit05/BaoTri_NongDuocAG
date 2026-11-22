using System;
using System.Drawing;
using System.Windows.Forms;

namespace CuahangNongduoc
{
    public class frmCauHinhKho : Form
    {
        private readonly RadioButton fifo = new RadioButton();
        private readonly RadioButton specific = new RadioButton();
        private readonly RadioButton averageCost = new RadioButton();
        private readonly RadioButton fifoCost = new RadioButton();
        private readonly NumericUpDown expiryDays = new NumericUpDown();

        public frmCauHinhKho()
        {
            WarehouseSettings.DemandManage();
            Text = "Cấu hình xuất kho";
            ClientSize = new Size(540, 415);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Label title = new Label();
            title.Text = "Phương pháp xuất kho áp dụng cho toàn cửa hàng";
            title.SetBounds(20, 20, 500, 25);
            fifo.Text = "Nhập trước xuất trước";
            fifo.SetBounds(25, 60, 470, 25);
            specific.Text = "Xuất chỉ định lô";
            specific.SetBounds(25, 95, 470, 25);
            GroupBox costs = new GroupBox();
            costs.Text = "Phương pháp giá xuất (giá vốn)";
            costs.SetBounds(20, 135, 500, 100);
            averageCost.Text = "Bình quân gia quyền sau mỗi lần nhập";
            averageCost.SetBounds(15, 25, 470, 25);
            fifoCost.Text = "Nhập trước xuất trước — giá nhập của lô xuất";
            fifoCost.SetBounds(15, 60, 470, 25);
            costs.Controls.AddRange(new Control[] { averageCost, fifoCost });
            Label note = new Label();
            note.Text = "Lựa chọn được lưu dùng chung cho mọi máy kết nối cùng cơ sở dữ liệu.\r\nPhiếu đã lưu giữ nguyên thông tin lô.";
            Label expiryLabel = new Label();
            expiryLabel.Text = "Cảnh báo trước hạn dùng (ngày):";
            expiryLabel.SetBounds(20, 250, 300, 25);
            expiryDays.Name = "numNguongHetHan";
            expiryDays.Maximum = 3650;
            expiryDays.SetBounds(330, 250, 100, 25);
            expiryDays.Value = WarehouseSettings.LoadExpiryDays();
            note.SetBounds(20, 300, 500, 40);
            Button save = new Button();
            save.Text = "Lưu";
            save.SetBounds(330, 365, 85, 30);
            save.Click += Save_Click;
            Button cancel = new Button();
            cancel.Text = "Đóng";
            cancel.SetBounds(425, 365, 85, 30);
            cancel.DialogResult = DialogResult.Cancel;
            Button history = new Button();
            history.Name = "btnLichSuCauHinh";
            history.Text = "Lịch sử thay đổi";
            history.SetBounds(20, 365, 150, 30);
            history.Click += delegate {
                try {
                    using (Form viewer = new Form())
                    {
                        viewer.Text = "Lịch sử cấu hình kho";
                        viewer.Size = new Size(1050, 480);
                        viewer.StartPosition = FormStartPosition.CenterParent;
                        DataGridView grid = new DataGridView();
                        grid.Dock = DockStyle.Fill;
                        grid.ReadOnly = true;
                        grid.AllowUserToAddRows = grid.AllowUserToDeleteRows = false;
                        grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
                        grid.BindingContext = new BindingContext();
                        grid.DataSource = WarehouseSettings.History();
                        grid.Columns["THOI_GIAN"].HeaderText = "Thời gian";
                        grid.Columns["THOI_GIAN"].DefaultCellStyle.Format = "dd/MM/yyyy HH:mm:ss";
                        grid.Columns["ID_TAI_KHOAN"].HeaderText = "Mã người đổi";
                        grid.Columns["TEN_DANG_NHAP"].HeaderText = "Tài khoản";
                        grid.Columns["HO_TEN"].HeaderText = "Họ tên";
                        grid.Columns["NOI_DUNG"].HeaderText = "Giá trị cũ → mới";
                        grid.Columns["NOI_DUNG"].FillWeight = 350;
                        grid.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
                        grid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
                        viewer.Controls.Add(grid);
                        viewer.ShowDialog(this);
                    }
                } catch (Exception error) { MessageBox.Show(this, error.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error); }
            };
            Controls.AddRange(new Control[] { title, fifo, specific, costs, expiryLabel, expiryDays, note, save, cancel, history });
            AcceptButton = save;
            CancelButton = cancel;
            string method = WarehouseSettings.Load();
            fifo.Checked = method == WarehouseSettings.FirstInFirstOut;
            specific.Checked = method == WarehouseSettings.SpecificLot;
            string costMethod = WarehouseSettings.LoadCostMethod();
            averageCost.Checked = costMethod == WarehouseSettings.WeightedAverage;
            fifoCost.Checked = costMethod == WarehouseSettings.FirstInFirstOut;
        }

        private void Save_Click(object sender, EventArgs e)
        {
            try
            {
                string method = fifo.Checked ? WarehouseSettings.FirstInFirstOut : WarehouseSettings.SpecificLot;
                string costMethod = averageCost.Checked ? WarehouseSettings.WeightedAverage : WarehouseSettings.FirstInFirstOut;
                WarehouseSettings.DemandManage();
                string previous = WarehouseSettings.Describe(WarehouseSettings.Load(), WarehouseSettings.LoadCostMethod(), WarehouseSettings.LoadExpiryDays());
                string next = WarehouseSettings.Describe(method, costMethod, (int)expiryDays.Value);
                if (previous != next && MessageBox.Show(this,
                    "Cấu hình này áp dụng cho toàn cửa hàng. Các phiếu đã lưu giữ nguyên.\r\n\r\nCũ: " + previous +
                    "\r\n\r\nMới: " + next + "\r\n\r\nBạn có muốn áp dụng thay đổi?",
                    Text, MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) != DialogResult.Yes) return;
                WarehouseSettings.Save(method, costMethod, (int)expiryDays.Value);
                MessageBox.Show(this, "Đã lưu cấu hình xuất kho.", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception error) { MessageBox.Show(this, error.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }
    }
}
