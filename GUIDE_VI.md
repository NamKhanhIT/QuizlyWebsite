# 📖 Hướng Dẫn Chi Tiết Quizly

## 🎯 Tổng Quan Hệ Thống

Quizly là nền tảng trắc nghiệm trực tuyến toàn diện với hai giao diện chính:

### 1. **Giao Diện Người Dùng (User Interface)**
Cho phép học sinh, sinh viên:
- Làm các bài thi trắc nghiệm
- Theo dõi tiến độ học tập
- Xem kết quả chi tiết
- Quản lý tài khoản cá nhân

### 2. **Giao Diện Quản Trị (Admin Panel)**
Cho phép admin:
- Quản lý toàn bộ hệ thống
- Phân tích dữ liệu
- Kiểm soát nội dung

---

## 📱 Hướng Dẫn Sử Dụng Giao Diện User

### **Trang Chủ (Homepage)**

```
┌─────────────────────────────────┐
│ Logo  [Trang Chủ] [Bộ Đề] [Blog]│  ← Navigation
│         [Đăng Nhập] [Đăng Ký]    │
└─────────────────────────────────┘

┌─────────────────────────────────┐
│  HERO SECTION                   │
│  "Thắc Nghiệm Thông Minh"       │
│  [Khám Phá Bộ Đề] [Tìm Hiểu]   │
│                                 │
│ 4 Danh Mục: Khoa Học, Lịch Sử,  │
│ Ngôn Ngữ, Địa Lý                │
└─────────────────────────────────┘

┌─────────────────────────────────┐
│ FEATURES:                       │
│ ✓ Chủ Đề Rộng Rãi              │
│ ✓ Phản Hồi Tức Thời            │
│ ✓ Cạnh Tranh Với Bạn Bè        │
│ ✓ Thành Tựu & Huy Hiệu         │
└─────────────────────────────────┘

┌─────────────────────────────────┐
│ STATS:                          │
│ 5000+ Bộ Đề  │  50000+ Users   │
└─────────────────────────────────┘
```

### **Danh Sách Bộ Đề (/quiz/list)**

```
┌──────────────────────────────────┐
│ SIDEBAR FILTERS          │ QUIZZES
├──────────────────────────┼────────────┐
│ 🔍 Tìm Kiếm              │ ┌────────┐ │
│                          │ │ Quiz 1 │ │
│ 📚 Danh Mục:             │ │ Toán   │ │
│  ☑ Khoa Học             │ │ 20 câu │ │
│  ☑ Lịch Sử              │ │ 60 phút│ │
│  ☑ Ngôn Ngữ             │ │ Dễ ★★  │ │
│                          │ │[Bắt Đ.]│ │
│ ⚡ Độ Khó:              │ └────────┘ │
│  ☐ Dễ                   │            │
│  ☑ Trung Bình           │ ┌────────┐ │
│  ☑ Khó                  │ │ Quiz 2 │ │
│                          │ │ ...    │ │
│ 💎 Premium Only          │ └────────┘ │
└──────────────────────────┴────────────┘
```

### **Trang Chi Tiết Bộ Đề (/quiz/detail/{id})**

```
┌─────────────────────────────────┐
│ > Trang Chủ > Bộ Đề > Chi Tiết  │
└─────────────────────────────────┘

┌──────────────────┬──────────────┐
│ THÔNG TIN ĐỀ THI │ SIDEBAR      │
├──────────────────┼──────────────┤
│ Tiêu Đề Bộ Đề    │ Giá: Miễn Phí│
│ 📚 Môn Học       │ [Bắt Đầu]   │
│ ❓ 50 câu hỏi    │ [Chia Sẻ]   │
│ ⏱️ 60 phút       │              │
│ ⭐ 4.5/5 (100)   │ ℹ️ Thông Tin│
│                  │ • 50 câu     │
│ ĐÁNH GIÁ:       │ • 60 phút    │
│ - Tuyệt vời!    │ • Dễ         │
│ - Rất tốt!      │              │
└──────────────────┴──────────────┘
```

### **Trang Làm Bài Thi (/quiz/start/{id})**

