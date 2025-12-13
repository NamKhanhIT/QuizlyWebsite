using QuizlyWebsite.Models.ViewModels;

namespace QuizlyWebsite.Services;

public interface IQuestionParserService
{
    QuestionParseResult ParseFromText(string text); //Đọc đề thi từ văn bản.
    Task<QuestionParseResult> ParseFromWordAsync(Stream fileStream); //Đọc đề thi từ file Word.
}
