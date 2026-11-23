using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Text.RegularExpressions;

namespace CuahangNongduoc
{
    // Initial grants are installed once. Runtime authorization reads the Access grants.
    public static class PermissionCatalog
    {
        public static readonly string[] All={"QuanLyTaiKhoan","QuanTri","SanPham","DonViTinh","NhaCungCap","KhachHang","DaiLy","LyDoChi","NhapHang","BanHang","XemPhieuBan","XemTatCaPhieuBan","SuaPhieuBan","XoaPhieu","ThuChi","BaoCaoTonKho","BaoCaoBanHang","BaoCaoCaNhan","XemGiaNhap","XemGiaVon","SuaDanhMuc","SuaGiaBan","ThietLapUuDai","XemNhatKy"};
        public static string[] ForRole(string role)
        {
            if(role=="ADMIN") return All;
            if(role=="KHO") return new string[]{"SanPham","DonViTinh","NhaCungCap","NhapHang","BaoCaoTonKho","XemGiaNhap","XemGiaVon"};
            if(role=="BANHANG") return new string[]{"SanPham","DonViTinh","KhachHang","DaiLy","BanHang","XemPhieuBan","BaoCaoBanHang","BaoCaoCaNhan"};
            if(role=="KETOAN") return new string[]{"SanPham","DonViTinh","KhachHang","DaiLy","LyDoChi","ThuChi","BaoCaoBanHang","BaoCaoTonKho","XemPhieuBan","XemTatCaPhieuBan","XemGiaNhap","XemGiaVon"};
            return new string[0];
        }
    }
    public interface IRolePolicy { bool CanAccess(string feature); }
    public class DenyAllRolePolicy:IRolePolicy { public bool CanAccess(string feature){return false;} }
    public class DatabaseRolePolicy:IRolePolicy
    {
        private readonly UserAccount user;
        public DatabaseRolePolicy(UserAccount account){user=account;}
        public bool CanAccess(string feature)
        {
            return user != null && user.IsActive && !user.IsDeleted && AccountRoles.IsValid(user.Role) && !String.IsNullOrEmpty(feature) &&
                (AccountRoles.IsAdmin(user.Role) || user.Permissions.Contains(feature));
        }
    }
    public static class RolePolicyFactory
    {
        public static IRolePolicy Create(string role)
        {
            if(AppSession.CurrentUser==null || AppSession.CurrentUser.Role!=role) return new DenyAllRolePolicy();
            return new DatabaseRolePolicy(AppSession.CurrentUser);
        }
    }
    public class AuthorizationProxy:IRolePolicy
    {
        private readonly UserAccount user; private readonly IRolePolicy policy;
        public AuthorizationProxy(UserAccount account,IRolePolicy rolePolicy){user=account;policy=rolePolicy;}
        public bool CanAccess(string feature){return user!=null && user.IsActive && !user.IsDeleted && policy!=null && policy.CanAccess(feature);}
    }
    public static class AccessControl
    {
        [ThreadStatic] internal static OleDbTransaction SaleTransaction;
        [ThreadStatic] internal static Dictionary<string, long> SaleAutomaticDiscounts;
        public static bool Can(string permission){return new DatabaseRolePolicy(AppSession.CurrentUser).CanAccess(permission);}
        public static void Demand(string permission)
        { if(!Can(permission)) throw new UnauthorizedAccessException("Bạn không có quyền thực hiện thao tác: "+permission); }
        public static void DemandCurrent(string permission)
        {
            Demand(permission);
            UserAccount account=new AccountRepository().GetById(AppSession.CurrentUser.Id);
            if(!new DatabaseRolePolicy(account).CanAccess(permission)) throw new UnauthorizedAccessException("Tài khoản hoặc quyền đã thay đổi. Vui lòng đăng nhập lại.");
            AppSession.CurrentUser.Role=account.Role;
            AppSession.CurrentUser.Permissions.Clear();
            AppSession.CurrentUser.Permissions.AddRange(account.Permissions);
        }
        public static string InvoiceScope(string alias)
        {
            Demand("XemPhieuBan");
            if(Can("XemTatCaPhieuBan")) return "1=1";
            return alias+".[ID_NHAN_VIEN]='"+AppSession.CurrentUser.Id.Replace("'","''")+"'";
        }
        private static bool Uses(string sql,string table){return Regex.IsMatch(sql,@"\b(?:FROM|JOIN|UPDATE|INTO)\s+\[?"+table+@"\]?\b",RegexOptions.IgnoreCase);}
        public static void PrepareRead(OleDbCommand command)
        {
            if(AppSession.CurrentUser==null) throw new UnauthorizedAccessException("Vui lòng đăng nhập.");
            if(!AppSession.CurrentUser.IsActive || AppSession.CurrentUser.IsDeleted) throw new UnauthorizedAccessException("Tài khoản không hoạt động.");
            UserAccount current = new AccountRepository().GetById(AppSession.CurrentUser.Id);
            if (current == null || !current.IsActive || current.IsDeleted) throw new UnauthorizedAccessException("Tài khoản đã bị khóa.");
            AppSession.CurrentUser.Permissions.Clear();
            AppSession.CurrentUser.Permissions.AddRange(current.Permissions);
            AppSession.CurrentUser.Role = current.Role;
            string sql=command.CommandText;
            if(Uses(sql,"TAI_KHOAN")) Demand("QuanLyTaiKhoan");
            if(Uses(sql,"NHAT_KY")) Demand("XemNhatKy");
            if(Uses(sql,"PHIEU_NHAP")) Demand("NhapHang");
            if(Uses(sql,"NHA_CUNG_CAP")) Demand("NhaCungCap");
            if(Uses(sql,"PHIEU_CHI") || Uses(sql,"PHIEU_THANH_TOAN") || Uses(sql,"DU_NO_KH")) Demand("ThuChi");
            if(Uses(sql,"CAU_HINH") || Uses(sql,"CAU_HINH_KHO")) Demand("QuanTri");
            string condition=null;
            if(Uses(sql,"PHIEU_BAN"))
            {
                Match match=Regex.Match(sql,@"(?:FROM|JOIN)\s+\[?PHIEU_BAN\]?\s*(\w+)?",RegexOptions.IgnoreCase);
                string alias=match.Groups[1].Value;
                if(alias.Length==0 || Regex.IsMatch(alias,@"^(WHERE|INNER|LEFT|RIGHT|ORDER|GROUP)$",RegexOptions.IgnoreCase)) alias="PHIEU_BAN";
                condition=InvoiceScope(alias);
            }
            else if(Uses(sql,"CHI_TIET_PHIEU_BAN")) condition="[ID_PHIEU_BAN] IN (SELECT PB.ID FROM PHIEU_BAN PB WHERE "+InvoiceScope("PB")+")";
            if(condition!=null && condition!="1=1")
            {
                int tail=sql.Length; Match end=Regex.Match(sql,@"\b(GROUP\s+BY|ORDER\s+BY)\b",RegexOptions.IgnoreCase); if(end.Success) tail=end.Index;
                string head=sql.Substring(0,tail).TrimEnd(); int where=head.IndexOf(" WHERE ",StringComparison.OrdinalIgnoreCase);
                if(where>=0) head=head.Substring(0,where)+" WHERE ("+head.Substring(where+7)+") AND ("+condition+")";
                else head+=" WHERE "+condition;
                command.CommandText=head+" "+sql.Substring(tail);
            }
        }
        public static bool IsCostField(string name)
        {
            string normalized=(name??"").Normalize(System.Text.NormalizationForm.FormD);
            System.Text.StringBuilder letters=new System.Text.StringBuilder();
            foreach(char value in normalized)
                if(System.Globalization.CharUnicodeInfo.GetUnicodeCategory(value)!=System.Globalization.UnicodeCategory.NonSpacingMark) letters.Append(value);
            string key=letters.ToString().ToUpperInvariant().Replace("_", "").Replace(" ", "");
            return key.Contains("GIANHAP") || key.Contains("GIAVON") || key.Contains("GIABQGQ");
        }
        public static void HideCosts(DataTable table)
        {
            if(Can("XemGiaNhap") && Can("XemGiaVon")) return;
            foreach(DataColumn column in table.Columns)
            {
                if(!IsCostField(column.ColumnName)) continue;
                foreach(DataRow row in table.Rows) row[column]=0;
            }
            table.AcceptChanges();
        }
        public static bool CanCreateProduct()
        {
            return Can("SanPham") && AppSession.CurrentUser != null &&
                (AccountRoles.IsAdmin(AppSession.CurrentUser.Role) ||
                (AccountRoles.Contains(AppSession.CurrentUser.Role, AccountRoles.Warehouse) && Can("NhapHang")));
        }

