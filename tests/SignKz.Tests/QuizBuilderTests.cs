using SignKz.Models;
using SignKz.Services;
using Xunit;

namespace SignKz.Tests;

public class QuizBuilderTests
{
    [Fact]
    public void Build_ReturnsFourDifferentOptionsWithCorrectAnswer()
    {
        List<Sign> signs = TestHelpers.MakeSigns();
        Random random = new Random(1);

        for (int i = 0; i < 100; i++)
        {
            QuizQuestion question = QuizBuilder.Build(signs, random);

            Assert.Equal(4, question.Options.Count);
            Assert.Equal(4, question.Options.Distinct().Count());
            Assert.Contains(question.Correct, question.Options);
        }
    }

    [Fact]
    public void Build_Throws_WhenTooFewSigns()
    {
        List<Sign> signs = new List<Sign> { new Sign { Id = "x" } };

        Assert.Throws<InvalidOperationException>(() => QuizBuilder.Build(signs, new Random()));
    }
}
