namespace EmergencyResponse.Application.AISecurity;

public enum TrustedContentBoundary
{
    SystemInstruction,
    DeveloperInstruction,
    UserInput,
    RetrievedKnowledge,
    ToolOutput,
    UploadedDocument,
    UploadedImage,
    ExternalEnvironmentalData,
    ModelOutput
}