        public static void DemandCreateProduct()
        {
            DemandCurrent("SanPham");
            if (!CanCreateProduct()) throw new UnauthorizedAccessException("Chi admin hoac thu kho duoc them san pham.");
        }

        public static bool CanAddCustomer()
        {
            return Can("BanHang") && Can("KhachHang");
        }

        public static void DemandWrite(string sql)
        {
            Match counter=Regex.Match(sql,@"^\s*UPDATE\s+THAM_SO\s+SET\s+(PHIEU_BAN|PHIEU_NHAP|PHIEU_THANH_TOAN|PHIEU_CHI)\s*=\s*\d+\s*$",RegexOptions.IgnoreCase);
            if(counter.Success)
            {
                string field=counter.Groups[1].Value.ToUpperInvariant();
                DemandCurrent(field=="PHIEU_BAN" ? "BanHang" : field=="PHIEU_NHAP" ? "NhapHang" : "ThuChi"); return;
            }
            if (Regex.IsMatch(sql, @"^\s*UPDATE\s+THAM_SO\s+SET\s+KHACH_HANG\s*=\s*\d+\s*$", RegexOptions.IgnoreCase)
                && !Can("SuaDanhMuc"))
            {
                DemandCurrent("BanHang");
                DemandCurrent("KhachHang");
                return;
            }
            bool delete=Regex.IsMatch(sql,@"^\s*DELETE\b",RegexOptions.IgnoreCase);
            if(delete && (Uses(sql,"PHIEU_BAN") || Uses(sql,"PHIEU_NHAP") || Uses(sql,"PHIEU_CHI") || Uses(sql,"PHIEU_THANH_TOAN"))) DemandCurrent("XoaPhieu");
            if(Uses(sql,"PHIEU_BAN") || Uses(sql,"CHI_TIET_PHIEU_BAN")) DemandCurrent("SuaPhieuBan");
            else if(Uses(sql,"PHIEU_NHAP") || Uses(sql,"MA_SAN_PHAM")) DemandCurrent("NhapHang");
            else if(Uses(sql,"PHIEU_CHI") || Uses(sql,"PHIEU_THANH_TOAN") || Uses(sql,"DU_NO_KH")) DemandCurrent("ThuChi");
            else if(Uses(sql,"THAM_SO") || Uses(sql,"CAU_HINH") || Uses(sql,"CAU_HINH_KHO")) DemandCurrent("QuanTri");
            else DemandCurrent("SuaDanhMuc");
        }
        private static bool IsConfiguredDiscount(DataRow row, string field)
        {
            long expected;
            return field == "TIEN_GIAM_GIA" && SaleTransaction != null && SaleAutomaticDiscounts != null &&
                SaleAutomaticDiscounts.TryGetValue(Convert.ToString(row["ID"]), out expected) &&
                Convert.ToDecimal(row[field]) == expected;
        }
        public static void ValidateRows(DataTable table,string sql)
        {
            foreach(DataRow row in table.Rows)
            {
                if(row.RowState==DataRowState.Unchanged || row.RowState==DataRowState.Detached) continue;
                if(row.RowState==DataRowState.Deleted)
                { DemandCurrent("XoaPhieu"); continue; }
                if ((Uses(sql, "PHIEU_BAN") && !Uses(sql, "CHI_TIET_PHIEU_BAN")) || Uses(sql, "PHIEU_NHAP"))
                    CuahangNongduoc.Controller.SaleInputValidation.ValidatePaymentRow(row);
                if(Uses(sql,"PHIEU_BAN") && !Uses(sql,"CHI_TIET_PHIEU_BAN"))
                {
                    if(row.RowState==DataRowState.Added)
                    {
                        DemandCurrent("BanHang"); row["ID_NHAN_VIEN"]=AppSession.CurrentUser.Id;
                        if(!Can("ThietLapUuDai")) foreach(string field in new string[]{"TIEN_CHIET_KHAU","TIEN_GIAM_GIA","TIEN_KHUYEN_MAI","GIAM_GIA_GIA_TRI","GIAM_GIA_TIEN"})
                            if(table.Columns.Contains(field) && row[field]!=DBNull.Value && Convert.ToDecimal(row[field])!=0 &&
                                !IsConfiguredDiscount(row, field))
                                throw new UnauthorizedAccessException("Nhân viên không được tự thay đổi mức ưu đãi.");
                    }
                    else DemandCurrent("SuaPhieuBan");
                }
                else if (Uses(sql, "SAN_PHAM") && !Uses(sql, "MA_SAN_PHAM") && row.RowState == DataRowState.Added)
                {
                    DemandCreateProduct();
                }
                else if (Uses(sql, "KHACH_HANG") && row.RowState == DataRowState.Added && !Can("SuaDanhMuc"))
                {
                    DemandCurrent("BanHang");
                    DemandCurrent("KhachHang");
                    if (!table.Columns.Contains("LOAI_KH") || row["LOAI_KH"] == DBNull.Value || Convert.ToBoolean(row["LOAI_KH"]))
                        throw new UnauthorizedAccessException("Chi duoc them khach hang, khong duoc them dai ly.");
                }
                else DemandWrite(sql);
            }
        }
        internal static string DescribeChanges(DataTable table)
        {
            System.Text.StringBuilder text=new System.Text.StringBuilder();
            foreach(DataRow row in table.Rows)
            {
                if(row.RowState==DataRowState.Unchanged || row.RowState==DataRowState.Detached) continue;
                text.Append(row.RowState).Append(": ");
                foreach(DataColumn column in table.Columns)
                {
                    string name=column.ColumnName;
                    if(name.ToUpperInvariant().Contains("MAT_KHAU") || name.ToUpperInvariant().Contains("MATKHAU") || name=="Salt") continue;
                    string oldValue=row.HasVersion(DataRowVersion.Original)?Convert.ToString(row[column,DataRowVersion.Original]):"";
                    string newValue=row.HasVersion(DataRowVersion.Current)?Convert.ToString(row[column,DataRowVersion.Current]):"";
                    if(oldValue!=newValue || name=="ID") text.Append(name).Append('[').Append(oldValue).Append(" -> ").Append(newValue).Append("]; ");
                }
                text.AppendLine();
            }
            return text.ToString();
        }
        public static void LogChange(string action,string target,string detail)
        {
            using(OleDbConnection c=new OleDbConnection(DataService.m_ConnectString))
            {c.Open();AccountRepository.Audit(c,null,action,target,detail);}
        }
    }
}

