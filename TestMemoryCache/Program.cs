namespace TestMemoryCache
{
    using System;
    using System.Runtime.Caching;

    class Program
    {
        private static readonly decimal _defaultRate = 0.005m;
        private static readonly string _commonSettingCacheName = "commonSetting";

        static void Main(string[] args)
        {
            Console.WriteLine("============ V1 ============");
            Console.WriteLine();

            Initialize();

            // 1. 會員 A 讀取共用設定，得到 0.005m ，是程式初始化時設定的 Rate。
            Console.WriteLine($"Common Rate: {GetCommonRate()}");
            // 0.005m
            Console.WriteLine();

            // 2. 會員 B 使用 CreateSetting 建立新的設定。
            var newSetting = CreateSetting("MemberB", 0.003m);
            Console.WriteLine($"New Setting. ProfileName: {newSetting.ProfileName}, CurrencyId: {newSetting.CurrencyId}, Rate: {newSetting.Rate}");
            // New Setting. ProfileName: MemberA, Rate: 0.003m
            Console.WriteLine();

            // 3. 會員 C 讀取共用設定，得到 0.003m ，與初始化設定的不相符。
            Console.WriteLine($"Common Rate: {GetCommonRate()}");
            // 0.003m
            Console.WriteLine();

            //Console.ReadKey();
            Console.WriteLine("============ V2 ============");
            Console.WriteLine();

            MemoryCache.Default.Remove(_commonSettingCacheName);
            Initialize();

            // 1. 會員 A 讀取共用設定，得到 0.005m ，是程式初始化時設定的 Rate。
            Console.WriteLine($"Common Rate: {GetCommonRate()}");
            // 0.005m
            Console.WriteLine();

            // 2. 會員 B 使用 CreateSettingV2 建立新的設定。
            newSetting = CreateSettingV2("MemberB", 0.003m);
            Console.WriteLine($"New Setting. ProfileName: {newSetting.ProfileName}, CurrencyId: {newSetting.CurrencyId}, Rate: {newSetting.Rate}");
            // New Setting. ProfileName: MemberA, Rate: 0.003m
            Console.WriteLine();

            // 3. 會員 C 讀取共用設定，得到 0.005m ，與初始化設定相符。
            Console.WriteLine($"Common Rate: {GetCommonRate()}");
            // 0.005m
            Console.WriteLine();

            Console.ReadKey();
        }

        public class Setting
        {
            public string ProfileName { get; set; }
            public string CurrencyId = "TWD";
            public decimal Rate { get; set; }
        }

        /// <summary>
        /// 初始化。
        /// </summary>
        private static void Initialize()
        {
            // 此為範例，未檢查程式安全性。
            MemoryCache.Default.Add(
                _commonSettingCacheName,
                new Setting()
                {
                    ProfileName = string.Empty,
                    Rate = _defaultRate
                },
                new CacheItemPolicy()
            );
        }

        /// <summary>
        /// 取得快取物件。
        /// </summary>
        /// <param name="cacheName"></param>
        /// <returns></returns>
        private static Setting GetCacheObject(string cacheName)
        {
            // 此為範例，未檢查程式安全性。
            return MemoryCache.Default[cacheName] as Setting;
        }

        /// <summary>
        /// 從快取取得 commonSetting 並讀取 Rate 。
        /// </summary>
        /// <returns></returns>
        private static decimal GetCommonRate()
        {
            var commonSetting = GetCacheObject(_commonSettingCacheName);
            Console.WriteLine($"GetCommonRate Hash, commonSetting: {commonSetting.GetHashCode()}");
            return commonSetting?.Rate ?? _defaultRate;
        }

        /// <summary>
        /// 從快取取得 commonSetting ，以 commonSetting 為藍本，創建新的 setting 。
        /// </summary>
        /// <param name="newProfileName"></param>
        /// <param name="rate"></param>
        /// <returns></returns>
        private static Setting CreateSetting(string newProfileName, decimal rate)
        {
            var commonSetting = GetCacheObject(_commonSettingCacheName);
            Console.WriteLine($"CreateSetting Hash, commonSetting: {commonSetting.GetHashCode()}");
            commonSetting.ProfileName = newProfileName;
            commonSetting.Rate = rate;
            return commonSetting;
        }

        /// <summary>
        /// 從快取取得 commonSetting ，以 commonSetting 為藍本，先 new 新的 setting 再設定。
        /// </summary>
        /// <param name="newProfileName"></param>
        /// <param name="rate"></param>
        /// <returns></returns>
        private static Setting CreateSettingV2(string newProfileName, decimal rate)
        {
            var commonSetting = GetCacheObject(_commonSettingCacheName);
            Console.WriteLine($"CreateSettingV2 Hash, commonSetting: {commonSetting.GetHashCode()}");
            var newSetting = new Setting()
            {
                ProfileName = newProfileName,
                CurrencyId = commonSetting.CurrencyId,
                Rate = rate
            };
            Console.WriteLine($"CreateSettingV2 Hash, newSetting: {newSetting.GetHashCode()}");
            return newSetting;
        }
    }
}