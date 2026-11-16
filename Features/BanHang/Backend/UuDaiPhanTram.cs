
using System;

namespace CuahangNongduoc.BusinessObject
{
    public class UuDaiPhanTram : IChienLuocUuDai
    {
        public decimal TinhUuDai(decimal soTienGoc, decimal giaTri)
        {
            if (soTienGoc < 0 || giaTri < 0 || giaTri > 100)
                throw new ArgumentOutOfRangeException(
                    "giaTri", "Ty le uu dai phai tu 0 den 100%.");

            return Math.Round(
                soTienGoc * giaTri / 100m,
                0,
                MidpointRounding.AwayFromZero);
        }
    }
}
