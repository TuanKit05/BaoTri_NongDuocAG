# Báo cáo Refactoring cho chức năng Thu – Chi

**Hệ thống:** Cửa hàng Nông dược An Giang (dự án `CuahangNongduoc.csproj`).  
**Phạm vi:** `Features/ThuChi/Backend` và `Features/ThuChi/Frontend`; cập nhật `CuahangNongduoc.csproj` để khai báo file mới.  
**Nguồn kỹ thuật:** https://refactoring.guru/refactoring/catalog  
**Chú ý:** đây là **Refactoring Techniques**, KHÔNG dùng tên hay đếm **Design Patterns** (GoF).

## Kết quả và nguyên tắc

- Refactoring.Guru có **66 kỹ thuật** trong **6 nhóm**. Đã áp dụng **22 kỹ thuật** tại nơi có mã nguồn phù hợp; **44 kỹ thuật không áp dụng**, có lý do tương ứng bên dưới.
- Các kỹ thuật đối nghịch như Extract/Inline Class, Pull Up/Push Down, Replace Inheritance with Delegation/Replace Delegation with Inheritance không nên cùng áp lên một cấu trúc chỉ để đủ con số.
- Giữ chữ ký public của các controller cho việc thêm, xóa, lưu, tìm kiếm, in; riêng `Tonghop` đổi thành `TongHop` và vẫn giữ wrapper tên cũ có `[Obsolete]`.
- Các `.Designer.cs`, `.resx` và tên tham số RDLC được giữ nguyên. Không đổi lược đồ cơ sở dữ liệu. Truy vấn Access được sửa riêng để dùng tham số OleDb theo vị trí và lọc ngày bằng khoảng nửa mở; các sửa này không tính vào 22 kỹ thuật refactoring.
- Nguồn nhắm đến **.NET Framework 2.0**, không thêm cú pháp hiện đại.

## 22 kỹ thuật đã áp dụng — vị trí trong mã nguồn

