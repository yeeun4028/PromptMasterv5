I will now replace the DPAPI implementation with standard AES-256 encryption using a hardcoded key (Plan A).

1.  **Remove Dependency**: Remove the `System.Security.Cryptography.ProtectedData` package from `PromptMasterv5.csproj`.
2.  **Update Helper**: Rewrite `Infrastructure/Helpers/EncryptionHelper.cs` to use `Aes` class.
    *   **Key**: Use a hardcoded 32-byte key derived from a static string (e.g., a hash of "PromptMaster_v5_Secret_Key").
    *   **IV**: Generate a random 16-byte IV for each encryption operation.
    *   **Format**: Prepend the IV to the ciphertext before Base64 encoding (Format: `Base64(IV + Ciphertext)`).
    *   **Unprotect**: Read the first 16 bytes as IV, decrypt the rest.
    *   **Fallback**: Keep the existing try-catch structure to handle plain-text fallback for backward compatibility.

This change will make the configuration portable across different Windows machines while still obscuring sensitive data in the file.