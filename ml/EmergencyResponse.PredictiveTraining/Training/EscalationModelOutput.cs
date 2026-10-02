namespace EmergencyResponse.PredictiveTraining.Training;

public sealed class EscalationModelOutput
{
    public bool PredictedLabel { get; set; }

    public float Probability { get; set; }

    public float Score { get; set; }
}
