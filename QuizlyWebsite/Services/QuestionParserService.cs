using QuizlyWebsite.Models.ViewModels;
using System.Text.RegularExpressions;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using System.Text;

namespace QuizlyWebsite.Services;

public class QuestionParserService : IQuestionParserService
{
    // Regex patterns for different question formats
    private static readonly Regex QuestionPattern = new Regex(
        @"^(Câu\s*(?:hỏi|số)?\s*:?\s*\d*|Câu\s*\d+|^\d+[\.\)]\s*|Câu\s*:)\s*(.+)$",
        RegexOptions.IgnoreCase | RegexOptions.Multiline);

    private static readonly Regex OptionPattern = new Regex(
        @"^(\*?|\(Đúng\)|\[Đúng\]|✓)\s*([A-D])[\.\):]\s*(.+)$",
        RegexOptions.IgnoreCase);

    public QuestionParseResult ParseFromText(string text)
    {
        var result = new QuestionParseResult();
        
        if (string.IsNullOrWhiteSpace(text))
        {
            result.Report = new ParseReport
            {
                TotalQuestions = 0,
                ValidCount = 0,
                ErrorCount = 1,
                Errors = new List<QuestionError>
                {
                    new QuestionError
                    {
                        QuestionNumber = 0,
                        ErrorReason = "Nội dung văn bản trống"
                    }
                }
            };
            return result;
        }

        text = text.Replace("\r\n", "\n").Replace("\r", "\n");
        var lines = text.Split('\n');

        var questions = new List<ParsedQuestion>();
        var errors = new List<QuestionError>();
        int questionNumber = 0;

        int i = 0;
        while (i < lines.Length)
        {
            int questionStartIndex = -1;
            string questionContent = string.Empty;
            
            while (i < lines.Length)
            {
                var line = lines[i].Trim();
                if (string.IsNullOrWhiteSpace(line))
                {
                    i++;
                    continue;
                }

                if (QuestionPattern.IsMatch(line) || 
                    line.StartsWith("Câu hỏi", StringComparison.OrdinalIgnoreCase) ||
                    Regex.IsMatch(line, @"^(Câu\s*\d+|^\d+[\.\)])", RegexOptions.IgnoreCase))
                {
                    questionStartIndex = i;
                    // Extract question content (remove question number/prefix)
                    var match = QuestionPattern.Match(line);
                    if (match.Success && match.Groups.Count > 2)
                    {
                        questionContent = match.Groups[match.Groups.Count - 1].Value.Trim();
                    }
                    else
                    {
                        questionContent = Regex.Replace(line, @"^(Câu\s*(?:hỏi|số)?\s*:?\s*\d*|Câu\s*\d+|^\d+[\.\)]\s*|Câu\s*:)\s*", "", RegexOptions.IgnoreCase).Trim();
                    }
                    break;
                }
                i++;
            }

            if (questionStartIndex == -1)
                break;

            questionNumber++;
            i = questionStartIndex + 1;

            var questionLines = new List<string> { questionContent };
            while (i < lines.Length)
            {
                var line = lines[i].Trim();
                if (string.IsNullOrWhiteSpace(line))
                {
                    i++;
                    continue;
                }

                // Nếu gặp đáp án hoặc câu hỏi mới thì dừng
                if (OptionPattern.IsMatch(line) || 
                    QuestionPattern.IsMatch(line) ||
                    Regex.IsMatch(line, @"^[A-D][\.\):]\s*", RegexOptions.IgnoreCase))
                {
                    break;
                }

                // Thêm dòng vào nội dung câu hỏi
                questionLines.Add(line);
                i++;
            }

            questionContent = string.Join(" ", questionLines).Trim();

            string? optionA = null;
            string? optionB = null;
            string? optionC = null;
            string? optionD = null;
            string? correctOption = null;
            int correctOptionCount = 0;

            while (i < lines.Length)
            {
                var line = lines[i].Trim();

                if (string.IsNullOrWhiteSpace(line))
                {
                    i++;
                    continue;
                }

                // Kiểm tra xem có phải là câu hỏi mới không
                if (QuestionPattern.IsMatch(line) || 
                    line.StartsWith("Câu hỏi", StringComparison.OrdinalIgnoreCase) ||
                    Regex.IsMatch(line, @"^(Câu\s*\d+|^\d+[\.\)])", RegexOptions.IgnoreCase))
                {
                    break;
                }

                // Parse đáp án với nhiều định dạng
                var optionMatch = OptionPattern.Match(line);
                if (optionMatch.Success)
                {
                    var optionLetter = optionMatch.Groups[2].Value.ToUpper();
                    var optionText = optionMatch.Groups[3].Value.Trim();
                    var isCorrect = !string.IsNullOrWhiteSpace(optionMatch.Groups[1].Value) || 
                                   optionMatch.Groups[1].Value.Contains("Đúng", StringComparison.OrdinalIgnoreCase) ||
                                   optionMatch.Groups[1].Value.Contains("✓") ||
                                   optionMatch.Groups[1].Value.Contains("✅");

                    switch (optionLetter)
                    {
                        case "A":
                            if (optionA == null)
                            {
                                optionA = optionText;
                                if (isCorrect)
                                {
                                    correctOption = "A";
                                    correctOptionCount++;
                                }
                            }
                            break;
                        case "B":
                            if (optionB == null)
                            {
                                optionB = optionText;
                                if (isCorrect)
                                {
                                    correctOption = "B";
                                    correctOptionCount++;
                                }
                            }
                            break;
                        case "C":
                            if (optionC == null)
                            {
                                optionC = optionText;
                                if (isCorrect)
                                {
                                    correctOption = "C";
                                    correctOptionCount++;
                                }
                            }
                            break;
                        case "D":
                            if (optionD == null)
                            {
                                optionD = optionText;
                                if (isCorrect)
                                {
                                    correctOption = "D";
                                    correctOptionCount++;
                                }
                            }
                            break;
                    }
                    i++;
                    continue;
                }

                var simpleMatch = Regex.Match(line, @"^(\*?)\s*([A-D])[\.\):]\s*(.+)$", RegexOptions.IgnoreCase);
                if (simpleMatch.Success)
                {
                    var optionLetter = simpleMatch.Groups[2].Value.ToUpper();
                    var optionText = simpleMatch.Groups[3].Value.Trim();
                    var hasStar = simpleMatch.Groups[1].Value == "*";

                    switch (optionLetter)
                    {
                        case "A":
                            if (optionA == null)
                            {
                                optionA = optionText;
                                if (hasStar)
                                {
                                    correctOption = "A";
                                    correctOptionCount++;
                                }
                            }
                            break;
                        case "B":
                            if (optionB == null)
                            {
                                optionB = optionText;
                                if (hasStar)
                                {
                                    correctOption = "B";
                                    correctOptionCount++;
                                }
                            }
                            break;
                        case "C":
                            if (optionC == null)
                            {
                                optionC = optionText;
                                if (hasStar)
                                {
                                    correctOption = "C";
                                    correctOptionCount++;
                                }
                            }
                            break;
                        case "D":
                            if (optionD == null)
                            {
                                optionD = optionText;
                                if (hasStar)
                                {
                                    correctOption = "D";
                                    correctOptionCount++;
                                }
                            }
                            break;
                    }
                }

                i++;
            }

            // Kiểm tra lỗi
            List<string> errorReasons = new();

            if (string.IsNullOrWhiteSpace(questionContent))
                errorReasons.Add("Nội dung câu hỏi trống");
            if (string.IsNullOrWhiteSpace(optionA))
                errorReasons.Add("Thiếu đáp án A");
            if (string.IsNullOrWhiteSpace(optionB))
                errorReasons.Add("Thiếu đáp án B");
            if (string.IsNullOrWhiteSpace(optionC))
                errorReasons.Add("Thiếu đáp án C");
            if (string.IsNullOrWhiteSpace(optionD))
                errorReasons.Add("Thiếu đáp án D");

            if (correctOptionCount == 0)
                errorReasons.Add("Không có đáp án đúng (cần đánh dấu * hoặc (Đúng) trước đáp án)");
            else if (correctOptionCount > 1)
                errorReasons.Add($"Có {correctOptionCount} đáp án đúng (chỉ được có 1 đáp án đúng)");

            if (errorReasons.Count > 0)
            {
                errors.Add(new QuestionError
                {
                    QuestionNumber = questionNumber,
                    QuestionContent = questionContent.Length > 100 ? questionContent.Substring(0, 100) + "..." : questionContent,
                    ErrorReason = string.Join("; ", errorReasons)
                });
            }
            else
            {
                questions.Add(new ParsedQuestion
                {
                    Question = questionContent,
                    OptionA = optionA!,
                    OptionB = optionB!,
                    OptionC = optionC!,
                    OptionD = optionD!,
                    CorrectOption = correctOption!
                });
            }
        }

        result.ValidQuestions = questions;
        result.Report = new ParseReport
        {
            TotalQuestions = questionNumber,
            ValidCount = questions.Count,
            ErrorCount = errors.Count,
            Errors = errors
        };

        return result;
    }

