using Controllers;
using Microsoft.AspNetCore.Mvc;
using Models.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TempAssureApiTests.Controllers
{
    public class TempAssureApiTests
    {
        [Fact]
        public async Task ValidateTemperature_ReturnsBadRequest_WhenPayloadNull()
        {
            var controller = new TemperatureController(new StubTemperatureService(new()));
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
            var controller = new TemperatureController(new StubTemperatureService(serviceResult));

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
            var controller = new TemperatureController(new StubTemperatureService(serviceResult));

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
    }
}
