# Đối chiếu cơ sở dữ liệu Access và sửa mã nguồn Thu – Chi

**Nguồn mã:** bản `BaoTri_NongDuocAG_ThuChi_Refactored.zip` đã refactor trước đó.  
**Cơ sở dữ liệu:** tệp người dùng cung cấp `cuahang(1).dll`.  
**Ngày đối chiếu:** 12/11/2026.  
**Nguyên tắc:** giữ Microsoft Access / OleDb / WinForms / .NET Framework 2.0 và **các Refactoring Techniques**; không thay bằng GoF Design Patterns.

## 1. Cơ sở dữ liệu thực tế

- File có chữ ký `Standard Jet DB`, kích thước **1,081,344 byte**, định dạng Microsoft Access Jet; tên mở rộng `.dll` không có nghĩa là thư viện .NET.
- Đã đọc cấu trúc tên trường trực tiếp từ các trang định nghĩa bảng của file Jet, **không suy đoán theo tên tệp**.
- Tệp được sao chép **nguyên vẹn, không sửa lược đồ/dữ liệu**, từ tên `cuahang(1).dll` sang `cuahang.dll` để khớp tên mà chương trình đang yêu cầu.
- SHA256 bản gốc/bản đóng gói (trùng nhau): `5769e6b9eefe4c87604ae52d3ba9f310c03148ae1f7515e3b682ec9d3b704383`.

| Bảng thực có | Các trường Thu – Chi đã xác nhận trong trang metadata của Jet |
|---|---|
| `PHIEU_CHI` | `ID`, `NGAY_CHI`, `TONG_TIEN`, `ID_LY_DO_CHI`, `GHI_CHU`, `ID_KHACH_HANG` |
| `PHIEU_THANH_TOAN` | `ID`, `NGAY_THANH_TOAN`, `TONG_TIEN`, `ID_KHACH_HANG`, `GHI_CHU` |
| `DU_NO_KH` | `ID_KHACH_HANG`, `THANG`, `NAM`, `DAU_KY`, `PHAT_SINH`, `DA_TRA`, `CUOI_KY` |
| `PHIEU_BAN` | `ID_KHACH_HANG`, `NGAY_BAN`, `CON_NO` |
| `LY_DO_CHI` | `ID`, `LY_DO` |
| `THAM_SO` | `PHIEU_CHI`, `PHIEU_THANH_TOAN`, `TEN_CUA_HANG`, `DIA_CHI`, `DIEN_THOAI` |

*Ghi chú:* hai cột `ID` của các bảng được đối chiếu thêm bằng các quan hệ và mã nguồn cũ; phần đọc metadata độc lập trong môi trường này xác minh trực tiếp các tên cột dài được liệt kê. `PHIEU_CHI` có cột `ID_KHACH_HANG` nhưng giao diện phiếu chi hiện tại không có ô chọn khách hàng; tôi **không tự động gán một khách hàng giả** cho cột này.

## 2. Những lỗi đã sửa và nơi sửa

| Thay đổi | Vị trí (dòng trong bản mới) |
|---|---|
| Khởi tạo kết nối từ thư mục chứa EXE | `Common/DataService.cs:15` |
| Dừng khi không thể kết nối | `Features/HeThong/Frontend/frmMain.cs:62` |
| Tra cứu phiếu chi theo đúng ngày chọn | `Features/ThuChi/Frontend/frmPhieuChi.cs:135` |
| Lọc phiếu chi theo ngày có giờ | `Features/ThuChi/Backend/PhieuChiFactory.cs:11` |
| Lọc phiếu thanh toán theo ngày có giờ | `Features/ThuChi/Backend/PhieuThanhToanFactory.cs:17` |
| Tính số tiền thanh toán trong tháng | `Features/ThuChi/Backend/PhieuThanhToanFactory.cs:37` |
| Tính công nợ từ phiếu bán trong tháng | `Features/BanHang/Backend/PhieuBanFactory.cs:57` |
| Chuẩn hóa OleDb cho dư nợ | `Features/ThuChi/Backend/DuNoKhachHangFactory.cs:19` |
| Tính dư nợ trong bộ nhớ | `Features/ThuChi/Backend/DuNoKhachHangController.cs:16` |
| Không mất kết quả tổng hợp chưa lưu | `Features/ThuChi/Backend/DuNoKhachHangController.cs:22` |
| Hoàn tất chỉnh sửa và lưu phiếu chi | `Features/ThuChi/Frontend/frmPhieuChi.cs:72` |
| Hoàn tất chỉnh sửa và lưu thanh toán | `Features/ThuChi/Frontend/frmThanhToan.cs:78` |
| Kiểm tra trước khi in dư nợ | `Features/ThuChi/Frontend/frmDunoKhachhang.cs:108` |
| Thêm Access vào project | `CuahangNongduoc.csproj:74` |

