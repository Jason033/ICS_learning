using System.Buffers.Binary;
using System.Globalization;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;

CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
var modes = new Dictionary<string, Action>(StringComparer.OrdinalIgnoreCase) {
    ["auth"] = Auth, ["tls"] = Tls, ["boundaries"] = Boundaries,
    ["rotation"] = Rotation, ["replay"] = Replay, ["audit"] = Audit, ["diagnose"] = Diagnose
};
try {
    string selected = args.Length == 0 ? "all" : args[0];
    if (selected.Equals("all", StringComparison.OrdinalIgnoreCase)) {
        foreach (var (name, action) in modes) { Console.WriteLine($"MODE {name}"); action(); }
    } else if (modes.TryGetValue(selected, out var action)) { Console.WriteLine($"MODE {selected}"); action(); }
    else throw new ArgumentException("Use all or: " + string.Join(", ", modes.Keys));
    Console.WriteLine("PASS end");
} catch (Exception error) {
    Console.Error.WriteLine($"FAIL {error.GetType().Name}: {error.Message}");
    Environment.ExitCode = 1;
}
static void Check(string name, bool condition) {
    if (!condition) throw new InvalidOperationException(name);
    Console.WriteLine("PASS " + name);
}
static void Equal<T>(string name, T actual, T expected) where T : notnull {
    Console.WriteLine($"DATA {name} actual={actual} expected={expected}");
    Check(name, EqualityComparer<T>.Default.Equals(actual, expected));
}
static LabPrincipal Reader(bool authenticated = true) => new("LabReader", authenticated,
    new HashSet<string>(StringComparer.Ordinal) { "status.read" }, new HashSet<string>(StringComparer.Ordinal) { "channel-A" });
static string Authorize(LabPrincipal principal, string resource, string action) {
    if (!principal.Authenticated) return "Unauthenticated";
    if (!principal.Actions.Contains(action)) return "MissingAction";
    if (!principal.Resources.Contains(resource)) return "WrongResource";
    return "Allowed";
}
static bool CredentialContext(string audience, string expectedAudience, int now, int expiry) =>
    audience == expectedAudience && now < expiry;
static void Auth() {
    Equal("unverified_principal_rejected", Authorize(Reader(false), "channel-A", "status.read"), "Unauthenticated");
    Equal("legal_read", Authorize(Reader(), "channel-A", "status.read"), "Allowed");
    Equal("write_rejected", Authorize(Reader(), "channel-A", "device.write"), "MissingAction");
    Equal("other_resource_rejected", Authorize(Reader(), "channel-B", "status.read"), "WrongResource");
    Check("wrong_audience_rejected", !CredentialContext("Api-A", "Api-B", 500, 600));
    Check("expired_context_rejected", !CredentialContext("Api-A", "Api-A", 600, 600));
    Check("matching_context", CredentialContext("Api-A", "Api-A", 500, 600));
}
static X509Certificate2 MakeRoot(RSA key, string commonName) {
    var request = new CertificateRequest("CN=" + commonName, key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
    request.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, false, 0, true));
    request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.KeyCertSign | X509KeyUsageFlags.CrlSign, true));
    request.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(request.PublicKey, false));
    return request.CreateSelfSigned(new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero), new DateTimeOffset(2040, 1, 1, 0, 0, 0, TimeSpan.Zero));
}
static X509Certificate2 MakeLeaf(X509Certificate2 issuer, bool serverUsage, bool expired) {
    using var key = RSA.Create(2048);
    var request = new CertificateRequest("CN=LabLeaf", key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
    request.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, true));
    request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature, true));
    request.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(new OidCollection { new Oid(serverUsage ? "1.3.6.1.5.5.7.3.1" : "1.3.6.1.5.5.7.3.2") }, true));
    var names = new SubjectAlternativeNameBuilder(); names.AddDnsName("bridge.lab.invalid");
    request.CertificateExtensions.Add(names.Build());
    return request.Create(issuer,
        new DateTimeOffset(expired ? 2028 : 2029, 1, 1, 0, 0, 0, TimeSpan.Zero),
        new DateTimeOffset(expired ? 2029 : 2031, 1, 1, 0, 0, 0, TimeSpan.Zero),
        RandomNumberGenerator.GetBytes(16));
}
static bool BuildOfflineChain(X509Certificate2 root, X509Certificate2 leaf) {
    using var chain = new X509Chain();
    chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
    chain.ChainPolicy.CustomTrustStore.Add(root);
    chain.ChainPolicy.VerificationTime = new DateTime(2030, 6, 1, 0, 0, 0, DateTimeKind.Utc);
    chain.ChainPolicy.DisableCertificateDownloads = true;
    // Offline model only: revocation is NOT verified, and this is NOT production TLS policy.
    chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
    return chain.Build(leaf);
}
static bool HasExplicitServerUsage(X509Certificate2 certificate) =>
    certificate.Extensions.OfType<X509EnhancedKeyUsageExtension>().Any(extension => extension.EnhancedKeyUsages.Cast<Oid>().Any(oid => oid.Value == "1.3.6.1.5.5.7.3.1"));
