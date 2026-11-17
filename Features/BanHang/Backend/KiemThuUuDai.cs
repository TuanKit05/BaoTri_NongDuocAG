using System;
using System.Windows.Forms;

namespace CuahangNongduoc.BusinessObject
{
    public class KiemThuUuDai
    {
        public static void Chay()
        {
            TinhTienHoaDon boTinh = new TinhTienHoaDon();

            KetQuaUuDai ketQua = boTinh.Tinh(
                1000000m,
                new UuDaiPhanTram(), 5m,
                new UuDaiCoDinh(), 30000m
            );

            string thongBao =
                "Tiền hàng: " + ketQua.TienHang +
                "\nChiết khấu: " + ketQua.TienChietKhau +
                "\nGiảm giá: " + ketQua.TienGiamGia +
                "\nThanh toán: " + ketQua.TongThanhToan;

            MessageBox.Show(
                thongBao,
                "Kiểm thử Strategy Pattern",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information
            );
        }
    }
}
