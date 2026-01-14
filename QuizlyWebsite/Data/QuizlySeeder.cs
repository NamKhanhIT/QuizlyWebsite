using QuizlyWebsite.Models;
using System.Security.Cryptography;
using System.Text;

namespace QuizlyWebsite.Data
{
    public class QuizlySeeder
    {
        private readonly QuizlyDbContext _context;
        private readonly ILogger<QuizlySeeder> _logger;

        public QuizlySeeder(QuizlyDbContext context, ILogger<QuizlySeeder> logger)
        {
            _context = context;
            _logger = logger;
        }

        // Simple password hashing function (replace with BCrypt if available)
        private static string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(hashedBytes);
            }
        }

        // Helper method to create lessons for a course
        private static List<TbLesson> GetLessonsForCourse(TbCourse course, int createdBy)
        {
            var lessons = new List<TbLesson>();
            var courseTitle = course.Title.ToLower();

            if (courseTitle.Contains("web development") || courseTitle.Contains("html") || courseTitle.Contains("css"))
            {
                lessons.Add(new TbLesson
                {
                    CourseId = course.Id,
                    Title = "Giới Thiệu HTML",
                    Content = @"<!-- YOUTUBE: https://www.youtube.com/watch?v=UB1O30fR-EE -->
                        <h2>Giới Thiệu HTML</h2>
                        <p>HTML (HyperText Markup Language) là ngôn ngữ đánh dấu tiêu chuẩn để tạo trang web. Đây là nền tảng của mọi website trên internet.</p>

                        <h3>🎯 Mục tiêu bài học</h3>
                        <ul>
                            <li>Hiểu được HTML là gì và vai trò của nó</li>
                            <li>Nắm vững cấu trúc cơ bản của một tài liệu HTML</li>
                            <li>Biết cách sử dụng các thẻ HTML phổ biến</li>
                        </ul>

                        <h3>📚 Các Thẻ Cơ Bản</h3>
                        <div class=""bg-blue-50 dark:bg-blue-900/20 p-4 rounded-lg my-4"">
                            <p class=""font-semibold mb-2"">Cấu trúc HTML cơ bản:</p>
                            <ul class=""space-y-1"">
                                <li><code>&lt;html&gt;</code> - Thẻ gốc, bao bọc toàn bộ tài liệu</li>
                                <li><code>&lt;head&gt;</code> - Phần đầu, chứa metadata</li>
                                <li><code>&lt;body&gt;</code> - Phần thân, chứa nội dung hiển thị</li>
                                <li><code>&lt;h1&gt;</code> đến <code>&lt;h6&gt;</code> - Tiêu đề (heading)</li>
                                <li><code>&lt;p&gt;</code> - Đoạn văn (paragraph)</li>
                                <li><code>&lt;a&gt;</code> - Liên kết (anchor)</li>
                                <li><code>&lt;img&gt;</code> - Hình ảnh (image)</li>
                            </ul>
                        </div>

                        <h3>💡 Ví dụ thực hành</h3>
                        <pre class=""bg-gray-100 dark:bg-gray-800 p-4 rounded-lg overflow-x-auto""><code>&lt;!DOCTYPE html&gt;
                        &lt;html&gt;
                        &lt;head&gt;
                            &lt;title&gt;Trang Web Đầu Tiên&lt;/title&gt;
                        &lt;/head&gt;
                        &lt;body&gt;
                            &lt;h1&gt;Chào mừng đến với HTML!&lt;/h1&gt;
                            &lt;p&gt;Đây là đoạn văn đầu tiên của bạn.&lt;/p&gt;
                        &lt;/body&gt;
                        &lt;/html&gt;</code></pre>

                        <h3>✅ Bài tập</h3>
                        <p>Tạo một trang HTML đơn giản với tiêu đề và 3 đoạn văn về bản thân bạn.</p>",
                    CreatedBy = createdBy,
                    CreatedAt = DateTime.Now,
                    IsApproved = course.IsApproved == true,
                    IsPreview = true
                });
                lessons.Add(new TbLesson
                {
                    CourseId = course.Id,
                    Title = "CSS Cơ Bản",
                    Content = @"<!-- YOUTUBE: https://www.youtube.com/watch?v=1Rs2ND1ryYc -->
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
                        <div class=""bg-purple-50 dark:bg-purple-900/20 p-4 rounded-lg my-4"">
                            <p class=""font-semibold mb-2"">Các loại selector phổ biến:</p>
                            <ul class=""space-y-2"">
                                <li><strong>Element Selectors</strong> - Chọn theo tên thẻ: <code>p { color: blue; }</code></li>
                                <li><strong>Class Selectors</strong> - Chọn theo class: <code>.my-class { font-size: 16px; }</code></li>
                                <li><strong>ID Selectors</strong> - Chọn theo ID: <code>#my-id { background: red; }</code></li>
                                <li><strong>Descendant Selectors</strong> - Chọn phần tử con: <code>div p { margin: 10px; }</code></li>
                            </ul>
                        </div>

                        <h3>🎯 Properties (Thuộc tính) quan trọng</h3>
                        <table class=""w-full border-collapse border border-gray-300 dark:border-gray-600 my-4"">
                            <thead>
                                <tr class=""bg-gray-100 dark:bg-gray-800"">
                                    <th class=""border border-gray-300 dark:border-gray-600 p-2 text-left"">Property</th>
                                    <th class=""border border-gray-300 dark:border-gray-600 p-2 text-left"">Mô tả</th>
                                </tr>
                            </thead>
                            <tbody>
                                <tr>
                                    <td class=""border border-gray-300 dark:border-gray-600 p-2""><code>color</code></td>
                                    <td class=""border border-gray-300 dark:border-gray-600 p-2"">Màu chữ</td>
                                </tr>
                                <tr>
                                    <td class=""border border-gray-300 dark:border-gray-600 p-2""><code>background-color</code></td>
                                    <td class=""border border-gray-300 dark:border-gray-600 p-2"">Màu nền</td>
                                </tr>
                                <tr>
                                    <td class=""border border-gray-300 dark:border-gray-600 p-2""><code>font-size</code></td>
                                    <td class=""border border-gray-300 dark:border-gray-600 p-2"">Kích thước chữ</td>
                                </tr>
                                <tr>
                                    <td class=""border border-gray-300 dark:border-gray-600 p-2""><code>margin</code></td>
                                    <td class=""border border-gray-300 dark:border-gray-600 p-2"">Khoảng cách bên ngoài</td>
                                </tr>
                                <tr>
                                    <td class=""border border-gray-300 dark:border-gray-600 p-2""><code>padding</code></td>
                                    <td class=""border border-gray-300 dark:border-gray-600 p-2"">Khoảng cách bên trong</td>
                                </tr>
                            </tbody>
                        </table>

                        <h3>💡 Ví dụ thực hành</h3>
                        <pre class=""bg-gray-100 dark:bg-gray-800 p-4 rounded-lg overflow-x-auto""><code>/* Style cho tiêu đề */
                        h1 {
                            color: #2b8cee;
                            font-size: 2rem;
                            text-align: center;
                        }

                        /* Style cho đoạn văn */
                        p {
                            color: #333;
                            line-height: 1.6;
                            margin: 1rem 0;
                        }

                        /* Style cho class highlight */
                        .highlight {
                            background-color: yellow;
                            padding: 2px 4px;
                            font-weight: bold;
                        }</code></pre>

                        <h3>✅ Bài tập</h3>
                        <p>Tạo một file CSS để style trang HTML bạn đã tạo ở bài trước. Thêm màu sắc, font chữ và spacing phù hợp.</p>",
                    CreatedBy = createdBy,
                    CreatedAt = DateTime.Now,
                    IsApproved = course.IsApproved == true,
                    IsPreview = true
                });
                lessons.Add(new TbLesson
                {
                    CourseId = course.Id,
                    Title = "Layout với Flexbox",
                    Content = "<h2>Flexbox Layout</h2><p>Flexbox giúp tạo layout linh hoạt và responsive.</p><h3>Container Properties</h3><ul><li>display: flex</li><li>flex-direction: row, column</li><li>justify-content: center, space-between</li><li>align-items: center, stretch</li></ul>",
                    CreatedBy = createdBy,
                    CreatedAt = DateTime.Now,
                    IsApproved = course.IsApproved == true,
                    IsPreview = false
                });
            }
            else if (courseTitle.Contains("javascript"))
            {
                lessons.Add(new TbLesson
                {
                    CourseId = course.Id,
                    Title = "JavaScript Cơ Bản",
                    Content = @"<!-- YOUTUBE: https://www.youtube.com/watch?v=W6NZfCO5SIk -->
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
                        <div class=""bg-yellow-50 dark:bg-yellow-900/20 p-4 rounded-lg my-4"">
                            <p class=""font-semibold mb-2"">Cách khai báo biến:</p>
                            <ul class=""space-y-1"">
                                <li><code>var</code> - Khai báo biến (ES5, không nên dùng)</li>
                                <li><code>let</code> - Khai báo biến có thể thay đổi (ES6+)</li>
                                <li><code>const</code> - Khai báo hằng số (ES6+)</li>
                            </ul>
                        </div>

                        <h3>🔢 Kiểu Dữ Liệu</h3>
                        <table class=""w-full border-collapse border border-gray-300 dark:border-gray-600 my-4"">
                            <thead>
                                <tr class=""bg-gray-100 dark:bg-gray-800"">
                                    <th class=""border border-gray-300 dark:border-gray-600 p-2 text-left"">Kiểu</th>
                                    <th class=""border border-gray-300 dark:border-gray-600 p-2 text-left"">Ví dụ</th>
                                </tr>
                            </thead>
                            <tbody>
                                <tr>
                                    <td class=""border border-gray-300 dark:border-gray-600 p-2""><code>Number</code></td>
                                    <td class=""border border-gray-300 dark:border-gray-600 p-2""><code>42, 3.14</code></td>
                                </tr>
                                <tr>
                                    <td class=""border border-gray-300 dark:border-gray-600 p-2""><code>String</code></td>
                                    <td class=""border border-gray-300 dark:border-gray-600 p-2""><code>""Hello"", 'World'</code></td>
                                </tr>
                                <tr>
                                    <td class=""border border-gray-300 dark:border-gray-600 p-2""><code>Boolean</code></td>
                                    <td class=""border border-gray-300 dark:border-gray-600 p-2""><code>true, false</code></td>
                                </tr>
                                <tr>
                                    <td class=""border border-gray-300 dark:border-gray-600 p-2""><code>Array</code></td>
                                    <td class=""border border-gray-300 dark:border-gray-600 p-2""><code>[1, 2, 3]</code></td>
                                </tr>
                                <tr>
                                    <td class=""border border-gray-300 dark:border-gray-600 p-2""><code>Object</code></td>
                                    <td class=""border border-gray-300 dark:border-gray-600 p-2""><code>{name: ""John""}</code></td>
                                </tr>
                            </tbody>
                        </table>

                        <h3>💡 Ví dụ thực hành</h3>
                        <pre class=""bg-gray-100 dark:bg-gray-800 p-4 rounded-lg overflow-x-auto""><code>// Khai báo biến
                        let userName = ""Nguyễn Văn A"";
                        const age = 25;
                        let isActive = true;

                        // Mảng
                        let fruits = [""Táo"", ""Chuối"", ""Cam""];

                        // Object
                        let person = {
                            name: ""Nguyễn Văn A"",
                            age: 25,
                            city: ""Hà Nội""
                        };

                        // In ra console
                        console.log(""Xin chào "", userName);
                        console.log(""Tuổi:"", age);</code></pre>

                        <h3>✅ Bài tập</h3>
                        <p>Tạo các biến để lưu thông tin của bạn (tên, tuổi, thành phố) và in ra console.</p>",
                    CreatedBy = createdBy,
                    CreatedAt = DateTime.Now,
                    IsApproved = course.IsApproved == true,
                    IsPreview = true
                });
                lessons.Add(new TbLesson
                {
                    CourseId = course.Id,
                    Title = "DOM Manipulation",
                    Content = @"<!-- YOUTUBE: https://www.youtube.com/watch?v=0ik6X4DJKCc -->
                    <h2>DOM Manipulation</h2>
                    <p>Thao tác với Document Object Model để thay đổi nội dung trang web một cách động. Đây là kỹ năng quan trọng để tạo tương tác cho website.</p>

                    <h3>🎯 Methods phổ biến</h3>
                    <div class=""bg-indigo-50 dark:bg-indigo-900/20 p-4 rounded-lg my-4"">
                        <p class=""font-semibold mb-2"">Các phương thức DOM quan trọng:</p>
                        <ul class=""space-y-2"">
                            <li><code>getElementById()</code> - Lấy element theo ID</li>
                            <li><code>querySelector()</code> - Lấy element theo CSS selector</li>
                            <li><code>querySelectorAll()</code> - Lấy tất cả elements</li>
                            <li><code>addEventListener()</code> - Thêm event listener</li>
                            <li><code>innerHTML</code> - Thay đổi nội dung HTML</li>
                            <li><code>textContent</code> - Thay đổi nội dung text</li>
                        </ul>
                    </div>

                    <h3>💡 Ví dụ thực hành</h3>
                    <pre class=""bg-gray-100 dark:bg-gray-800 p-4 rounded-lg overflow-x-auto""><code>// Lấy element
                    const button = document.getElementById(""myButton"");
                    const heading = document.querySelector(""h1"");

                    // Thêm event listener
                    button.addEventListener(""click"", function() {
                        heading.textContent = ""Button được click!"";
                        heading.style.color = ""blue"";
                    });

                    // Thay đổi nội dung
                    document.getElementById(""demo"").innerHTML = ""Nội dung mới"";</code></pre>

                    <h3>✅ Bài tập</h3>
                    <p>Tạo một button và khi click sẽ thay đổi màu và nội dung của một đoạn văn.</p>",
                    CreatedBy = createdBy,
                    CreatedAt = DateTime.Now,
                    IsApproved = course.IsApproved == true,
                    IsPreview = true
                });
                lessons.Add(new TbLesson
                {
                    CourseId = course.Id,
                    Title = "Async/Await",
                    Content = "<h2>Async/Await</h2><p>Xử lý bất đồng bộ trong JavaScript.</p><h3>Concepts</h3><ul><li>Promise - Lời hứa xử lý bất đồng bộ</li><li>async function - Hàm bất đồng bộ</li><li>await - Chờ kết quả</li></ul>",
                    CreatedBy = createdBy,
                    CreatedAt = DateTime.Now,
                    IsApproved = course.IsApproved == true,
                    IsPreview = false
                });
                lessons.Add(new TbLesson
                {
                    CourseId = course.Id,
                    Title = "ES6+ Features",
                    Content = "<h2>ES6+ Features</h2><p>Các tính năng mới trong JavaScript ES6+.</p><h3>Features</h3><ul><li>Arrow Functions - Hàm mũi tên</li><li>Template Literals - Chuỗi template</li><li>Destructuring - Phân rã</li><li>Spread Operator - Toán tử trải rộng</li></ul>",
                    CreatedBy = createdBy,
                    CreatedAt = DateTime.Now,
                    IsApproved = course.IsApproved == true,
                    IsPreview = false
                });
            }
            else if (courseTitle.Contains("c#") || courseTitle.Contains("csharp"))
            {
                lessons.Add(new TbLesson
                {
                    CourseId = course.Id,
                    Title = "Giới Thiệu C#",
                    Content = @"<!-- YOUTUBE: https://www.youtube.com/watch?v=GhQdlIFylQ8 -->
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
                        <div class=""bg-green-50 dark:bg-green-900/20 p-4 rounded-lg my-4"">
                            <p class=""font-semibold mb-2"">Các khái niệm cơ bản:</p>
                            <ul class=""space-y-1"">
                                <li><strong>Khai báo biến:</strong> <code>int age = 25;</code>, <code>string name = ""John"";</code></li>
                                <li><strong>Vòng lặp:</strong> <code>for</code>, <code>while</code>, <code>foreach</code></li>
                                <li><strong>Điều kiện:</strong> <code>if</code>, <code>else</code>, <code>switch</code></li>
                                <li><strong>Hàm:</strong> <code>public void MyMethod() { }</code></li>
                            </ul>
                        </div>

                        <h3>💡 Ví dụ Hello World</h3>
                        <pre class=""bg-gray-100 dark:bg-gray-800 p-4 rounded-lg overflow-x-auto""><code>using System;

                        class Program
                        {
                            static void Main()
                            {
                                Console.WriteLine(""Xin chào, thế giới!"");
                                Console.WriteLine(""Chào mừng đến với C#!"");
                            }
                        }</code></pre>

                        <h3>✅ Bài tập</h3>
                        <p>Tạo chương trình C# in ra tên và tuổi của bạn.</p>",
                    CreatedBy = createdBy,
                    CreatedAt = DateTime.Now,
                    IsApproved = course.IsApproved == true,
                    IsPreview = true
                });
                lessons.Add(new TbLesson
                {
                    CourseId = course.Id,
                    Title = "Lập Trình Hướng Đối Tượng",
                    Content = @"<!-- YOUTUBE: https://www.youtube.com/watch?v=ZqgPoEkTZgY -->
                        <h2>OOP trong C#</h2>
                        <p>Lập trình hướng đối tượng (OOP) là phương pháp lập trình dựa trên khái niệm ""đối tượng"". C# hỗ trợ đầy đủ 4 nguyên lý của OOP.</p>

                        <h3>🏛️ 4 Nguyên Lý OOP</h3>
                        <div class=""grid grid-cols-1 md:grid-cols-2 gap-4 my-4"">
                            <div class=""bg-blue-50 dark:bg-blue-900/20 p-4 rounded-lg"">
                                <h4 class=""font-bold text-blue-700 dark:text-blue-300 mb-2"">1. Encapsulation (Đóng gói)</h4>
                                <p class=""text-sm"">Ẩn chi tiết triển khai, chỉ expose những gì cần thiết.</p>
                            </div>
                            <div class=""bg-green-50 dark:bg-green-900/20 p-4 rounded-lg"">
                                <h4 class=""font-bold text-green-700 dark:text-green-300 mb-2"">2. Inheritance (Kế thừa)</h4>
                                <p class=""text-sm"">Class con kế thừa thuộc tính và phương thức từ class cha.</p>
                            </div>
                            <div class=""bg-purple-50 dark:bg-purple-900/20 p-4 rounded-lg"">
                                <h4 class=""font-bold text-purple-700 dark:text-purple-300 mb-2"">3. Polymorphism (Đa hình)</h4>
                                <p class=""text-sm"">Một interface có nhiều cách triển khai khác nhau.</p>
                            </div>
                            <div class=""bg-orange-50 dark:bg-orange-900/20 p-4 rounded-lg"">
                                <h4 class=""font-bold text-orange-700 dark:text-orange-300 mb-2"">4. Abstraction (Trừu tượng)</h4>
                                <p class=""text-sm"">Tập trung vào những gì đối tượng làm, không phải cách làm.</p>
                            </div>
                        </div>

                        <h3>💡 Ví dụ Class và Object</h3>
                        <pre class=""bg-gray-100 dark:bg-gray-800 p-4 rounded-lg overflow-x-auto""><code>// Định nghĩa class
                        public class Person
                        {
                            // Properties
                            public string Name { get; set; }
                            public int Age { get; set; }
    
                            // Method
                            public void Introduce()
                            {
                                Console.WriteLine($""Tôi là {Name}, {Age} tuổi."");
                            }
                        }

                        // Tạo object
                        Person person = new Person
                        {
                            Name = ""Nguyễn Văn A"",
                            Age = 25
                        };

                        person.Introduce();</code></pre>

                        <h3>✅ Bài tập</h3>
                        <p>Tạo class <code>Student</code> với các thuộc tính: Name, Age, Grade và method để hiển thị thông tin.</p>",
                    CreatedBy = createdBy,
                    CreatedAt = DateTime.Now,
                    IsApproved = course.IsApproved == true,
                    IsPreview = true
                });
                lessons.Add(new TbLesson
                {
                    CourseId = course.Id,
                    Title = "LINQ và Collections",
                    Content = "<h2>LINQ và Collections</h2><p>Language Integrated Query để truy vấn dữ liệu.</p><h3>LINQ Methods</h3><ul><li>Where() - Lọc</li><li>Select() - Chọn</li><li>OrderBy() - Sắp xếp</li><li>GroupBy() - Nhóm</li></ul>",
                    CreatedBy = createdBy,
                    CreatedAt = DateTime.Now,
                    IsApproved = course.IsApproved == true,
                    IsPreview = false
                });
            }
            else if (courseTitle.Contains("asp.net") || courseTitle.Contains("mvc"))
            {
                lessons.Add(new TbLesson
                {
                    CourseId = course.Id,
                    Title = "Giới Thiệu ASP.NET Core MVC",
                    Content = @"<!-- YOUTUBE: https://www.youtube.com/watch?v=hZ1DASYd9rk -->
                    <h2>ASP.NET Core MVC</h2>
                    <p>Framework web của Microsoft để xây dựng ứng dụng web hiện đại, hiệu suất cao và cross-platform.</p>

                    <h3>🏗️ MVC Pattern</h3>
                    <div class=""grid grid-cols-1 md:grid-cols-3 gap-4 my-4"">
                        <div class=""bg-blue-50 dark:bg-blue-900/20 p-4 rounded-lg"">
                            <h4 class=""font-bold text-blue-700 dark:text-blue-300 mb-2"">Model</h4>
                            <p class=""text-sm"">Dữ liệu và logic nghiệp vụ. Đại diện cho cấu trúc dữ liệu của ứng dụng.</p>
                        </div>
                        <div class=""bg-green-50 dark:bg-green-900/20 p-4 rounded-lg"">
                            <h4 class=""font-bold text-green-700 dark:text-green-300 mb-2"">View</h4>
                            <p class=""text-sm"">Giao diện người dùng. Hiển thị dữ liệu từ Model cho người dùng.</p>
                        </div>
                        <div class=""bg-purple-50 dark:bg-purple-900/20 p-4 rounded-lg"">
                            <h4 class=""font-bold text-purple-700 dark:text-purple-300 mb-2"">Controller</h4>
                            <p class=""text-sm"">Xử lý request từ người dùng, tương tác với Model và trả về View.</p>
                        </div>
                    </div>

                    <h3>💡 Ví dụ Controller</h3>
                    <pre class=""bg-gray-100 dark:bg-gray-800 p-4 rounded-lg overflow-x-auto""><code>public class HomeController : Controller
                    {
                        public IActionResult Index()
                        {
                            var model = new { Message = ""Xin chào MVC!"" };
                            return View(model);
                        }
                    }</code></pre>

                    <h3>✅ Bài tập</h3>
                    <p>Tạo một Controller với action Index trả về View hiển thị ""Chào mừng đến với ASP.NET Core MVC"".</p>",
                    CreatedBy = createdBy,
                    CreatedAt = DateTime.Now,
                    IsApproved = course.IsApproved == true,
                    IsPreview = true
                });
                lessons.Add(new TbLesson
                {
                    CourseId = course.Id,
                    Title = "Dependency Injection",
                    Content = "<h2>Dependency Injection</h2><p>Pattern để quản lý dependencies trong ứng dụng.</p><h3>Concepts</h3><ul><li>Service Registration - Đăng ký service</li><li>Constructor Injection - Inject qua constructor</li><li>Service Lifetime - Singleton, Scoped, Transient</li></ul>",
                    CreatedBy = createdBy,
                    CreatedAt = DateTime.Now,
                    IsApproved = course.IsApproved == true,
                    IsPreview = true
                });
                lessons.Add(new TbLesson
                {
                    CourseId = course.Id,
                    Title = "Entity Framework Core",
                    Content = "<h2>Entity Framework Core</h2><p>ORM framework để làm việc với database.</p><h3>Features</h3><ul><li>Code First - Tạo DB từ code</li><li>Migrations - Quản lý thay đổi schema</li><li>LINQ to SQL - Truy vấn bằng LINQ</li></ul>",
                    CreatedBy = createdBy,
                    CreatedAt = DateTime.Now,
                    IsApproved = course.IsApproved == true,
                    IsPreview = false
                });
            }
            else if (courseTitle.Contains("react"))
            {
                lessons.Add(new TbLesson
                {
                    CourseId = course.Id,
                    Title = "Giới Thiệu React.js",
                    Content = "<h2>React.js</h2><p>Thư viện JavaScript để xây dựng giao diện người dùng.</p><h3>Concepts</h3><ul><li>Components - Thành phần</li><li>JSX - JavaScript XML</li><li>Props - Thuộc tính</li><li>State - Trạng thái</li></ul>",
                    CreatedBy = createdBy,
                    CreatedAt = DateTime.Now,
                    IsApproved = course.IsApproved == true,
                    IsPreview = true
                });
                lessons.Add(new TbLesson
                {
                    CourseId = course.Id,
                    Title = "Hooks trong React",
                    Content = "<h2>React Hooks</h2><p>Các hooks để quản lý state và side effects.</p><h3>Hooks</h3><ul><li>useState - Quản lý state</li><li>useEffect - Side effects</li><li>useContext - Context API</li><li>useReducer - State management phức tạp</li></ul>",
                    CreatedBy = createdBy,
                    CreatedAt = DateTime.Now,
                    IsApproved = course.IsApproved == true,
                    IsPreview = true
                });
                lessons.Add(new TbLesson
                {
                    CourseId = course.Id,
                    Title = "Redux State Management",
                    Content = "<h2>Redux</h2><p>Thư viện quản lý state cho ứng dụng JavaScript.</p><h3>Concepts</h3><ul><li>Store - Kho lưu trữ state</li><li>Actions - Hành động</li><li>Reducers - Xử lý actions</li><li>Dispatch - Gửi actions</li></ul>",
                    CreatedBy = createdBy,
                    CreatedAt = DateTime.Now,
                    IsApproved = course.IsApproved == true,
                    IsPreview = false
                });
            }
            else if (courseTitle.Contains("database") || courseTitle.Contains("sql"))
            {
                lessons.Add(new TbLesson
                {
                    CourseId = course.Id,
                    Title = "Thiết Kế Database",
                    Content = "<h2>Database Design</h2><p>Nguyên tắc thiết kế database hiệu quả.</p><h3>Concepts</h3><ul><li>Normalization - Chuẩn hóa</li><li>Primary Key - Khóa chính</li><li>Foreign Key - Khóa ngoại</li><li>Indexes - Chỉ mục</li></ul>",
                    CreatedBy = createdBy,
                    CreatedAt = DateTime.Now,
                    IsApproved = course.IsApproved == true,
                    IsPreview = true
                });
                lessons.Add(new TbLesson
                {
                    CourseId = course.Id,
                    Title = "SQL Queries",
                    Content = "<h2>SQL Queries</h2><p>Các câu lệnh SQL cơ bản và nâng cao.</p><h3>Statements</h3><ul><li>SELECT - Truy vấn</li><li>INSERT - Thêm dữ liệu</li><li>UPDATE - Cập nhật</li><li>DELETE - Xóa</li><li>JOIN - Kết nối bảng</li></ul>",
                    CreatedBy = createdBy,
                    CreatedAt = DateTime.Now,
                    IsApproved = course.IsApproved == true,
                    IsPreview = true
                });
                lessons.Add(new TbLesson
                {
                    CourseId = course.Id,
                    Title = "Stored Procedures và Triggers",
                    Content = "<h2>Stored Procedures & Triggers</h2><p>Tạo và sử dụng stored procedures và triggers.</p><h3>Features</h3><ul><li>Stored Procedures - Thủ tục lưu trữ</li><li>Triggers - Kích hoạt tự động</li><li>Functions - Hàm SQL</li></ul>",
                    CreatedBy = createdBy,
                    CreatedAt = DateTime.Now,
                    IsApproved = course.IsApproved == true,
                    IsPreview = false
                });
            }
            else
            {
                // Default lessons for any other course
                lessons.Add(new TbLesson
                {
                    CourseId = course.Id,
                    Title = "Bài 1: Giới Thiệu",
                    Content = "<h2>Giới Thiệu Khóa Học</h2><p>Chào mừng bạn đến với khóa học này!</p><p>Trong bài học đầu tiên, chúng ta sẽ tìm hiểu về các khái niệm cơ bản.</p>",
                    CreatedBy = createdBy,
                    CreatedAt = DateTime.Now,
                    IsApproved = course.IsApproved == true,
                    IsPreview = true
                });
                lessons.Add(new TbLesson
                {
                    CourseId = course.Id,
                    Title = "Bài 2: Nội Dung Chính",
                    Content = "<h2>Nội Dung Chính</h2><p>Bài học này sẽ đi sâu vào các nội dung chính của khóa học.</p>",
                    CreatedBy = createdBy,
                    CreatedAt = DateTime.Now,
                    IsApproved = course.IsApproved == true,
                    IsPreview = true
                });
            }

            return lessons;
        }

        public async Task SeedAsync()
        {
            try
            {
                await _context.Database.EnsureCreatedAsync();

                // Check if data already seeded - if admin user exists, skip seeding
                if (_context.TbUsers.Any(u => u.Email == "admin@quizly.com"))
                {
                    _logger.LogInformation("Database already seeded");
                    return;
                }

                _logger.LogInformation("Starting database seeding...");

                // Create admin user
                var adminUser = new TbUser
                {
                    Username = "admin",
                    Email = "admin@quizly.com",
                    PasswordHash = HashPassword("admin123"),
                    FullName = "Administrator",
                    Role = "Admin",
                    CreatedAt = DateTime.Now
                };

                // Create sample user (regular user)
                var regularUser = new TbUser
                {
                    Username = "user1",
                    Email = "user@quizly.com",
                    PasswordHash = HashPassword("user123"),
                    FullName = "John User",
                    Role = "User",
                    CreatedAt = DateTime.Now
                };

                _context.TbUsers.Add(adminUser);
                _context.TbUsers.Add(regularUser);

                await _context.SaveChangesAsync();
                _logger.LogInformation("Created sample users");

                // Create sample courses
                var courses = new List<TbCourse>
                {
                    new TbCourse
                    {
                        Title = "Introduction to Web Development",
                        Description = "Learn the basics of web development including HTML, CSS, and JavaScript",
                        CreatedBy = regularUser.Id,
                        CreatedAt = DateTime.Now,
                        IsApproved = true,
                        IsPaid = false,
                        FreeLessonCount = 3
                    },
                    new TbCourse
                    {
                        Title = "Advanced C# Programming",
                        Description = "Master advanced concepts in C# and .NET",
                        CreatedBy = regularUser.Id,
                        CreatedAt = DateTime.Now,
                        IsApproved = false,
                        IsPaid = false,
                        FreeLessonCount = 2
                    },
                    new TbCourse
                    {
                        Title = "Lập Trình C# Cơ Bản",
                        Description = "Khóa học này sẽ giúp bạn nắm vững các kiến thức cơ bản về ngôn ngữ lập trình C#. Bạn sẽ học về cú pháp, biến, hàm, lớp, và các khái niệm lập trình hướng đối tượng. Phù hợp cho người mới bắt đầu.",
                        CreatedBy = regularUser.Id,
                        CreatedAt = DateTime.Now,
                        IsApproved = true,
                        IsPaid = false,
                        FreeLessonCount = 3
                    },
                    new TbCourse
                    {
                        Title = "HTML & CSS Cơ Bản",
                        Description = "Học cách xây dựng website từ đầu với HTML và CSS. Khóa học bao gồm các thẻ HTML cơ bản, cách tạo layout với CSS, responsive design, và các kỹ thuật styling hiện đại.",
                        CreatedBy = regularUser.Id,
                        CreatedAt = DateTime.Now,
                        IsApproved = true,
                        IsPaid = false,
                        FreeLessonCount = 2
                    },
                    new TbCourse
                    {
                        Title = "JavaScript Cho Người Mới Bắt Đầu",
                        Description = "Khóa học JavaScript từ cơ bản đến nâng cao. Học về DOM manipulation, events, async/await, và các framework phổ biến. Bao gồm nhiều bài tập thực hành.",
                        CreatedBy = regularUser.Id,
                        CreatedAt = DateTime.Now,
                        IsApproved = true,
                        IsPaid = false,
                        FreeLessonCount = 4
                    },
                    new TbCourse
                    {
                        Title = "ASP.NET Core MVC Nâng Cao",
                        Description = "Khóa học chuyên sâu về ASP.NET Core MVC. Học về dependency injection, middleware, authentication, authorization, Entity Framework Core, và deployment. Dành cho developers có kinh nghiệm.",
                        CreatedBy = regularUser.Id,
                        CreatedAt = DateTime.Now,
                        IsApproved = true,
                        IsPaid = true,
                        FreeLessonCount = 2
                    },
                    new TbCourse
                    {
                        Title = "React.js & Redux Mastery",
                        Description = "Khóa học toàn diện về React.js và Redux. Học về hooks, context API, state management, routing, và các best practices. Bao gồm dự án thực tế xây dựng ứng dụng e-commerce.",
                        CreatedBy = regularUser.Id,
                        CreatedAt = DateTime.Now,
                        IsApproved = true,
                        IsPaid = true,
                        FreeLessonCount = 3
                    },
                    new TbCourse
                    {
                        Title = "Database Design & SQL Server",
                        Description = "Học thiết kế database chuyên nghiệp, normalization, indexing, stored procedures, triggers, và query optimization. Phù hợp cho database administrators và backend developers.",
                        CreatedBy = regularUser.Id,
                        CreatedAt = DateTime.Now,
                        IsApproved = true,
                        IsPaid = true,
                        FreeLessonCount = 2
                    }
                };

                _context.TbCourses.AddRange(courses);

                await _context.SaveChangesAsync();
                _logger.LogInformation($"Created {courses.Count} sample courses");

                // Create sample lessons for each course
                var allLessons = new List<TbLesson>();
                
                foreach (var course in courses)
                {
                    // Create lessons based on course title
                    var courseLessons = GetLessonsForCourse(course, regularUser.Id);
                    allLessons.AddRange(courseLessons);
                }

                if (allLessons.Any())
                {
                    _context.TbLessons.AddRange(allLessons);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation($"Created {allLessons.Count} sample lessons for {courses.Count} courses");

                    // Create lesson progress for first lesson of first course
                    var firstLesson = allLessons.FirstOrDefault();
                    if (firstLesson != null)
                    {
                        var progress = new TbLessonProgress
                        {
                            UserId = regularUser.Id,
                            LessonId = firstLesson.Id,
                            IsCompleted = true,
                            CompletedAt = DateTime.Now.AddDays(-1)
                        };
                        _context.TbLessonProgresses.Add(progress);
                        await _context.SaveChangesAsync();
                    }
                }
                else
                {
                    _logger.LogWarning("No lessons created");
                }

                // Create user XP records
                var userXp = new TbUserXp
                {
                    UserId = regularUser.Id,
                    Xp = 50,
                    Level = 1
                };

                var adminXp = new TbUserXp
                {
                    UserId = adminUser.Id,
                    Xp = 200,
                    Level = 2
                };

                _context.TbUserXps.Add(userXp);
                _context.TbUserXps.Add(adminXp);

                await _context.SaveChangesAsync();
                _logger.LogInformation("Created user XP records");

                _logger.LogInformation("Database seeding completed successfully!");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error seeding database: {ex.Message}");
                throw;
            }
        }
    }
}
