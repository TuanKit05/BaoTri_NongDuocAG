using System;
using System.Collections.Generic;
using System.Text;
using CuahangNongduoc.DataLayer;
using CuahangNongduoc.BusinessObject;
using System.Windows.Forms;
using System.Data;
using System.IO;
using System.Xml;

namespace CuahangNongduoc.Controller
{
    public class PhieuBanController
    {
        PhieuBanFactory factory = new PhieuBanFactory();

        BindingSource bs = new BindingSource();

        public PhieuBanController()
        {
            bs.DataSource = factory.LayPhieuBan("-1");
        }

        public void TaiPhieuBan(string id)
        {
            bs.DataSource = factory.LayPhieuBan(id);
        }

        public DataRow NewRow()
        {
            return factory.NewRow();
        }

        public void Add(DataRow row)
        {
            factory.Add(row);
        }

        public void HuyThayDoi()
        {
            bs.CancelEdit();
            factory.HuyThayDoi();
            bs.ResetBindings(false);
        }

        public void Update()
        {
            bs.MoveNext();
            factory.Save();
        }

        /// <summary>
        /// Lưu phiếu bán và chi tiết (kèm trừ tồn) trong CÙNG một transaction.
        /// Được gọi bởi các thao tác trong khung ThaoTacPhieuBan.
        /// </summary>
        public void LuuPhieuVaChiTiet(ChiTietPhieuBanController ctrlChiTiet)
        {
            if (ctrlChiTiet == null || (factory.CoPhieuMoi() && !ctrlChiTiet.CoDongMoi()))
                throw new InvalidOperationException("Phieu ban phai co it nhat mot dong hang.");

            AccessControl.DemandCurrent("BanHang");
            bs.EndEdit();
            using (System.Data.OleDb.OleDbConnection conn = new System.Data.OleDb.OleDbConnection(DataService.m_ConnectString))
            {
                conn.Open();
                using (System.Data.OleDb.OleDbTransaction tr = conn.BeginTransaction())
                {
                    try
                    {
                        AccessControl.SaleTransaction = tr;
                        factory.SaveTrongGiaoDich(conn, tr);
                        ctrlChiTiet.SaveTrongGiaoDich(conn, tr);
                        tr.Commit();
                        AccessControl.SaleTransaction = null;
                    }
                    catch
                    {
                        AccessControl.SaleTransaction = null;
                        ErrorLog.Rollback(tr, "Rollback Access");
                        throw;
                    }
                }
            }
            factory.ChapNhanThayDoi();
            ctrlChiTiet.ChapNhanThayDoi();
        }

        /// <summary>
        /// Xóa phiếu bán: hoàn tồn, xóa chi tiết và xóa phiếu trong CÙNG một transaction.
        /// Không áp giới hạn bản dùng thử. Sau khi commit thì xóa mục phí trong phihoadon.xml.
        /// </summary>
        public void XoaPhieuBan(String id)
        {
            AccessControl.DemandCurrent("XoaPhieu");
            ChiTietPhieuBanController ctrlChiTiet = new ChiTietPhieuBanController();
            using (System.Data.OleDb.OleDbConnection conn = new System.Data.OleDb.OleDbConnection(DataService.m_ConnectString))
            {
                conn.Open();
                using (System.Data.OleDb.OleDbTransaction tr = conn.BeginTransaction())
                {
                    try
                    {
                        ctrlChiTiet.XoaTheoPhieuTrongGiaoDich(conn, tr, id);
                        factory.XoaPhieuTrongGiaoDich(conn, tr, id);
                        tr.Commit();
                    }
                    catch
                    {
                        ErrorLog.Rollback(tr, "Rollback Access");
                        throw;
                    }
                }
            }
            XoaPhi(id);
        }

        // ---------- Lưu trữ phí và giảm giá trong phihoadon.xml (không đổi CSDL) ----------

        static XmlNode TimNodePhieu(XmlNode goc, String id)
        {
            foreach (XmlNode n in goc.ChildNodes)
            {
                if (n.Name == "Phieu" && n.Attributes["Id"] != null && n.Attributes["Id"].Value == id)
                    return n;
            }
            return null;
        }

        static void LuuAnToan(XmlDocument doc, String path)
        {
            // Ghi file tạm rồi thay thế để không bị ghi dở khi lỗi giữa chừng
            String tam = path + ".tmp";
            doc.Save(tam);
            if (File.Exists(path))
                File.Replace(tam, path, null);
            else
                File.Move(tam, path);
        }

        static void ThemThuocTinh(XmlDocument doc, XmlElement cha, String ten, String giaTri)
        {
            XmlElement con = doc.CreateElement(ten);
            con.InnerText = giaTri;
            cha.AppendChild(con);
        }