| STT | Kỹ thuật Refactoring.Guru | Vị trí trong `Features/ThuChi/` | Tác dụng |
|---:|---|---|---|
| 1 | **Extract Method** | `Backend/PhieuChiController.cs` — `TaoPhieuChi`; `Backend/PhieuThanhToanController.cs` — `TaoPhieuThanhToan`; `Backend/DuNoKhachHangController.cs` — `TinhCongNo`, `GhiDuNo` | Tách ánh xạ dòng dữ liệu, tính công nợ và ghi công nợ thành hàm có trách nhiệm rõ. |
| 2 | **Extract Variable** | `Frontend/frmPhieuChi.cs` — `toolTimKiem_Click`; `Frontend/frmThanhToan.cs` — `toolTimKiem_Click` | Đặt tên rõ cho mã đối tượng và ngày tìm kiếm trước khi gọi controller. |
| 3 | **Inline Temp** | `Backend/DuNoKhachHangFactory.cs` — `LoadSchema` | Bỏ biến `OleDbCommand` chỉ dùng một lần. |
| 4 | **Replace Temp with Query** | `Backend/DuNoKhachHang.cs` — `TinhCuoiKy`; `Backend/DuNoKhachHangController.cs` — `TinhCongNo` | Đưa phép tính cuối kỳ vào truy vấn nghiệp vụ `TinhCuoiKy()`. |
| 5 | **Substitute Algorithm** | `Frontend/frmDunoKhachhang.cs` — `NamHopLe` | Dùng `Int32.TryParse` và kiểm tra miền năm thay cho luồng bắt lỗi chuyển đổi. |
| 6 | **Move Method** | `Backend/ThuChiBinding.cs` — `HienThi`, `GanLienKet`; được gọi từ hai controller phiếu | Chuyển phần cấu hình binding dùng chung khỏi controller. |
| 7 | **Extract Class** | `Backend/ThuChiBinding.cs`, `Backend/ThuChiScalarQuery.cs`, `Frontend/ThuChiReportParameters.cs` | Tách các trách nhiệm binding, truy vấn scalar và tham số báo cáo. |
| 8 | **Replace Magic Number with Symbolic Constant** | `Frontend/frmDunoKhachhang.cs` — `NamNhoNhat`, `NamLonNhat` | Đặt tên cho giới hạn năm hợp lệ. |
| 9 | **Decompose Conditional** | `Frontend/frmDunoKhachhang.cs` — `NamHopLe` | Đóng gói điều kiện kiểm tra năm thành hàm có tên theo nghiệp vụ. |
| 10 | **Consolidate Conditional Expression** | `Frontend/frmDunoKhachhang.cs` — `NamHopLe` | Gom điều kiện parse và giới hạn năm thành biểu thức Boolean. |
| 11 | **Remove Control Flag** | `Frontend/frmDunoKhachhang.cs` — `NamHopLe` | Bỏ cờ `ok` và nhánh `try/catch` chỉ để kiểm tra năm. |
| 12 | **Replace Nested Conditional with Guard Clauses** | `Backend/PhieuChiController.cs` — `LayPhieuChi`; `Backend/PhieuThanhToanController.cs` — `LayPhieuThanhToan` | Trả về sớm nếu truy vấn không có dòng, trước khi ánh xạ đối tượng. |
| 13 | **Rename Method** | `Backend/DuNoKhachHangController.cs` — `TongHop`; `Frontend/frmDunoKhachhang.cs` — nơi gọi; giữ `Tonghop` làm wrapper `[Obsolete]` | Chuẩn hóa tên hàm, đồng thời giữ tương thích lời gọi cũ. |
| 14 | **Separate Query from Modifier** | `Backend/DuNoKhachHangController.cs` — `TinhCongNo` và `GhiDuNo` | Tách việc tính/đọc công nợ khỏi thao tác ghi vào `DataRow`. |
| 15 | **Parameterize Method** | `Backend/ThuChiBinding.cs` — `HienThi` | Dùng chung binding với tham số tên cột đối tượng và ngày. |
| 16 | **Preserve Whole Object** | `Frontend/ThuChiReportParameters.cs` — `Tao(PhieuThuChiCoBan)`; `frmInPhieuChi.cs`, `frmInPhieuThanhToan.cs` | Truyền cả chứng từ vào hàm tạo tham số báo cáo thay vì chỉ truyền số tiền. |
| 17 | **Introduce Parameter Object** | `Backend/ThuChiBinding.cs` — lớp `ThuChiControls` | Nhóm 7 control luôn được truyền cùng nhau thành một đối tượng. |
| 18 | **Replace Exception with Test** | `Frontend/frmDunoKhachhang.cs` — `NamHopLe` | Dùng `Int32.TryParse` để xác thực dữ liệu thay cho exception thông thường. |
| 19 | **Pull Up Field** | `Backend/PhieuThuChiCoBan.cs` — các trường `ID`, `TONG_TIEN`, `GHI_CHU` | Đưa các trường chung của hai loại chứng từ lên lớp cơ sở. |
| 20 | **Pull Up Method** | `Backend/PhieuThuChiCoBan.cs` — các property `Id`, `TongTien`, `GhiChu` | Đưa property dùng chung lên lớp cơ sở. |
| 21 | **Pull Up Constructor Body** | `Backend/PhieuThuChiCoBan.cs`, `Backend/PhieuChi.cs`, `Backend/PhieuThanhToan.cs` — constructor nhận `id` | Chuyển phần gán `id` chung vào constructor lớp cơ sở. |
| 22 | **Extract Superclass** | `Backend/PhieuThuChiCoBan.cs`; lớp `PhieuChi` và `PhieuThanhToan` | Tạo lớp cha cho dữ liệu và hành vi chung của chứng từ thu/chi. |

## Đối chiếu đầy đủ 66/66 kỹ thuật trên Refactoring.Guru

Ghi chú: **Áp dụng** chỉ dành cho kỹ thuật có biến đổi mã nguồn thực tế trong bản nộp. **Không áp dụng** nghĩa là kỹ thuật đã được xem xét nhưng không có vấn đề tương ứng hoặc sẽ làm hệ thống phức tạp/sai hợp đồng.

### Composing Methods

| Kỹ thuật | Trạng thái | Lý do hoặc phần áp dụng |
|---|---|---|
| Extract Method | **Áp dụng** | `Backend/PhieuChiController.cs` — Tách khối chuyển dòng dữ liệu, tính công nợ, ghi công nợ thành các phương thức riêng. |
| Inline Method | Không áp dụng | Các hàm trung gian hiện có là ranh giới Controller–Factory; không nên bỏ chỉ để đạt số lượng. |
| Extract Variable | **Áp dụng** | `Frontend/frmPhieuChi.cs` — Đặt tên cho mã lý do, mã khách hàng và ngày lọc trước khi truyền vào thao tác tìm kiếm. |
| Inline Temp | **Áp dụng** | `Backend/DuNoKhachHangFactory.cs` — Bỏ biến OleDbCommand chỉ dùng một lần trong LoadSchema. |
| Replace Temp with Query | **Áp dụng** | `Backend/DuNoKhachHang.cs` — Thay biến tính số dư cuối kỳ bằng TinhCuoiKy() của đối tượng công nợ. |
| Split Temporary Variable | Không áp dụng | Không có biến tạm được dùng liên tiếp cho hai ý nghĩa nghiệp vụ khác nhau; các biến lặp đã loại bỏ. |
| Remove Assignments to Parameters | Không áp dụng | Không phát hiện tham số bị gán lại trong phạm vi refactor. |
| Replace Method with Method Object | Không áp dụng | Sau khi tách TinhCongNo/GhiDuNo, không còn phương thức cần object hóa trạng thái cục bộ. |
| Substitute Algorithm | **Áp dụng** | `Frontend/frmDunoKhachhang.cs` — Thay giải thuật kiểm tra năm bằng TryParse + so sánh khoảng, không dựa vào exception thông thường. |

