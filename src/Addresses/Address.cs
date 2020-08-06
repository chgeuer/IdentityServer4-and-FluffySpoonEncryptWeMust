namespace Addresses
{
    public static class Address
    {
        public static string EXTERNAL_DNS => "baldur.geuer-pollmann.de";
        public static string LOCAL_LAPTOP => "beam-lan";

        public static string STS => $"https://{EXTERNAL_DNS}";
        public static string Service => $"https://{LOCAL_LAPTOP}:6001/identity";
    }
}