using System.ComponentModel.DataAnnotations;

namespace ContosoSupportCopilot.Infrastructure.Configuration;

public class EvaluationOptions
{
    public const string SectionName = "Evaluation";

    // Stored results and cached judge responses. The folder is listed in .gitignore.
    [Required(ErrorMessage = "Evaluation:StoragePath is required")]
    public string StoragePath { get; set; } = "evaluation-results";

    // The identity the evaluation runs as. Only the process that runs the evaluation assigns it every area
    // under test in Rag:ProductAreasByEngineer, and only if it has no areas of its own; a web host never does.
    [Required(ErrorMessage = "Evaluation:RequesterId is required")]
    public string RequesterId { get; set; } = "evaluation-runner";

    // The judges' deployment: a different model from the one that writes the answers, ideally a stronger
    // one, because another deployment of the same model shares its blind spots. Empty uses AI:ChatDeployment,
    // which only exercises the judge path and logs a warning; set it before you trust a judge's score.
    public string? JudgeDeployment { get; set; }

    // Null runs the judges whenever AI:Provider is not Offline; false skips them, for example to run
    // only the deterministic metrics on every change and the judges on a schedule.
    public bool? UseJudges { get; set; }

    // Reasoning models count their reasoning tokens against the judge's output limit, so the judge
    // client raises the evaluators' own limits of 800 and 1,600 tokens to this ceiling. Tokens that a
    // judgment does not use cost nothing.
    [Range(800, 128_000, ErrorMessage = "Evaluation:JudgeMaxOutputTokens must be between 800 and 128000")]
    public int JudgeMaxOutputTokens { get; set; } = 16_000;
}