### 2.1. Tìm kiếm ngày

Các truy vấn của `PhieuChiFactory` và `PhieuThanhToanFactory` sử dụng khoảng **[ngày, ngày kế tiếp)**; vì Access có thể lưu cả phần giờ, lọc `NGAY_CHI = @ngay` hoặc `NGAY_THANH_TOAN = @ngay` dễ bỏ sót phiếu. Các tham số sử dụng `?` theo **đúng thứ tự** của `OleDbCommand.Parameters`.

### 2.2. Cách tính dư nợ không đổi nghiệp vụ

- `DAU_KY` = `DU_NO_KH.CUOI_KY` kỳ trước.
- `PHAT_SINH` = tổng `PHIEU_BAN.CON_NO` trong tháng được tổng hợp.
- `DA_TRA` = tổng `PHIEU_THANH_TOAN.TONG_TIEN` cùng tháng.
- `CUOI_KY` = `DAU_KY + PHAT_SINH - DA_TRA`.

**Sửa quan trọng:** `DuNoKhachHangController.TongHop()` trước đây gọi `factory.Clear(thang, nam)` **xóa vĩnh viễn số liệu kỳ đã có** ngay sau khi bấm Tổng hợp. Giờ phương thức nạp các dòng kỳ đang có, tính và cập nhật/chèn trong `DataTable` rồi chờ **Lưu**. Nếu có dữ liệu tổng hợp chưa lưu, lần Tổng hợp tiếp theo được chặn để không mất sửa đổi. `DataService.ExecuteNoneQuery()` áp dụng cập nhật trong một transaction của Access.

**Giới hạn nghiệp vụ vẫn tồn tại:** cách tính này giả định các kỳ trước đã được tổng hợp và lưu đúng. Nếu sửa phiếu bán/phiếu thu ở một tháng đã khóa sổ, cần tổng hợp lại từ tháng đó rồi các tháng tiếp theo theo thứ tự. Hiện chưa bổ sung chế độ khóa sổ hoặc kiểm toán lịch sử.

### 2.3. Mã phiếu và giao diện

- Khi tạo phiếu chi/phiếu thanh toán mới, `TONG_TIEN` mặc định 0, `GHI_CHU` trống, `NGAY_*` là ngày hiện tại; không sao chép nhầm số tiền/ghi chú của phiếu cũ.
- Nhập/chỉnh sửa trên giao diện được `ValidateChildren`, `DataGridView.EndEdit`, `BindingSource.EndEdit` trước khi `Save`.
- In phiếu và in dư nợ chỉ cho bản ghi đã lưu, tránh truy vấn bản ghi chưa có trong Access.
- Nếu không thể kết nối, ứng dụng đưa thông báo thay vì tiếp tục với dữ liệu rỗng; lỗi SQL/ghi dữ liệu không bị nuốt im lặng.

## 3. Các kỹ thuật Refactoring được giữ và mở rộng

Bản này kế thừa các thay đổi trước gồm `Extract Method`, `Extract Class`, `Move Method`, `Pull Up Field`, `Pull Up Method`, `Extract Superclass`, `Introduce Parameter Object`, `Replace Nested Conditional with Guard Clauses`, `Separate Query from Modifier`, v.v. Các chỉnh sửa mới tiếp tục dùng **Extract Method** (`LuuThayDoi`), **Decompose Conditional / Guard Clauses** (xác minh kỳ & bản ghi trước khi in), và chuẩn hóa câu truy vấn (không phải Design Pattern).

