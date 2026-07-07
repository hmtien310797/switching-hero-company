// using System;
// using System.Collections.Generic;
// using System.Globalization;
// using Immortal_Switch.Scripts.Currency;
// using Immortal_Switch.Scripts.Level.Stage;
//
// namespace Immortal_Switch.Scripts.Reward
// {
//     public class OnlineIdleRewardBuffer
//     {
//         private readonly Dictionary<CurrencyType, double> collectingRewards = new Dictionary<CurrencyType, double>();
//         private readonly Dictionary<CurrencyType, double> inFlightRewards = new Dictionary<CurrencyType, double>();
//
//         public void Add(CurrencyType currencyType, double amount)
//         {
//             if (currencyType == CurrencyType.none)
//                 return;
//
//             if (amount <= 0)
//                 return;
//
//             if (!collectingRewards.ContainsKey(currencyType))
//                 collectingRewards[currencyType] = 0d;
//
//             collectingRewards[currencyType] += amount;
//         }
//
//         public bool HasCollectingReward()
//         {
//             foreach (var pair in collectingRewards)
//             {
//                 if (pair.Value >= 1d)
//                     return true;
//             }
//
//             return false;
//         }
//
//         public bool HasInFlightReward()
//         {
//             foreach (var pair in inFlightRewards)
//             {
//                 if (pair.Value >= 1d)
//                     return true;
//             }
//
//             return false;
//         }
//
//         public List<StageReward> BeginFlush()
//         {
//             List<StageReward> result = new List<StageReward>();
//             List<CurrencyType> keys = new List<CurrencyType>(collectingRewards.Keys);
//
//             for (int i = 0; i < keys.Count; i++)
//             {
//                 string key = keys[i];
//
//                 if (!collectingRewards.TryGetValue(key, out double rawAmount))
//                     continue;
//
//                 double amount = Math.Floor(rawAmount);
//
//                 if (amount <= 0)
//                     continue;
//
//                 AddToDictionary(inFlightRewards, key, amount);
//
//                 result.Add(new StageReward
//                 {
//                     currencyType = key,
//                     amount = amount.ToString("0", CultureInfo.InvariantCulture)
//                 });
//                 
//                 double remain = rawAmount - amount;
//
//                 if (remain > 0)
//                 {
//                     collectingRewards[key] = remain;
//                 }
//                 else
//                 {
//                     collectingRewards.Remove(key);
//                 }
//             }
//
//             return result;
//         }
//
//         public void CommitFlush()
//         {
//             inFlightRewards.Clear();
//         }
//
//         public void RollbackFlush()
//         {
//             List<string> keys = new List<string>(inFlightRewards.Keys);
//
//             for (int i = 0; i < keys.Count; i++)
//             {
//                 string key = keys[i];
//
//                 if (!inFlightRewards.TryGetValue(key, out double amount))
//                     continue;
//
//                 AddToDictionary(collectingRewards, key, amount);
//             }
//
//             inFlightRewards.Clear();
//         }
//
//         public double GetPreviewAmount(string currencyType)
//         {
//             double total = 0d;
//
//             if (collectingRewards.TryGetValue(currencyType, out double collecting))
//                 total += collecting;
//
//             if (inFlightRewards.TryGetValue(currencyType, out double inFlight))
//                 total += inFlight;
//
//             return Math.Floor(total);
//         }
//
//         public void ClearAll()
//         {
//             collectingRewards.Clear();
//             inFlightRewards.Clear();
//         }
//
//         public Dictionary<string, double> BuildPreviewSnapshot()
//         {
//             Dictionary<string, double> snapshot = new Dictionary<string, double>();
//
//             foreach (var pair in collectingRewards)
//             {
//                 if (!snapshot.ContainsKey(pair.Key))
//                     snapshot[pair.Key] = 0d;
//
//                 snapshot[pair.Key] += pair.Value;
//             }
//
//             foreach (var pair in inFlightRewards)
//             {
//                 if (!snapshot.ContainsKey(pair.Key))
//                     snapshot[pair.Key] = 0d;
//
//                 snapshot[pair.Key] += pair.Value;
//             }
//
//             return snapshot;
//         }
//
//         private static void AddToDictionary(Dictionary<string, double> dict, string key, double amount)
//         {
//             if (!dict.ContainsKey(key))
//                 dict[key] = 0d;
//
//             dict[key] += amount;
//         }
//     }
// }