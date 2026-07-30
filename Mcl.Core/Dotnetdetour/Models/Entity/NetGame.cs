using System.Collections.Generic;
using Newtonsoft.Json;

namespace Mcl.Core.Dotnetdetour.Models.Entity;

public class NetGameResponse
{
    [JsonProperty("code")]
    public int Code { get; set; } = 0;          // 默认 0

    [JsonProperty("details")]
    public string Details { get; set; } = "";   // 空字符串

    [JsonProperty("entity")]
    public NetGameEntity NetGameEntity { get; set; } = new NetGameEntity();

    [JsonProperty("message")]
    public string Message { get; set; } = "正常返回";

    [JsonProperty("summary_md5")]
    public string SummaryMd5 { get; set; } = "";
}

public class NetGameEntity
{
    [JsonProperty("available_scope")]
    public int AvailableScope { get; set; } = 1;

    [JsonProperty("balance_grade")]
    public int BalanceGrade { get; set; } = 0;

    [JsonProperty("brief_summary")]
    public string BriefSummary { get; set; } = "Hello Piggod!";

    [JsonProperty("developer_name")]
    public string DeveloperName { get; set; } = "开发者名称";

    [JsonProperty("download_num")]
    public int DownloadNum { get; set; } = 114514;

    [JsonProperty("entity_id")]
    public string EntityId { get; set; } = "4667047051898536811";

    [JsonProperty("game_status")]
    public int GameStatus { get; set; } = 0;

    [JsonProperty("goods_state")]
    public int GoodsState { get; set; } = 1;

    [JsonProperty("is_apollo")]
    public string IsApollo { get; set; } = "1";

    [JsonProperty("is_auth")]
    public bool IsAuth { get; set; } = false;

    [JsonProperty("item_type")]
    public int ItemType { get; set; } = 1;

    [JsonProperty("item_version")]
    public string ItemVersion { get; set; } = "1.0";

    [JsonProperty("like_num")]
    public int LikeNum { get; set; } = 114514;

    [JsonProperty("lobby_max_num")]
    public int LobbyMaxNum { get; set; } = 0;

    [JsonProperty("lobby_min_num")]
    public int LobbyMinNum { get; set; } = 0;

    [JsonProperty("master_type_id")]
    public string MasterTypeId { get; set; } = "2";

    [JsonProperty("mcversions")]
    public List<McVersion> McVersions { get; set; } = new List<McVersion>
    {
        new McVersion
        {
            EntityId = "322953",
            ItemId = "4667047051898536811",
            JavaVersion = 0,
            McVersionId = "13"
        }
    };

    [JsonProperty("mod_id")]
    public int ModId { get; set; } = 0;

    [JsonProperty("name")]
    public string Name { get; set; } = "Name";

    [JsonProperty("normal_number")]
    public string NormalNumber { get; set; } = "";

    [JsonProperty("online_count")]
    public string OnlineCount { get; set; } = "1";   // 最新值

    [JsonProperty("publish_time")]
    public long PublishTime { get; set; } = 1758765743;

    [JsonProperty("rarity")]
    public int Rarity { get; set; } = 0;

    [JsonProperty("rel_iid")]
    public int RelIid { get; set; } = 0;

    [JsonProperty("resource_version")]
    public int ResourceVersion { get; set; } = 0;

    [JsonProperty("review_status")]
    public int ReviewStatus { get; set; } = 1;

    [JsonProperty("season_begin")]
    public int SeasonBegin { get; set; } = 0;

    [JsonProperty("season_number")]
    public int SeasonNumber { get; set; } = 0;

    [JsonProperty("secondary_type_id")]
    public string SecondaryTypeId { get; set; } = "11";

    [JsonProperty("vanity_number")]
    public string VanityNumber { get; set; } = "";

    [JsonProperty("vip_only")]
    public bool VipOnly { get; set; } = false;
    
    [JsonProperty("title_image_url")]
    public string TitleImageUrl { get; set; } = "https://x19.fp.ps.netease.com/file/6454d69a3b7500ae44b1223b299k5Ip904";
    
    [JsonProperty("ip")]
    public string Ip { get; set; } = "";
    
    [JsonProperty("port")]
    public int Port { get; set; } = 0;
}

public class McVersion
{
    [JsonProperty("entity_id")]
    public string EntityId { get; set; } = "322953";

    [JsonProperty("item_id")]
    public string ItemId { get; set; } = "4667047051898536811";

    [JsonProperty("java_version")]
    public int JavaVersion { get; set; } = 0;

    [JsonProperty("mc_version_id")]
    public string McVersionId { get; set; } = "13";
}