## 4. Kết quả kiểm tra

**Đạt kiểm tra tĩnh** trên bản bàn giao:

- Xác nhận chữ ký Jet và tên trường từ bảng metadata thực của cơ sở dữ liệu.
- SHA256 file dữ liệu nguyên gốc và `cuahang.dll` trong project **giống hệt**.
- Cả **115** tệp được khai báo ở `<Compile>` đều tồn tại; `cuahang.dll` được khai báo `<Content>` và `CopyToOutputDirectory=PreserveNewest`.
- Kiểm tra tên xử lý sự kiện của 6 form Thu – Chi với các file `.Designer.cs`.
- Xác nhận 4 tham số `ten_cua_hang`, `dia_chi`, `dien_thoai`, `bang_chu` của cả 3 báo cáo liên quan.
- Kiểm tra các truy vấn Thu – Chi không còn trộn cú pháp `@thang/@nam/@ngay/@kh` với tham số `OleDb` của các factory đã sửa.
- Kiểm tra `TongHop` không gọi `Clear`, tránh xóa dữ liệu kỳ đã lưu.

**Chưa kiểm thử tích hợp:** môi trường tạo bản vá không có runtime Windows 32-bit, Microsoft Jet OLEDB 4.0, WinForms hoặc công cụ biên dịch .NET Framework 2.0; do vậy không thể xác nhận rằng mọi thao tác đã chạy thành công trên máy Windows và không khẳng định đã thực hiện truy vấn live trên database. Những vấn đề phụ thuộc phiên bản VS/Jet và các ràng buộc dữ liệu thực tế có thể chỉ lộ khi chạy.

## 5. Hướng dẫn chạy và kiểm thử tay

1. Giải nén `BaoTri_NongDuocAG_ThuChi_DaSuaTheoCSDL.zip`, mở `CuahangNongduoc.sln` bằng Visual Studio hỗ trợ .NET Framework 2.0 hoặc đã cài targeting pack phù hợp.
2. Chọn nền tảng **x86** để dùng provider `Microsoft.Jet.OLEDB.4.0` (Jet 4.0 không chạy trong tiến trình x64).
3. Kiểm tra sau khi build có `bin/Debug/cuahang.dll` hoặc `bin/Release/cuahang.dll`; không đổi tên file này thành `.NET .dll`. Đảm bảo thư mục có quyền ghi và **sao lưu cơ sở dữ liệu trước khi thử**.
4. Mở form Phiếu Chi: thêm một phiếu (lý do, ngày, số tiền, ghi chú), lưu, tìm lại bằng **ngày trong form tìm kiếm**, in. Thử với phiếu có thời gian khác 00:00 để xác nhận bộ lọc.
5. Mở Thanh Toán: thêm phiếu khách hàng, lưu, tìm theo ngày, in; kiểm tra ô khách hàng và số tiền khớp dòng Access.
6. Mở Dư Nợ: tổng hợp một tháng có dữ liệu, **chưa bấm Lưu** thì đóng form/mở lại, số dư đã lưu cũ phải còn. Tổng hợp và Lưu, mở lại đối chiếu bốn cột `DAU_KY`, `PHAT_SINH`, `DA_TRA`, `CUOI_KY`.
7. Thử tổng hợp hai lần liên tiếp khi lần đầu chưa lưu: nhận thông báo cần lưu trước; không được xóa kỳ trước. Thử nhập năm không hợp lệ: có thông báo, không thay đổi dữ liệu.

**Lưu ý bảo mật:** bản ZIP có **cơ sở dữ liệu thực của bạn** để chương trình có thể mở trực tiếp. Không đăng ZIP này lên repository công khai hoặc chia sẻ nếu dữ liệu chứa thông tin khách hàng/giao dịch riêng tư.