        public static void GhiPhi(PhieuBan phi)
        {
            AccessControl.DemandCurrent("BanHang");
            if (!AccessControl.Can("ThietLapUuDai") && (phi.PhanTramGiam != 0 || phi.TienGiam != 0))
                throw new UnauthorizedAccessException("Nhân viên không được tự nhập mức ưu đãi.");
            String path = ThamSo.LayDuongDanPhiHoaDon();
            XmlDocument doc = new XmlDocument();
            if (File.Exists(path))
                doc.Load(path);
            else
                doc.LoadXml("<PhiHoaDon />");

            XmlElement goc = doc.DocumentElement;
            XmlNode cu = TimNodePhieu(goc, phi.Id);
            if (cu != null)
                goc.RemoveChild(cu);

            XmlElement el = doc.CreateElement("Phieu");
            el.SetAttribute("Id", phi.Id);
            ThemThuocTinh(doc, el, "PhiVanChuyen", phi.PhiVanChuyen.ToString());
            ThemThuocTinh(doc, el, "PhanTramGiam", phi.PhanTramGiam.ToString(System.Globalization.CultureInfo.InvariantCulture));
            ThemThuocTinh(doc, el, "TienGiam", phi.TienGiam.ToString());
            foreach (DichVuPhieuBan dv in phi.DichVu)
            {
                XmlElement d = doc.CreateElement("DichVu");
                d.SetAttribute("Ten", dv.Ten);
                d.SetAttribute("TienDV", dv.TienDV.ToString());
                el.AppendChild(d);
            }
            goc.AppendChild(el);
            LuuAnToan(doc, path);
        }

        public static PhieuBan DocPhi(String id)
        {
            PhieuBan ph = new PhieuBan();
            ph.Id = id;
            String path = ThamSo.LayDuongDanPhiHoaDon();
            if (!File.Exists(path))
                return ph;

            XmlDocument doc = new XmlDocument();
            doc.Load(path);
            XmlNode node = TimNodePhieu(doc.DocumentElement, id);
            if (node == null)
                return ph;

            foreach (XmlNode con in node.ChildNodes)
            {
                long so;
                if (con.Name == "PhiVanChuyen")
                {
                    ph.PhiVanChuyen = long.TryParse(con.InnerText, out so) ? so : 0;
                }
                else if (con.Name == "PhanTramGiam")
                {
                    double pct;
                    ph.PhanTramGiam = double.TryParse(con.InnerText, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out pct) ? pct : 0;
                }
                else if (con.Name == "TienGiam")
                {
                    ph.TienGiam = long.TryParse(con.InnerText, out so) ? so : 0;
                }
                else if (con.Name == "DichVu")
                {
                    DichVuPhieuBan dv = new DichVuPhieuBan();
                    dv.Ten = con.Attributes["Ten"] != null ? con.Attributes["Ten"].Value : "";
                    dv.TienDV = (con.Attributes["TienDV"] != null && long.TryParse(con.Attributes["TienDV"].Value, out so)) ? so : 0;
                    ph.DichVu.Add(dv);
                }
            }
            return ph;
        }

        public static void XoaPhi(String id)
        {
            String path = ThamSo.LayDuongDanPhiHoaDon();
            if (!File.Exists(path))
                return;
            XmlDocument doc = new XmlDocument();
            doc.Load(path);
            XmlNode cu = TimNodePhieu(doc.DocumentElement, id);
            if (cu == null)
                return;
            doc.DocumentElement.RemoveChild(cu);
            LuuAnToan(doc, path);
        }

        public void HienthiPhieuBanLe(BindingNavigator bn, DataGridView dg)
        {
            bs.DataSource = factory.DanhsachPhieuBanLe();
            bn.BindingSource = bs;
            dg.DataSource = bs;
        }

        public void HienthiPhieuBanSi(BindingNavigator bn, DataGridView dg)
        {
            bs.DataSource = factory.DanhsachPhieuBanSi();
            bn.BindingSource = bs;
            dg.DataSource = bs;
        }

        public void HienthiPhieuBan(BindingNavigator bn, ComboBox cmb, TextBox txt, DateTimePicker dt,
            NumericUpDown numTongTien, NumericUpDown numDatra, NumericUpDown numConNo)
        {
            bn.BindingSource = bs;
            // Header fields are copied explicitly: an empty list must not push DBNull into Value bindings.
            foreach (Control control in new Control[] { cmb, txt, dt, numTongTien, numDatra, numConNo })
                control.DataBindings.Clear();
            HienThiThongTinPhieu(cmb, txt, dt, numTongTien, numDatra, numConNo);
        }

        public void HienThiThongTinPhieu(ComboBox cmb, TextBox txt, DateTimePicker dt,
            NumericUpDown numTongTien, NumericUpDown numDatra, NumericUpDown numConNo)
        {
            DataRowView current = bs.Current as DataRowView;
            txt.Text = current == null ? String.Empty : Convert.ToString(current["ID"]);
            cmb.SelectedIndex = -1;
            if (current != null) cmb.SelectedValue = current["ID_KHACH_HANG"];
            dt.Value = current == null || current["NGAY_BAN"] == DBNull.Value ? DateTime.Today : Convert.ToDateTime(current["NGAY_BAN"]);
            DisplayAmount(numTongTien, current, "TONG_TIEN");
            DisplayAmount(numDatra, current, "DA_TRA");
            DisplayAmount(numConNo, current, "CON_NO");
        }

