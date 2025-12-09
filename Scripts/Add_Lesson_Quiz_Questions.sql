-- Script to add quiz questions for lessons
-- Each lesson will have 5-10 quiz questions to verify learning

-- Get the first available user ID (preferably admin) and a subject ID
DECLARE @CreatedByUserId INT;
DECLARE @SubjectId INT;

-- Get admin user
SELECT TOP 1 @CreatedByUserId = Id 
FROM tb_Users 
WHERE Role = 'Admin'
ORDER BY Id;

-- If no admin found, get any user
IF @CreatedByUserId IS NULL
BEGIN
    SELECT TOP 1 @CreatedByUserId = Id FROM tb_Users ORDER BY Id;
END

-- Get first subject (or create a default one)
SELECT TOP 1 @SubjectId = Id FROM tb_Subjects ORDER BY Id;

IF @SubjectId IS NULL
BEGIN
    -- Create a default subject if none exists
    DECLARE @CategoryId INT;
    SELECT TOP 1 @CategoryId = Id FROM tb_Categories ORDER BY Id;
    
    IF @CategoryId IS NOT NULL
    BEGIN
        INSERT INTO tb_Subjects (CategoryId, Title)
        VALUES (@CategoryId, N'Lập Trình Web');
        SET @SubjectId = SCOPE_IDENTITY();
    END
END

PRINT 'Using User ID: ' + CAST(@CreatedByUserId AS VARCHAR(10)) + ' as quiz creator';
PRINT 'Using Subject ID: ' + CAST(@SubjectId AS VARCHAR(10));

-- Variables for lesson IDs
DECLARE @LessonId1 INT, @LessonId2 INT, @LessonId3 INT, @LessonId4 INT, @LessonId5 INT;
DECLARE @ExamId1 INT, @ExamId2 INT, @ExamId3 INT, @ExamId4 INT, @ExamId5 INT;

-- Get lesson IDs (assuming they exist from Add_Sample_Courses.sql)
SELECT TOP 1 @LessonId1 = Id FROM tb_Lessons WHERE Title = N'Giới Thiệu HTML' ORDER BY Id DESC;
SELECT TOP 1 @LessonId2 = Id FROM tb_Lessons WHERE Title = N'CSS Cơ Bản' ORDER BY Id DESC;
SELECT TOP 1 @LessonId3 = Id FROM tb_Lessons WHERE Title = N'JavaScript Cơ Bản' ORDER BY Id DESC;
SELECT TOP 1 @LessonId4 = Id FROM tb_Lessons WHERE Title = N'Giới Thiệu C#' ORDER BY Id DESC;
SELECT TOP 1 @LessonId5 = Id FROM tb_Lessons WHERE Title = N'Lập Trình Hướng Đối Tượng' ORDER BY Id DESC;

-- Create exams for lessons
-- Lesson 1: Giới Thiệu HTML
IF @LessonId1 IS NOT NULL
BEGIN
    INSERT INTO tb_Exams (SubjectId, Title, Difficulty, Duration, QuestionCount, Price, IsPremium, CreatedAt, AvgRating, TotalReviews, CreatedBy, IsApproved, IsPaid, LessonId)
    VALUES (@SubjectId, N'Kiểm Tra: Giới Thiệu HTML', N'Dễ', 15, 5, 0, 0, GETDATE(), 0, 0, @CreatedByUserId, 1, 0, @LessonId1);
    SET @ExamId1 = SCOPE_IDENTITY();

    -- Add questions
    INSERT INTO tb_Questions (ExamId, Content, OptionA, OptionB, OptionC, OptionD, CorrectOption, Marks)
    VALUES
    (@ExamId1, N'HTML là viết tắt của gì?', N'HyperText Markup Language', N'High Tech Modern Language', N'Home Tool Markup Language', N'Hyperlink and Text Markup Language', 'A', 2.0),
    (@ExamId1, N'Thẻ nào là thẻ gốc của một tài liệu HTML?', N'<head>', N'<html>', N'<body>', N'<title>', 'B', 2.0),
    (@ExamId1, N'Thẻ nào được sử dụng để tạo tiêu đề lớn nhất?', N'<h6>', N'<h1>', N'<heading>', N'<title>', 'B', 2.0),
    (@ExamId1, N'Thẻ nào được sử dụng để tạo liên kết?', N'<link>', N'<a>', N'<href>', N'<url>', 'B', 2.0),
    (@ExamId1, N'Thuộc tính nào của thẻ <img> chỉ định đường dẫn đến hình ảnh?', N'href', N'src', N'link', N'url', 'B', 2.0);

    UPDATE tb_Exams SET QuestionCount = 5 WHERE Id = @ExamId1;
    PRINT 'Created quiz for Lesson 1: Giới Thiệu HTML';
END

