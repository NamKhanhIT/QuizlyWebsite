# 🎓 Quizly - Nền Tảng Trắc Nghiệm Trực Tuyến

## 📋 Mô Tả Dự Án

Quizly là một nền tảng trắc nghiệm trực tuyến hiện đại, đầy đủ tính năng dành cho học sinh, sinh viên và những người muốn kiểm tra kiến thức. Hệ thống bao gồm:

- **Giao diện người dùng (User Interface)**: Làm bài thi, xem kết quả, quản lý hồ sơ
- **Giao diện quản trị (Admin Panel)**: Quản lý đề thi, người dùng, thanh toán, báo cáo

## 🎨 Tính Năng Chính

### Cho Người Dùng:
- ✅ Danh sách bộ đề thi với bộ lọc và tìm kiếm
- ✅ Giao diện làm bài thi chuyên nghiệp với bộ đếm thời gian
- ✅ Xem kết quả chi tiết với đáp án chính xác
- ✅ Hồ sơ cá nhân với thống kê
- ✅ Quản lý gói hội viên Premium
- ✅ Lịch sử mua bộ đề
- ✅ Giao diện responsive, đẹp mắt

### Cho Admin:
- ✅ Dashboard với thống kê toàn hệ thống
- ✅ Quản lý bộ đề thi (CRUD)
- ✅ Quản lý câu hỏi theo bộ đề
- ✅ Quản lý người dùng
- ✅ Quản lý danh mục và môn học
- ✅ Theo dõi thanh toán & doanh thu
- ✅ Báo cáo chi tiết
- ✅ Biểu đồ phân tích

## 🛠️ Công Nghệ Sử Dụng

