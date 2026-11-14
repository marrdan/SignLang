using SignKz.Models;

namespace SignKz.Services;

public static class QuizBuilder
{
    // One correct sign + 3 wrong signs, in random order.
    public static QuizQuestion Build(List<Sign> allSigns, Random random)
    {
        if (allSigns.Count < 4)
        {
            throw new InvalidOperationException("At least 4 signs are needed for a quiz.");
        }

        Sign correct = allSigns[random.Next(allSigns.Count)];

        List<Sign> options = new List<Sign>();
        options.Add(correct);

        while (options.Count < 4)
        {
            Sign candidate = allSigns[random.Next(allSigns.Count)];
            if (!options.Contains(candidate))
            {
                options.Add(candidate);
            }
        }

        // Shuffle (Fisher-Yates)
        for (int i = options.Count - 1; i > 0; i--)
        {
            int j = random.Next(i + 1);
            Sign temp = options[i];
            options[i] = options[j];
            options[j] = temp;
        }

        QuizQuestion question = new QuizQuestion();
        question.Correct = correct;
        question.Options = options;
        return question;
    }
}
