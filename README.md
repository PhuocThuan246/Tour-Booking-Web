# Website đặt tour du lịch 

Hệ thống đặt tour trực tuyến với đầy đủ chức năng đặt tour, quản lý tour, phân quyền người dùng, xác thực OTP email và dashboard thống kê doanh thu.  
Ứng dụng được xây dựng bằng ASP.NET Core MVC + Entity Framework + SQL Server.

---

## Giới thiệu dự án
Mục tiêu của hệ thống là hỗ trợ doanh nghiệp lữ hành:
- Quảng bá và giới thiệu tour du lịch theo nhiều vùng miền
- Cho phép khách hàng đặt tour trực tuyến
- Quản lý tour, khách hàng, booking và doanh thu
- Giảm thao tác thủ công & tăng tự động hóa

Hệ thống mô hình hóa đầy đủ nghiệp vụ thực tế của doanh nghiệp du lịch: lưu thông tin tour, tạo lịch khởi hành, đặt tour, thống kê doanh số.  

---

## Công nghệ sử dụng
- ASP.NET Core MVC: Xây dựng ứng dụng Web 
- Entity Framework Core: ORM & truy vấn dữ liệu 
- SQL Server: Lưu trữ dữ liệu 
- Identity + JWT + OTP: Đăng nhập, phân quyền, bảo mật 
- NLog: Ghi log toàn hệ thống
- Razor View + Bootstrap: UI/Frontend 

---

## Tính năng hệ thống

### Người dùng (User)
- Đăng ký / đăng nhập qua OTP email
- Xem danh sách tour, lọc theo loại & giá
- Xem chi tiết tour (hình ảnh, mô tả, lịch trình)
- Đặt tour online → chọn ngày đi, số lượng, thanh toán
- Quản lý tài khoản và lịch sử booking
- OTP email register → VerifyEmailCode → Login

### Quản trị viên (Admin)
- CRUD Tour & Category
- Quản lý Booking, User
- Xem thống kê doanh thu – số booking – top tour
- Dashboard trực quan hỗ trợ ra quyết định

---

## Database Structure

| Bảng | Chức năng |
|---|---|
| Users | Tài khoản khách hàng & admin |
| Tours | Danh sách tour du lịch |
| Categories | Nhóm tour (Bắc/Trung/Nam/Quốc tế) |
| TourSchedules | Ngày khởi hành + slots còn lại |
| Bookings | Đơn đặt tour |
| Images | Thư viện hình ảnh tour |

