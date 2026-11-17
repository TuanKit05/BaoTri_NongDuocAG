
using System;

namespace CuahangNongduoc.BusinessObject
{
    public class UuDaiCoDinh : IChienLuocUuDai
    {
        public decimal TinhUuDai(decimal soTienGoc, decimal giaTri)
        {
            if (soTienGoc < 0 || giaTri < 0)
                throw new ArgumentOutOfRangeException(
                    "giaTri", "So tien uu dai khong duoc am.");

            return Math.Min(
                soTienGoc,
                Math.Round(giaTri, 0, MidpointRounding.AwayFromZero));
        }
    }
}
