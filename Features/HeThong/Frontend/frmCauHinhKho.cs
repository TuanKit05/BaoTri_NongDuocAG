using System;
using System.Drawing;
using System.Windows.Forms;

namespace CuahangNongduoc
{
    public class frmCauHinhKho : Form
    {
        private readonly RadioButton fifo = new RadioButton();
        private readonly RadioButton specific = new RadioButton();

        public frmCauHinhKho()
        {
            AccessControl.DemandCurrent("QuanTri");
            Text = "Cấu hình xuất kho";
            ClientSize = new Size(540, 245);
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
            Label note = new Label();
            note.Text = "Lựa chọn được lưu dùng chung cho mọi máy kết nối cùng cơ sở dữ liệu.\r\nPhiếu đã lưu giữ nguyên thông tin lô.";
            note.SetBounds(20, 140, 500, 40);
            Button save = new Button();
            save.Text = "Lưu";
            save.SetBounds(330, 195, 85, 30);
            save.Click += Save_Click;
            Button cancel = new Button();
            cancel.Text = "Đóng";
            cancel.SetBounds(425, 195, 85, 30);
            cancel.DialogResult = DialogResult.Cancel;
            Controls.AddRange(new Control[] { title, fifo, specific, note, save, cancel });
            AcceptButton = save;
            CancelButton = cancel;
            string method = WarehouseSettings.Load();
            fifo.Checked = method == WarehouseSettings.FirstInFirstOut;
            specific.Checked = method == WarehouseSettings.SpecificLot;
        }

        private void Save_Click(object sender, EventArgs e)
        {
            try
            {
                string method = fifo.Checked ? WarehouseSettings.FirstInFirstOut : WarehouseSettings.SpecificLot;
                if (WarehouseSettings.Load() != method && MessageBox.Show(this,
                    "Đổi phương pháp xuất kho cho toàn cửa hàng? Các phiếu đã lưu giữ nguyên.",
                    Text, MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
                WarehouseSettings.Save(method);
                MessageBox.Show(this, "Đã lưu cấu hình xuất kho.", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception error) { MessageBox.Show(this, error.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }
    }
}
