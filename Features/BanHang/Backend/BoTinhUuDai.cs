using System;

namespace CuahangNongduoc.BusinessObject
{
    public class BoTinhUuDai
    {
        private IChienLuocUuDai chienLuoc;

        public BoTinhUuDai(IChienLuocUuDai chienLuoc)
        {
            if (chienLuoc == null)
                throw new ArgumentNullException("chienLuoc");

            this.chienLuoc = chienLuoc;
        }

        public void ThayDoiChienLuoc(IChienLuocUuDai chienLuocMoi)
        {
            if (chienLuocMoi == null)
                throw new ArgumentNullException("chienLuocMoi");

            this.chienLuoc = chienLuocMoi;
        }

        public decimal TinhUuDai(decimal soTienGoc, decimal giaTri)
        {
            return chienLuoc.TinhUuDai(soTienGoc, giaTri);
        }
    }
}