- **Backend**: ASP.NET Core 8.0 (C#)
- **Frontend**: Razor Views, Tailwind CSS
- **Database**: SQL Server
- **ORM**: Entity Framework Core
- **Charts**: Chart.js
- **Icons**: Material Design Icons

## 📁 Cấu Trúc Dự Án

```
QuizlyWebsite/
├── Controllers/          # Controllers chính
│   ├── QuizController.cs
│   ├── UserController.cs
│   └── HomeController.cs
├── Areas/Admin/          # Admin Area
│   ├── Controllers/
│   │   └── HomeController.cs
│   └── Views/
│       ├── Home/
│       ├── Shared/
│       └── _Layout.cshtml
├── Models/              # Entity Models
│   ├── User.cs
│   ├── Exam.cs
│   ├── Question.cs
│   ├── ExamResult.cs
│   └── ... (18+ models)
├── Data/
│   └── QuizlyDbContext.cs  # DbContext
├── Views/               # User Views
│   ├── Quiz/
│   │   ├── List.cshtml
│   │   ├── Detail.cshtml
│   │   ├── Start.cshtml
│   │   └── Result.cshtml
│   ├── User/
│   │   ├── Profile.cshtml
│   │   ├── Results.cshtml
│   │   ├── Membership.cshtml
│   │   └── PurchaseHistory.cshtml
│   ├── Home/
│   ├── Shared/
│   └── _Layout.cshtml
├── wwwroot/
│   ├── css/
│   │   ├── animations.css
│   │   └── site.css
│   └── js/
│       ├── interactions.js
│       └── site.js
├── appsettings.json     # Cấu hình
└── Program.cs          # Startup
```

## ⚙️ Cài Đặt & Chạy

### Yêu Cầu
- .NET 8.0 SDK
- SQL Server 2019+
- Visual Studio 2022 hoặc VS Code

### Bước 1: Clone Repository
```bash
git clone <repository-url>
cd QuizlyWebsite
```

### Bước 2: Cấu Hình Database
1. Mở `appsettings.json`
2. Cập nhật connection string:
```json
"ConnectionStrings": {
  "DefaultConnection": "Server=.;Database=QuizlyDB;Trusted_Connection=true;TrustServerCertificate=true;Encrypt=false;"
}
```

### Bước 3: Tạo Database
Chạy SQL Script `QuizlyDB.sql` trong SQL Server Management Studio:
```bash
# Hoặc dùng PowerShell
sqlcmd -S . -U sa -P <password> -i QuizlyDB.sql
```

### Bước 4: Khôi Phục Dependencies
```bash
dotnet restore
```

### Bước 5: Chạy Ứng Dụng
```bash
dotnet run
```

Ứng dụng sẽ chạy tại `https://localhost:5001`

## 🎮 Hướng Dẫn Sử Dụng

### Người Dùng Thường
1. **Đăng ký/Đăng nhập** tại trang chủ
2. **Khám phá bộ đề** từ menu "Bộ Đề Thi"
3. **Làm bài thi** bằng cách nhấp vào "Bắt Đầu Làm Bài"
4. **Xem kết quả** sau khi nộp bài
5. **Quản lý hồ sơ** từ menu cá nhân

### Admin
1. Truy cập `/admin` (yêu cầu vai trò Admin)
2. **Dashboard**: Xem tổng quan thống kê
3. **Quản Lý Đề Thi**: Thêm, sửa, xóa bộ đề
4. **Quản Lý Câu Hỏi**: Quản lý câu hỏi cho mỗi bộ đề
5. **Quản Lý Người Dùng**: Kiểm soát tài khoản người dùng
6. **Thanh Toán**: Xem lịch sử giao dịch
7. **Báo Cáo**: Xem analytics chi tiết

## 🎨 Giao Diện

### Màu Sắc Chính
- **Primary**: #2b8cee (Xanh dương)
- **Success**: #22c55e (Xanh lá)
- **Danger**: #ef4444 (Đỏ)
- **Warning**: #f59e0b (Cam)

### Font
- **Display**: Lexend
- **Body**: Lexend

### Responsive
- Mobile-first design
- Tailwind CSS breakpoints
- Fully responsive layout

## 📊 Database Schema

### Bảng Chính
- `tb_Users`: Người dùng
- `tb_Categories`: Danh mục
- `tb_Subjects`: Môn học
- `tb_Exams`: Bộ đề thi
- `tb_Questions`: Câu hỏi
- `tb_ExamResults`: Kết quả bài thi
- `tb_ExamResultDetails`: Chi tiết câu trả lời
- `tb_ExamSessions`: Phiên làm bài
- `tb_Payments`: Thanh toán
- `tb_UserMemberships`: Hội viên
- `tb_ExamReviews`: Đánh giá
- ... (và các bảng khác)

## 🔐 Bảo Mật

- Hash mật khẩu (Password hashing)
- Session management
- Role-based access control
- SQL Injection prevention (EF Core parameterized queries)
- CSRF protection

## 📈 Tối Ưu Hiệu Năng

- Eager loading các liên kết
- Pagination cho danh sách
- CDN cho Tailwind CSS
- Minified CSS/JS
- Image optimization

## 🚀 Triển Khai

### Azure App Service
```bash
dotnet publish -c Release
# Upload folder bin/Release/net8.0/publish
```

### Docker
```bash
docker build -t quizly .
docker run -p 5000:80 quizly
```

## 📝 Ghi Chú Phát Triển

- [ ] Thêm xác thực 2 lớp (2FA)
- [ ] Tích hợp thanh toán VNPay
- [ ] Tạo bộ đề cho người dùng
- [ ] Hệ thống thông báo realtime
- [ ] API REST để di động
- [ ] Unit tests
- [ ] CI/CD pipeline

## 🤝 Đóng Góp

Nếu bạn muốn đóng góp:
1. Fork repository
2. Tạo branch mới (`git checkout -b feature/AmazingFeature`)
3. Commit changes (`git commit -m 'Add AmazingFeature'`)
4. Push to branch (`git push origin feature/AmazingFeature`)
5. Mở Pull Request

## 📄 Giấy Phép

Dự án này được cấp phép dưới MIT License

## 📞 Hỗ Trợ

Nếu có vấn đề, vui lòng tạo issue trên GitHub hoặc liên hệ: support@quizly.com

---

**Phiên Bản**: 1.0.0  
**Cập Nhật Lần Cuối**: 03/12/2024