```
┌──────────────────────────────────────────┐
│ Bộ Đề XXX         Câu 15/50    ⏱️ 45:30  │
├──────────────┬───────────────────────────┤
│ QUESTIONS:   │ QUESTION AREA             │
│              │                           │
│  1  2  3  4  │ Câu 15:                  │
│  5  6  7  8  │ "Nội dung câu hỏi?"      │
│  9  10 11 12 │                           │
│  ...         │ ⊕ A: Đáp án A            │
│              │ ⊕ B: Đáp án B            │
│ ┌──────────┐ │ ⊕ C: Đáp án C            │
│ │   15     │ │ ⊕ D: Đáp án D            │
│ │ (Active) │ │                           │
│ └──────────┘ │ [Trước] ......... [Tiếp] │
│              │                [Nộp Bài] │
└──────────────┴───────────────────────────┘

Timer bắt đầu: Đếm ngược từ 60 phút
- Nếu hết giờ: Tự động nộp bài
- Cảnh báo khi còn 5 phút
```

### **Trang Kết Quả (/quiz/result/{resultId})**

```
┌────────────────────────────────┐
│          ✓ ĐẠT                  │
│  Bộ Đề: "Toán Cao Cấp"        │
│                                │
│  ┌──────┬──────┬──────┐        │
│  │ 85.5 │  34  │  16  │        │
│  │Điểm  │Đúng  │Sai   │        │
│  └──────┴──────┴──────┘        │
│                                │
│  Thực hiện: 10:30 → 11:28     │
│                                │
│ [Làm Lại] [Danh Sách Bộ Đề]  │
└────────────────────────────────┘

┌────────────────────────────────┐
│ CHI TIẾT CÂU TRẢ LỜI:          │
│                                │
│ Câu 1: "...?"                  │
│ ✓ Đúng - A (bạn chọn)         │
│                                │
│ Câu 2: "...?"                  │
│ ✗ Sai - B (bạn chọn)          │
│     Đáp án đúng: C             │
│                                │
│ ...                            │
└────────────────────────────────┘
```

### **Hồ Sơ Cá Nhân (/user/profile)**

```
┌──────────────────────────────────┐
│        [BANNER]                  │
│ [Avatar] Tên Người Dùng          │
│          email@example.com       │
│          [User] [Tham gia...]   │
│                        [Chỉnh Sửa]
├──────────────────────────────────┤
│ STATS:                           │
│ ┌────────┬────────┬───────┬────┐│
│ │0 Bài   │0% Đạt │0 Điểm │0   ││
│ │Hoàn Th │Kỹ     │Cao    │Huy ││
│ │ành     │Năng   │Nhất   │Hiệu││
│ └────────┴────────┴───────┴────┘│
├──────────────────────────────────┤
│ TABS: [Hoạt Động] [Thành Tựu]  │
│       [Cài Đặt]                 │
│                                  │
│ [Hoạt Động Gần Đây]             │
│ Chưa có hoạt động nào            │
└──────────────────────────────────┘
```

---

## ⚙️ Hướng Dẫn Admin Panel

### **Dashboard Admin (/admin)**

```
┌─────────┬──────────────────────────────┐
│ SIDEBAR │ HEADER                       │
├─────────┼──────────────────────────────┤
│ 📊 Dashboard      │ 🔍 [Tìm kiếm]     │
│ 📝Đề Thi        │ 🔔 📱 👤          │
│ 👥 Người Dùng   │                    │
│ 📚 Danh Mục     │ STATS CARDS:       │
│ 💳 Thanh Toán   │ ┌─────┬─────┬────┐ │
│ 📊 Báo Cáo      │ │5000 │50K  │1M  │ │
│                 │ │Đề   │Users│Câu │ │
│ [Quay Về]       │ └─────┴─────┴────┘ │
│                 │                    │
│                 │ CHARTS:            │
│                 │ [Bar Chart] [Line] │
└─────────┴──────────────────────────────┘
```

### **Quản Lý Đề Thi (/admin/exams)**

```
┌─────────────────────────────────────────┐
│ Danh Sách Đề Thi        [+ Thêm Đề Thi]│
├─────────────────────────────────────────┤
│ Tên Đề     │ Môn │ Câu │ ĐK │ Giá │...│
├─────────────────────────────────────────┤
│ Toán Cao A │Toán │ 50  │Dễ  │0   │Sửa│
│ Anh Văn B  │Anh  │ 40  │KB  │50k │Sửa│
│ Lý 12      │Lý   │ 60  │Khó │0   │Sửa│
└─────────────────────────────────────────┘

[◄ Trước]  Trang 1 / 5  [Tiếp ►]
```

### **Quản Lý Câu Hỏi**