static bool NameMatches(X509Certificate2 leaf, string host) => leaf.MatchesHostname(host, false, false);
static void Tls() {
    using var rootKey = RSA.Create(2048);
    using var root = MakeRoot(rootKey, "LabRoot");
    using var leaf = MakeLeaf(root, true, false);
    using var wrongUsage = MakeLeaf(root, false, false);
    using var expired = MakeLeaf(root, true, true);
    using var otherKey = RSA.Create(2048);
    using var otherRoot = MakeRoot(otherKey, "OtherLabRoot");
    Check("trusted_chain_at_fixed_2030_time", BuildOfflineChain(root, leaf));
    Check("san_name_matches", NameMatches(leaf, "bridge.lab.invalid"));
    Check("wrong_host_rejected", !NameMatches(leaf, "radio.lab.invalid"));
    Check("server_usage_matches", HasExplicitServerUsage(leaf));
    Check("client_only_usage_rejected", !HasExplicitServerUsage(wrongUsage));
    Check("expired_leaf_rejected", !BuildOfflineChain(root, expired));
    Check("untrusted_root_rejected", !BuildOfflineChain(otherRoot, leaf));
    Check("chain_does_not_imply_correct_host", BuildOfflineChain(root, leaf) && !NameMatches(leaf, "radio.lab.invalid"));
    Console.WriteLine("LIMIT revocation_not_tested no_real_TLS_handshake no_OS_store_changes");
}
static bool AllRequiredSegmentsProtected(IReadOnlyList<LabSegment> path) =>
    path.Count > 0 && path.All(segment => segment.Confidential);
static void Boundaries() {
    Check("partial_tunnel_not_entire_path", !AllRequiredSegmentsProtected(new[] { new LabSegment("A-GW1", false), new LabSegment("GW1-GW2", true), new LabSegment("GW2-B", false) }));
    Check("all_segments_protected", AllRequiredSegmentsProtected(new[] { new LabSegment("A-B", true) }));
    Check("empty_path_not_verified", !AllRequiredSegmentsProtected(Array.Empty<LabSegment>()));
    Equal("model_payload_budget", 1500 - 80 - 40, 1380);
    Equal("large_outer_bytes", 1460 + 40 + 80, 1580);
    Equal("small_voice_outer_bytes", 160 + 40 + 80, 280);
    Check("direction_A_B_context_mismatch", !ContextMatches("V2", "V1"));
    Check("direction_B_A_context_matches", ContextMatches("V2", "V2"));
}
static bool ContextMatches(string receivedVersion, string expectedVersion) => receivedVersion == expectedVersion;
static bool CanUse(LabKeyVersion key, int now) => now >= key.AcceptFrom && now < key.AcceptUntil;
static byte[] Mac(byte[] key, byte[] bytes) => HMACSHA256.HashData(key, bytes);
static bool VerifyMac(byte[] key, byte[] bytes, byte[] tag) =>
    tag.Length == 32 && CryptographicOperations.FixedTimeEquals(Mac(key, bytes), tag);
static string VerifyVersionedMac(IReadOnlyDictionary<string, LabTrustedKey> trusted, string keyId, int now, byte[] bytes, byte[] tag) {
    if (!trusted.TryGetValue(keyId, out var selected)) return "UnknownKey";
    if (!CanUse(selected.Version, now)) return "KeyOutsideWindow";
    return VerifyMac(selected.Secret, bytes, tag) ? "Accepted" : "BadMac";
}
static byte[] ManifestBytes(int version, string payload) => JsonSerializer.SerializeToUtf8Bytes(new LabManifest(version, payload));
static bool VerifyManifest(RSA trustedSigner, int version, string payload, byte[] signature, int minimumVersion) =>
    trustedSigner.VerifyData(ManifestBytes(version, payload), signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pss) && version >= minimumVersion;