-- Lesson 2: CSS Cơ Bản
IF @LessonId2 IS NOT NULL
BEGIN
    INSERT INTO tb_Exams (SubjectId, Title, Difficulty, Duration, QuestionCount, Price, IsPremium, CreatedAt, AvgRating, TotalReviews, CreatedBy, IsApproved, IsPaid, LessonId)
    VALUES (@SubjectId, N'Kiểm Tra: CSS Cơ Bản', N'Dễ', 15, 6, 0, 0, GETDATE(), 0, 0, @CreatedByUserId, 1, 0, @LessonId2);
    SET @ExamId2 = SCOPE_IDENTITY();

    -- Add questions
    INSERT INTO tb_Questions (ExamId, Content, OptionA, OptionB, OptionC, OptionD, CorrectOption, Marks)
    VALUES
    (@ExamId2, N'CSS là viết tắt của gì?', N'Computer Style Sheets', N'Cascading Style Sheets', N'Creative Style Sheets', N'Colorful Style Sheets', 'B', 1.67),
    (@ExamId2, N'Cú pháp nào đúng để chọn tất cả các thẻ <p>?', N'p { }', N'.p { }', N'#p { }', N'*p { }', 'A', 1.67),
    (@ExamId2, N'Thuộc tính CSS nào được dùng để thay đổi màu chữ?', N'text-color', N'color', N'font-color', N'text-style', 'B', 1.67),
    (@ExamId2, N'Thuộc tính nào được dùng để thay đổi màu nền?', N'background', N'bg-color', N'background-color', N'color-background', 'C', 1.67),
    (@ExamId2, N'Cú pháp nào đúng để chọn phần tử có class="example"?', N'example { }', N'.example { }', N'#example { }', N'*example { }', 'B', 1.67),
    (@ExamId2, N'Thuộc tính nào được dùng để thay đổi kích thước chữ?', N'text-size', N'font-size', N'text-style', N'size', 'B', 1.67);

    UPDATE tb_Exams SET QuestionCount = 6 WHERE Id = @ExamId2;
    PRINT 'Created quiz for Lesson 2: CSS Cơ Bản';
END

-- Lesson 3: JavaScript Cơ Bản
IF @LessonId3 IS NOT NULL
BEGIN
    INSERT INTO tb_Exams (SubjectId, Title, Difficulty, Duration, QuestionCount, Price, IsPremium, CreatedAt, AvgRating, TotalReviews, CreatedBy, IsApproved, IsPaid, LessonId)
    VALUES (@SubjectId, N'Kiểm Tra: JavaScript Cơ Bản', N'Trung Bình', 20, 7, 0, 0, GETDATE(), 0, 0, @CreatedByUserId, 1, 0, @LessonId3);
    SET @ExamId3 = SCOPE_IDENTITY();

    -- Add questions
    INSERT INTO tb_Questions (ExamId, Content, OptionA, OptionB, OptionC, OptionD, CorrectOption, Marks)
    VALUES
    (@ExamId3, N'Từ khóa nào được dùng để khai báo biến có thể thay đổi trong ES6?', N'var', N'let', N'const', N'variable', 'B', 1.43),
    (@ExamId3, N'Kiểu dữ liệu nào KHÔNG phải là kiểu nguyên thủy trong JavaScript?', N'String', N'Number', N'Boolean', N'Object', 'D', 1.43),
    (@ExamId3, N'Phương thức nào được dùng để thêm phần tử vào cuối mảng?', N'push()', N'pop()', N'shift()', N'unshift()', 'A', 1.43),
    (@ExamId3, N'Toán tử nào được dùng để so sánh cả giá trị và kiểu dữ liệu?', N'==', N'===', N'=', N'!=', 'B', 1.43),
    (@ExamId3, N'Hàm nào được dùng để hiển thị thông báo trong console?', N'print()', N'console.log()', N'alert()', N'log()', 'B', 1.43),
    (@ExamId3, N'Cú pháp nào đúng để khai báo một mảng?', N'var arr = ()', N'var arr = []', N'var arr = {}', N'var arr = <>', 'B', 1.43),
    (@ExamId3, N'Phương thức nào được dùng để lấy độ dài của chuỗi?', N'length()', N'size()', N'length', N'count()', 'C', 1.43);

    UPDATE tb_Exams SET QuestionCount = 7 WHERE Id = @ExamId3;
    PRINT 'Created quiz for Lesson 3: JavaScript Cơ Bản';
END

