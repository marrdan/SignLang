namespace SignKz.Models;

public class QuizQuestion
{
    public Sign Correct { get; set; } = new Sign();
    public List<Sign> Options { get; set; } = new List<Sign>();
}
