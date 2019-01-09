分享 .NET 的快取功能 (System.Runtime.Caching.MemoryCache) 的問題，如果我有誤解還請不吝告知，謝謝。

# 流程

流程：
1. 程式初始化時將共用物件存入快取。
2. 會員操作，使用函式調用快取物件，調用快取物件之後有下列行為：
   1. 讀取共用設定。
   2. 以共用設定為藍本，建立會員自己的設定。

# 基礎程式碼

私有屬性：

```
        private static readonly decimal _defaultRate = 0.005m;
        private static readonly string _commonSettingCacheName = "commonSetting";
```

快取物件類別：

```
        public class Setting
        {
            public string ProfileName { get; set; }
            public string CurrencyId = "TWD";
            public decimal Rate { get; set; }
        }
```

程式先將共用的設定存入快取：

```
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
```

調用快取物件：

```
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
```

調用快取物件並執行業務邏輯：

```
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
```

# 異常流程

1. 會員 A 讀取共用設定，得到 0.005m ，是程式初始化時設定的 Rate。

    ```
        Console.WriteLine($"Common Rate: {GetCommonRate()}");
        // 0.005m
    ```

2. 會員 B 建立了新的設定。

    ```
        var newSetting = CreateSetting("MemberB", 0.003m);
        Console.WriteLine($"New Setting. ProfileName: {newSetting.ProfileName}, Rate: {newSetting.Rate}");
        // New Setting. ProfileName: MemberB, Rate: 0.003m
    ```

3. 會員 C 讀取共用設定，得到 0.003m ，與初始化設定的不相符。

    ```
        Console.WriteLine($"Common Rate: {GetCommonRate()}");
        // 0.003m
    ```

# 異常原因

取得快取物件，這時是傳址而非 new 新的物件，所以修改物件的屬性之後，快取物件的屬性就被修改了。

## CreateSetting 流程

1. 會員 A 讀取共用設定，快取物件的 Hash 是 55915408

    ```
    GetCommonRate Hash, commonSetting: 55915408
    Common Rate: 0.005
    ```

2. 會員 B 使用 CreateSetting 建立新的設定，快取物件的 Hash 仍是 55915408 ，代表是同一個物件，所以 CreateSetting 其實是修改到快取物件的屬性。

    ```
    CreateSetting Hash, commonSetting: 55915408
    New Setting. ProfileName: MemberB, CurrencyId: TWD, Rate: 0.003
    ```

3. 會員 C 讀取共用設定，快取物件的 Hash 仍是 55915408 ，但是快取物件的屬性已經被修改了。

    ```
    CreateSetting Hash, commonSetting: 55915408
    Common Rate: 0.003
    ```

## CreateSettingV2 流程

1. 會員 A 讀取共用設定，快取物件的 Hash 是 33476626

    ```
    GetCommonRate Hash, commonSetting: 33476626
    Common Rate: 0.005
    ```

2. 會員 B 使用 CreateSettingV2 建立新的設定，快取物件的 Hash 仍是 33476626 ；新建物件去修改屬性，新物件的 Hash 是 32854180 ，所以 CreateSettingV2 並不會修改到快取物件的屬性。

    ```
    CreateSettingV2 Hash, commonSetting: 33476626
    CreateSettingV2 Hash, newSetting: 32854180
    New Setting. ProfileName: MemberB, CurrencyId: TWD, Rate: 0.003
    ```

3. 會員 C 讀取共用設定，快取物件的 Hash 仍是 33476626 ，但是快取物件的屬性已經被修改了。

    ```
    CreateSetting Hash, commonSetting: 33476626
    Common Rate: 0.003
    ```