### Moving Features between Objects

| Kỹ thuật | Trạng thái | Lý do hoặc phần áp dụng |
|---|---|---|
| Move Method | **Áp dụng** | `Backend/ThuChiBinding.cs` — Chuyển logic binding khỏi PhieuChiController/PhieuThanhToanController sang ThuChiBinding. |
| Move Field | Không áp dụng | Thuộc tính chung cần Pull Up Field lên lớp cha thay vì di chuyển ngang sang lớp khác. |
| Extract Class | **Áp dụng** | `Backend/ThuChiBinding.cs` — Tách ThuChiBinding, ThuChiReportParameters, ThuChiScalarQuery để gom trách nhiệm. |
| Inline Class | Không áp dụng | Các lớp tách ra có trách nhiệm dùng chung, gộp lại sẽ tái tạo mã lặp. |
| Hide Delegate | Không áp dụng | Controller đã che chi tiết Factory đối với giao diện; thêm tầng ủy quyền mới không cần thiết. |
| Remove Middle Man | Không áp dụng | Xóa lớp Controller để giao diện gọi thẳng DataLayer sẽ vi phạm phân lớp đang có. |
| Introduce Foreign Method | Không áp dụng | Không có phương thức còn thiếu trong thư viện bên ngoài cần bổ sung ở lớp gọi. |
| Introduce Local Extension | Không áp dụng | Không có thư viện đóng cần mở rộng để giải quyết hành vi hiện tại. |

### Organizing Data

| Kỹ thuật | Trạng thái | Lý do hoặc phần áp dụng |
|---|---|---|
| Self Encapsulate Field | Không áp dụng | Các trường lưu trữ mô hình đã có property, không phát hiện truy cập trực tiếp gây vấn đề. |
| Replace Data Value with Object | Không áp dụng | Không có kiểu nguyên thủy đơn lẻ chứa đủ hành vi để buộc tách object mới. |
| Change Value to Reference | Không áp dụng | Không có các value object cần quản lý chung danh tính. |
| Change Reference to Value | Không áp dụng | KhachHang/LyDoChi là thực thể tham chiếu; đổi sang value sẽ có rủi ro ngữ nghĩa. |
| Replace Array with Object | Không áp dụng | Không có mảng chứa các trường dị loại. |
| Duplicate Observed Data | Không áp dụng | Giao diện đã dùng BindingSource/DataTable làm lớp dữ liệu liên kết. |
| Change Unidirectional Association to Bidirectional | Không áp dụng | Không có liên kết cần tạo quan hệ hai chiều. |
| Change Bidirectional Association to Unidirectional | Không áp dụng | Không có quan hệ hai chiều thừa cần loại bỏ. |
| Replace Magic Number with Symbolic Constant | **Áp dụng** | `Frontend/frmDunoKhachhang.cs` — Đưa ngưỡng năm 2000 và 9999 thành hằng NamNhoNhat/NamLonNhat. |
| Encapsulate Field | Không áp dụng | Các trường BusinessObject vốn private và truy cập qua property. |
| Encapsulate Collection | Không áp dụng | Không có collection công khai cần đóng gói quyền sửa đổi. |
| Replace Type Code with Class | Không áp dụng | ID_LY_DO_CHI, ID_KHACH_HANG là khóa ngoại DB chứ không phải type code nội bộ. |
| Replace Type Code with Subclasses | Không áp dụng | Không có giá trị type code điều khiển hành vi để tách subclass. |
| Replace Type Code with State/Strategy | Không áp dụng | Không có type code chuyển trạng thái; không đưa Design Pattern vào sai yêu cầu. |
| Replace Subclass with Fields | Không áp dụng | PhieuChi và PhieuThanhToan có thuộc tính/nghiệp vụ riêng, không chỉ khác nhau ở hằng. |

### Simplifying Conditional Expressions

