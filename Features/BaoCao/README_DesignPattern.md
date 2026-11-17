# Thống kê hóa đơn giảm giá và khuyến mãi

## Mục đích

Chức năng dành cho **quản lý hoặc chủ cửa hàng**. Người có quyền có thể thống kê các hóa đơn có chiết khấu, giảm giá hoặc khuyến mãi trong một khoảng ngày, xem dữ liệu của tất cả nhân viên hoặc chọn riêng một nhân viên.

Báo cáo gồm:

- Chi tiết hóa đơn: mã, ngày bán, nhân viên lập, tổng tiền, chiết khấu/giảm giá, khuyến mãi và ghi chú.
- Tổng hợp theo nhân viên: số hóa đơn ưu đãi và tổng tiền ưu đãi của mỗi nhân viên.
- Xuất kết quả thành CSV để mở bằng Excel.

Ngày kết thúc được tính trọn ngày. Ví dụ chọn từ 01/11 đến 14/11 thì truy vấn lấy hóa đơn từ đầu ngày 01/11 đến trước 00:00 ngày 15/11.

## Quyền truy cập

Nhân viên thường không được xem báo cáo toàn cửa hàng. Sau khi xác thực tài khoản, luồng đăng nhập phải cấp quyền cho phiên hiện tại:

```csharp
// Gán true sau khi xác minh tài khoản là quản lý/chủ cửa hàng.
EmployeeSession.Current.SetCurrent(maTaiKhoan, hoTen, coQuyenXemToanBoHoaDon);
```

Khi đăng xuất:

```csharp
EmployeeSession.Current.Clear();
```

Dự án hiện tại chưa có màn hình đăng nhập tích hợp với phiên này. Cần nối hai lời gọi trên vào nhánh đăng nhập/đăng xuất hiện có. Không lấy quyền từ lựa chọn nhân viên trên form báo cáo.

## Cách hoạt động

1. Quản lý/chủ cửa hàng chọn từ ngày, đến ngày và một nhân viên hoặc **Tất cả nhân viên**.
2. Hệ thống kiểm tra quyền và khoảng ngày.
3. CSDL lọc hóa đơn theo ngày và chỉ lấy hóa đơn có ít nhất một khoản chiết khấu, giảm giá hoặc khuyến mãi khác 0.
4. Danh sách được sắp xếp theo nhân viên, ngày và mã hóa đơn. Phần tổng hợp cộng số hóa đơn và tiền ưu đãi theo từng nhân viên.
5. Có thể xuất các dòng chi tiết và tổng kết sang CSV.

Khoản chiết khấu/giảm giá hiện được tính bằng `TIEN_CHIET_KHAU + TIEN_GIAM_GIA`; khoản khuyến mãi lấy từ `TIEN_KHUYEN_MAI`; tổng ưu đãi là tổng hai khoản đó. Nếu quy tắc nghiệp vụ của CSDL khác, điều chỉnh phép tính tại `DataServiceInvoiceAdapter.Find`.

## CSDL được sử dụng

Adapter đọc từ `PHIEU_BAN` và nối `TAI_KHOAN` qua `PHIEU_BAN.ID_NHAN_VIEN = TAI_KHOAN.ID`.

Các cột sử dụng:

- `PHIEU_BAN`: `ID`, `NGAY_BAN`, `ID_NHAN_VIEN`, `TONG_TIEN`, `TIEN_CHIET_KHAU`, `TIEN_GIAM_GIA`, `TIEN_KHUYEN_MAI`, `GHI_CHU_KHUYEN_MAI`.
- `TAI_KHOAN`: `ID`, `HO_TEN`.

Chuỗi kết nối lấy từ `DataService.m_ConnectString`. Gói mã nguồn không kèm tệp cơ sở dữ liệu.

## Vị trí các mẫu thiết kế

Danh mục Refactoring.Guru có 22 mẫu thiết kế GoF: 5 mẫu khởi tạo, 7 mẫu cấu trúc và 10 mẫu hành vi.

Tất cả lớp pattern của chức năng nằm trong `ThongKeUuDaiPatterns.cs`, trừ giao diện và điểm mở menu được nêu riêng.

