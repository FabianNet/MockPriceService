namespace Mock.Pricing.Service.Base.Model
{
    public class Price
    {
        public Price(string productName, double value)
        {
            ProductName = productName;
            Value = value;
        }

        public Price()
        {
        }

        public string ProductName { get; set; }
        public double Value { get; set; }
    }
}