-- Lesson 4: Giới Thiệu C#
IF @LessonId4 IS NOT NULL
BEGIN
    INSERT INTO tb_Exams (SubjectId, Title, Difficulty, Duration, QuestionCount, Price, IsPremium, CreatedAt, AvgRating, TotalReviews, CreatedBy, IsApproved, IsPaid, LessonId)
    VALUES (@SubjectId, N'Kiểm Tra: Giới Thiệu C#', N'Trung Bình', 20, 8, 0, 0, GETDATE(), 0, 0, @CreatedByUserId, 1, 0, @LessonId4);
    SET @ExamId4 = SCOPE_IDENTITY();

    -- Add questions
    INSERT INTO tb_Questions (ExamId, Content, OptionA, OptionB, OptionC, OptionD, CorrectOption, Marks)
    VALUES
    (@ExamId4, N'C# được phát triển bởi công ty nào?', N'Google', N'Microsoft', N'Apple', N'Oracle', 'B', 1.25),
    (@ExamId4, N'Cú pháp nào đúng để khai báo biến kiểu int?', N'int x = 10;', N'int x := 10;', N'var x = 10;', N'int x -> 10;', 'A', 1.25),
    (@ExamId4, N'Phương thức nào là điểm vào của chương trình C#?', N'Main()', N'Start()', N'Begin()', N'Run()', 'A', 1.25),
    (@ExamId4, N'Namespace nào chứa Console class?', N'System', N'System.IO', N'System.Console', N'Console', 'A', 1.25),
    (@ExamId4, N'Cú pháp nào đúng để in ra màn hình trong C#?', N'print("Hello");', N'Console.Write("Hello");', N'System.out.println("Hello");', N'echo "Hello";', 'B', 1.25),
    (@ExamId4, N'Kiểu dữ liệu nào được dùng cho số thập phân trong C#?', N'int', N'float', N'decimal', N'Cả float và decimal', 'D', 1.25),
    (@ExamId4, N'Vòng lặp nào được dùng để lặp qua các phần tử trong mảng?', N'for', N'foreach', N'while', N'Cả for và foreach', 'D', 1.25),
    (@ExamId4, N'Cú pháp nào đúng để khai báo một mảng số nguyên?', N'int[] arr = new int[5];', N'int arr[] = new int[5];', N'array<int> arr = new array[5];', N'int arr = [5];', 'A', 1.25);

    UPDATE tb_Exams SET QuestionCount = 8 WHERE Id = @ExamId4;
    PRINT 'Created quiz for Lesson 4: Giới Thiệu C#';
END

-- Lesson 5: Lập Trình Hướng Đối Tượng
IF @LessonId5 IS NOT NULL
BEGIN
    INSERT INTO tb_Exams (SubjectId, Title, Difficulty, Duration, QuestionCount, Price, IsPremium, CreatedAt, AvgRating, TotalReviews, CreatedBy, IsApproved, IsPaid, LessonId)
    VALUES (@SubjectId, N'Kiểm Tra: Lập Trình Hướng Đối Tượng', N'Khó', 25, 10, 0, 0, GETDATE(), 0, 0, @CreatedByUserId, 1, 0, @LessonId5);
    SET @ExamId5 = SCOPE_IDENTITY();

    -- Add questions
    INSERT INTO tb_Questions (ExamId, Content, OptionA, OptionB, OptionC, OptionD, CorrectOption, Marks)
    VALUES
    (@ExamId5, N'OOP là viết tắt của gì?', N'Object-Oriented Programming', N'Object-Oriented Process', N'Object-Oriented Protocol', N'Object-Oriented Procedure', 'A', 1.0),
    (@ExamId5, N'Nguyên lý OOP nào cho phép class con kế thừa từ class cha?', N'Encapsulation', N'Inheritance', N'Polymorphism', N'Abstraction', 'B', 1.0),
    (@ExamId5, N'Nguyên lý OOP nào ẩn chi tiết triển khai?', N'Encapsulation', N'Inheritance', N'Polymorphism', N'Abstraction', 'A', 1.0),
    (@ExamId5, N'Từ khóa nào được dùng để kế thừa class trong C#?', N'extends', N'inherits', N':', N'::', 'C', 1.0),
    (@ExamId5, N'Access modifier nào cho phép truy cập từ bất kỳ đâu?', N'private', N'protected', N'public', N'internal', 'C', 1.0),
    (@ExamId5, N'Phương thức nào được gọi khi tạo object mới?', N'Constructor', N'Destructor', N'Initializer', N'Creator', 'A', 1.0),
    (@ExamId5, N'Từ khóa nào được dùng để override method trong class con?', N'override', N'overload', N'virtual', N'new', 'A', 1.0),
    (@ExamId5, N'Abstract class có thể được khởi tạo trực tiếp không?', N'Có', N'Không', N'Tùy trường hợp', N'Chỉ trong một số trường hợp', 'B', 1.0),
    (@ExamId5, N'Interface trong C# có thể chứa implementation không?', N'Có, luôn luôn', N'Không, chỉ có signature', N'Có, từ C# 8.0', N'Tùy trường hợp', 'C', 1.0),
    (@ExamId5, N'Property nào cho phép đọc và ghi?', N'{ get; }', N'{ set; }', N'{ get; set; }', N'{ get; private set; }', 'C', 1.0);

    UPDATE tb_Exams SET QuestionCount = 10 WHERE Id = @ExamId5;
    PRINT 'Created quiz for Lesson 5: Lập Trình Hướng Đối Tượng';
END

PRINT '';
PRINT 'All lesson quizzes created successfully!';
PRINT 'Each lesson now has 5-10 quiz questions to verify learning.';