```
┌──────────────────────────────────────┐
│ Câu Hỏi - Bộ Đề: "Toán Cao Cấp"    │
│                    [+ Thêm Câu Hỏi]  │
├──────────────────────────────────────┤
│ Nội dung         │ Đáp án │ Điểm │ ... │
├──────────────────────────────────────┤
│ "Tính tích phân" │   A    │  1   │ Sửa │
│ "Giải phương ... │   C    │  1   │ Sửa │
│ "Định lý nào..." │   B    │  2   │ Sửa │
└──────────────────────────────────────┘
```

### **Thanh Toán & Doanh Thu (/admin/payments)**

```
┌──────────────────────────────────────┐
│ Tổng Doanh Thu: 1,500,000 VND       │
├──────────────────────────────────────┤
│ Người Dùng │ Đề │ Số Tiền │ Trạng │...
├──────────────────────────────────────┤
│ Nguyễn A   │ T. │  50,000 │Hoàn T│ │
│ Trần B     │ A. │ 100,000 │Hoàn T│ │
│ Phạm C     │ L. │  25,000 │Pending│ │
└──────────────────────────────────────┘
```

---

## 🔧 Cấu Hình & Tuỳ Chỉnh

### **Thay Đổi Màu Sắc**

File: `Views/Shared/_Layout.cshtml`

```html
<script id="tailwind-config">
    tailwind.config = {
        theme: {
            extend: {
                colors: {
                    "primary": "#2b8cee",    // Thay đổi ở đây
                    "success": "#22c55e",
                    "danger": "#ef4444",
                }
            },
        },
    }
</script>
```

### **Thêm Danh Mục Mới**

Admin → Danh Mục → [+ Thêm Danh Mục]

```
Tiêu Đề: "Tin Học"
[Tạo]
```

### **Tạo Bộ Đề Mới**

Admin → Đề Thi → [+ Thêm Đề Thi]

```
Tiêu Đề: "Tin Học Cơ Bản"
Danh Mục: Tin Học
Môn Học: Tin Học
Độ Khó: Dễ
Thời Gian: 60 phút
Giá: 0 (Miễn Phí) hoặc 50000
Premium: ☐ Có ☑ Không
[Tạo]
```

### **Thêm Câu Hỏi**

Admin → Đề Thi → [ID] → Câu Hỏi → [+ Thêm]

```
Nội Dung: "Phần mềm nào là ..."
A: "Microsoft Word"
B: "Adobe Photoshop" ✓
C: "Visual Studio"
D: "Sublime Text"
Điểm: 1
[Lưu]
```

---

## 📊 Thống Kê & Báo Cáo

Admin → Báo Cáo

### **Báo Cáo Theo Dõi:**
- Số bài thi hoàn thành
- Tỷ lệ thành công
- Điểm trung bình
- Bộ đề phổ biến
- Doanh thu

---

## 🎨 Tính Năng Nổi Bật

### **1. Bộ Đếm Thời Gian Thông Minh**
- Tự động nộp bài khi hết giờ
- Cảnh báo khi còn 5 phút
- Đỏ màu và nhấp nháy khi sắp hết

### **2. Điều Hướng Câu Hỏi**
- Lưới câu hỏi 5x5 (có thể thay đổi)
- Nhấp vào số để nhảy tới câu hỏi
- Hiển thị trạng thái: chưa trả lời, đã trả lời

### **3. Xem Lại Đáp Án**
- So sánh đáp án đúng/sai
- Hiển thị từng câu chi tiết
- Giải thích (tương lai)

### **4. Thống Kê Cá Nhân**
- Lịch sử bài thi
- Tiến độ theo bộ đề
- Huy hiệu và thành tựu

---

## 🚨 Xử Lý Sự Cố

### **Lỗi: "Không Thể Kết Nối Database"**
```
✓ Kiểm tra connection string trong appsettings.json
✓ Kiểm tra SQL Server đang chạy
✓ Kiểm tra quyền truy cập database
```

### **Lỗi: "404 Not Found"**
```
✓ Kiểm tra URL có đúng không
✓ Kiểm tra route có được đăng ký
✓ Kiểm tra controller/view tồn tại
```

### **Lỗi: "Timeout"**
```
✓ Tăng timeout trong connection string
✓ Kiểm tra query chậm
✓ Thêm index vào database
```

---

## 📚 Tài Liệu Tham Khảo

- [ASP.NET Core Docs](https://docs.microsoft.com/aspnet/core)
- [Tailwind CSS](https://tailwindcss.com)
- [Entity Framework Core](https://docs.microsoft.com/ef/core)
- [Chart.js](https://www.chartjs.org)

---

**Phiên bản**: 1.0  
**Cập nhật**: 03/12/2024