static void Rotation() {
    var k0 = new LabKeyVersion("K0", 0, 100);
    var k1 = new LabKeyVersion("K1", 80, 200);
    Check("old_before_overlap", CanUse(k0, 79));
    Check("new_before_start_rejected", !CanUse(k1, 79));
    Check("both_at_start", CanUse(k0, 80) && CanUse(k1, 80));
    Check("old_before_cutoff", CanUse(k0, 99));
    Check("old_at_cutoff_rejected", !CanUse(k0, 100));
    Check("new_at_old_cutoff", CanUse(k1, 100));
    byte[] secret = RandomNumberGenerator.GetBytes(32);
    try {
        var trusted = new Dictionary<string, LabTrustedKey>(StringComparer.Ordinal) { ["K1"] = new(k1, secret) };
        byte[] data = Encoding.UTF8.GetBytes("Lab schema v1");
        Check("mac_with_local_key", VerifyMac(secret, data, Mac(secret, data)));
        Equal("known_id_uses_trusted_key", VerifyVersionedMac(trusted, "K1", 90, data, Mac(secret, data)), "Accepted");
        Equal("unknown_key_id_rejected", VerifyVersionedMac(trusted, "AttackerKey", 90, data, Mac(secret, data)), "UnknownKey");
        Equal("known_key_before_start_rejected", VerifyVersionedMac(trusted, "K1", 79, data, Mac(secret, data)), "KeyOutsideWindow");
        Equal("known_key_at_cutoff_rejected", VerifyVersionedMac(trusted, "K1", 200, data, Mac(secret, data)), "KeyOutsideWindow");
        Check("changed_bytes_rejected", !VerifyMac(secret, Encoding.UTF8.GetBytes("Lab schema v2"), Mac(secret, data)));
    } finally { CryptographicOperations.ZeroMemory(secret); }
    using var signer = RSA.Create(2048);
    byte[] signedV2 = signer.SignData(ManifestBytes(2, "LabImage"), HashAlgorithmName.SHA256, RSASignaturePadding.Pss);
    byte[] signedV4 = signer.SignData(ManifestBytes(4, "LabImage"), HashAlgorithmName.SHA256, RSASignaturePadding.Pss);
    Check("signed_new_manifest", VerifyManifest(signer, 4, "LabImage", signedV4, 3));
    Check("signed_old_version_rejected", !VerifyManifest(signer, 2, "LabImage", signedV2, 3));
    Check("unsigned_version_change_rejected", !VerifyManifest(signer, 4, "LabImage", signedV2, 3));
    Check("changed_payload_rejected", !VerifyManifest(signer, 4, "ChangedImage", signedV4, 3));
    Console.WriteLine("LIMIT no_password_database no_firmware_flash no_Secure_Boot_changes");
}
static string FrameStatus(ReadOnlySpan<byte> bytes) {
    if (bytes.Length < 4) return "NeedHeader";
    uint length = BinaryPrimitives.ReadUInt32BigEndian(bytes);
    if (length > 1024) return "TooLong";
    if (bytes.Length - 4 < length) return "NeedBody";
    return "Complete";
}
static byte[] ProtectedBytes(LabEnvelope envelope) => JsonSerializer.SerializeToUtf8Bytes(envelope);
static bool WithinLifetime(int issued, int now) => issued <= now && (long)now - issued <= 100;
static string Receive(byte[] trustedKey, LabEnvelope envelope, byte[] receivedTag, string expectedSession, int now, LabReplayWindow window) {
    if (!VerifyMac(trustedKey, ProtectedBytes(envelope), receivedTag)) return "BadMac";
    if (envelope.Session != expectedSession) return "WrongSession";
    if (!WithinLifetime(envelope.IssuedAt, now)) return "ExpiredOrFuture";
    if (!window.TryAccept(envelope.Sequence)) return "ReplayOrOld";
    return "Accepted";
}
static void Replay() {
    Equal("short_header", FrameStatus(new byte[] { 0, 0 }), "NeedHeader");
    Equal("length_before_allocation", FrameStatus(new byte[] { 255, 255, 255, 255 }), "TooLong");
    Equal("partial_body", FrameStatus(new byte[] { 0, 0, 0, 4, 1, 2 }), "NeedBody");
    Equal("complete_frame", FrameStatus(new byte[] { 0, 0, 0, 2, 1, 2 }), "Complete");
    var window = new LabReplayWindow(32);
    Check("sequence_100", window.TryAccept(100));
    Check("sequence_102", window.TryAccept(102));
    Check("out_of_order_101", window.TryAccept(101));
    Check("duplicate_102_rejected", !window.TryAccept(102));
    Check("old_70_rejected", !window.TryAccept(70));
    byte[] key = RandomNumberGenerator.GetBytes(32);
    try {
        var packet = new LabEnvelope("K1", "S7", "R30", 10000, 10, "channel-A", "pulse");
        byte[] goodTag = Mac(key, ProtectedBytes(packet));
        byte[] corruptTag = goodTag.ToArray(); corruptTag[0] ^= 1;
        Equal("bad_high_sequence", Receive(key, packet, corruptTag, "S7", 11, window), "BadMac");
        Equal("highest_not_poisoned", window.Highest, 102L);
        var next = packet with { Sequence = 103 };
        Equal("next_valid_packet", Receive(key, next, Mac(key, ProtectedBytes(next)), "S7", 11, window), "Accepted");
        Equal("valid_mac_replay", Receive(key, next, Mac(key, ProtectedBytes(next)), "S7", 12, window), "ReplayOrOld");
        Equal("changed_resource_rejected", Receive(key, next with { Resource = "channel-B" }, Mac(key, ProtectedBytes(next)), "S7", 12, window), "BadMac");
        var otherSession = next with { Session = "S8", Sequence = 104 };
        Equal("wrong_session_rejected", Receive(key, otherSession, Mac(key, ProtectedBytes(otherSession)), "S7", 12, window), "WrongSession");
        var expired = next with { Sequence = 105, IssuedAt = 0 };
        Equal("expired_envelope_rejected", Receive(key, expired, Mac(key, ProtectedBytes(expired)), "S7", 101, window), "ExpiredOrFuture");
        bool invalidUtf8 = false;
        try { new UTF8Encoding(false, true).GetString(new byte[] { 0xc3, 0x28 }); } catch (DecoderFallbackException) { invalidUtf8 = true; }
        Check("invalid_utf8_rejected", invalidUtf8);
    } finally { CryptographicOperations.ZeroMemory(key); }
}
static void ValidateId(string id) {
    if (id.Length is < 1 or > 32 || !id.All(c => char.IsAsciiLetterOrDigit(c) || c is '.' or '_' or '-'))
        throw new ArgumentException("Audit identifier outside Lab contract");
}
static string BuildAudit(LabRequest request, LabPrincipal principal, string decision) {
    var audit = new LabAuditRecord(request.Id, principal.Id, request.Resource, request.Action, decision, "v1");
    ValidateId(audit.RequestId); ValidateId(audit.SubjectId); ValidateId(audit.Resource); ValidateId(audit.Action); ValidateId(audit.PolicyVersion);
    return JsonSerializer.Serialize(audit);
}
static bool RequestedRightsAllowed(IEnumerable<string> requested, HashSet<string> allowed) => requested.All(allowed.Contains);
static void Audit() {
    var fileRights = new HashSet<string>(StringComparer.Ordinal) { "Read" };
    Check("necessary_read_allowed", RequestedRightsAllowed(new[] { "Read" }, fileRights));
    Check("unnecessary_write_rejected", !RequestedRightsAllowed(new[] { "Read", "Write" }, fileRights));
    Check("parent_search_required", !new[] { true, false }.All(search => search));
    var request = new LabRequest("R1", "channel-A", "status.read", "SyntheticPasswordDoNotUse", "SyntheticBearerDoNotUse");
    string serialized = BuildAudit(request, Reader(), "Allowed");
    Check("audit_no_password", !serialized.Contains(request.Password, StringComparison.Ordinal));
    Check("audit_no_token", !serialized.Contains(request.Token, StringComparison.Ordinal));
    Check("audit_fields_are_whitelisted", !serialized.Contains("Password", StringComparison.Ordinal) && !serialized.Contains("Token", StringComparison.Ordinal));
    Console.WriteLine("DATA audit=" + serialized);
    bool rejected = false;
    try { BuildAudit(request with { Id = "R1\nFAKE Allowed" }, Reader(), "Denied"); } catch (ArgumentException) { rejected = true; }
    Check("newline_identifier_rejected", rejected);
}
static string SecurityDecision(bool? name, bool? authenticated, bool? authorized) {
    if (name is null) return "NeedEvidence";
    if (name == false) return "NameMismatch";
    if (authenticated is null) return "NeedEvidence";
    if (authenticated == false) return "Unauthenticated";
    if (authorized is null) return "NeedEvidence";
    return authorized == true ? "Allowed" : "Forbidden";
}
static void Diagnose() {
    Equal("case_A_identity_boundary", SecurityDecision(false, true, true), "NameMismatch");
    Equal("case_A_authorization_boundary", Authorize(Reader(), "channel-A", "tx.start"), "MissingAction");
    Equal("case_A_legal_read", SecurityDecision(true, true, true), "Allowed");
    Equal("missing_security_observation", SecurityDecision(null, true, true), "NeedEvidence");
    Check("case_B_context_mismatch", !ContextMatches("V4", "V3"));
    var k5 = new LabKeyVersion("K5", 0, 120); var k6 = new LabKeyVersion("K6", 100, 220);
    Check("case_C_overlap", CanUse(k5, 110) && CanUse(k6, 110));
    Check("case_C_old_key_cutoff", !CanUse(k5, 120));
    byte[] key = RandomNumberGenerator.GetBytes(32);
    try {
        var window = new LabReplayWindow(32); var operations = new HashSet<string>(StringComparer.Ordinal); int pulses = 0;
        var original = new LabEnvelope("K6", "S9", "Pulse7", 40, 10, "channel-A", "pulse");
        string first = Receive(key, original, Mac(key, ProtectedBytes(original)), "S9", 10, window);
        if (first == "Accepted" && operations.Add(original.Request)) pulses++;
        Equal("original_message", first, "Accepted");
        Equal("same_message_rejected", Receive(key, original, Mac(key, ProtectedBytes(original)), "S9", 11, window), "ReplayOrOld");
        var retry = original with { Sequence = 41 };
        string second = Receive(key, retry, Mac(key, ProtectedBytes(retry)), "S9", 12, window);
        if (second == "Accepted" && operations.Add(retry.Request)) pulses++;
        Equal("new_authenticated_attempt", second, "Accepted");
        Equal("same_business_intent_once_in_this_process", pulses, 1);
        var restartedOperations = new HashSet<string>(StringComparer.Ordinal);
        Check("restart_loses_in_memory_deduplication", restartedOperations.Add(original.Request));
    } finally { CryptographicOperations.ZeroMemory(key); }
    Console.WriteLine("LIMIT single_process_memory_only no_global_exactly_once no_real_security_deployment");
}
sealed record LabPrincipal(string Id, bool Authenticated, HashSet<string> Actions, HashSet<string> Resources);
sealed record LabSegment(string Name, bool Confidential);
sealed record LabKeyVersion(string Id, int AcceptFrom, int AcceptUntil);
sealed record LabTrustedKey(LabKeyVersion Version, byte[] Secret);
sealed record LabManifest(int Version, string Payload);
sealed record LabEnvelope(string KeyId, string Session, string Request, long Sequence, int IssuedAt, string Resource, string Action);
sealed record LabRequest(string Id, string Resource, string Action, string Password, string Token);
sealed record LabAuditRecord(string RequestId, string SubjectId, string Resource, string Action, string Decision, string PolicyVersion);
sealed class LabReplayWindow(int width) {
    readonly HashSet<long> seen = new();
    public long Highest { get; private set; } = -1;
    public bool TryAccept(long sequence) {
        if (width <= 0 || sequence < 0) throw new ArgumentOutOfRangeException();
        if (Highest >= 0 && sequence <= Highest - width) return false;
        if (seen.Contains(sequence)) return false;
        if (sequence > Highest) Highest = sequence;
        seen.RemoveWhere(old => old <= Highest - width);
        seen.Add(sequence);
        return true;
    }
}
