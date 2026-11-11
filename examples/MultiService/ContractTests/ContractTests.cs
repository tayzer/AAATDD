using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass, DoNotParallelize]
public sealed class ContractTests
{
    [TestMethod]
    public async Task ProducerOutcomeIsAcceptedByTheConsumer()
    {
        await using var cluster = new Cluster();
        await cluster.Start();
        using var submitted = await cluster.Order("contract", false);
        Assert.AreEqual(HttpStatusCode.Accepted, submitted.StatusCode);
        Assert.AreEqual("completed", await cluster.WaitForOrder("contract", "completed"));
        using var response = await cluster.Http.GetAsync(cluster.Endpoints["payments"] + "/payments/contract");
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        using var outcome = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        using var contract = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "payment-outcome.json")));
        foreach (var property in contract.RootElement.GetProperty("requiredProperties").EnumerateArray())
            Assert.IsTrue(outcome.RootElement.TryGetProperty(property.GetString()!, out _), "Producer omitted " + property.GetString());
        Assert.AreEqual(contract.RootElement.GetProperty("schemaVersion").GetInt32(), outcome.RootElement.GetProperty("schemaVersion").GetInt32());
        Assert.AreEqual("contract", outcome.RootElement.GetProperty("orderId").GetString());
        Assert.IsTrue(contract.RootElement.GetProperty("statuses").EnumerateArray().Any(value => value.GetString() == outcome.RootElement.GetProperty("status").GetString()));
        using var replayed = await cluster.Http.PostAsJsonAsync(cluster.Endpoints["orders"] + "/payment-outcomes", outcome.RootElement);
        Assert.AreEqual(HttpStatusCode.OK, replayed.StatusCode);
    }

    [TestMethod]
    public async Task ConsumerRejectsUnsupportedOutcomeVersion()
    {
        await using var cluster = new Cluster();
        await cluster.Start();
        using var response = await cluster.Http.PostAsJsonAsync(cluster.Endpoints["orders"] + "/payment-outcomes",
            new { schemaVersion = 2, orderId = "contract", status = "approved" });
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
