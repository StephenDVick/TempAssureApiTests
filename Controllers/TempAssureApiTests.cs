using Controllers;
using Microsoft.AspNetCore.Mvc;
using Models.DTOs;
using Microsoft.EntityFrameworkCore;
using Data;
using Models;
using Microsoft.EntityFrameworkCore.InMemory; // Add this using directive at the top of the file

namespace TempAssureApiTests.Controllers
{
    internal class StubAppDbContext : AppDbContext
    {
        public StubAppDbContext() : base(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options) { }
    }

    public class TempAssureApiTests
    {
        [Fact]
        public async Task ValidateTemperature_ReturnsBadRequest_WhenPayloadNull()
        {
            var controller = new TemperatureController(new StubTemperatureService(new()), null!);
            var result = await controller.ValidateTemperature(null!, CancellationToken.None);

            Assert.IsType<BadRequestObjectResult>(result.Result);

        }

        [Fact]
        public async Task ValidateTemperature_ReturnsOk_WithValidationPayload()
        {
            var serviceResult = new TemperatureValidationResult
            {
                Compliant = true,
                ProductId = 101,
                Threshold = new ThresholdInfo(32, 36, "VendorA", "SKU1", "Top")
            };
            var controller = new TemperatureController(new StubTemperatureService(serviceResult), null!);

            var result = await controller.ValidateTemperature(new TemperatureReading
            {
                Vendor = "VendorA",
                Sku = "SKU1",
                Position = "Top",
                Temperature = 34
            }, CancellationToken.None);

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var body = Assert.IsType<TemperatureValidationResult>(ok.Value);

            Assert.True(body.Compliant);
            Assert.Equal(101, body.ProductId);

            Assert.NotNull(body.Threshold);
            var threshold = body.Threshold!;
            Assert.Equal(32, threshold.Min);
            Assert.Equal(36, threshold.Max);
            Assert.Equal("VendorA", threshold.Vendor);
            Assert.Equal("SKU1", threshold.Sku);
            Assert.Equal("Top", threshold.Position);
        }

        [Fact]
        public async Task ValidateTemperature_ReturnsOk_WithNonCompliantPayload()
        {
            var serviceResult = new TemperatureValidationResult
            {
                Compliant = false,
                ProductId = 202,
                Threshold = new ThresholdInfo(32, 36, "VendorB", "SKU2", "Bottom")
            };
            var controller = new TemperatureController(new StubTemperatureService(serviceResult), null!);

            var result = await controller.ValidateTemperature(new TemperatureReading
            {
                Vendor = "VendorB",
                Sku = "SKU2",
                Position = "Bottom",
                Temperature = 40
            }, CancellationToken.None);

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var body = Assert.IsType<TemperatureValidationResult>(ok.Value);

            Assert.False(body.Compliant);
            Assert.Equal(202, body.ProductId);
        }

        [Fact]
        public async Task UploadReading_ReturnsBadRequest_WhenPayloadNull()
        {
            var db = new StubAppDbContext();
            var controller = new TemperatureController(new StubTemperatureService(new()), db);
            var result = await controller.UploadReading(null!, CancellationToken.None);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task UploadReading_ReturnsNotFound_WhenPoNotFound()
        {
            var db = new StubAppDbContext();
            var controller = new TemperatureController(new StubTemperatureService(new()), db);

            var result = await controller.UploadReading(new TemperatureReading
            {
                PoId = 1,
                Vendor = "VendorA",
                Sku = "SKU1",
                Position = "Top",
                Temperature = 34
            }, CancellationToken.None);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task UploadReading_ReturnsOk_WhenValid()
        {
            var db = new StubAppDbContext();
            var po = new TempQcPo { Id = 1, PoNumber = "PO1", Vendor = "VendorA" };
            db.TempQcPos.Add(po);
            await db.SaveChangesAsync();

            var serviceResult = new TemperatureValidationResult
            {
                Compliant = true,
                ProductId = 101,
                Threshold = new ThresholdInfo(32, 36, "VendorA", "SKU1", "Top")
            };
            var controller = new TemperatureController(new StubTemperatureService(serviceResult), db);

            var result = await controller.UploadReading(new TemperatureReading
            {
                PoId = 1,
                Vendor = "VendorA",
                Sku = "SKU1",
                Position = "Top",
                Temperature = 34
            }, CancellationToken.None);

            var ok = Assert.IsType<OkObjectResult>(result);
            dynamic body = ok.Value!;
            Assert.Equal(1, (int)body.ProductId); // Since it's the first product, Id=1
            Assert.Equal(serviceResult, (TemperatureValidationResult)body.Validation);

            var addedProduct = db.TempQcProducts.First();
            Assert.Equal("SKU1", addedProduct.Sku);
            Assert.Equal(34, addedProduct.Temperature);
            Assert.Equal("Top", addedProduct.Position);
            Assert.Equal(1, addedProduct.TempQcPoId);
            Assert.False(addedProduct.ApprovedDeviation);
        }
    }
}
