namespace Management_System_WPF.Models
{
    public class Buyer
    {
        public int BuyerId { get; set; }
        public string Name { get; set; } = "";
        public string Phone { get; set; } = "";

        // Add this
        public int SerialNumber { get; set; }
    }
}