    public async Task<QuestionParseResult> ParseFromWordAsync(Stream fileStream)
    {
        try
        {
            // Reset stream position
            if (fileStream.CanSeek)
                fileStream.Position = 0;

            using var document = WordprocessingDocument.Open(fileStream, false);
            var body = document.MainDocumentPart?.Document?.Body;

            if (body == null)
            {
                return new QuestionParseResult
                {
                    Report = new ParseReport
                    {
                        TotalQuestions = 0,
                        ValidCount = 0,
                        ErrorCount = 1,
                        Errors = new List<QuestionError>
                        {
                            new QuestionError
                            {
                                QuestionNumber = 0,
                                ErrorReason = "Không thể đọc nội dung file Word. Vui lòng kiểm tra file có đúng định dạng .docx không."
                            }
                        }
                    }
                };
            }

            var textBuilder = new StringBuilder();
            var paragraphs = body.Elements<Paragraph>();

            foreach (var para in paragraphs)
            {
                // Extract text from paragraph, handling runs and text elements
                var paraText = ExtractTextFromParagraph(para);
                
                if (!string.IsNullOrWhiteSpace(paraText))
                {
                    textBuilder.AppendLine(paraText);
                }
            }

            // Also check tables in Word document
            var tables = body.Elements<Table>();
            foreach (var table in tables)
            {
                foreach (var row in table.Elements<TableRow>())
                {
                    var rowTexts = new List<string>();
                    foreach (var cell in row.Elements<TableCell>())
                    {
                        var cellText = ExtractTextFromTableCell(cell);
                        if (!string.IsNullOrWhiteSpace(cellText))
                        {
                            rowTexts.Add(cellText.Trim());
                        }
                    }
                    if (rowTexts.Any())
                    {
                        textBuilder.AppendLine(string.Join(" | ", rowTexts));
                    }
                }
            }

            var textContent = textBuilder.ToString();
            
            if (string.IsNullOrWhiteSpace(textContent))
            {
                return new QuestionParseResult
                {
                    Report = new ParseReport
                    {
                        TotalQuestions = 0,
                        ValidCount = 0,
                        ErrorCount = 1,
                        Errors = new List<QuestionError>
                        {
                            new QuestionError
                            {
                                QuestionNumber = 0,
                                ErrorReason = "File Word không chứa nội dung văn bản"
                            }
                        }
                    }
                };
            }

            return await Task.FromResult(ParseFromText(textContent));
        }
        catch (Exception ex)
        {
            return new QuestionParseResult
            {
                Report = new ParseReport
                {
                    TotalQuestions = 0,
                    ValidCount = 0,
                    ErrorCount = 1,
                    Errors = new List<QuestionError>
                    {
                        new QuestionError
                        {
                            QuestionNumber = 0,
                            ErrorReason = $"Lỗi khi đọc file Word: {ex.Message}. Vui lòng đảm bảo file là định dạng .docx hợp lệ."
                        }
                    }
                }
            };
        }
    }

    private string ExtractTextFromParagraph(Paragraph para)
    {
        var textBuilder = new StringBuilder();
        
        foreach (var run in para.Elements<Run>())
        {
            foreach (var text in run.Elements<Text>())
            {
                textBuilder.Append(text.Text);
            }
        }
        
        return textBuilder.ToString().Trim();
    }

    private string ExtractTextFromTableCell(TableCell cell)
    {
        var textBuilder = new StringBuilder();
        
        foreach (var para in cell.Elements<Paragraph>())
        {
            var paraText = ExtractTextFromParagraph(para);
            if (!string.IsNullOrWhiteSpace(paraText))
            {
                textBuilder.Append(paraText);
                textBuilder.Append(" ");
            }
        }
        
        return textBuilder.ToString().Trim();
    }
}
