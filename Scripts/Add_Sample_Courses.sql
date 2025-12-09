-- Script to add sample courses to the database
-- This script adds several sample courses with different types (free/paid, approved/pending)

-- ============================================
-- STEP 1: DELETE OLD DATA (to avoid duplicates)
-- ============================================
PRINT '';
PRINT 'Deleting old sample courses and related data...';

-- Delete lesson progress for lessons that belong to sample courses
DELETE FROM tb_LessonProgress
WHERE LessonId IN (
    SELECT l.Id 
    FROM tb_Lessons l
    INNER JOIN tb_Courses c ON l.CourseId = c.Id
    WHERE c.Title IN (
        N'Lập Trình C# Cơ Bản',
        N'HTML & CSS Cơ Bản',
        N'JavaScript Cho Người Mới Bắt Đầu',
        N'ASP.NET Core MVC Nâng Cao',
        N'React.js & Redux Mastery',
        N'Database Design & SQL Server',
        N'Python Cho Data Science',
        N'Node.js & Express.js Backend Development'
    )
);

-- Delete lessons for sample courses
DELETE FROM tb_Lessons
WHERE CourseId IN (
    SELECT Id 
    FROM tb_Courses 
    WHERE Title IN (
        N'Lập Trình C# Cơ Bản',
        N'HTML & CSS Cơ Bản',
        N'JavaScript Cho Người Mới Bắt Đầu',
        N'ASP.NET Core MVC Nâng Cao',
        N'React.js & Redux Mastery',
        N'Database Design & SQL Server',
        N'Python Cho Data Science',
        N'Node.js & Express.js Backend Development'
    )
);

-- Delete sample courses
DELETE FROM tb_Courses
WHERE Title IN (
    N'Lập Trình C# Cơ Bản',
    N'HTML & CSS Cơ Bản',
    N'JavaScript Cho Người Mới Bắt Đầu',
    N'ASP.NET Core MVC Nâng Cao',
    N'React.js & Redux Mastery',
    N'Database Design & SQL Server',
    N'Python Cho Data Science',
    N'Node.js & Express.js Backend Development'
);

PRINT 'Old sample courses deleted successfully.';
PRINT '';

-- ============================================
-- STEP 2: INSERT NEW DATA
-- ============================================

-- Get the first available user ID (preferably admin)
DECLARE @CreatedByUserId INT;

-- Try to get admin user first, then any user
SELECT TOP 1 @CreatedByUserId = Id 
FROM tb_Users 
WHERE Role = 'Admin'
ORDER BY Id;

-- If no admin found, get any user
IF @CreatedByUserId IS NULL
BEGIN
    SELECT TOP 1 @CreatedByUserId = Id FROM tb_Users ORDER BY Id;
END

-- If still no user found, you may need to create a user first
IF @CreatedByUserId IS NULL
BEGIN
    PRINT 'Warning: No users found in database. Please create a user first.';
    RETURN;
END

PRINT 'Using User ID: ' + CAST(@CreatedByUserId AS VARCHAR(10)) + ' as course creator';

-- Insert sample courses
INSERT INTO tb_Courses (Title, Description, CreatedBy, IsApproved, CreatedAt, IsPaid, FreeLessonCount)
VALUES
-- Free and Approved Courses
(
    N'Lập Trình C# Cơ Bản',
    N'Khóa học này sẽ giúp bạn nắm vững các kiến thức cơ bản về ngôn ngữ lập trình C#. Bạn sẽ học về cú pháp, biến, hàm, lớp, và các khái niệm lập trình hướng đối tượng. Phù hợp cho người mới bắt đầu.',
    @CreatedByUserId,
    1, -- IsApproved = true
    GETDATE(),
    0, -- IsPaid = false (free)
    3  -- FreeLessonCount = 3 lessons free
),
(
    N'HTML & CSS Cơ Bản',
    N'Học cách xây dựng website từ đầu với HTML và CSS. Khóa học bao gồm các thẻ HTML cơ bản, cách tạo layout với CSS, responsive design, và các kỹ thuật styling hiện đại.',
    @CreatedByUserId,
    1, -- IsApproved = true
    GETDATE(),
    0, -- IsPaid = false (free)
    2  -- FreeLessonCount = 2 lessons free
),
(
    N'JavaScript Cho Người Mới Bắt Đầu',
    N'Khóa học JavaScript từ cơ bản đến nâng cao. Học về DOM manipulation, events, async/await, và các framework phổ biến. Bao gồm nhiều bài tập thực hành.',
    @CreatedByUserId,
    1, -- IsApproved = true
    GETDATE(),
    0, -- IsPaid = false (free)
    4  -- FreeLessonCount = 4 lessons free
),