namespace CuahangNongduoc
{
    public static class SecuredUi
    {
        private static readonly Dictionary<System.Windows.Forms.Form,bool> configured = new Dictionary<System.Windows.Forms.Form,bool>();
        public static void ApplyOpenForms(object sender,EventArgs e)
        {
            List<System.Windows.Forms.Form> forms = new List<System.Windows.Forms.Form>();
            foreach(System.Windows.Forms.Form form in System.Windows.Forms.Application.OpenForms) forms.Add(form);
            foreach(System.Windows.Forms.Form form in forms) ApplyForm(form);
        }
        public static void ApplyForm(System.Windows.Forms.Form form)
        {
            if(form == null || configured.ContainsKey(form) || form.IsDisposed) return;
            configured[form]=true;
            form.FormClosed += delegate { configured.Remove(form); };
            string name=form.GetType().Name;
            string permission=null;
            if(name=="frmQuanLyTaiKhoan") permission="QuanLyTaiKhoan";
            else if(name=="frmSanPham") permission="SanPham";
            else if(name=="frmNhaCungCap") permission="NhaCungCap";
            else if(name=="frmDonViTinh") permission="DonViTinh";
            else if(name=="frmKhachHang") permission="KhachHang";
            else if(name=="frmDaiLy") permission="DaiLy";
            else if(name=="frmLyDoChi") permission="LyDoChi";
            else if(name=="frmBanLe" || name=="frmBanSi" || name.StartsWith("frmDanhsachPhieuBan")) permission="BanHang";
            else if(name=="frmNhapHang" || name=="frmDanhsachPhieuNhap") permission="NhapHang";
            else if(name=="frmThanhToan" || name=="frmPhieuChi" || name=="frmDunoKhachhang") permission="ThuChi";
            else if(name=="frmSoLuongTon" || name=="frmThongKeTonKho" || name=="frmSanphamHethan") permission="BaoCaoTonKho";
            else if(name=="frmSoLuongBan" || name=="frmDoanhThu" || name=="frmThongKeUuDai") permission="BaoCaoBanHang";
            else if(name=="frmThongtinCuahang") permission="QuanTri";
            if(permission!=null && !AccessControl.Can(permission))
            {
                System.Windows.Forms.MessageBox.Show("Bạn không có quyền mở chức năng này.","Phân quyền"); form.Close(); return;
            }
            bool catalog=name=="frmSanPham" || name=="frmNhaCungCap" || name=="frmDonViTinh" || name=="frmKhachHang" || name=="frmDaiLy" || name=="frmLyDoChi";
            ConfigureControls(form,catalog && !AccessControl.Can("SuaDanhMuc"),name=="frmBanLe" || name=="frmBanSi");
            frmSanPham products = form as frmSanPham;
            if (products != null) products.ApplyProductPermissions();
            frmKhachHang customers = form as frmKhachHang;
            if (customers != null) customers.ConfigureCustomerCreation();
        }
        private static void ConfigureCostColumn(System.Windows.Forms.DataGridViewColumn column)
        {
            if(!AccessControl.Can("XemGiaNhap") && AccessControl.IsCostField(column.Name+" "+column.DataPropertyName)) column.Visible=false;
        }
        private static void ConfigureControls(System.Windows.Forms.Control parent,bool readOnly,bool sale)
        {
            foreach(System.Windows.Forms.Control control in parent.Controls)
            {
                string name=control.Name.ToLowerInvariant();
                if(!AccessControl.Can("XemGiaNhap") && AccessControl.IsCostField(name+" "+control.Text)) control.Visible=false;
                System.Windows.Forms.DataGridView grid=control as System.Windows.Forms.DataGridView;
                if(grid!=null)
                {
                    if(readOnly){grid.ReadOnly=true;grid.AllowUserToAddRows=false;grid.AllowUserToDeleteRows=false;}
                    else if(!AccessControl.Can("XoaPhieu") && !sale) grid.AllowUserToDeleteRows=false;
                    foreach(System.Windows.Forms.DataGridViewColumn column in grid.Columns) ConfigureCostColumn(column);
                    grid.ColumnAdded += delegate(object sender, System.Windows.Forms.DataGridViewColumnEventArgs e) { ConfigureCostColumn(e.Column); };
                }
                if(readOnly && !(control.Parent is System.Windows.Forms.ToolStrip) && (control is System.Windows.Forms.TextBox || control is System.Windows.Forms.ComboBox || control is System.Windows.Forms.NumericUpDown)) control.Enabled=false;
                if(readOnly && control is System.Windows.Forms.Button && !name.Contains("thoat") && !name.Contains("close") && !name.Contains("tim")) control.Enabled=false;
                if(sale && !AccessControl.Can("ThietLapUuDai") && (name.Contains("chietkhau") || name.Contains("giamgia") || name.Contains("phantramgiam"))) control.Enabled=false;
                System.Windows.Forms.ToolStrip strip=control as System.Windows.Forms.ToolStrip;
                if(strip!=null) foreach(System.Windows.Forms.ToolStripItem item in strip.Items)
                {
                    string key=item.Name.ToLowerInvariant();
                    if((key.Contains("delete") || key.Contains("xoa")) && !AccessControl.Can("XoaPhieu")) item.Enabled=false;
                    if(readOnly && (key.Contains("save") || key.Contains("luu") || key.Contains("addnew") || key.Contains("add") || key.Contains("chinhsua"))) item.Enabled=false;
                    if(sale && key.Contains("chinhsua") && !AccessControl.Can("SuaPhieuBan")) item.Enabled=false;
                }
                ConfigureControls(control,readOnly,sale);
            }
        }
    }
}
