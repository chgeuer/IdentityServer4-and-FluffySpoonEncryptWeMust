namespace Addresses
{
    public static class Address
    {
        public static string LOCAL_LAPTOP => "localhost";

        public static string STS => $"https://{LOCAL_LAPTOP}";
        public static string Service => $"http://{LOCAL_LAPTOP}:6001/showmemyidentity";
    }
}