| Kỹ thuật | Trạng thái | Lý do hoặc phần áp dụng |
|---|---|---|
| Decompose Conditional | **Áp dụng** | `Frontend/frmDunoKhachhang.cs` — Tách kiểm tra hợp lệ năm thành hàm NamHopLe(string). |
| Consolidate Conditional Expression | **Áp dụng** | `Frontend/frmDunoKhachhang.cs` — Kết hợp parse + hai điều kiện miền giá trị trong biểu thức bool rõ nghĩa. |
| Consolidate Duplicate Conditional Fragments | Không áp dụng | Không có mã giống hệt ở cả hai nhánh điều kiện cần chuyển ra ngoài. |
| Remove Control Flag | **Áp dụng** | `Frontend/frmDunoKhachhang.cs` — Bỏ bool ok và nhánh try/catch điều khiển khi kiểm tra năm. |
| Replace Nested Conditional with Guard Clauses | **Áp dụng** | `Backend/PhieuChiController.cs` — Trả về null sớm khi không tìm thấy phiếu trước khi ánh xạ dòng dữ liệu. |
| Replace Conditional with Polymorphism | Không áp dụng | Không có nhiều nhánh theo phân loại đối tượng đòi hỏi đa hình. |
| Introduce Null Object | Không áp dụng | Các phương thức LayPhieu... đã dùng hợp đồng null; thay bằng Null Object có thể đổi hành vi gọi. |
| Introduce Assertion | Không áp dụng | Chưa có bất biến nghiệp vụ đã được xác thực đủ để assert an toàn. |

### Simplifying Method Calls

| Kỹ thuật | Trạng thái | Lý do hoặc phần áp dụng |
|---|---|---|
| Rename Method | **Áp dụng** | `Backend/DuNoKhachHangController.cs` — Đổi Tonghop thành TongHop cho nhất quán, giữ wrapper Tonghop [Obsolete] để tương thích. |
| Add Parameter | Không áp dụng | Không có thiếu tham số thực sự; bảo lưu chữ ký public đang được WinForms gọi. |
| Remove Parameter | Không áp dụng | Các tham số event/control public đều đang có người dùng; không cắt hợp đồng gọi. |
| Separate Query from Modifier | **Áp dụng** | `Backend/DuNoKhachHangController.cs` — Tách TinhCongNo (đọc + tính) khỏi GhiDuNo (ghi vào DataRow). |
| Parameterize Method | **Áp dụng** | `Backend/ThuChiBinding.cs` — Dùng HienThi có tham số tên cột FK và ngày để thay nhiều khối binding giống nhau. |
| Replace Parameter with Explicit Methods | Không áp dụng | Hiển thị và tìm kiếm đã là các phương thức public riêng, không có method điều khiển bởi cờ. |
| Preserve Whole Object | **Áp dụng** | `Frontend/ThuChiReportParameters.cs` — Truyền PhieuChi/PhieuThanhToan trực tiếp vào Tao(phieu) thay vì chỉ chuyển TongTien. |
| Replace Parameter with Method Call | Không áp dụng | Chưa có giá trị dẫn xuất truyền thừa mà method nhận có thể truy cập trực tiếp an toàn. |
| Introduce Parameter Object | **Áp dụng** | `Backend/ThuChiBinding.cs` — Nhóm 7 controls thu/chi trong ThuChiControls. |
| Remove Setting Method | Không áp dụng | Thuộc tính mẫu dữ liệu cần setter cho mapping/binding và việc sửa phiếu. |
| Hide Method | Không áp dụng | Không thu hẹp phạm vi hàm public cũ nhằm tránh làm hỏng các nơi gọi ngoài thư mục. |
| Replace Constructor with Factory Method | Không áp dụng | Constructor chứng từ chỉ gán ID; không có lựa chọn khởi tạo phức tạp. |
| Replace Error Code with Exception | Không áp dụng | Hợp đồng Save() bool của tầng DataLayer cần giữ để tránh phá API. |
| Replace Exception with Test | **Áp dụng** | `Frontend/frmDunoKhachhang.cs` — Thay Convert.ToInt32 trong try/catch bằng Int32.TryParse. |

### Dealing with Generalization

