using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using IksAdminApi;
using LogsApi;

namespace IksAdmin_Logs;

public class Main : AdminModule
{
    /* 
    
    ## ВСЁ СВЯЗАННОЕ С АДМИНОМ
    admin - CurrentName админа
    adminId - SteamId админа
    adminIp - Ip админа
    flags - CurrentFlags админа
    immunity - CurrentImmunity админа
    group - название группы админа
    end - дата окончания админ привелегии

    ## ЦЕЛЬ
    target - ник цели
    targetId - SteamId цели
    targetIp - Ip цели

    ## ДРУГОЕ
    end - дата окончания чего либо
    serverid - сервер айди
    server - название сервера (из конфига админки)
    address - ip:port сервера (из конфига админки)
    reason - причина действия
    duration - срок наказания
    type - тип наказания (Для бана, мута)

    */
    public override string ModuleName => "IksAdmin_Logs";
    public override string ModuleVersion => "1.0.0";
    public override string ModuleAuthor => "iks__";

    private ILogsApi _logsApi = null!;
    private ILogger _logger = null!;
    private PluginConfig _config = null!;
    public override void OnAllPluginsLoaded(bool hotReload)
    {
        base.OnAllPluginsLoaded(hotReload);
        PluginConfig.Instance = PluginConfig.Instance.ReadOrCreate(ModuleDirectory + "/../../configs/plugins/IksAdmin_Modules/Logs.json", PluginConfig.Instance);
        _config = PluginConfig.Instance;
        _logsApi = LogsCoreUtils.Api;
        _logger = _logsApi.CreateBaseLogger("admin_logs");
        _logger.CanLogToDiscord = _config.LogToDiscord;
        _logger.CanLogToVk = _config.LogToVk;
        _logger.CanLogToConsole = _config.LogToConsole;
        _logger.CanLogToFile = _config.LogToFile;
        _logsApi.RegisterLogger(_logger);
    }

    public override void Ready()
    {
        base.Ready();
        Api.OnDynamicEvent += OnDynamicEvent;
        Api.OnBanPost += OnBan;
        Api.SuccessUnban += OnUnban;
        Api.OnCommPost += OnComm;
        Api.SuccessUnComm += OnUnComm;
    }

    private string BanType(int type) {
        switch (type)
        {
            case 0:
                return "SteamID";
            case 1:
                return "SteamID";
            case 2:
                return "SteamID & IP";
            default:
                return "SteamID";
        }
    }
    private string CommType(int type) {
        switch (type)
        {
            case 0:
                return "Mute";
            case 1:
                return "Gag";
            case 2:
                return "Silence";
            default:
                return "Mute";
        }
    }


    private void OnUnComm(Admin admin, PlayerComm comm)
    {
        Server.NextFrame(() => {
            string embedKey = "unban";
            var embed = GetEmbed(embedKey);
            embed.SetKeyValues(
                ["admin", "adminId", "reason", "target", "targetIp", "targetId", "type"],
                admin.CurrentName, admin.SteamId, comm.UnbanReason ?? "", comm.Name ?? "", comm.Ip ?? "", comm.SteamId ?? "", "", CommType(comm.MuteType)
            );
            _logger.LogToAll(embed.ReplaceKeyValues(Localizer[embedKey]), vkChatId: _config.VkChatId, discordEmebed: embed, discordChannel: GetDiscordChannel(embedKey));
        });
    }
    private string GetDateString(int unixTimeStamp)
    {
        return unixTimeStamp == 0 ? Localizer["NEVER"] : Utils.GetDateString(unixTimeStamp);
    }
    private void OnUnban(Admin admin, PlayerBan ban)
    {
        Server.NextFrame(() => {
            string embedKey = "unban";
            var embed = GetEmbed(embedKey);
            embed.SetKeyValues(
                ["end", "admin", "adminId", "target", "duration", "type", "targetId", "targetIp", "type", "reason"],
                GetDateString(ban.EndAt) , admin!.CurrentName, admin.SteamId, ban.Name ?? "null", AdminUtils.GetDurationString(ban.Duration), ban.BanType, ban.SteamId ?? "null", ban.Ip ?? "null", BanType(ban.BanType), ban.UnbanReason!
            );
            _logger.LogToAll(embed.ReplaceKeyValues(Localizer[embedKey]), vkChatId: _config.VkChatId, discordEmebed: embed, discordChannel: GetDiscordChannel(embedKey));
        });
    }

    private HookResult OnDynamicEvent(EventData data)
    {
        switch (data.EventKey)
        {
            case "error":
                OnError(data.Get<string>("text"));
                break;
        }
        return HookResult.Continue;
    }

    private void OnError(string text)
    {
        string embedKey = "error";
        var embed = GetEmbed(embedKey);
        embed.SetKeyValues(
            ["text"],
            text
        );
        _logger.LogToAll(embed.ReplaceKeyValues(Localizer[embedKey]), vkChatId: _config.VkChatId, discordEmebed: embed, discordChannel: GetDiscordChannel(embedKey));
    }

    private HookResult OnComm(PlayerComm comm, ref bool announce)
    {
        Server.NextFrame(() => {
            var embedKey = "comm";
            var embed = GetEmbed(embedKey);
            embed.SetKeyValues(
                ["admin", "target", "duration", "type", "targetId", "targetIp"],
                comm.Admin!.CurrentName, comm.Name!, AdminUtils.GetDurationString(comm.Duration), CommType(comm.MuteType), comm.SteamId ?? "null", comm.Ip ?? "null"
            );
            _logger.LogToAll(embed.ReplaceKeyValues(Localizer[embedKey]), vkChatId: _config.VkChatId, discordEmebed: embed, discordChannel: GetDiscordChannel("comm"));
        });
        return HookResult.Continue;
    }

    private HookResult OnBan(PlayerBan ban, ref bool announce)
    {
        Server.NextFrame(() => {
            var embedKey = "ban";
            var embed = GetEmbed(embedKey);
            embed.SetKeyValues(
                ["end", "admin", "adminId", "target", "duration", "type", "targetId", "targetIp", "type", "reason"],
                GetDateString(ban.EndAt) ,ban.Admin!.CurrentName, ban.Admin.SteamId, ban.Name ?? "null", AdminUtils.GetDurationString(ban.Duration), ban.BanType, ban.SteamId ?? "null", ban.Ip ?? "null", BanType(ban.BanType), ban.Reason
            );
            _logger.LogToAll(embed.ReplaceKeyValues(Localizer[embedKey]), vkChatId: _config.VkChatId, discordEmebed: embed, discordChannel: GetDiscordChannel("ban"));
        });
        return HookResult.Continue;
    }

    private ulong GetDiscordChannel(string embedKey)
    {
        if (_config.CustomChannels.TryGetValue(embedKey, out var id))
            return id;
        return _config.DiscordChannel;
    }

    private DiscordEmbed GetEmbed(string name)
    {
        var embed = _config.Embeds[name];
        embed.WithAuthor(_config.Author);
        return embed;
    }
}
