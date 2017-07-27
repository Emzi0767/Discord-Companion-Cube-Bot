using Newtonsoft.Json;

namespace Emzi0767.CompanionCube
{
    public sealed class MailmanSettings
    {
        public const string MetaKey = "mailman_settings";

        [JsonProperty("guild")]
        public ulong Guild { get; set; }

        [JsonProperty("channel")]
        public ulong Channel { get; set; }
    }
}
