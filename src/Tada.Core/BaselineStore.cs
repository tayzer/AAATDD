using System.Text.RegularExpressions;

namespace Tada;

public sealed partial class BaselineStore(Repository repository)
{
    public Repository Repository => repository;
    public string Prepare() => Publish(repository.Capture());

    internal string PrepareEngineering(Engineering engineering) => Publish(repository.Capture(engineering));

    private string Publish(Inventory inventory)
    {
        string id = CanonicalJson.Identity(inventory);
        string path = BaselinePath(id);
        if (!File.Exists(path))
        {
            string snapshot = Snapshot(id);
            string stage = repository.StateDirectory("scratch", Guid.NewGuid().ToString("N"));
            string sources = Path.Combine(stage, "sources");
            Directory.CreateDirectory(sources);
            try
            {
                foreach (var file in inventory.Files)
                {
                    string target = Path.Combine(sources, file.Path);
                    Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                    File.Copy(repository.Resolve(file.Path), target, overwrite: false);
                    File.SetAttributes(target, FileAttributes.Normal);
                    if (CanonicalJson.FileDigest(target) != file.Digest)
                        throw new TadaException("Oracle changed during capture. Prepare again after edits stop.");
                }
                if (!Directory.Exists(snapshot))
                {
                    try { Directory.Move(sources, snapshot); }
                    catch (IOException) when (Directory.Exists(snapshot)) { ValidateSnapshot(); }
                }
                else ValidateSnapshot();
                string stagedInventory = Path.Combine(stage, "inventory.json");
                File.WriteAllBytes(stagedInventory, CanonicalJson.Encode(inventory));
                try { File.Move(stagedInventory, path, overwrite: false); }
                catch (IOException) when (File.Exists(path)) { Read(id); }

                void ValidateSnapshot()
                {
                    var existing = new Repository(snapshot);
                    foreach (var input in inventory.Files)
                        if (CanonicalJson.FileDigest(existing.Resolve(input.Path)) != input.Digest)
                            throw new TadaException("Existing captured bytes do not match this baseline; do not overwrite history.");
                }
            }
            finally
            {
                // Only the fresh, owned GUID directory is removed. Published snapshots are outside it.
                Repository.NoLink(stage);
                Directory.Delete(stage, recursive: true);
            }
        }
        Read(id);
        return id;
    }

    public Inventory Read(string baseline)
    {
        var path = BaselinePath(baseline);
        var inventory = Data.Read<Inventory>(path);
        if (inventory.SchemaVersion is not (1 or 2 or 3 or 4 or 5 or 6 or 7) || CanonicalJson.Identity(inventory) != baseline)
            throw new TadaException("Baseline identity does not match its content.");
        var snapshot = new Repository(Snapshot(baseline));
        foreach (var file in inventory.Files)
            if (CanonicalJson.FileDigest(snapshot.Resolve(file.Path)) != file.Digest)
                throw new TadaException($"Captured baseline bytes changed: {file.Path}");
        return inventory;
    }

    public string Snapshot(string baseline)
    {
        BaselinePath(baseline);
        return Path.Combine(repository.StateDirectory("snapshots"), baseline[7..]);
    }

    public Review Review(string baseline, string? comparedTo = null)
    {
        Read(baseline);
        if (comparedTo != null) Read(comparedTo);
        var record = new Review(Guid.NewGuid().ToString("N"), baseline, DateTimeOffset.UtcNow, comparedTo);
        Data.WriteNew(RecordPath("reviews", record.Id), record);
        return record;
    }

    public Approval Approve(string reviewId, string by, string? replaces = null)
    {
        if (string.IsNullOrWhiteSpace(by)) throw new TadaException("An explicit approver declaration is required.");
        var review = Data.Read<Review>(RecordPath("reviews", reviewId));
        if (review.Id != reviewId) throw new TadaException("Review identity mismatch.");
        var inventory = Read(review.Baseline);
        if (inventory.Engineering != null) throw new TadaException("Supporting checks are captured by a verification plan and cannot receive business approval.");
        CheckCurrent(review.Baseline);
        if (replaces != null)
        {
            if (replaces == review.Baseline) throw new TadaException("A replacement must identify a different baseline revision.");
            if (review.ComparedTo != replaces) throw new TadaException("Review the replacement with --against the revision being replaced before approval.");
            var previous = Read(replaces);
            Approved(replaces);
            if (previous.Scope.ScenarioIds.Except(inventory.Scope.ScenarioIds).Any() ||
                previous.Scope.RequirementIds.Except(inventory.Scope.RequirementIds).Any())
                throw new TadaException("Replacement would drop obligations. Partial removal is unsupported; obtain an explicit scoped amendment.");
        }
        var approval = new Approval(Guid.NewGuid().ToString("N"), review.Id, review.Baseline, by,
            DateTimeOffset.UtcNow, replaces);
        Data.WriteNew(RecordPath("approvals", approval.Id), approval);
        return approval;
    }

    public Approval Approved(string baseline)
    {
        if (Read(baseline).Engineering != null) throw new TadaException("Supporting checks are not business acceptance baselines.");
        string directory = repository.StateDirectory("approvals");
        var approvals = Directory.EnumerateFiles(directory, "*.json").Select(path =>
        {
            Repository.NoLink(path);
            var item = Data.Read<Approval>(path);
            if (item.Id != Path.GetFileNameWithoutExtension(path)) throw new TadaException("Stored approval identity does not match its record path.");
            return item;
        }).ToArray();
        if (approvals.Any(a => a.Replaces == baseline)) throw new TadaException("Baseline has been explicitly superseded.");
        var approval = approvals.Where(a => a.Baseline == baseline).OrderBy(a => a.Created).FirstOrDefault()
            ?? throw new TadaException("Baseline is not approved. Tests cannot approve expectations.");
        var review = Data.Read<Review>(RecordPath("reviews", approval.Review));
        if (review.Baseline != baseline || review.Created > approval.Created)
            throw new TadaException("Approval does not identify an earlier review of this revision.");
        return approval;
    }

    public void CheckCurrent(string baseline)
    {
        var inventory = Read(baseline);
        if (CanonicalJson.Identity(repository.Capture(inventory.Engineering)) != baseline)
            throw new TadaException("Oracle inputs changed or the selected file set differs. Prepare, review and approve a new revision.");
    }

    public string BaselinePath(string id)
    {
        if (!DigestPattern().IsMatch(id)) throw new TadaException("Expected sha256:<64 lowercase hex> baseline identity.");
        string path = Path.Combine(repository.StateDirectory("baselines"), id[7..] + ".json");
        if (File.Exists(path)) Repository.NoLink(path);
        return path;
    }

    public string RecordPath(string kind, string id)
    {
        if (!Guid.TryParseExact(id, "N", out _)) throw new TadaException("Expected a record ID printed by TADA.");
        string directory = repository.StateDirectory(kind);
        string path = Path.Combine(directory, id + ".json");
        if (File.Exists(path)) Repository.NoLink(path);
        return path;
    }

    [GeneratedRegex(@"\Asha256:[a-f0-9]{64}\z", RegexOptions.CultureInvariant)]
    private static partial Regex DigestPattern();
}