| Nhóm | Mẫu | Lớp/phương thức | Áp dụng |
|---|---|---|---|
| Khởi tạo | Factory Method | `ReportExporterFactory.Create`, `CsvExporterFactory.Create` | Tạo exporter CSV thông qua giao diện chung. |
| Khởi tạo | Abstract Factory | `IReportServiceFactory`, `AccessReportServiceFactory` | Tạo bộ repository và report template dùng với Access. |
| Khởi tạo | Builder | `ReportCriteriaBuilder` | Dựng tiêu chí nhân viên và khoảng ngày. |
| Khởi tạo | Prototype | `ReportCriteria.Clone` | Sao chép bộ lọc để lưu và khôi phục. |
| Khởi tạo | Singleton | `EmployeeSession.Current` | Lưu phiên đăng nhập cùng quyền xem toàn cửa hàng. |
| Cấu trúc | Adapter | `DataServiceInvoiceAdapter` | Đóng gói truy vấn Access sau giao diện `IInvoiceReportRepository`. |
| Cấu trúc | Bridge | `ReportView`, `GridReportView`, `IReportRenderer` | Tách kết quả báo cáo khỏi phần hiển thị DataGridView. |
| Cấu trúc | Composite | `IReportNode`, `InvoiceNode`, `InvoiceGroupNode` | Gom cây kết quả theo nhân viên, ưu đãi và hóa đơn; cộng dồn số lượng/giá trị. |
| Cấu trúc | Decorator | `BasicInvoiceRow`, `InvoiceRowDecorator` | Bổ sung nhãn nhóm và tổng ưu đãi khi hiển thị hóa đơn. |
| Cấu trúc | Facade | `DiscountReportFacade` | Cung cấp điểm gọi thống nhất để tạo report và exporter. |
| Cấu trúc | Flyweight | `BenefitLabel`, `BenefitLabelFactory` | Tái sử dụng nhãn nhân viên và nội dung ưu đãi lặp lại. |
| Cấu trúc | Proxy | `AuthorizedCachedReportProxy` | Chặn tài khoản không có quyền xem toàn bộ nhân viên trước khi truy vấn. |
| Hành vi | Chain of Responsibility | `CriteriaValidator`, `SessionValidator`, `DateRangeValidator` | Kiểm tra phiên/quyền và khoảng ngày theo chuỗi. |
| Hành vi | Command | `LoadReportCommand`, `ExportReportCommand` | Đóng gói việc tải và xuất báo cáo thành lệnh. |
| Hành vi | Iterator | `InvoiceReportCollection`, `InvoiceReportIterator` | Duyệt hóa đơn để dựng các dòng kết quả. |
| Hành vi | Mediator | `IReportMediator`, `frmThongKeUuDai.Search`, `Export` | Điều phối nút, tiêu chí lọc và thao tác báo cáo qua form. |
| Hành vi | Memento | `CriteriaMemento`, `RestoreLastCriteria` | Lưu và khôi phục bộ lọc gần nhất. |
| Hành vi | Observer | `ReportSubject`, `IReportObserver` | Thông báo kết quả/trạng thái/lỗi cho giao diện. |
| Hành vi | State | `ReportStateContext`, `IdleReportState`, `LoadingReportState`, `ReadyReportState`, `FailedReportState` | Quản lý tiến trình và điều kiện cho phép xuất. |
| Hành vi | Strategy | `IGroupingStrategy`, `GroupByEmployeeAndPromotionStrategy` | Gom hóa đơn theo nhân viên rồi theo ưu đãi. |
| Hành vi | Template Method | `ReportTemplate.Build`, `DiscountPromotionReport` | Cố định các bước kiểm tra, truy vấn, gom nhóm và tổng hợp. |
| Hành vi | Visitor | `IReportVisitor`, `BenefitSummaryVisitor` | Tính tổng tiền ưu đãi và số hóa đơn trên cây kết quả. |

## Tệp chính

- `ThongKeUuDaiPatterns.cs`: mô hình dữ liệu, truy vấn, kiểm tra quyền, gom nhóm và các pattern.
- `frmThongKeUuDai.cs`: sự kiện giao diện, lọc ngày, hiển thị chi tiết/tổng hợp và xuất CSV.
- `frmThongKeUuDai.Designer.cs`: bố cục WinForms Designer.
- `Features/HeThong/Frontend/frmMain.cs`: mục menu và kiểm tra quyền trước khi mở báo cáo.
- `CuahangNongduoc.csproj`: khai báo các tệp C# của chức năng.

## Lưu ý khi tích hợp

Các pattern và giao diện đã có trong project, nhưng quyền thực tế chỉ được xác định đúng sau khi nối `EmployeeSession` với đăng nhập. Chưa thể xác nhận truy vấn với dữ liệu thật nếu chưa chạy ứng dụng trên máy có CSDL tương ứng.
