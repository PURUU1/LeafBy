namespace LeafBy.Models
{
    public class WeatherWidgetViewModel
    {
        // Geographical Data
        public string Location { get; set; } = string.Empty;

        // Weather Data
        public int TemperatureCelsius { get; set; }
        public string Condition { get; set; } = string.Empty;

        // Gardening Insights (Calculated dynamically in the controller)
        public string GrowthPotentialLevel { get; set; } = string.Empty;
        public int GrowthPercentage { get; set; }

        // Optional: Error handling flag just in case the API fails
        public bool IsError { get; set; } = false;
        public string ErrorMessage { get; set; } = string.Empty;
    }
}
