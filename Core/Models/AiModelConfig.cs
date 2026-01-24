using System.Text.Json.Serialization;

namespace PromptMasterv5.Core.Models
{
    public partial class AiModelConfig : CommunityToolkit.Mvvm.ComponentModel.ObservableObject
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [JsonPropertyName("baseUrl")]
        public string BaseUrl { get; set; } = "";

        [JsonPropertyName("apiKey")]
        public string ApiKey { get; set; } = "";

        [JsonPropertyName("modelName")]
        public string ModelName { get; set; } = "";

        [CommunityToolkit.Mvvm.ComponentModel.ObservableProperty]
        [property: JsonPropertyName("isEnableForTranslation")]
        private bool isEnableForTranslation;

        [CommunityToolkit.Mvvm.ComponentModel.ObservableProperty]
        [property: JsonPropertyName("isEnableForOcr")]
        private bool isEnableForOcr;
    }
}