        private static void DisplayAmount(NumericUpDown input, DataRowView row, string column)
        {
            decimal value = row == null || row[column] == DBNull.Value ? 0 : Convert.ToDecimal(row[column]);
            input.Value = Math.Max(input.Minimum, Math.Min(input.Maximum, value));
        }

        public PhieuBan LayPhieuBan(String id)
        {
            DataTable tbl = factory.LayPhieuBan(id);
            PhieuBan ph = null;
            if (tbl.Rows.Count > 0)
            {
                ph = new PhieuBan();
                ph.Id = Convert.ToString(tbl.Rows[0]["ID"]);

                ph.NgayBan = Convert.ToDateTime(tbl.Rows[0]["NGAY_BAN"]);
                ph.TongTien = Convert.ToInt64(tbl.Rows[0]["TONG_TIEN"]);
                ph.DaTra = Convert.ToInt64(tbl.Rows[0]["DA_TRA"]);
                ph.ConNo = Convert.ToInt64(tbl.Rows[0]["CON_NO"]);
                KhachHangController ctrlKH = new KhachHangController();
                ph.KhachHang = ctrlKH.LayKhachHang(Convert.ToString(tbl.Rows[0]["ID_KHACH_HANG"]));
                ChiTietPhieuBanController ctrl = new ChiTietPhieuBanController();
                ph.ChiTiet = ctrl.ChiTietPhieuBan(ph.Id);
            }
            return ph;
        }

        public void TimPhieuBan(String maKH, DateTime dt)
        {
            factory.TimPhieuBan(maKH, dt);
        }
    }

    // =====================================================================
    // TEMPLATE METHOD (refactoring.guru): khung chung cho mọi thao tác trên
    // phiếu bán: KiemTra -> LuuDuLieu -> HauXuLy. Khung ThucHien() cố định,
    // lớp con chỉ viết các bước của mình.
    // =====================================================================
    public abstract class ThaoTacPhieuBan
    {
        protected readonly PhieuBanController ctrl;

        protected ThaoTacPhieuBan(PhieuBanController ctrl)
        {
            this.ctrl = ctrl;
        }

        // Khung cố định: không cho lớp con ghi đè
        public bool ThucHien()
        {
            if (!KiemTra())
                return false;
            LuuDuLieu();
            HauXuLy();
            return true;
        }

        // Trả về false nếu không được phép thực hiện (đã thông báo lý do)
        protected abstract bool KiemTra();

        // Ném exception nếu lỗi; giao dịch đã được rollback ở tầng dưới
        protected abstract void LuuDuLieu();

        protected virtual void HauXuLy()
        {
        }
    }

    // Lập hóa đơn mới: kiểm tra giới hạn bản dùng thử và trùng mã, ghi phí XML,
    // lưu phiếu + chi tiết + trừ tồn trong một transaction.
    public class LapHoaDonMoi : ThaoTacPhieuBan
    {
        private readonly ChiTietPhieuBanController ctrlChiTiet;
        private readonly PhieuBan phi;

        public LapHoaDonMoi(PhieuBanController ctrl, ChiTietPhieuBanController ctrlChiTiet, PhieuBan phi)
            : base(ctrl)
        {
            this.ctrlChiTiet = ctrlChiTiet;
            this.phi = phi;
        }

        protected override bool KiemTra()
        {
            // Giữ kiểm tra trùng mã phiếu.
            if (new PhieuBanController().LayPhieuBan(phi.Id) != null)
            {
                MessageBox.Show(
                    "Mã phiếu bán đã tồn tại. Vui lòng chọn mã khác.",
                    "Phiếu bán",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                return false;
            }

            return true;
        }

        protected override void LuuDuLieu()
        {
            // Ghi phí trước; nếu CSDL lỗi thì mục XML sẽ được ghi đè ở lần lưu sau
            PhieuBanController.GhiPhi(phi);
            ctrl.LuuPhieuVaChiTiet(ctrlChiTiet);
        }
    }

    // Xóa hóa đơn: không áp giới hạn bản dùng thử. Hoàn tồn, xóa chi tiết, xóa phiếu và mục phí.
    public class XoaHoaDon : ThaoTacPhieuBan
    {
        private readonly String maPhieu;

        public XoaHoaDon(PhieuBanController ctrl, String maPhieu)
            : base(ctrl)
        {
            this.maPhieu = maPhieu;
        }

        protected override bool KiemTra()
        {
            return !String.IsNullOrEmpty(maPhieu);
        }

        protected override void LuuDuLieu()
        {
            ctrl.XoaPhieuBan(maPhieu);
        }
    }
}