using System.Net;
using System.Net.Http.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass, DoNotParallelize]
public sealed class ServiceTests
{
    [TestMethod]
    public async Task ReservationAndReleaseAreIdempotent()
    {
        await using var cluster = new Cluster();
        await cluster.Start();
        for (int attempt = 0; attempt < 2; attempt++)
        {
            using var response = await cluster.Http.PostAsync(cluster.Endpoints["inventory"] + "/reserve/reservation", null);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        }
        Assert.AreEqual(0L, await cluster.Number("inventory", "/stock", "available"));
        for (int attempt = 0; attempt < 2; attempt++)
        {
            using var response = await cluster.Http.PostAsync(cluster.Endpoints["inventory"] + "/release/reservation", null);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        }
        Assert.AreEqual(1L, await cluster.Number("inventory", "/stock", "available"));
    }

    [TestMethod]
    public async Task RepeatedPaymentCreatesOneCharge()
    {
        await using var cluster = new Cluster();
        await cluster.Start();
        for (int attempt = 0; attempt < 2; attempt++)
        {
            using var response = await cluster.Http.PostAsJsonAsync(cluster.Endpoints["payments"] + "/payments/payment", new { declinePayment = false });
            Assert.AreEqual(HttpStatusCode.Accepted, response.StatusCode);
        }
        Assert.AreEqual(1L, await cluster.Number("payments", "/charges", "count"));
    }
}
