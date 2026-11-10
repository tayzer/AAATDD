using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Reqnroll;

[assembly: DoNotParallelize]

[Binding]
public sealed class JourneyBindings
{
    private readonly Cluster cluster = new();
    private HttpStatusCode submitted;
    private HttpStatusCode replayed = HttpStatusCode.OK;

    [Given("the three services are ready with one available item")]
    public async Task Ready() => await cluster.Start();

    [When("I submit an order with an approved payment")]
    public async Task Approved() => await Submit(false);

    [When("I submit an order with a declined payment")]
    public async Task Declined() => await Submit(true);

    [When("I submit the same approved order twice")]
    public async Task Duplicate()
    {
        await Submit(false);
        await Submit(false);
    }

    private async Task Submit(bool declined)
    {
        using var response = await cluster.Order("journey", declined);
        submitted = response.StatusCode;
    }

    [Then("the order becomes completed within 10 seconds")]
    public async Task Completed()
    {
        Assert.AreEqual(HttpStatusCode.Accepted, submitted);
        Assert.AreEqual("completed", await cluster.WaitForOrder("journey", "completed"));
    }

    [Then("the order becomes rejected within 10 seconds")]
    public async Task Rejected()
    {
        Assert.AreEqual(HttpStatusCode.Accepted, submitted);
        Assert.AreEqual("rejected", await cluster.WaitForOrder("journey", "rejected"));
    }

    [When("I deliver the recorded payment outcome again")]
    public async Task Replay()
    {
        using var response = await cluster.Http.GetAsync(cluster.Endpoints["payments"] + "/payments/journey");
        if (response.StatusCode != HttpStatusCode.OK) { replayed = response.StatusCode; return; }
        using var outcome = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        using var delivery = await cluster.Http.PostAsJsonAsync(cluster.Endpoints["orders"] + "/payment-outcomes", outcome.RootElement);
        replayed = delivery.StatusCode;
    }

    [Then("zero items are available")]
    public async Task Reserved()
    {
        Assert.AreEqual(HttpStatusCode.OK, replayed);
        Assert.AreEqual(0L, await cluster.Number("inventory", "/stock", "available"));
    }

    [Then("one item is available")]
    public async Task Released() => Assert.AreEqual(1L, await cluster.Number("inventory", "/stock", "available"));

    [Then("one charge exists")]
    public async Task Charged() => Assert.AreEqual(1L, await cluster.Number("payments", "/charges", "count"));

    [Then("no charges exist")]
    public async Task Uncharged() => Assert.AreEqual(0L, await cluster.Number("payments", "/charges", "count"));

    [AfterScenario]
    public async Task Cleanup() => await cluster.DisposeAsync();
}
