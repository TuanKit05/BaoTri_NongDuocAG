param([string]$ProjectRoot='D:\NongDuoc_BaoTri', [string]$AssemblyPath='')
$ErrorActionPreference='Stop'
if (!$AssemblyPath) { $AssemblyPath=Join-Path $ProjectRoot 'bin\Debug\CuahangNongduoc.exe' }
$reportCommon=Join-Path $ProjectRoot 'Lib\Microsoft.ReportViewer.Common.dll'
$reportWinForms=Join-Path $ProjectRoot 'Lib\Microsoft.ReportViewer.WinForms.dll'
[void][Reflection.Assembly]::LoadFrom($reportCommon)
[void][Reflection.Assembly]::LoadFrom($reportWinForms)
[void][Reflection.Assembly]::LoadFrom($AssemblyPath)
$code=@'
using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Reporting.WinForms;
using CuahangNongduoc.BusinessObject;
public static class SalePrintFixture
{
    public static void Render(string target, int count)
    {
        PhieuBan invoice = new PhieuBan();
        invoice.Id = "YC11-PRINT-TEST";
        invoice.NgayBan = new DateTime(2026, 11, 22);
        invoice.TongTien = 123456789012345;
        invoice.DaTra = 123456789012000;
        invoice.ConNo = 345;
        invoice.KhachHang = new KhachHang {
            HoTen = "Đại lý vật tư nông nghiệp có tên dài để kiểm tra xuống dòng đầy đủ KET-KH",
            DiaChi = "Đường Nguyễn Văn Linh, phường Long Xuyên, tỉnh An Giang, địa chỉ giao hàng dài để kiểm tra hiển thị không bị cắt chữ KET-DC",
            DienThoai = "0901234567"
        };
        invoice.ChiTiet = new List<ChiTietPhieuBan>();
        for (int i = 1; i <= count; i++)
            invoice.ChiTiet.Add(new ChiTietPhieuBan {
                MaSanPham = new MaSanPham { Id = "LO-" + i, NgayHetHan = new DateTime(2099, 12, 31),
                    SanPham = new SanPham { TenSanPham = "Sản phẩm bảo vệ thực vật có tên thương mại rất dài, quy cách đóng gói và hướng dẫn sử dụng cần xuống dòng đầy đủ KET-SP-" + i.ToString("D3") } },
                SoLuong = 123456, DonGia = 1234567890, ThanhTien = 152414813427840
            });
        using (LocalReport report = new LocalReport())
        {
            using (Stream main = typeof(PhieuBan).Assembly.GetManifestResourceStream("CuahangNongduoc.Report.rptPhieuBan.rdlc"))
                report.LoadReportDefinition(main);
            using (Stream detail = typeof(PhieuBan).Assembly.GetManifestResourceStream("CuahangNongduoc.Report.rptChiTietPhieuBan.rdlc"))
                report.LoadSubreportDefinition("rptChiTietPhieuBan", detail);

            report.DataSources.Add(new ReportDataSource("CuahangNongduoc_BusinessObject_PhieuBan", CuahangNongduoc.SalePrintData.Header(invoice)));
            report.SubreportProcessing += delegate(object sender, SubreportProcessingEventArgs e) {
                e.DataSources.Add(new ReportDataSource("CuahangNongduoc_BusinessObject_ChiTietPhieuBan", CuahangNongduoc.SalePrintData.Details(invoice)));
            };
            report.SetParameters(new ReportParameter[] {
                new ReportParameter("ten_cua_hang", "CỬA HÀNG NÔNG DƯỢC - KIỂM THỬ BỐ CỤC IN"),
                new ReportParameter("dia_chi", "Địa chỉ cửa hàng rất dài dùng kiểm tra cách xuống dòng và không vượt chiều rộng trang giấy A4 KET-CH"),
                new ReportParameter("dien_thoai", "0901234567"),
                new ReportParameter("bang_chu", "Một trăm hai mươi ba nghìn tỷ bốn trăm năm mươi sáu tỷ bảy trăm tám mươi chín triệu không trăm mười hai nghìn ba trăm bốn mươi lăm KET-BC")
            });
            string mime, encoding, extension; string[] streams; Warning[] warnings;
            byte[] pdf = report.Render("PDF", null, out mime, out encoding, out extension, out streams, out warnings);
            if (warnings != null)
                foreach (Warning warning in warnings)
                    throw new InvalidOperationException(warning.Code + ": " + warning.Message);
            File.WriteAllBytes(target, pdf);
        }
    }
}
'@
$folder=Join-Path (Join-Path $ProjectRoot 'obj') 'PrintYC11'
[void][IO.Directory]::CreateDirectory($folder)
$source=Join-Path $folder 'SalePrintFixture.cs'
$fixture=Join-Path $folder 'SalePrintFixture.dll'
[IO.File]::WriteAllText($source,$code,[Text.Encoding]::UTF8)
& "$env:WINDIR\Microsoft.NET\Framework\v4.0.30319\csc.exe" /nologo /target:library "/out:$fixture" "/reference:$AssemblyPath" "/reference:$reportCommon" "/reference:$reportWinForms" /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.Data.dll $source
if($LASTEXITCODE -ne 0){throw 'Report fixture compilation failed'}
[void][Reflection.Assembly]::LoadFrom($fixture)
[SalePrintFixture]::Render((Join-Path $folder 'sale-short.pdf'),2)
[SalePrintFixture]::Render((Join-Path $folder 'sale-multipage.pdf'),30)
Write-Output 'PASS: embedded sale report and subreport rendered without warnings for long Vietnamese text, large amounts, short and multi-page invoices.'
Write-Output ('Print fixtures: '+$folder)
