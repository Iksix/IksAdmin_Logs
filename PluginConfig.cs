using IksAdminApi;
using LogsApi;

namespace IksAdmin_Logs;
public class PluginConfig : PluginCFG<PluginConfig>
{
    private DiscordEmbed exampleEmbed = new DiscordEmbed();
    public PluginConfig() {
        exampleEmbed.AddField("Example", "example", false);
        Embeds.Add("example", exampleEmbed);
    }
    public static PluginConfig Instance = new PluginConfig();

    public bool LogToDiscord {get; set;} = true;
    public bool LogToVk {get; set;} = false;
    public bool LogToFile {get; set;} = true;
    public bool LogToConsole {get; set;} = true;


    public string Author {get; set;} = "Test server";
    public string ConsoleColor {get; set;} = "Cyan";
    public long VkChatId {get; set;} = 0;
    public ulong DiscordChannel {get; set;} = 0;

    public Dictionary<string, ulong> CustomChannels {get; set;} = new()
    {
        {"ban", 123},
        {"comm", 123},
    };

    public Dictionary<string, DiscordEmbed> Embeds {get; set;} = new() {
    };
}