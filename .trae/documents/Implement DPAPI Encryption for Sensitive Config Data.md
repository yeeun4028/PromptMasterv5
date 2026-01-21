I will implement DPAPI encryption for sensitive configuration fields.

1.  **Add Dependency**: Manually add the `System.Security.Cryptography.ProtectedData` package reference to `PromptMasterv5.csproj`.
2.  **Create Encryption Helper**: Implement `Infrastructure/Helpers/EncryptionHelper.cs` to handle `ProtectedData.Protect` (encrypt) and `Unprotect` (decrypt) using `DataProtectionScope.CurrentUser`. It will also handle Base64 encoding/decoding.
3.  **Create JSON Converter**: Implement `Infrastructure/Converters/JsonEncryptedStringConverter.cs` which uses the helper to encrypt strings during JSON serialization and decrypt them during deserialization. It will include fallback logic to handle existing plain-text data.
4.  **Apply Encryption**: Update `Core/Models/AppConfig.cs` and `Core/Models/ApiProfile.cs` to apply the `[JsonConverter(typeof(JsonEncryptedStringConverter))]` attribute to sensitive properties (`Password`, `AiApiKey`, `AiTranslateApiKey`, `Key1`, `Key2`).

This ensures that when the configuration is saved to disk, these fields are encrypted, but they remain accessible as plain text within the application memory. Existing plain-text configurations will be automatically upgraded to encrypted format upon the next save.