using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using CuahangNongduoc.BusinessObject;
using CuahangNongduoc.Controller;


namespace CuahangNongduoc
{
    public partial class frmDunoKhachhang : Form
    {
        private const int NamNhoNhat = 2000;
        private const int NamLonNhat = 9999;

        public frmDunoKhachhang()
        {
            InitializeComponent();
        }
        DuNoKhachHangController ctrl = new DuNoKhachHangController();
        KhachHangController ctrlKH = new KhachHangController();
        private void frmDunoKhachhang_Load(object sender, EventArgs e)
        {

            this.toolThang.SelectedIndex = DateTime.Now.Month - 1;
            this.toolNam.Text = DateTime.Now.Year.ToString();
            ctrlKH.HienthiKhachHangChungDataGridviewComboBox(colKhachHang);

        }

        private void btnTongHop_Click(object sender, EventArgs e)
        {


        }

        // Decompose Conditional: dieu kien hop le duoc dat ten theo nghiep vu.
        private static bool NamHopLe(String giaTriNam)
        {
            int nam;
            return Int32.TryParse(giaTriNam, out nam) &&
                nam >= NamNhoNhat && nam <= NamLonNhat;
        }

        private void toolNam_Validating(object sender, CancelEventArgs e)
        {
            if (NamHopLe(toolNam.Text))
                return;

            MessageBox.Show("Thông tin năm không hợp lệ!", "Tong Hop Du No",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            e.Cancel = true;
        }

        private void toolTongHop_Click(object sender, EventArgs e)
        {
            if (!NamHopLe(toolNam.Text) || toolThang.SelectedIndex < 0)
            {
                MessageBox.Show("Tháng hoặc năm tổng hợp không hợp lệ.");
                return;
            }
            toolProgress.Visible = true;
            try
            {
                ctrl.TongHop(toolThang.SelectedIndex + 1, Convert.ToInt32(toolNam.Text),
                    toolProgress, dataGridView, bindingNavigator);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không thể tổng hợp dư nợ: " + ex.Message,
                    "Lỗi tổng hợp", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                toolProgress.Visible = false;
            }
        }

        private void toolLuu_Click(object sender, EventArgs e)
        {
            if (bindingNavigator.BindingSource == null)
            {
                MessageBox.Show("Chưa có kết quả tổng hợp dư nợ để lưu.");
                return;
            }
            try
            {
                dataGridView.EndEdit();
                if (bindingNavigator.BindingSource != null)
                    bindingNavigator.BindingSource.EndEdit();
                ctrl.Save();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không thể lưu dư nợ: " + ex.Message,
                    "Lỗi lưu dữ liệu", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void toolThoat_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void toolIn_Click(object sender, EventArgs e)
        {
            DataRowView row = bindingNavigator.BindingSource == null ? null :
                bindingNavigator.BindingSource.Current as DataRowView;
            if (row == null) return;
            if (row.Row.RowState != DataRowState.Unchanged)
            {
                MessageBox.Show("Vui lòng lưu kết quả dư nợ trước khi in.");
                return;
            }
            KhachHangController ctrlKH = new KhachHangController();
            DuNoKhachHang dn = new DuNoKhachHang();

            dn.Thang = Convert.ToInt32(row["THANG"]);
            dn.Nam = Convert.ToInt32(row["NAM"]);
            dn.DauKy = Convert.ToInt64(row["DAU_KY"]);
            dn.PhatSinh = Convert.ToInt64(row["PHAT_SINH"]);
            dn.DaTra = Convert.ToInt64(row["DA_TRA"]);
            dn.CuoiKy = Convert.ToInt64(row["CUOI_KY"]);
            dn.KhachHang = ctrlKH.LayKhachHang(Convert.ToString(row["ID_KHACH_HANG"]));

            frmInDunoKhachHang InDuNo = new frmInDunoKhachHang(dn);
            InDuNo.Show();
        }
    }
}
