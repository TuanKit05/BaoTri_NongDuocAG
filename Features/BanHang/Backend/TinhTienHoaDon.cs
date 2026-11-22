using System;

namespace CuahangNongduoc.BusinessObject
{
    public static class TienTe
    {
        // Existing price columns store whole VND. Normalize before multiplying.
        public static decimal ThanhTien(decimal donGia, decimal soLuong)
        {
            if (donGia < 0 || soLuong < 0)
                throw new ArgumentOutOfRangeException("Số lượng và đơn giá không được âm.");
            donGia = Decimal.Round(donGia, 0, MidpointRounding.AwayFromZero);
            decimal value = Decimal.Round(checked(donGia * soLuong), 0, MidpointRounding.AwayFromZero);
            if (value > Int64.MaxValue)
                throw new OverflowException("Thành tiền vượt giới hạn lưu trữ.");
            return value;
        }
    }

    public class TinhTienHoaDon
    {
        public KetQuaUuDai Tinh(
            decimal tienHang,
            IChienLuocUuDai chienLuocChietKhau,
            decimal giaTriChietKhau,
            IChienLuocUuDai chienLuocGiamGia,
            decimal giaTriGiamGia)
        {
            if (tienHang < 0)
                throw new ArgumentOutOfRangeException("tienHang");

            if (chienLuocChietKhau == null ||
                chienLuocGiamGia == null)
                throw new ArgumentNullException("chienLuoc");

            BoTinhUuDai boTinhChietKhau =
                new BoTinhUuDai(chienLuocChietKhau);

            decimal tienChietKhau = boTinhChietKhau.TinhUuDai(
                tienHang, giaTriChietKhau);

            decimal tienSauChietKhau = tienHang - tienChietKhau;

            BoTinhUuDai boTinhGiamGia =
                new BoTinhUuDai(chienLuocGiamGia);

            decimal tienGiamGia = boTinhGiamGia.TinhUuDai(
                tienSauChietKhau, giaTriGiamGia);

            KetQuaUuDai ketQua = new KetQuaUuDai();

            ketQua.TienHang = tienHang;
            ketQua.TienChietKhau = tienChietKhau;
            ketQua.TienGiamGia = tienGiamGia;
            ketQua.TongThanhToan =
                tienSauChietKhau - tienGiamGia;

            return ketQua;
        }
    }
}
