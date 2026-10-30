namespace ContosoSupportCopilot.Domain.Workflows;

public enum WorkflowStep
{
    Triage,
    KnowledgeRetrieval,
    Drafting,
    Escalation,
    Completed,
    Failed
}