| Kỹ thuật | Trạng thái | Lý do hoặc phần áp dụng |
|---|---|---|
| Pull Up Field | **Áp dụng** | `Backend/PhieuThuChiCoBan.cs` — Chuyển các trường ID, TONG_TIEN, GHI_CHU từ hai chứng từ lên PhieuThuChiCoBan. |
| Pull Up Method | **Áp dụng** | `Backend/PhieuThuChiCoBan.cs` — Chuyển các getter/setter property Id, TongTien, GhiChu lên PhieuThuChiCoBan. |
| Pull Up Constructor Body | **Áp dụng** | `Backend/PhieuThuChiCoBan.cs` — Đưa phép gán Id trong các constructor có tham số về constructor base(id). |
| Push Down Method | Không áp dụng | Các property chung đều được cả hai loại phiếu sử dụng. |
| Push Down Field | Không áp dụng | Các trường chung đều được cả hai loại phiếu sử dụng. |
| Extract Subclass | Không áp dụng | Hai lớp phiếu riêng đã hiện diện; thêm subclass không mang lại khác biệt nghiệp vụ. |
| Extract Superclass | **Áp dụng** | `Backend/PhieuThuChiCoBan.cs` — Rút PhieuThuChiCoBan từ PhieuChi và PhieuThanhToan. |
| Extract Interface | Không áp dụng | Đã có superclass chung cho các property tương đồng; thêm interface rỗng/không sử dụng là dư thừa. |
| Collapse Hierarchy | Không áp dụng | Gộp hai chứng từ lại sẽ mất khác biệt khách hàng/lý do chi/ngày tương ứng. |
| Form Template Method | Không áp dụng | Không có chuỗi bước kế thừa đồng hình để lập template mà không tác động WinForms. |
| Replace Inheritance with Delegation | Không áp dụng | Quan hệ lớp cha ở chứng từ là is-a hợp lý, thay bằng delegation sẽ làm RDLC phức tạp. |
| Replace Delegation with Inheritance | Không áp dụng | Controller ủy quyền Factory theo quan hệ uses-a, không phải is-a. |

## Phạm vi tệp

**Tệp mới do refactoring (4):**

- `Features/ThuChi/Backend/PhieuThuChiCoBan.cs`
- `Features/ThuChi/Backend/ThuChiBinding.cs`
- `Features/ThuChi/Backend/ThuChiScalarQuery.cs`
- `Features/ThuChi/Frontend/ThuChiReportParameters.cs`

**Tệp sửa do refactoring:** `CuahangNongduoc.csproj`; 9 tệp Backend (`DuNoKhachHang`, `DuNoKhachHangController`, `DuNoKhachHangFactory`, `PhieuChi`, `PhieuChiController`, `PhieuChiFactory`, `PhieuThanhToan`, `PhieuThanhToanController`, `PhieuThanhToanFactory`); 6 tệp Frontend (`frmDunoKhachhang`, `frmInDunoKhachHang`, `frmInPhieuChi`, `frmInPhieuThanhToan`, `frmPhieuChi`, `frmThanhToan`).

## Các chỉnh sửa cơ sở dữ liệu được giữ lại riêng

Các mục dưới đây là chỉnh sửa tương thích với Access từ bản làm việc mới nhất, **không được tính vào 22 kỹ thuật refactoring**:

- Các truy vấn `OleDb` dùng dấu `?` và thêm tham số theo đúng thứ tự vị trí.
- Lọc ngày bằng khoảng nửa mở `>= ngày bắt đầu` và `< ngày kế tiếp`, tránh bỏ sót bản ghi có phần giờ.
- Tổng hợp dư nợ tạo/cập nhật bản nháp; chỉ ghi khi bấm **Lưu**. `TongHop` không xóa dữ liệu dư nợ đã lưu.
- Không đổi lược đồ cơ sở dữ liệu. CSDL `cuahang.dll` trong gói có cùng SHA-256 với `cuahang(2).dll` bạn gửi.

## Kiểm tra và giới hạn

- Đã kiểm tra các tệp nguồn mới được khai báo trong `.csproj` và các đường dẫn nguồn có trong gói.
- Đã so sánh mã SHA-256 của CSDL kèm dự án với CSDL đính kèm: trùng khớp.
- Không biên dịch hoặc chạy WinForms/Access trong môi trường này. Vì vậy chưa xác nhận build/runtime trên Windows, Jet OLEDB, ReportViewer và cấu hình x86.
- Dự án nhắm đến .NET Framework 2.0; giữ nguyên `.Designer.cs`, `.resx`, tên tham số RDLC và hợp đồng public cần cho giao diện.

**Cách kiểm tra trên máy Windows:** mở `CuahangNongDuoc.sln` bằng Visual Studio phù hợp, chọn x86, kiểm tra thư viện ReportViewer/XPExplorerBar, sau đó kiểm tra thêm phiếu chi, phiếu thu/thanh toán, lọc theo ngày, in báo cáo, tổng hợp dư nợ và lưu kết quả với bản sao dữ liệu Access.

---

*Danh mục được đối chiếu với: https://refactoring.guru/refactoring/catalog*