-- Paid and Approved Courses
(
    N'ASP.NET Core MVC Nâng Cao',
    N'Khóa học chuyên sâu về ASP.NET Core MVC. Học về dependency injection, middleware, authentication, authorization, Entity Framework Core, và deployment. Dành cho developers có kinh nghiệm.',
    @CreatedByUserId,
    1, -- IsApproved = true
    GETDATE(),
    1, -- IsPaid = true (paid)
    2  -- FreeLessonCount = 2 lessons free preview
),
(
    N'React.js & Redux Mastery',
    N'Khóa học toàn diện về React.js và Redux. Học về hooks, context API, state management, routing, và các best practices. Bao gồm dự án thực tế xây dựng ứng dụng e-commerce.',
    @CreatedByUserId,
    1, -- IsApproved = true
    GETDATE(),
    1, -- IsPaid = true (paid)
    3  -- FreeLessonCount = 3 lessons free preview
),
(
    N'Database Design & SQL Server',
    N'Học thiết kế database chuyên nghiệp, normalization, indexing, stored procedures, triggers, và query optimization. Phù hợp cho database administrators và backend developers.',
    @CreatedByUserId,
    1, -- IsApproved = true
    GETDATE(),
    1, -- IsPaid = true (paid)
    2  -- FreeLessonCount = 2 lessons free preview
),

-- Pending Approval Courses
(
    N'Python Cho Data Science',
    N'Khóa học Python chuyên về Data Science với pandas, numpy, matplotlib, và scikit-learn. Học cách phân tích dữ liệu, visualization, và machine learning cơ bản.',
    @CreatedByUserId,
    0, -- IsApproved = false (pending)
    GETDATE(),
    0, -- IsPaid = false (free)
    2  -- FreeLessonCount = 2 lessons free
),
(
    N'Node.js & Express.js Backend Development',
    N'Xây dựng RESTful APIs với Node.js và Express.js. Học về middleware, authentication với JWT, database integration, error handling, và testing.',
    @CreatedByUserId,
    0, -- IsApproved = false (pending)
    GETDATE(),
    1, -- IsPaid = true (paid)
    2  -- FreeLessonCount = 2 lessons free preview
);

PRINT 'Successfully added 8 sample courses!';
PRINT '  - 3 Free & Approved courses';
PRINT '  - 3 Paid & Approved courses';
PRINT '  - 2 Pending approval courses (1 free, 1 paid)';

-- Now add lessons for each course
PRINT '';
PRINT 'Adding lessons for courses...';

-- Get course IDs (assuming they were just inserted)
DECLARE @CourseId1 INT, @CourseId2 INT, @CourseId3 INT, @CourseId4 INT, @CourseId5 INT, @CourseId6 INT, @CourseId7 INT, @CourseId8 INT;

-- Get the course IDs in order
SELECT TOP 1 @CourseId1 = Id FROM tb_Courses WHERE Title = N'Lập Trình C# Cơ Bản' ORDER BY Id DESC;
SELECT TOP 1 @CourseId2 = Id FROM tb_Courses WHERE Title = N'HTML & CSS Cơ Bản' ORDER BY Id DESC;
SELECT TOP 1 @CourseId3 = Id FROM tb_Courses WHERE Title = N'JavaScript Cho Người Mới Bắt Đầu' ORDER BY Id DESC;
SELECT TOP 1 @CourseId4 = Id FROM tb_Courses WHERE Title = N'ASP.NET Core MVC Nâng Cao' ORDER BY Id DESC;
SELECT TOP 1 @CourseId5 = Id FROM tb_Courses WHERE Title = N'React.js & Redux Mastery' ORDER BY Id DESC;
SELECT TOP 1 @CourseId6 = Id FROM tb_Courses WHERE Title = N'Database Design & SQL Server' ORDER BY Id DESC;
SELECT TOP 1 @CourseId7 = Id FROM tb_Courses WHERE Title = N'Python Cho Data Science' ORDER BY Id DESC;
SELECT TOP 1 @CourseId8 = Id FROM tb_Courses WHERE Title = N'Node.js & Express.js Backend Development' ORDER BY Id DESC;

