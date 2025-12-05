namespace StorhaugenWebsite.Models
{
    public class DeviceSettings
    {
        public string Theme { get; set; } = "Dark";
        public string ViewMode { get; set; } = "list";
        public string SortBy { get; set; } = "date";
        public bool SortDescending { get; set; } = true;
    }
}