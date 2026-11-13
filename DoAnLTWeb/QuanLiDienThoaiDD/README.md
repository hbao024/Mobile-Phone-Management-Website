# MobileHub

Website quản lý và bán điện thoại di động, được xây dựng trên ASP.NET MVC 5, .NET Framework 4.7.2 và Entity Framework 6 Database First.

## Chạy project

1. Mở `QuanLiDienThoaiDD.sln` bằng Visual Studio.
2. Khôi phục các package NuGet nếu Visual Studio yêu cầu.
3. Chạy script `QL_HANGHOA_PHUKIEN_WEB.sql` trên SQL Server.
4. Kiểm tra `data source` và tên database trong `QuanLiDienThoaiDD/Web.config`.
5. Build solution và chạy bằng IIS Express.

## Tài khoản demo

| Tài khoản | Mật khẩu | Vai trò |
|---|---|---|
| admin | Admin@123 | Quản trị viên |
| nhanvien | Nhanvien@123 | Nhân viên |
| khachhang | Khachhang@123 | Khách hàng |

Tài khoản demo chỉ phục vụ trình diễn. Cần đổi mật khẩu trước khi triển khai thật.

## Phần nền tảng đã triển khai

- Forms Authentication và đăng nhập bằng tên tài khoản hoặc email.
- Đăng xuất có kiểm tra quyền và chống gửi biểu mẫu giả mạo.
- Phân quyền theo mã vai trò `ADMIN`, `NHANVIEN`, `KHACHHANG`.
- Khu vực `Admin` với layout Sidebar/Navbar và Dashboard.
- Dashboard hiển thị doanh thu, đơn hàng, sản phẩm và cảnh báo tồn kho.
- Seed tự động thay hash placeholder trong dữ liệu mẫu bằng hash hợp lệ.

## Lưu ý về Entity Framework

Project hiện dùng Entity Framework 6 Database First với `Models/Model1.edmx`, không dùng ASP.NET Core hoặc EF Core migrations. Khi thay đổi schema SQL, cần cập nhật lại model từ database trong Visual Studio.
