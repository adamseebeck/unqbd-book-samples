namespace ContosoSupportCopilot.Domain.Services;

public interface ISensitiveDataMasker
{
    string MaskSensitiveContent(string content);
}
