using System;
using System.Collections.Generic;
using Newtonsoft.Json;

// ── transmutation/list ───────────────────────────────────────────────────────
// Xem Docs/be-transmutation-rpc-spec.md mục 3 — toàn bộ state hệ thống Luyện Hóa
// (level/exp/energy/equips/pending). Request luôn "{}", giống weapon/list, hero/list.

[Serializable]
public class TransmutationModifierDto
{
    /// <summary>Tên StatType phía client (vd "Atk", "MaxHp", "CritChance"...).</summary>
    [JsonProperty("stat_type")] public string StatType;
    /// <summary>"Add" | "Multiply".</summary>
    [JsonProperty("op")]        public string Op;
    [JsonProperty("value")]     public float  Value;
    [JsonProperty("is_unique")] public bool   IsUnique;
}

/// <summary>Dùng cho equips[] và pending — có item_type.</summary>
[Serializable]
public class TransmutationItemDto
{
    /// <summary>"Weapon" | "Gloves" | "Shield" | "Helmet" | "Armor" | "Boots" | "Ring" | "Necklace" | "Relic" | "Pendant".</summary>
    [JsonProperty("item_type")] public string ItemType;
    [JsonProperty("cfg_id")]    public string CfgId;
    /// <summary>D|C|B|A|S|SS|SSS|R|SR.</summary>
    [JsonProperty("tier")]      public string Tier;
    /// <summary>Level riêng của artifact — không liên quan level hệ thống Luyện Hóa.</summary>
    [JsonProperty("level")]     public int    Level;
    [JsonProperty("modifiers")] public List<TransmutationModifierDto> Modifiers;
}

/// <summary>Dùng cho current_equip / equipped / replaced — cùng item_type với pending trong response, không lặp lại field.</summary>
[Serializable]
public class TransmutationItemBaseDto
{
    [JsonProperty("cfg_id")]    public string CfgId;
    [JsonProperty("tier")]      public string Tier;
    [JsonProperty("level")]     public int    Level;
    [JsonProperty("modifiers")] public List<TransmutationModifierDto> Modifiers;
}

/// <summary>Dùng cho dismantled — không có modifiers (item đã huỷ).</summary>
[Serializable]
public class TransmutationDismantledItemDto
{
    [JsonProperty("cfg_id")] public string CfgId;
    [JsonProperty("tier")]   public string Tier;
    [JsonProperty("level")]  public int    Level;
}

/// <summary>Response từ transmutation/list — nguồn sự thật toàn bộ state Luyện Hóa.</summary>
[Serializable]
public class TransmutationListResponse
{
    /// <summary>Level hệ thống Luyện Hóa — KHÁC level account/hero.</summary>
    [JsonProperty("level")]   public int  Level;
    [JsonProperty("exp")]     public long Exp;
    /// <summary>Currency riêng để roll — KHÁC field "energy" của player/me (xem Docs/be-transmutation-rpc-spec.md mục 7).</summary>
    [JsonProperty("energy")]  public long Energy;
    /// <summary>Tối đa 1 entry mỗi item_type.</summary>
    [JsonProperty("equips")]  public List<TransmutationItemDto> Equips;
    /// <summary>null nếu không có item nào đang chờ resolve.</summary>
    [JsonProperty("pending")] public TransmutationItemDto Pending;
}

// ── transmutation/fuse ────────────────────────────────────────────────────────
// Request luôn "{}" — RNG nằm hoàn toàn server-side, không có field nào để gửi lên.

/// <summary>Response từ transmutation/fuse. Khi Success = false, chỉ Error có giá trị.</summary>
[Serializable]
public class TransmutationFuseResponse
{
    [JsonProperty("success")] public bool   Success;
    /// <summary>"NOT_ENOUGH_ENERGY" | "CONFIG_NOT_FOUND"</summary>
    [JsonProperty("error")]   public string Error;

    /// <summary>false nếu đây là pending cũ trả lại (chưa resolve) — không phải lỗi, không tốn thêm energy.</summary>
    [JsonProperty("is_new_pending")] public bool IsNewPending;
    [JsonProperty("pending")]        public TransmutationItemDto     Pending;
    /// <summary>Item đang mặc cùng item_type với pending — null nếu slot đang trống.</summary>
    [JsonProperty("current_equip")]  public TransmutationItemBaseDto CurrentEquip;

    [JsonProperty("energy_spent")]   public long EnergySpent;
    [JsonProperty("energy_balance")] public long EnergyBalance;
    [JsonProperty("exp_gained")]     public long ExpGained;
    [JsonProperty("level")]          public int  Level;
    [JsonProperty("is_level_up")]    public bool IsLevelUp;
}

// ── transmutation/equip ───────────────────────────────────────────────────────
// Request luôn "{}" — luôn áp dụng cho pending hiện tại của user, không cần gửi id.

/// <summary>Response từ transmutation/equip. Khi Success = false, chỉ Error có giá trị.</summary>
[Serializable]
public class TransmutationEquipResponse
{
    [JsonProperty("success")]   public bool   Success;
    /// <summary>"NO_PENDING_ITEM"</summary>
    [JsonProperty("error")]     public string Error;

    [JsonProperty("item_type")] public string ItemType;
    [JsonProperty("equipped")]  public TransmutationItemBaseDto Equipped;
    /// <summary>Item bị thay thế ở cùng item_type — null nếu slot trước đó đang trống.</summary>
    [JsonProperty("replaced")]  public TransmutationItemBaseDto Replaced;
}

// ── transmutation/dismantle ───────────────────────────────────────────────────
// Request luôn "{}".

/// <summary>Response từ transmutation/dismantle. Khi Success = false, chỉ Error có giá trị.</summary>
[Serializable]
public class TransmutationDismantleResponse
{
    [JsonProperty("success")]        public bool   Success;
    /// <summary>"NO_PENDING_ITEM"</summary>
    [JsonProperty("error")]          public string Error;

    [JsonProperty("item_type")]      public string ItemType;
    [JsonProperty("dismantled")]     public TransmutationDismantledItemDto Dismantled;

    /// <summary>Hoàn lại theo tier của item bị huỷ — 0 nếu BE chưa implement phần refund (vẫn hợp lệ, xem Docs/be-transmutation-rpc-spec.md mục 6-7).</summary>
    [JsonProperty("energy_refund")]  public long EnergyRefund;
    [JsonProperty("energy_balance")] public long EnergyBalance;
    [JsonProperty("exp_gained")]     public long ExpGained;
    [JsonProperty("level")]          public int  Level;
    [JsonProperty("is_level_up")]    public bool IsLevelUp;
}
