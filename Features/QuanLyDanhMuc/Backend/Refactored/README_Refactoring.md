# Tái cấu trúc phần Quản lý Danh mục
## Áp dụng kỹ thuật từ Refactoring Guru

### Các kỹ thuật đã sử dụng

| Kỹ thuật (Refactoring Guru) | Áp dụng vào |
|-----------------------------|-------------|
| **Extract Superclass** | Tạo `BaseFactory` và `BaseController` |
| **Pull Up Method** | Kéo các method trùng lặp (`DanhSach`, `Save`, `NewRow`, `Add`, `HienthiDataGridview`...) lên base class |
| **Template Method** | Định nghĩa skeleton algorithm trong base, để subclass override phần khác biệt (Hook method `TableName`) |
| **Form Template Method** | `HienthiAutoComboBox`, `TaoDataGridViewComboBoxColumn` |

---

### Cấu trúc mới

```
Backend/Refactored/
├── BaseFactory.cs          ← Superclass cho tất cả Factory
├── BaseController.cs       ← Superclass cho tất cả Controller
├── DonViTinhFactory.cs     ← Chỉ còn ~15 dòng (trước ~35 dòng)
├── DonViTinhController.cs
├── LyDoChiFactory.cs       ← Chỉ còn ~12 dòng (trước ~35 dòng)
├── LyDoChiController.cs
├── NhaCungCapFactory.cs
├── NhaCungCapController.cs
├── KhachHangFactory.cs
├── KhachHangController.cs
├── SanPhamFactory.cs
└── SanPhamController.cs
```

---

### Lợi ích đạt được

1. **Loại bỏ Duplicate Code**  
   - Trước: mỗi Factory đều copy-paste `Danhsach()`, `Save()`, `NewRow()`, `Add()`  
   - Sau: chỉ viết 1 lần trong `BaseFactory`

2. **Dễ mở rộng**  
   - Thêm danh mục mới (ví dụ: `LoaiSanPham`) chỉ cần:
   ```csharp
   public class LoaiSanPhamFactory : BaseFactory
   {
       protected override string TableName => "LOAI_SAN_PHAM";
   }
   ```

3. **Giữ nguyên interface công khai**  
   - Tất cả method public cũ vẫn tồn tại → Frontend **không cần sửa**.

4. **Tuân thủ Open/Closed Principle**  
   - Mở để mở rộng (thêm Factory mới), đóng để sửa đổi (không đụng vào Base).

