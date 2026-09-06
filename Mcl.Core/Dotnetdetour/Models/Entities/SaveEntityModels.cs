using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Mcl.Core.Dotnetdetour.Models.Entity
{
    // API 响应基础结构
    public class BaseResponse<T>
    {
        [JsonProperty("code")]
        public int Code { get; set; }
        
        [JsonProperty("message")]
        public string Message { get; set; }
        
        [JsonProperty("details")]
        public string Details { get; set; }
        
        [JsonProperty("total")]
        public int Total { get; set; }

        [JsonProperty("entities")]
        public List<T> Entities { get; set; }

        [JsonProperty("entity")]
        public T Entity { get; set; }
    }

    // 存档实体
    public class SaveEntity
    {
        [JsonProperty("backup_id")]
        public int BackupId { get; set; }

        [JsonProperty("save_id")]
        public string SaveId { get; set; }

        [JsonProperty("res_id")]
        public string ResId { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("timestamp")]
        public long Timestamp { get; set; }

        [JsonProperty("expire_time")]
        public long ExpireTime { get; set; }

        [JsonProperty("size")]
        public long Size { get; set; }
    }

    // 用于 UI 绑定的包装类 (ViewModel)
    public class SaveItemViewModel
    {
        public SaveEntity OriginalData { get; set; }

        public string Name => OriginalData.Name;
        public int BackupId => OriginalData.BackupId;
        public string SaveId => OriginalData.SaveId;
        public string ResId => OriginalData.ResId;
        
        // 格式化文件大小 (KB/MB)
        public string FormattedSize => OriginalData.Size >= 1024 
            ? $"{(OriginalData.Size / 1024.0):F2} MB" 
            : $"{OriginalData.Size} KB";

        // 时间戳转本地时间 (.NET 4.8.1 支持 DateTimeOffset.FromUnixTimeSeconds)
        public string SaveTime => DateTimeOffset.FromUnixTimeSeconds(OriginalData.Timestamp).LocalDateTime.ToString("yyyy-MM-dd HH:mm:ss");
        public string ExpireTime => DateTimeOffset.FromUnixTimeSeconds(OriginalData.ExpireTime).LocalDateTime.ToString("yyyy-MM-dd HH:mm:ss");

        public SaveItemViewModel(SaveEntity entity)
        {
            OriginalData = entity;
        }
    }
}