-- Insert lessons for each course
-- Course 1: Lập Trình C# Cơ Bản
IF @CourseId1 IS NOT NULL
BEGIN
    INSERT INTO tb_Lessons (CourseId, Title, Content, CreatedBy, IsApproved, CreatedAt, IsPreview)
    VALUES
    (@CourseId1, N'Giới Thiệu C#', N'<!-- YOUTUBE: https://www.youtube.com/watch?v=GhQdlIFylQ8 -->
<h2>Giới Thiệu C#</h2>
<p>C# là ngôn ngữ lập trình hướng đối tượng mạnh mẽ của Microsoft. Được thiết kế để xây dựng các ứng dụng Windows, web và mobile.</p>

<h3>💪 Điểm mạnh của C#</h3>
<ul>
    <li>Type-safe và memory-safe</li>
    <li>Hỗ trợ đầy đủ OOP</li>
    <li>Ecosystem phong phú (.NET)</li>
    <li>Hiệu suất cao</li>
</ul>

<h3>📝 Cú Pháp Cơ Bản</h3>
<div class="bg-green-50 dark:bg-green-900/20 p-4 rounded-lg my-4">
    <p class="font-semibold mb-2">Các khái niệm cơ bản:</p>
    <ul class="space-y-1">
        <li><strong>Khai báo biến:</strong> <code>int age = 25;</code>, <code>string name = "John";</code></li>
        <li><strong>Vòng lặp:</strong> <code>for</code>, <code>while</code>, <code>foreach</code></li>
        <li><strong>Điều kiện:</strong> <code>if</code>, <code>else</code>, <code>switch</code></li>
    </ul>
</div>

<h3>💡 Ví dụ Hello World</h3>
<pre class="bg-gray-100 dark:bg-gray-800 p-4 rounded-lg"><code>using System;

class Program
{
    static void Main()
    {
        Console.WriteLine("Xin chào, thế giới!");
    }
}</code></pre>', @CreatedByUserId, 1, GETDATE(), 1),
    (@CourseId1, N'Lập Trình Hướng Đối Tượng', N'<!-- YOUTUBE: https://www.youtube.com/watch?v=ZqgPoEkTZgY -->
<h2>OOP trong C#</h2>
<p>Lập trình hướng đối tượng (OOP) là phương pháp lập trình dựa trên khái niệm "đối tượng". C# hỗ trợ đầy đủ 4 nguyên lý của OOP.</p>

<h3>🏛️ 4 Nguyên Lý OOP</h3>
<ul>
    <li><strong>Encapsulation (Đóng gói)</strong> - Ẩn chi tiết triển khai</li>
    <li><strong>Inheritance (Kế thừa)</strong> - Class con kế thừa từ class cha</li>
    <li><strong>Polymorphism (Đa hình)</strong> - Một interface có nhiều cách triển khai</li>
    <li><strong>Abstraction (Trừu tượng)</strong> - Tập trung vào những gì đối tượng làm</li>
</ul>

<h3>💡 Ví dụ Class và Object</h3>
<pre class="bg-gray-100 dark:bg-gray-800 p-4 rounded-lg"><code>public class Person
{
    public string Name { get; set; }
    public int Age { get; set; }
    
    public void Introduce()
    {
        Console.WriteLine($"Tôi là {Name}, {Age} tuổi.");
    }
}</code></pre>', @CreatedByUserId, 1, GETDATE(), 1),
    (@CourseId1, N'LINQ và Collections', N'<h2>LINQ và Collections</h2>
<p>Language Integrated Query để truy vấn dữ liệu một cách dễ dàng và mạnh mẽ.</p>

<h3>🔍 LINQ Methods</h3>
<ul>
    <li><code>Where()</code> - Lọc các phần tử thỏa điều kiện</li>
    <li><code>Select()</code> - Chọn và biến đổi phần tử</li>
    <li><code>OrderBy()</code> - Sắp xếp tăng dần</li>
    <li><code>GroupBy()</code> - Nhóm các phần tử</li>
</ul>

<h3>💡 Ví dụ</h3>
<pre class="bg-gray-100 dark:bg-gray-800 p-4 rounded-lg"><code>var numbers = new[] { 1, 2, 3, 4, 5 };
var evens = numbers.Where(n => n % 2 == 0);
var doubled = numbers.Select(n => n * 2);</code></pre>', @CreatedByUserId, 1, GETDATE(), 0);
END

-- Course 2: HTML & CSS Cơ Bản
IF @CourseId2 IS NOT NULL
BEGIN
    INSERT INTO tb_Lessons (CourseId, Title, Content, CreatedBy, IsApproved, CreatedAt, IsPreview)
    VALUES
    (@CourseId2, N'Giới Thiệu HTML', N'<!-- YOUTUBE: https://www.youtube.com/watch?v=UB1O30fR-EE -->
<h2>Giới Thiệu HTML</h2>
<p>HTML (HyperText Markup Language) là ngôn ngữ đánh dấu tiêu chuẩn để tạo trang web. Đây là nền tảng của mọi website trên internet.</p>

<h3>🎯 Mục tiêu bài học</h3>
<ul>
    <li>Hiểu được HTML là gì và vai trò của nó</li>
    <li>Nắm vững cấu trúc cơ bản của một tài liệu HTML</li>
    <li>Biết cách sử dụng các thẻ HTML phổ biến</li>
</ul>

<h3>📚 Các Thẻ Cơ Bản</h3>
<ul>
    <li><code>&lt;html&gt;</code> - Thẻ gốc, bao bọc toàn bộ tài liệu</li>
    <li><code>&lt;head&gt;</code> - Phần đầu, chứa metadata</li>
    <li><code>&lt;body&gt;</code> - Phần thân, chứa nội dung hiển thị</li>
    <li><code>&lt;h1&gt;</code> đến <code>&lt;h6&gt;</code> - Tiêu đề (heading)</li>
    <li><code>&lt;p&gt;</code> - Đoạn văn (paragraph)</li>
    <li><code>&lt;a&gt;</code> - Liên kết (anchor)</li>
    <li><code>&lt;img&gt;</code> - Hình ảnh (image)</li>
</ul>

<h3>💡 Ví dụ thực hành</h3>
<pre class="bg-gray-100 dark:bg-gray-800 p-4 rounded-lg"><code>&lt;!DOCTYPE html&gt;
&lt;html&gt;
&lt;head&gt;
    &lt;title&gt;Trang Web Đầu Tiên&lt;/title&gt;
&lt;/head&gt;
&lt;body&gt;
    &lt;h1&gt;Chào mừng đến với HTML!&lt;/h1&gt;
    &lt;p&gt;Đây là đoạn văn đầu tiên của bạn.&lt;/p&gt;
&lt;/body&gt;
&lt;/html&gt;</code></pre>', @CreatedByUserId, 1, GETDATE(), 1),
    (@CourseId2, N'CSS Cơ Bản', N'<!-- YOUTUBE: https://www.youtube.com/watch?v=1Rs2ND1ryYc -->
<h2>CSS (Cascading Style Sheets)</h2>
<p>CSS được sử dụng để định dạng và bố cục trang web. Với CSS, bạn có thể biến một trang web đơn giản thành một tác phẩm nghệ thuật!</p>

<h3>🎨 Tại sao cần CSS?</h3>
<ul>
    <li>Tách biệt nội dung và giao diện</li>
    <li>Dễ dàng bảo trì và cập nhật</li>
    <li>Tạo giao diện đẹp mắt và chuyên nghiệp</li>
    <li>Responsive design cho mọi thiết bị</li>
</ul>

<h3>🔍 Selectors (Bộ chọn)</h3>
<ul>
    <li><strong>Element Selectors</strong> - Chọn theo tên thẻ: <code>p { color: blue; }</code></li>
    <li><strong>Class Selectors</strong> - Chọn theo class: <code>.my-class { font-size: 16px; }</code></li>
    <li><strong>ID Selectors</strong> - Chọn theo ID: <code>#my-id { background: red; }</code></li>
</ul>

<h3>💡 Ví dụ thực hành</h3>
<pre class="bg-gray-100 dark:bg-gray-800 p-4 rounded-lg"><code>h1 {
    color: #2b8cee;
    font-size: 2rem;
    text-align: center;
}

p {
    color: #333;
    line-height: 1.6;
}</code></pre>', @CreatedByUserId, 1, GETDATE(), 1);
END

-- Course 3: JavaScript Cho Người Mới Bắt Đầu
IF @CourseId3 IS NOT NULL
BEGIN
    INSERT INTO tb_Lessons (CourseId, Title, Content, CreatedBy, IsApproved, CreatedAt, IsPreview)
    VALUES
    (@CourseId3, N'JavaScript Cơ Bản', N'<!-- YOUTUBE: https://www.youtube.com/watch?v=W6NZfCO5SIk -->
<h2>JavaScript Cơ Bản</h2>
<p>JavaScript là ngôn ngữ lập trình phía client để tạo tương tác cho trang web. Đây là ngôn ngữ phổ biến nhất trên thế giới!</p>

<h3>🚀 Tại sao học JavaScript?</h3>
<ul>
    <li>Ngôn ngữ duy nhất chạy trên trình duyệt</li>
    <li>Có thể làm việc cả frontend và backend (Node.js)</li>
    <li>Hệ sinh thái thư viện phong phú</li>
    <li>Cơ hội việc làm cao</li>
</ul>

<h3>📦 Biến và Kiểu Dữ Liệu</h3>
<ul>
    <li><code>var</code>, <code>let</code>, <code>const</code> - Khai báo biến</li>
    <li><code>Number</code>, <code>String</code>, <code>Boolean</code> - Kiểu dữ liệu cơ bản</li>
    <li><code>Array</code>, <code>Object</code> - Cấu trúc dữ liệu</li>
</ul>

<h3>💡 Ví dụ</h3>
<pre class="bg-gray-100 dark:bg-gray-800 p-4 rounded-lg"><code>let userName = "Nguyễn Văn A";
const age = 25;
let fruits = ["Táo", "Chuối", "Cam"];</code></pre>', @CreatedByUserId, 1, GETDATE(), 1),
    (@CourseId3, N'DOM Manipulation', N'<!-- YOUTUBE: https://www.youtube.com/watch?v=0ik6X4DJKCc -->
<h2>DOM Manipulation</h2>
<p>Thao tác với Document Object Model để thay đổi nội dung trang web một cách động.</p>

<h3>🎯 Methods phổ biến</h3>
<ul>
    <li><code>getElementById()</code> - Lấy element theo ID</li>
    <li><code>querySelector()</code> - Lấy element theo CSS selector</li>
    <li><code>addEventListener()</code> - Thêm event listener</li>
    <li><code>innerHTML</code> - Thay đổi nội dung HTML</li>
</ul>

<h3>💡 Ví dụ</h3>
<pre class="bg-gray-100 dark:bg-gray-800 p-4 rounded-lg"><code>// Lấy element
const button = document.getElementById("myButton");

// Thêm event listener
button.addEventListener("click", function() {
    alert("Button được click!");
});</code></pre>', @CreatedByUserId, 1, GETDATE(), 1),
    (@CourseId3, N'Async/Await', N'<h2>Async/Await</h2>
<p>Xử lý bất đồng bộ trong JavaScript một cách dễ đọc và dễ hiểu hơn.</p>

<h3>🔑 Concepts</h3>
<ul>
    <li><code>Promise</code> - Lời hứa xử lý bất đồng bộ</li>
    <li><code>async function</code> - Hàm bất đồng bộ</li>
    <li><code>await</code> - Chờ kết quả từ Promise</li>
</ul>

<h3>💡 Ví dụ</h3>
<pre class="bg-gray-100 dark:bg-gray-800 p-4 rounded-lg"><code>async function fetchData() {
    const response = await fetch("https://api.example.com/data");
    const data = await response.json();
    return data;
}</code></pre>', @CreatedByUserId, 1, GETDATE(), 0),
    (@CourseId3, N'ES6+ Features', N'<h2>ES6+ Features</h2>
<p>Các tính năng mới trong JavaScript ES6+ giúp code ngắn gọn và mạnh mẽ hơn.</p>

<h3>✨ Features</h3>
<ul>
    <li><code>Arrow Functions</code> - Hàm mũi tên: <code>() => {}</code></li>
    <li><code>Template Literals</code> - Chuỗi template: <code>`Hello ${name}`</code></li>
    <li><code>Destructuring</code> - Phân rã: <code>const {name, age} = person</code></li>
    <li><code>Spread Operator</code> - Toán tử trải rộng: <code>[...array]</code></li>
</ul>', @CreatedByUserId, 1, GETDATE(), 0);
END

-- Course 4: ASP.NET Core MVC Nâng Cao
IF @CourseId4 IS NOT NULL
BEGIN
    INSERT INTO tb_Lessons (CourseId, Title, Content, CreatedBy, IsApproved, CreatedAt, IsPreview)
    VALUES
    (@CourseId4, N'Giới Thiệu ASP.NET Core MVC', N'<h2>ASP.NET Core MVC</h2><p>Framework web của Microsoft để xây dựng ứng dụng web.</p><h3>MVC Pattern</h3><ul><li>Model - Dữ liệu và logic nghiệp vụ</li><li>View - Giao diện người dùng</li><li>Controller - Xử lý request</li></ul>', @CreatedByUserId, 1, GETDATE(), 1),
    (@CourseId4, N'Dependency Injection', N'<h2>Dependency Injection</h2><p>Pattern để quản lý dependencies trong ứng dụng.</p><h3>Concepts</h3><ul><li>Service Registration - Đăng ký service</li><li>Constructor Injection - Inject qua constructor</li><li>Service Lifetime - Singleton, Scoped, Transient</li></ul>', @CreatedByUserId, 1, GETDATE(), 1),
    (@CourseId4, N'Entity Framework Core', N'<h2>Entity Framework Core</h2><p>ORM framework để làm việc với database.</p><h3>Features</h3><ul><li>Code First - Tạo DB từ code</li><li>Migrations - Quản lý thay đổi schema</li><li>LINQ to SQL - Truy vấn bằng LINQ</li></ul>', @CreatedByUserId, 1, GETDATE(), 0);
END

-- Course 5: React.js & Redux Mastery
IF @CourseId5 IS NOT NULL
BEGIN
    INSERT INTO tb_Lessons (CourseId, Title, Content, CreatedBy, IsApproved, CreatedAt, IsPreview)
    VALUES
    (@CourseId5, N'Giới Thiệu React.js', N'<h2>React.js</h2><p>Thư viện JavaScript để xây dựng giao diện người dùng.</p><h3>Concepts</h3><ul><li>Components - Thành phần</li><li>JSX - JavaScript XML</li><li>Props - Thuộc tính</li><li>State - Trạng thái</li></ul>', @CreatedByUserId, 1, GETDATE(), 1),
    (@CourseId5, N'Hooks trong React', N'<h2>React Hooks</h2><p>Các hooks để quản lý state và side effects.</p><h3>Hooks</h3><ul><li>useState - Quản lý state</li><li>useEffect - Side effects</li><li>useContext - Context API</li><li>useReducer - State management phức tạp</li></ul>', @CreatedByUserId, 1, GETDATE(), 1),
    (@CourseId5, N'Redux State Management', N'<h2>Redux</h2><p>Thư viện quản lý state cho ứng dụng JavaScript.</p><h3>Concepts</h3><ul><li>Store - Kho lưu trữ state</li><li>Actions - Hành động</li><li>Reducers - Xử lý actions</li><li>Dispatch - Gửi actions</li></ul>', @CreatedByUserId, 1, GETDATE(), 0);
END

-- Course 6: Database Design & SQL Server
IF @CourseId6 IS NOT NULL
BEGIN
    INSERT INTO tb_Lessons (CourseId, Title, Content, CreatedBy, IsApproved, CreatedAt, IsPreview)
    VALUES
    (@CourseId6, N'Thiết Kế Database', N'<h2>Database Design</h2><p>Nguyên tắc thiết kế database hiệu quả.</p><h3>Concepts</h3><ul><li>Normalization - Chuẩn hóa</li><li>Primary Key - Khóa chính</li><li>Foreign Key - Khóa ngoại</li><li>Indexes - Chỉ mục</li></ul>', @CreatedByUserId, 1, GETDATE(), 1),
    (@CourseId6, N'SQL Queries', N'<h2>SQL Queries</h2><p>Các câu lệnh SQL cơ bản và nâng cao.</p><h3>Statements</h3><ul><li>SELECT - Truy vấn</li><li>INSERT - Thêm dữ liệu</li><li>UPDATE - Cập nhật</li><li>DELETE - Xóa</li><li>JOIN - Kết nối bảng</li></ul>', @CreatedByUserId, 1, GETDATE(), 1),
    (@CourseId6, N'Stored Procedures và Triggers', N'<h2>Stored Procedures & Triggers</h2><p>Tạo và sử dụng stored procedures và triggers.</p><h3>Features</h3><ul><li>Stored Procedures - Thủ tục lưu trữ</li><li>Triggers - Kích hoạt tự động</li><li>Functions - Hàm SQL</li></ul>', @CreatedByUserId, 1, GETDATE(), 0);
END

-- Course 7: Python Cho Data Science
IF @CourseId7 IS NOT NULL
BEGIN
    INSERT INTO tb_Lessons (CourseId, Title, Content, CreatedBy, IsApproved, CreatedAt, IsPreview)
    VALUES
    (@CourseId7, N'Giới Thiệu Python', N'<h2>Python Cho Data Science</h2><p>Python là ngôn ngữ lập trình mạnh mẽ cho phân tích dữ liệu.</p><h3>Libraries</h3><ul><li>pandas - Xử lý dữ liệu</li><li>numpy - Tính toán số học</li><li>matplotlib - Visualization</li></ul>', @CreatedByUserId, 0, GETDATE(), 1),
    (@CourseId7, N'Data Analysis với Pandas', N'<h2>Pandas</h2><p>Thư viện pandas để phân tích và xử lý dữ liệu.</p><h3>Features</h3><ul><li>DataFrame - Cấu trúc dữ liệu</li><li>Data Cleaning - Làm sạch dữ liệu</li><li>Data Aggregation - Tổng hợp dữ liệu</li></ul>', @CreatedByUserId, 0, GETDATE(), 1);
END

-- Course 8: Node.js & Express.js Backend Development
IF @CourseId8 IS NOT NULL
BEGIN
    INSERT INTO tb_Lessons (CourseId, Title, Content, CreatedBy, IsApproved, CreatedAt, IsPreview)
    VALUES
    (@CourseId8, N'Giới Thiệu Node.js', N'<h2>Node.js</h2><p>Node.js là runtime environment để chạy JavaScript phía server.</p><h3>Concepts</h3><ul><li>Event Loop - Vòng lặp sự kiện</li><li>Modules - Mô-đun</li><li>NPM - Node Package Manager</li></ul>', @CreatedByUserId, 0, GETDATE(), 1),
    (@CourseId8, N'Express.js Framework', N'<h2>Express.js</h2><p>Framework web cho Node.js.</p><h3>Features</h3><ul><li>Routing - Định tuyến</li><li>Middleware - Phần mềm trung gian</li><li>Template Engines - Engine template</li></ul>', @CreatedByUserId, 0, GETDATE(), 1);
END

PRINT 'Successfully added lessons for all courses!';
