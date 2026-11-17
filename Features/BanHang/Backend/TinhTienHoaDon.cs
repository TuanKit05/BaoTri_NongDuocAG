using System;

namespace CuahangNongduoc.BusinessObject
{
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
