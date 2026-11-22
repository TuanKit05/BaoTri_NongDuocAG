using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace CuahangNongduoc
{
    public partial class frmSoLuongTon : Form
    {
        public frmSoLuongTon()
        {
            InitializeComponent();
            CuahangNongduoc.ResponsiveLayout.Attach(this);
        }

        private void frmSoLuongTon_Load(object sender, EventArgs e)
        {
            IList<CuahangNongduoc.BusinessObject.SoLuongTon> data = CuahangNongduoc.Controller.SanPhamController.LaySoLuongTon();
            DataTable table = new DataTable();
            table.Columns.Add("MaSanPham", typeof(string));
            table.Columns.Add("TenSanPham", typeof(string));
            table.Columns.Add("DonGiaNhap", typeof(decimal));
            table.Columns.Add("GiaBanLe", typeof(decimal));
            table.Columns.Add("GiaBanSi", typeof(decimal));
            table.Columns.Add("DonViTinh", typeof(string));
            table.Columns.Add("SoLuong", typeof(int));
            CuahangNongduoc.Controller.DonViTinhController units = new CuahangNongduoc.Controller.DonViTinhController();
            Dictionary<string, string> unitNames = new Dictionary<string, string>();
            foreach (CuahangNongduoc.BusinessObject.SoLuongTon item in data)
            {
                CuahangNongduoc.BusinessObject.SanPham product = item.SanPham;
                string unitName = "";
                int unitId;
                if (Int32.TryParse(product.IdDonViTinh, out unitId))
                {
                    if (!unitNames.TryGetValue(product.IdDonViTinh, out unitName))
                    {
                        CuahangNongduoc.BusinessObject.DonViTinh unit = units.LayDVT(unitId);
                        unitName = unit == null ? "" : unit.TenDonViTinh;
                        unitNames[product.IdDonViTinh] = unitName;
                    }
                }
                table.Rows.Add(product.Id, product.TenSanPham, product.DonGiaNhap,
                    product.GiaBanLe, product.GiaBanSi, unitName, item.SoLuong);
            }
            this.SoLuongTonBindingSource.DataSource = table;
            this.reportViewer.RefreshReport();
        }
    }
}
